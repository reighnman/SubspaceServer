﻿using Microsoft.Extensions.ObjectPool;
using Npgsql;
using NpgsqlTypes;
using SS.Core;
using SS.Core.ComponentInterfaces;
using SS.Matchmaking.Interfaces;
using System.Buffers;

namespace SS.Matchmaking.Modules
{
    [ModuleInfo($"""
        Functionality to save game data into a PostgreSQL database.
        In global.conf, the SS.Matchmaking:DatabaseConnectionString setting is required.
        """)]
    public class PostgreSqlGameStats : IModule, IGameStatsRepository
    {
        private IConfigManager _configManager;
        private ILogManager _logManager;
        private InterfaceRegistrationToken<IGameStatsRepository> _iGameStatsRepositoryToken;
        
        private NpgsqlDataSource _dataSource;
        private readonly ObjectPool<List<string>> s_stringListPool = new DefaultObjectPool<List<string>>(new StringListPooledObjectPolicy());

        #region Module members

        public bool Load(
            ComponentBroker broker,
            IConfigManager configManager,
            ILogManager logManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));

            string connectionString = configManager.GetStr(configManager.Global, "SS.Matchmaking", "DatabaseConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logManager.LogM(LogLevel.Error, nameof(PostgreSqlGameStats), "Missing connection string (global.conf: SS.Matchmaking:DatabaseConnectionString).");
                return false;
            }

            _dataSource = NpgsqlDataSource.Create(connectionString);
            _iGameStatsRepositoryToken = broker.RegisterInterface<IGameStatsRepository>(this);
            return true;
        }

        public bool Unload(ComponentBroker broker)
        {
            broker.UnregisterInterface(ref _iGameStatsRepositoryToken);

            return true;
        }

        #endregion

        #region IGameStatsRepository

        public async Task<long?> SaveGameAsync(Stream jsonStream)
        {
            try
            {
                NpgsqlCommand command = _dataSource.CreateCommand("select ss.save_game_bytea($1)");
                await using (command.ConfigureAwait(false))
                {
                    command.Parameters.AddWithValue(NpgsqlDbType.Bytea, jsonStream);
                    //await command.PrepareAsync().ConfigureAwait(false);

                    var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync().ConfigureAwait(false))
                            throw new Exception("Expected a row.");

                        return reader.GetInt64(0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logManager.LogM(LogLevel.Error, nameof(PostgreSqlGameStats), $"Error saving game to the database. {ex}");
                // TODO: add a fallback mechanism that saves the match json to a file to later send to the database as a retry?
                // would need something to periodically look for files and try to retry the save
                return null;
            }
        }

        public async Task GetPlayerRatingsAsync(long gameTypeId, Dictionary<string, int> playerRatingDictionary)
        {
            if (playerRatingDictionary is null)
                throw new ArgumentNullException(nameof(playerRatingDictionary));

            if (playerRatingDictionary.Comparer != StringComparer.OrdinalIgnoreCase)
                throw new ArgumentException("Comparer must be StringComparer.OrdinalIgnoreCase.", nameof(playerRatingDictionary));

            if (playerRatingDictionary.Count == 0)
                return;

            try
            {
                NpgsqlCommand command = _dataSource.CreateCommand("select * from ss.get_player_rating($1,$2)");
                await using (command.ConfigureAwait(false))
                {
                    // Using ArrayPool<string> is possible, but the array can be larger than needed.
                    // An ArraySegment can be passed as a parameter value, but it'll be boxed.
                    // So, it seems using a List from a pool is the only allocation free way.
                    List<string> playerNameList = s_stringListPool.Get();

                    try
                    {
                        command.Parameters.AddWithValue(NpgsqlDbType.Bigint, gameTypeId);

                        foreach (string playerName in playerRatingDictionary.Keys)
                            playerNameList.Add(playerName);

                        command.Parameters.AddWithValue(NpgsqlDbType.Array | NpgsqlDbType.Varchar, playerNameList);
                        //await command.PrepareAsync().ConfigureAwait(false);

                        var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                        await using (reader.ConfigureAwait(false))
                        {
                            char[] playerNameArray = ArrayPool<char>.Shared.Rent(Constants.MaxPlayerNameLength);

                            try
                            {
                                int playerNameColumn = reader.GetOrdinal("player_name");
                                int ratingColumn = reader.GetOrdinal("rating");

                                while (await reader.ReadAsync().ConfigureAwait(false))
                                {
                                    string playerName = GetPlayerName(reader, playerNameColumn, playerNameArray, playerNameList);
                                    if (playerName is null)
                                        continue;

                                    playerRatingDictionary[playerName] = reader.GetInt32(ratingColumn);
                                }
                            }
                            finally
                            {
                                ArrayPool<char>.Shared.Return(playerNameArray);
                            }
                        }
                    }
                    finally
                    {
                        s_stringListPool.Return(playerNameList);
                    }
                }
            }
            catch (Exception ex)
            {
                _logManager.LogM(LogLevel.Error, nameof(PostgreSqlGameStats), $"Error getting player stats. {ex}");
            }


            // Local function that reads the player name from the DataReader without allocating a string, instead reusing the existing string instance.
            static string GetPlayerName(NpgsqlDataReader reader, int ordinal, char[] buffer, List<string> playerNameList)
            {
                long charsRead = reader.GetChars(ordinal, 0, buffer, 0, Constants.MaxPlayerNameLength); // unfortunately, no overload for Span<char> so have to use a pooled char[] as the buffer
                ReadOnlySpan<char> playerNameSpan = buffer.AsSpan(0, (int)charsRead);

                foreach (string playerName in playerNameList)
                {
                    if (MemoryExtensions.Equals(playerName, playerNameSpan, StringComparison.OrdinalIgnoreCase))
                        return playerName;
                }

                return null;
            }
        }

        #endregion

        #region Object pooling types

        private class StringListPooledObjectPolicy : IPooledObjectPolicy<List<string>>
        {
            public List<string> Create()
            {
                return new List<string>();
            }

            public bool Return(List<string> obj)
            {
                if (obj is null)
                    return false;

                obj.Clear();
                return true;
            }
        }

        #endregion
    }
}

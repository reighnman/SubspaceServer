﻿using SS.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text;

namespace SS.Core.ComponentInterfaces
{
    /// <summary>
    /// types of chat messages
    /// </summary>
    public enum ChatMessageType : byte
    {
        /// <summary>
        /// arena messages (in green)
        /// </summary>
        Arena = 0,

        /// <summary>
        /// macros as public arena chat
        /// </summary>
        PubMacro = 1,

        /// <summary>
        /// public arena chat
        /// </summary>
        Pub = 2,

        /// <summary>
        /// team message
        /// </summary>
        Freq = 3,

        /// <summary>
        /// enemy team messages
        /// </summary>
        EnemyFreq = 4,

        /// <summary>
        /// within-arena private messages
        /// </summary>
        Private = 5,

        /// <summary>
        /// cross-arena or cross-zone private messages
        /// </summary>
        RemotePrivate = 7,

        /// <summary>
        /// red sysop warning text
        /// </summary>
        SysopWarning = 8,

        /// <summary>
        /// chat channel messages
        /// </summary>
        Chat = 9,

        /// <summary>
        /// moderator chat messages (internal only)
        /// </summary>
        ModChat = 10,

        /// <summary>
        /// msgs that function as commands (internal only)
        /// </summary>
        Command = 11,

        /// <summary>
        /// commands that go to the biller (internal only)
        /// </summary>
        BillerCommand = 12,
    }

    /// <summary>
    /// Mask that tells what chat message types are allowed/disallowed.
    /// </summary>
    public struct ChatMask(int data)
	{
        private BitVector32 _maskVector = new(data);

		public static ChatMask operator |(ChatMask a, ChatMask b) => new(a._maskVector.Data | b._maskVector.Data);

        public int Value => _maskVector.Data;

        public bool IsClear => _maskVector.Data == 0;

        public bool IsRestricted(ChatMessageType messageType) => _maskVector[BitVector32Masks.GetMask((int)messageType)];

        public bool IsAllowed(ChatMessageType messageType) => !IsRestricted(messageType);

        public void SetRestricted(ChatMessageType messageType) => _maskVector[BitVector32Masks.GetMask((int)messageType)] = true;

        public void SetAllowed(ChatMessageType messageType) => _maskVector[BitVector32Masks.GetMask((int)messageType)] = false;

        public void Set(ChatMessageType messageType, bool isRestricted)
        {
            if (isRestricted)
                SetRestricted(messageType);
            else
                SetAllowed(messageType);
        }

        public void Clear() => _maskVector = new BitVector32(0);
    }

    public interface IChat : IComponentInterface
    {
        #region SendMessage

        /// <summary>
        /// Sends a green message to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="handler">The message to send.</param>
        void SendMessage(Player player, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green message to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendMessage(Player player, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green message to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green message to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, string message);

        /// <summary>
        /// Sends a green message to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, StringBuilder message);

        #endregion

        #region SendMessage (with sound)

        /// <summary>
        /// Sends a green arena message plus sound code to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="handler">The message to send.</param>
        void SendMessage(Player player, ChatSound sound, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendMessage(Player player, ChatSound sound, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, ChatSound sound, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green arena message plus sound code to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, ChatSound sound, string message);

        /// <summary>
        /// Sends a green arena message plus sound code to a player.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendMessage(Player player, ChatSound sound, StringBuilder message);

        #endregion

        #region SendSetMessage

        /// <summary>
        /// Sends a green arena message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="handler">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        /// <summary>
        void SendSetMessage(HashSet<Player> set, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green arena message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, string message);

        /// <summary>
        /// Sends a green arena message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, StringBuilder message);

        #endregion

        #region SendSetMessage (with sound)

        /// <summary>
        /// Sends a green arena message plus sound code to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="handler">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ChatSound sound, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ChatSound sound, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ChatSound sound, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green arena message plus sound code to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ChatSound sound, string message);

        /// <summary>
        /// Sends a green arena message plus sound code to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendSetMessage(HashSet<Player> set, ChatSound sound, StringBuilder message);

        #endregion

        #region SendArenaMessage

        /// <summary>
        /// Sends a green arena message to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="handler">The message to send.</param>
        void SendArenaMessage(Arena? arena, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendArenaMessage(Arena? arena, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green arena message to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, string message);

        /// <summary>
        /// Sends a green arena message to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, StringBuilder message);

        #endregion

        #region SendArenaMessage (with sound)

        /// <summary>
        /// Sends a green arena message plus sound code to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="handler">The message to send.</param>
        void SendArenaMessage(Arena? arena, ChatSound sound, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendArenaMessage(Arena? arena, ChatSound sound, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a green arena message plus sound code to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, ChatSound sound, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a green arena message plus sound code to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, ChatSound sound, string message);

        /// <summary>
        /// Sends a green arena message plus sound code to all players in an arena.
        /// </summary>
        /// <param name="arena">The arena to send to, or <see langword="null"/> for all arenas.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="message">The message to send.</param>
        void SendArenaMessage(Arena? arena, ChatSound sound, StringBuilder message);

        #endregion

        #region SendAnyMessage

        /// <summary>
        /// Sends an arbitrary chat message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="type">The type of message.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="from">The player the message if from.</param>
        /// <param name="handler">The message to send.</param>
        void SendAnyMessage(HashSet<Player> set, ChatMessageType type, ChatSound sound, Player? from, [InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends an arbitrary chat message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="type">The type of message.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="from">The player the message if from.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendAnyMessage(HashSet<Player> set, ChatMessageType type, ChatSound sound, Player? from, IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends an arbitrary chat message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="type">The type of message.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="from">The player the message if from.</param>
        /// <param name="message">The message to send.</param>
        void SendAnyMessage(HashSet<Player> set, ChatMessageType type, ChatSound sound, Player? from, ReadOnlySpan<char> message);

        /// <summary>
        /// Sends an arbitrary chat message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="type">The type of message.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="from">The player the message if from.</param>
        /// <param name="message">The message to send.</param>
        void SendAnyMessage(HashSet<Player> set, ChatMessageType type, ChatSound sound, Player? from, string message);

        /// <summary>
        /// Sends an arbitrary chat message to a set of players.
        /// </summary>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="type">The type of message.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="from">The player the message if from.</param>
        /// <param name="message">The message to send.</param>
        void SendAnyMessage(HashSet<Player> set, ChatMessageType type, ChatSound sound, Player? from, StringBuilder message);

        #endregion

        #region SendModMessage

        /// <summary>
        /// Sends a moderator chat message to all connected staff.
        /// </summary>
        /// <param name="handler">The message to send.</param>
        void SendModMessage([InterpolatedStringHandlerArgument("")] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a moderator chat message to all connected staff.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The message to send.</param>
        void SendModMessage(IFormatProvider provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref StringBuilderBackedInterpolatedStringHandler handler);

        /// <summary>
        /// Sends a moderator chat message to all connected staff.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendModMessage(ReadOnlySpan<char> message);

        /// <summary>
        /// Sends a moderator chat message to all connected staff.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendModMessage(string message);

        /// <summary>
        /// Sends a moderator chat message to all connected staff.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void SendModMessage(StringBuilder message);

        #endregion

        /// <summary>
        /// Sends a remote private message to a set of players.
        /// </summary>
        /// <remarks>
        /// This should only be used from billing server modules.
        /// </remarks>
        /// <param name="set">The players to send the message to.</param>
        /// <param name="sound">The sound to send.</param>
        /// <param name="squad">The squad the message is for, or <see cref="ReadOnlySpan{byte}.Empty"/> for no squad.</param>
        /// <param name="sender">The name of the sender.</param>
        /// <param name="message">The message to send.</param>
        void SendRemotePrivMessage(HashSet<Player> set, ChatSound sound, ReadOnlySpan<char> squad, ReadOnlySpan<char> sender, ReadOnlySpan<char> message);

        #region Chat Mask

        /// <summary>
        /// Gets the chat mask for an arena.
        /// </summary>
        /// <param name="arena">The arena to get the chat mask for.</param>
        /// <returns>The chat mask.</returns>
        ChatMask GetArenaChatMask(Arena arena);

        /// <summary>
        /// Sets the chat mask for an arena.
        /// </summary>
        /// <param name="arena">The arena to set the chat mask for.</param>
        /// <param name="mask">The chat mask to set.</param>
        void SetArenaChatMask(Arena arena, ChatMask mask);

        /// <summary>
        /// Gets the chat mask for a player.
        /// </summary>
        /// <param name="player">The player to get the chat mask for.</param>
        /// <returns>The chat mask.</returns>
        ChatMask GetPlayerChatMask(Player player);

        /// <summary>
        /// Gets the chat mask for a player, including the remaining time for the mask.
        /// </summary>
        /// <param name="player">The player to get the chat mask for.</param>
        /// <param name="mask">The chat mask.</param>
        /// <param name="remaining">The remaining time on the mask. <see langword="null"/> means there is no expiration (valid until the next arena change).</param>
        void GetPlayerChatMask(Player player, out ChatMask mask, out TimeSpan? remaining);

        /// <summary>
        /// Sets the chat mask for a player.
        /// </summary>
        /// <param name="player">The player to set the mask for.</param>
        /// <param name="mask">The chat mask to set.</param>
        /// <param name="timeout">Zero to set a session mask (valid until the next arena change), or a number of seconds for the mask to be valid.</param>
        void SetPlayerChatMask(Player player, ChatMask mask, int timeout);

        #endregion

        #region SendWrappedText

        /// <summary>
        /// A utility function for sending lists of items in a chat message.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="text">The message to send.</param>
        void SendWrappedText(Player player, string text);

        /// <summary>
        /// A utility function for sending lists of items in a chat message.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="sb">The message to send.</param>
        void SendWrappedText(Player player, StringBuilder sb);

        #endregion
    }
}

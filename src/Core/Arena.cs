using SS.Core.ComponentInterfaces;
using SS.Core.Modules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace SS.Core
{
    // TODO: looks like the arena stuff has changed somewhat dramatically with the new version...
    public enum ArenaState
    {
        /// <summary>
        /// someone wants to enter the arena. first, the config file must be loaded, callbacks called
        /// </summary>
        DoInit0,

        /// <summary>
        /// waiting for first round of callbacks
        /// </summary>
        WaitHolds0, 

        /// <summary>
        /// attaching and more callbacks
        /// </summary>
        DoInit1, 

        /// <summary>
        /// waiting on modules to do init work.
        /// </summary>
        WaitHolds1, 

        /// <summary>
        /// load persistent data.
        /// </summary>
        DoInit2, 

        /// <summary>
        /// waiting on the database
        /// </summary>
        WaitSync1,

        /// <summary>
        /// now the arena is fully created. core can now send the arena 
        /// responses to players waiting to enter this arena
        /// </summary>
        Running,

        /// <summary>
        /// the arena is running for a little while, but isn't accepting new players
        /// </summary>
        Closing,

        /// <summary>
        /// the arena is being reaped, first put info in database
        /// </summary>
        DoWriteData, 

        /// <summary>
        /// waiting on the database to finish before we can unregister modules...
        /// </summary>
        WaitSync2, 

        /// <summary>
        /// arena destroy callbacks.
        /// </summary>
        DoDestroy1, 

        /// <summary>
        /// waiting for modules to do destroy work.
        /// </summary>
        WaitHolds2, 

        /// <summary>
        /// finish destroy process.
        /// </summary>
        DoDestroy2
    }

    public enum ArenaAction
    {
        /// <summary>when arena is created</summary>
	    Create,

	    /// <summary>when config file changes</summary>
        ConfChanged,

        /// <summary>when the arena is destroyed</summary>
        Destroy, 
	    
        /// <summary>really really early</summary>
        PreCreate, 

	    /// <summary>really really late</summary>
        PostDestroy
    };

    public class Arena : ComponentBroker, IArenaTarget
    {
        private const string PUBLIC = "(public)";
        private const string GLOBAL = "(global)";

        private const int MAX_ARENA_NAME_LENGTH = 16;

        public const int DEFAULT_SPEC_FREQ = 8025;

        /// <summary>
        /// what state the arena is in. @see ARENA_DO_INIT, etc.
        /// </summary>
	    public ArenaState Status;
	    
        /// <summary>
        /// the full name of the arena
        /// </summary>
	    public readonly string Name;

        internal readonly ArenaManager Manager;

	    /// <summary>
	    /// the name of the arena, minus any trailing digits.
        /// the basename is used in many places to allow easy sharing of
        /// settings and things among copies of one basic arena.
	    /// </summary>
        public readonly string BaseName;

	    /// <summary>
        /// a handle to the main config file for this arena
	    /// </summary>
	    public ConfigHandle Cfg;

        /// <summary>
        /// the frequency for spectators in this arena.
        /// this setting is so commonly used, it deserves a spot here.
        /// </summary>
        [ConfigHelp("Team", "SpectatorFrequency", ConfigScope.Arena, typeof(int), Range = "0-9999", DefaultValue = "8025",
            Description = "The frequency that spectators are assigned to, by default.")]
        public short SpecFreq;

	    /// <summary>
	    /// how many players are in ships in this arena.
        /// call GetPopulationSummary to update this.
	    /// </summary>
	    public int Playing;

	    /// <summary>
	    /// how many players total are in this arena.
        /// call GetPopulationSummary to update this.
	    /// </summary>
	    public int Total;

        /// <summary>
        /// whether this arena should not be destroyed when
        /// there are no players inside it.
        /// </summary>
        public bool KeepAlive;

        /// <summary>
        /// space for private data associated with this arena
        /// </summary>
        Dictionary<int, object> _arenaExtraData = new Dictionary<int,object>();

        internal Arena(ComponentBroker parent, string name, ArenaManager manager) : base(parent)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Cannot be null or white-space.", nameof(name));

            Debug.Assert(parent == manager.Broker);
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));

            Name = name;
            BaseName = name.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
            if (string.IsNullOrEmpty(BaseName))
                BaseName = PUBLIC;

            Status = ArenaState.DoInit0;
            Cfg = null;
        }

        /// <summary>
        /// Per Arena Data
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[int key]
        {
            get { return _arenaExtraData[key]; }
            set { _arenaExtraData[key] = value; }
        }

        /// <summary>
        /// This will tell you if an arena is considered a "public" arena.
        /// </summary>
        public bool IsPublic
        {
            get { return string.Equals(BaseName, PUBLIC, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// Whether the arena is private (name starts with #).
        /// </summary>
        public bool IsPrivate
        {
            get { return Name[0] == '#'; }
        }

        public void RemovePerArenaData(int key)
        {
            object pad;
            if (_arenaExtraData.TryGetValue(key, out pad))
            {
                _arenaExtraData.Remove(key);

                IDisposable disposable = pad as IDisposable;
                if (disposable != null)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // ignore any errors
                    }
                }
            }
        }

        // TODO: Maybe a way to synchronize?
        //public void Lock()
        //{
        //    //Manager.
        //    //Manager.Broker
        //}

        #region IArenaTarget Members

        Arena IArenaTarget.Arena
        {
            get { return this; }
        }

        #endregion

        #region ITarget Members

        TargetType ITarget.Type
        {
            get { return TargetType.Arena; }
        }

        #endregion

        #region Team Target

        /// <summary>
        /// Dictionary of immutable TeamTarget objects that can be reused.
        /// This is to reduce allocations (e.g. rather than allocate a new one each time a team target is needed).
        /// </summary>
        private readonly ConcurrentDictionary<int, TeamTarget> _teamTargets = new();

        public TeamTarget GetTeamTarget(int freq) => _teamTargets.GetOrAdd(freq, (f) => new TeamTarget(this, f));

        public void CleanupTeamTargets()
        {
            if (_teamTargets.Count == 0)
                return;

            IPlayerData playerData = GetInterface<IPlayerData>();

            if (playerData != null)
            {
                try
                {
                    playerData.Lock();

                    try
                    {
                        // TODO: The ConcurrentDictionary enumerator is not a struct, it allocates an object.
                        // Maybe change this to a regular Dictionary + locking, but then can't remove while iterating,
                        // would need another collection of type int to store the IDs to remove in.
                        // So the collection of ints would then need to come from a pool, otherwise there would be an allocation.
                        // Or maybe stackalloc an array + keep track of a count, as there shouldn't be that many teams in the first place.
                        foreach (var team in _teamTargets)
                        {
                            int freq = team.Key;

                            if (!HasPlayerOnFreq(playerData, this, freq))
                                _teamTargets.TryRemove(freq, out _);
                        }
                    }
                    finally
                    {
                        playerData.Unlock();
                    }
                }
                finally
                {
                    ReleaseInterface(ref playerData);
                }
            }

            static bool HasPlayerOnFreq(IPlayerData playerData, Arena arena, int freq)
            {
                foreach (Player p in playerData.PlayerList)
                {
                    if (p.Arena == arena && p.Freq == freq)
                        return true;
                }

                return false;
            }
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}

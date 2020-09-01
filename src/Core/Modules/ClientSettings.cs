﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS.Core.ComponentCallbacks;
using SS.Core.ComponentInterfaces;
using SS.Core.Packets;

namespace SS.Core.Modules
{
    [CoreModuleInfo]
    public class ClientSettings : IModule, IClientSettings
    {
        private ModuleManager _mm;
        private IPlayerData _playerData;
        private INetwork _net;
        private ILogManager _logManager;
        private IConfigManager _configManager;
        private IArenaManagerCore _arenaManager;

        private int _adkey;
        private int _pdkey;

        private object _setMtx = new object();

        private Random _random = new Random();

        private class ArenaClientSettingsData
        {
            public ClientSettingsPacket cs = //new ClientSettingsPacket(new byte[] {0x0f,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x01,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x10,0x31,0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x50,0x30,0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x58,0x31,0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x40,0x31,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x50,0x31,0x06,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x48,0x50,0x31,0x1a,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0xdc,0x05,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x64,0x03,0x03,0x00,0x00,0x64,0x00,0x00,0x00,0x00,0x01,0x48,0x50,0x31,0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x70,0x17,0x00,0x00,0xa0,0x0f,0x00,0x00,0x00,0x00,0x64,0x00,0x14,0x00,0x1e,0x00,0x2c,0x01,0x32,0x00,0x0e,0x01,0x96,0x00,0xd0,0x07,0xd0,0x07,0x00,0x00,0xf4,0x01,0x64,0x00,0x4d,0x01,0x64,0x00,0xfa,0x00,0xe6,0x00,0x11,0x00,0xb2,0x0c,0x7e,0x04,0xa4,0x06,0xc8,0x00,0x0f,0x00,0xda,0x07,0x90,0x01,0xe8,0x03,0x28,0x00,0x02,0x00,0xfa,0x00,0xa6,0x00,0x64,0x00,0xb0,0x04,0x90,0x01,0xb8,0x0b,0x01,0x00,0x7d,0x00,0x19,0x00,0x32,0x00,0x96,0x00,0x7d,0x00,0xe8,0x03,0xf4,0x01,0x1e,0x00,0x64,0x00,0x00,0x00,0xb0,0x04,0x0c,0x00,0x40,0x00,0xc4,0x09,0x05,0x18,0x05,0x03,0x03,0x03,0x03,0x03,0x03,0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x5f,0x54,0x31,0x02,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x40,0x0d,0x03,0x00,0xe0,0x70,0x72,0x00,0x26,0x02,0x00,0x00,0x40,0x1f,0x00,0x00,0xdc,0x05,0x00,0x00,0x90,0x5f,0x01,0x00,0x84,0x03,0x00,0x00,0x0f,0x27,0x00,0x00,0x88,0x13,0x00,0x00,0xe0,0x2e,0x00,0x00,0x60,0xae,0x0a,0x00,0xa0,0x86,0x01,0x00,0xb8,0x0b,0x00,0x00,0xe8,0x03,0x00,0x00,0x19,0x00,0x00,0x00,0x7c,0x15,0x00,0x00,0xb8,0x0b,0x00,0x00,0xa0,0x86,0x01,0x00,0x00,0x00,0x00,0x00,0xb8,0x0b,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xf4,0x01,0x96,0x00,0x14,0x00,0x50,0x00,0x20,0x03,0x48,0x00,0xc8,0x00,0xbc,0x02,0x03,0x00,0x06,0x00,0x16,0x00,0x0a,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xe1,0x00,0x00,0x02,0xe8,0x03,0x01,0x00,0x02,0x00,0x10,0x27,0xbc,0x02,0x0a,0x00,0x06,0x00,0x40,0x1f,0xa0,0x0f,0x2c,0x01,0x58,0x02,0xe8,0x03,0xfe,0xff,0xc8,0x00,0xf4,0x01,0x0a,0x00,0xe8,0x03,0x14,0x00,0x90,0x01,0xe8,0x03,0x80,0x00,0x70,0x17,0x00,0x00,0xe8,0x03,0xe8,0x03,0x32,0x00,0x00,0x00,0xe8,0x03,0xe8,0x03,0x00,0x00,0x14,0x00,0xc8,0x00,0xf4,0x01,0x00,0x00,0x00,0x00,0x64,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x01,0x01,0x01,0x01,0x00,0x06,0x00,0x0c,0x01,0x01,0x00,0x00,0x01,0x0a,0x00,0x00,0x01,0x00,0x00,0x00,0x01,0x01,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x28,0x28,0x28,0x28,0x19,0x28,0x07,0x19,0x19,0x19,0x28,0x28,0x0a,0x00,0x1e,0x19,0x0a,0x0a,0x1e,0x19,0x46,0x46,0x28,0x1e,0x82,0xc8,0x32,0x3c});
                new ClientSettingsPacket();

            /// <summary>
            /// prizeweight partial sums. 1-28 are used for now, representing prizes 1 to 28.
            /// 0 = null prize
            /// </summary>
            public ushort[] pwps = new ushort[32];
        }

        private class PlayerClientSettingsData
        {
        }

        #region IModule Members

        Type[] IModule.InterfaceDependencies
        {
            get
            {
                return new Type[] {
                    typeof(IPlayerData), 
                    typeof(INetwork), 
                    typeof(ILogManager), 
                    typeof(IConfigManager), 
                    typeof(IArenaManagerCore), 
                };
            }
        }

        bool IModule.Load(ModuleManager mm, Dictionary<Type, IComponentInterface> interfaceDependencies)
        {
            _mm = mm;
            _playerData = interfaceDependencies[typeof(IPlayerData)] as IPlayerData;
            _net = interfaceDependencies[typeof(INetwork)] as INetwork;
            _logManager = interfaceDependencies[typeof(ILogManager)] as ILogManager;
            _configManager = interfaceDependencies[typeof(IConfigManager)] as IConfigManager;
            _arenaManager = interfaceDependencies[typeof(IArenaManagerCore)] as IArenaManagerCore;

            _adkey = _arenaManager.AllocateArenaData<ArenaClientSettingsData>();
            _pdkey = _playerData.AllocatePlayerData<PlayerClientSettingsData>();

            ArenaActionCallback.Register(_mm, arenaAction);
            PlayerActionCallback.Register(_mm, playerAction);

            _mm.RegisterInterface<IClientSettings>(this);
            return true;
        }

        bool IModule.Unload(ModuleManager mm)
        {
            _mm.UnregisterInterface<IClientSettings>();

            ArenaActionCallback.Unregister(_mm, arenaAction);
            PlayerActionCallback.Unregister(_mm, playerAction);

            _arenaManager.FreeArenaData(_adkey);
            _playerData.FreePlayerData(_pdkey);

            return true;
        }

        #endregion

        #region IClientSettings Members

        void IClientSettings.SendClientSettings(Player p)
        {
            if (p == null)
                return;

            Arena arena = p.Arena;
            if (arena == null)
                return;

            ArenaClientSettingsData ad = arena[_adkey] as ArenaClientSettingsData;

            lock (_setMtx)
            {
                sendOneSettings(p, ad);
            }
        }

        uint IClientSettings.GetChecksum(Player p, uint key)
        {
            return 0;
        }

        Prize IClientSettings.GetRandomPrize(Arena arena)
        {
            if(arena == null)
                return 0;

            ArenaClientSettingsData ad = arena[_adkey] as ArenaClientSettingsData;
            if (ad == null)
                return 0;

            int max = ad.pwps[28];

            if (max == 0)
                return 0;

            int i = 0;
            int j = 28;

            int r;

            lock (_random)
            {
                r = _random.Next(0, max);
            }

            // binary search
            while (r >= ad.pwps[i])
            {
                int m = (i + j) / 2;
                if (r < ad.pwps[m])
                    j = m;
                else
                    i = m + 1;
            }

            return (Prize)i;
        }

        ClientSettingOverrideKey IClientSettings.GetOverrideKey(string section, string key)
        {
            return new ClientSettingOverrideKey();
        }

        void IClientSettings.ArenaOverride(Arena arena, ClientSettingOverrideKey key, int val)
        {
            
        }

        void IClientSettings.PlayerOverride(Player p, ClientSettingOverrideKey key)
        {
            
        }

        #endregion

        private void arenaAction(Arena arena, ArenaAction action)
        {
            if (arena == null)
                return;

            ArenaClientSettingsData ad = arena[_adkey] as ArenaClientSettingsData;

            lock (_setMtx)
            {
                if (action == ArenaAction.Create)
                {
                    loadSettings(ad, arena.Cfg);
                }
                else if(action == ArenaAction.ConfChanged)
                {
                }
                else if (action == ArenaAction.Destroy)
                {
                    // mark settings as destroyed (for asserting later)
                }
            }
        }

        private void loadSettings(ArenaClientSettingsData ad, ConfigHandle ch)
        {
            if (ad == null)
                return;

            if (ch == null)
                return;
            
            ClientSettingsPacket cs = ad.cs;
            cs.Type = (byte)S2CPacketType.Settings;
            ClientSettingsPacket.ClientBitSet bitset = cs.BitSet;
            bitset.ExactDamage = _configManager.GetInt(ch, "Bullet", "ExactDamage", 0) != 0;
            bitset.HideFlags = _configManager.GetInt(ch, "Spectator", "HideFlags", 0) != 0;
            bitset.NoXRadar = _configManager.GetInt(ch, "Spectator", "NoXRadar", 0) != 0;
            bitset.SlowFramerate = (byte)_configManager.GetInt(ch, "Misc", "SlowFrameCheck", 0);
            bitset.DisableScreenshot = _configManager.GetInt(ch, "Misc", "DisableScreenshot", 0) != 0;
            bitset.MaxTimerDrift = (byte)_configManager.GetInt(ch, "Misc", "MaxTimerDrift", 0);
            bitset.DisableBallThroughWalls = _configManager.GetInt(ch, "Soccer", "DisableWallPass", 0) != 0;
            bitset.DisableBallKilling = _configManager.GetInt(ch, "Soccer", "DisableBallKilling", 0) != 0;

            // ships
            for (int i = 0; i < 8; i++)
            {
                ClientSettingsPacket.ShipSettings ss = cs.Ships[i];
                string shipName = ClientSettingsConfig.ShipNames[i];

                // basic stuff
                for (int j = 0; j < ss.LongSet.Length; j++)
                    ss.LongSet[j] = _configManager.GetInt(ch, shipName, ClientSettingsConfig.ShipLongNames[j], 0);

                for (int j = 0; j < ss.ShortSet.Length; j++)
                    ss.ShortSet[j] = (short)_configManager.GetInt(ch, shipName, ClientSettingsConfig.ShipShortNames[j], 0);

                for (int j = 0; j < ss.ByteSet.Length; j++)
                    ss.ByteSet[j] = (byte)_configManager.GetInt(ch, shipName, ClientSettingsConfig.ShipByteNames[j], 0);

                // weapon bits
                ClientSettingsPacket.ShipSettings.WeaponBits wb = ss.Weapons;
                wb.ShrapnelMax = (byte)_configManager.GetInt(ch, shipName, "ShrapnelMax", 0);
                wb.ShrapnelRate = (byte)_configManager.GetInt(ch, shipName, "ShrapnelRate", 0);
                wb.AntiWarpStatus = (byte)_configManager.GetInt(ch, shipName, "AntiWarpStatus", 0);
                wb.CloakStatus = (byte)_configManager.GetInt(ch, shipName, "CloakStatus", 0);
                wb.StealthStatus = (byte)_configManager.GetInt(ch, shipName, "StealthStatus", 0);
                wb.XRadarStatus = (byte)_configManager.GetInt(ch, shipName, "XRadarStatus", 0);
                wb.InitialGuns = (byte)_configManager.GetInt(ch, shipName, "InitialGuns", 0);
                wb.MaxGuns = (byte)_configManager.GetInt(ch, shipName, "MaxGuns", 0);
                wb.InitialBombs = (byte)_configManager.GetInt(ch, shipName, "InitialBombs", 0);
                wb.MaxBombs = (byte)_configManager.GetInt(ch, shipName, "MaxBombs", 0);
                wb.DoubleBarrel = _configManager.GetInt(ch, shipName, "DoubleBarrel", 0) != 0;
                wb.EmpBomb = _configManager.GetInt(ch, shipName, "EmpBomb", 0) != 0;
                wb.SeeMines = _configManager.GetInt(ch, shipName, "SeeMines", 0) != 0;

                // strange bitfield
                ClientSettingsPacket.ShipSettings.MiscBitField misc = ss.MiscBits;
                misc.SeeBombLevel = (byte)_configManager.GetInt(ch, shipName, "SeeBombLevel", 0);
                misc.DisableFastShooting = _configManager.GetInt(ch, shipName, "DisableFastShooting", 0) != 0;
                misc.Radius = (byte)_configManager.GetInt(ch, shipName, "Radius", 0);
            }

            // spawn locations
            for (int i = 0; i < 4; i++)
            {
                string xName = "Team#-X".Replace('#', char.Parse(i.ToString()));
                string yName = "Team#-Y".Replace('#', char.Parse(i.ToString()));
                string rName = "Team#-Radius".Replace('#', char.Parse(i.ToString()));

                cs.SpawnPosition[i].X = (ushort)_configManager.GetInt(ch, "Spawn", xName, 0);
                cs.SpawnPosition[i].Y = (ushort)_configManager.GetInt(ch, "Spawn", yName, 0);
                cs.SpawnPosition[i].R = (ushort)_configManager.GetInt(ch, "Spawn", rName, 0);
            }

            // rest of settings
            for (int i = 0; i < cs.LongSet.Length; i++)
                cs.LongSet[i] = _configManager.GetInt(ch, ClientSettingsConfig.LongNames[i], null, 0);

            for (int i = 0; i < cs.ShortSet.Length; i++)
                cs.ShortSet[i] = (short)_configManager.GetInt(ch, ClientSettingsConfig.ShortNames[i], null, 0);

            for (int i = 0; i < cs.ByteSet.Length; i++)
                cs.ByteSet[i] = (byte)_configManager.GetInt(ch, ClientSettingsConfig.ByteNames[i], null, 0);

            ushort total = 0;
            ad.pwps[0] = 0;
            for (int i = 0; i < cs.PrizeWeightSet.Length; i++)
            {
                cs.PrizeWeightSet[i] = (byte)_configManager.GetInt(ch, ClientSettingsConfig.PrizeWeightNames[i], null, 0);
                ad.pwps[i+1] = (total += cs.PrizeWeightSet[i]);
            }

            if (_configManager.GetInt(ch, "Prize", "UseDeathPrizeWeights", 0) != 0)
            {
                // overrride prizeweights for greens dropped when a player is killed

                // likelyhood of an empty prize appearing
                total = ad.pwps[0] = (ushort)_configManager.GetInt(ch, "DPrizeWeight", "NullPrize", 0);
                for (int i = 0; i < cs.PrizeWeightSet.Length; i++)
                {
                    ad.pwps[i + 1] = (total += (ushort)_configManager.GetInt(ch, ClientSettingsConfig.DeathPrizeWeightNames[i], null, 0));
                }
            }

            // funky ones
            cs.LongSet[0] *= 1000; // BulletDamageLevel
            cs.LongSet[1] *= 1000; // BombDamageLevel
            cs.LongSet[10] *= 1000; // BurstDamageLevel
            cs.LongSet[11] *= 1000; // BulletDamageUpgrade
            cs.LongSet[16] *= 1000; // InactiveShrapDamage
        }

        private void playerAction(Player p, PlayerAction action, Arena arena)
        {
            if (p == null)
                return;

            if (action == PlayerAction.LeaveArena || action == PlayerAction.Disconnect)
            {
                // reset/free player overrides on any arena change
                // TODO
            }
        }

        private void sendOneSettings(Player p, ArenaClientSettingsData ad)
        {
            if (p == null)
                return;

            if (ad == null)
                return;

            // do mask
            // TODO

            if (ad.cs.Type == (byte)S2CPacketType.Settings)
                _net.SendToOne(p, ad.cs.Bytes, ClientSettingsPacket.Length, NetSendFlags.Reliable);
        }
    }
}

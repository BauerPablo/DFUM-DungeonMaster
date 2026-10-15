// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using System;
using Wenzil.Console;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Weather;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Utility;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Console commands for DFU native multiplayer session management.
    /// </summary>
    public static class DFUNetworkCommands
    {
        public static void RegisterCommands()
        {
            try
            {
                ConsoleCommandsDatabase.RegisterCommand(NetHost.name, NetHost.description, NetHost.usage, NetHost.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetJoin.name, NetJoin.description, NetJoin.usage, NetJoin.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetDisconnect.name, NetDisconnect.description, NetDisconnect.usage, NetDisconnect.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetStatus.name, NetStatus.description, NetStatus.usage, NetStatus.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetEntities.name, NetEntities.description, NetEntities.usage, NetEntities.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetGui.name, NetGui.description, NetGui.usage, NetGui.Execute);
                ConsoleCommandsDatabase.RegisterCommand(TtrpgGui.name, TtrpgGui.description, TtrpgGui.usage, TtrpgGui.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmGui.name, DmGui.description, DmGui.usage, DmGui.Execute);
                ConsoleCommandsDatabase.RegisterCommand(LootGui.name, LootGui.description, LootGui.usage, LootGui.Execute);
                ConsoleCommandsDatabase.RegisterCommand(QuestGui.name, QuestGui.description, QuestGui.usage, QuestGui.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetQuest.name, NetQuest.description, NetQuest.usage, NetQuest.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmSpawn.name, DmSpawn.description, DmSpawn.usage, DmSpawn.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmDespawn.name, DmDespawn.description, DmDespawn.usage, DmDespawn.Execute);
                ConsoleCommandsDatabase.RegisterCommand(GatherParty.name, GatherParty.description, GatherParty.usage, GatherParty.Execute);
                ConsoleCommandsDatabase.RegisterCommand(SetTime.name, SetTime.description, SetTime.usage, SetTime.Execute);
                ConsoleCommandsDatabase.RegisterCommand(SetWeather.name, SetWeather.description, SetWeather.usage, SetWeather.Execute);
                ConsoleCommandsDatabase.RegisterCommand(TimeScale.name, TimeScale.description, TimeScale.usage, TimeScale.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetLog.name, NetLog.description, NetLog.usage, NetLog.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmInvis.name, DmInvis.description, DmInvis.usage, DmInvis.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmGod.name, DmGod.description, DmGod.usage, DmGod.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmFreeCam.name, DmFreeCam.description, DmFreeCam.usage, DmFreeCam.Execute);
                ConsoleCommandsDatabase.RegisterCommand("freecam", DmFreeCam.description, DmFreeCam.usage, DmFreeCam.Execute);
                ConsoleCommandsDatabase.RegisterCommand(DmTeleport.name, DmTeleport.description, DmTeleport.usage, DmTeleport.Execute);
                ConsoleCommandsDatabase.RegisterCommand(NetPvp.name, NetPvp.description, NetPvp.usage, NetPvp.Execute);
            }
            catch (Exception ex)
            {
                DaggerfallUnity.LogMessage($"[DFU Network] Error registering network console commands: {ex.Message}", true);
            }
        }

        private static class NetHost
        {
            public static readonly string name = "net_host";
            public static readonly string description = "Starts a multiplayer host (server + local client) using KCP transport.";
            public static readonly string usage = "net_host [port (default: 7777)]";

            public static string Execute(params string[] args)
            {
                ushort port = 7777;
                if (args != null && args.Length > 0 && !ushort.TryParse(args[0], out port))
                {
                    return $"Invalid port '{args[0]}'. Must be between 1 and 65535.";
                }

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available on GameManager.";

                if (NetworkSession.IsActive)
                    return $"Session already active: {NetworkSession.GetStatusString()}. Use 'net_disconnect' first.";

                mgr.StartHosting(port);
                return $"Hosting started on port {port} (KCP).";
            }
        }

        private static class NetJoin
        {
            public static readonly string name = "net_join";
            public static readonly string description = "Connects to a remote DFU host as client.";
            public static readonly string usage = "net_join <ip> [port (default: 7777)]";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                {
                    return "Usage: " + usage;
                }

                string ip = args[0];
                ushort port = 7777;
                if (args.Length > 1 && !ushort.TryParse(args[1], out port))
                {
                    return $"Invalid port '{args[1]}'.";
                }

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available on GameManager.";

                if (NetworkSession.IsActive)
                    return $"Session already active: {NetworkSession.GetStatusString()}. Use 'net_disconnect' first.";

                mgr.ConnectToServer(ip, port);
                return $"Connecting to {ip}:{port}...";
            }
        }

        private static class NetDisconnect
        {
            public static readonly string name = "net_disconnect";
            public static readonly string description = "Disconnects from the active multiplayer session.";
            public static readonly string usage = "net_disconnect";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (NetworkSession.IsSinglePlayer)
                    return "No active multiplayer session.";

                mgr.DisconnectSession();
                return "Session disconnected.";
            }
        }

        private static class NetStatus
        {
            public static readonly string name = "net_status";
            public static readonly string description = "Displays current network session status.";
            public static readonly string usage = "net_status";

            public static string Execute(params string[] args)
            {
                string status = $"Session State: {NetworkSession.GetStatusString()}\n" +
                                $"IsServer: {NetworkSession.IsServer}\n" +
                                $"IsClient: {NetworkSession.IsClient}\n" +
                                $"IsHost: {NetworkSession.IsHost}\n" +
                                $"IsSinglePlayer: {NetworkSession.IsSinglePlayer}\n" +
                                $"ShouldHandleSpawns: {NetworkSession.ShouldHandleSpawns}";

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr != null && NetworkSession.IsServer)
                {
                    status += $"\nConnected Clients: {mgr.connectedClients.Count}";
                }

                status += $"\nTracked Entities: {DFUNetworkRegistry.Count}";

                return status;
            }
        }

        private static class NetEntities
        {
            public static readonly string name = "net_entities";
            public static readonly string description = "Lists all registered network entities.";
            public static readonly string usage = "net_entities";

            public static string Execute(params string[] args)
            {
                var entities = DFUNetworkRegistry.GetAllEntities();
                int count = DFUNetworkRegistry.Count;
                if (count == 0)
                    return "No network entities currently registered.";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"Tracked Entities ({count}):");
                foreach (var kvp in entities)
                {
                    string details = kvp.Value != null ? kvp.Value.name : "<null>";
                    NativeNetworkEntity netEnt = kvp.Value != null ? kvp.Value.GetComponent<NativeNetworkEntity>() : null;
                    string proxyTag = netEnt != null ? (netEnt.isClientProxy ? "[Proxy]" : "[Authoritative]") : "[Unknown]";
                    sb.AppendLine($" - {kvp.Key}: {details} {proxyTag}");
                }
                return sb.ToString().TrimEnd();
            }
        }

        private static class NetGui
        {
            public static readonly string name = "net_gui";
            public static readonly string description = "Toggles the on-screen network debug GUI overlay / TTRPG Red Tab.";
            public static readonly string usage = "net_gui";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (mgr.showTtrpgSuite && mgr.ttrpgSelectedTab == 0)
                {
                    mgr.showTtrpgSuite = false;
                }
                else
                {
                    mgr.showTtrpgSuite = true;
                    mgr.ttrpgSelectedTab = 0;
                }
                return $"TTRPG Master Suite (Red & Sesión) is now {(mgr.showTtrpgSuite ? "ENABLED" : "DISABLED")}.";
            }
        }

        private static class TtrpgGui
        {
            public static readonly string name = "ttrpg_gui";
            public static readonly string description = "Toggles the unified TTRPG Master Suite (Tabs: Red, Spawns, Mundo, Quests, Loot, Party).";
            public static readonly string usage = "ttrpg_gui [tabIndex (0-5)]";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (args.Length > 0 && int.TryParse(args[0], out int tab) && tab >= 0 && tab <= 5)
                {
                    mgr.ttrpgSelectedTab = tab;
                    mgr.showTtrpgSuite = true;
                    string tabName = (mgr.ttrpgTabNames != null && tab < mgr.ttrpgTabNames.Length) ? mgr.ttrpgTabNames[tab] : tab.ToString();
                    return $"TTRPG Master Suite opened to tab {tab} ({tabName}).";
                }

                mgr.showTtrpgSuite = !mgr.showTtrpgSuite;
                return $"TTRPG Master Suite is now {(mgr.showTtrpgSuite ? "ENABLED" : "DISABLED")}.";
            }
        }

        private static class DmGui
        {
            public static readonly string name = "dm_gui";
            public static readonly string description = "Toggles the Dungeon Master (DM) enemy spawner GUI overlay / TTRPG Spawns Tab.";
            public static readonly string usage = "dm_gui";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (mgr.showTtrpgSuite && mgr.ttrpgSelectedTab == 1)
                {
                    mgr.showTtrpgSuite = false;
                }
                else
                {
                    mgr.showTtrpgSuite = true;
                    mgr.ttrpgSelectedTab = 1;
                }
                return $"TTRPG Master Suite (Spawns & NPCs) is now {(mgr.showTtrpgSuite ? "ENABLED" : "DISABLED")}.";
            }
        }

        private static class LootGui
        {
            public static readonly string name = "loot_gui";
            public static readonly string description = "Toggles the DM Loot and Container Spawner GUI overlay / TTRPG Loot Tab.";
            public static readonly string usage = "loot_gui";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (mgr.showTtrpgSuite && mgr.ttrpgSelectedTab == 4)
                {
                    mgr.showTtrpgSuite = false;
                }
                else
                {
                    mgr.showTtrpgSuite = true;
                    mgr.ttrpgSelectedTab = 4;
                }
                return $"TTRPG Master Suite (Loot & Forja) is now {(mgr.showTtrpgSuite ? "ENABLED" : "DISABLED")}.";
            }
        }

        private static class DmSpawn
        {
            public static readonly string name = "dm_spawn";
            public static readonly string description = "Spawns an enemy 2 meters in front of the player (Host/Server only).";
            public static readonly string usage = "dm_spawn [enemyType (default: Rat)]";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.ShouldHandleSpawns)
                    return "Only the Host/Server can spawn authoritative enemies.";

                MobileTypes enemyType = MobileTypes.Rat;
                if (args != null && args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    if (!Enum.TryParse<MobileTypes>(args[0], true, out enemyType))
                    {
                        return $"Unknown enemy type '{args[0]}'. Examples: Rat, GiantBat, SkeletalWarrior, Zombie, Orc, Thief, GrizzlyBear.";
                    }
                }

                var spawned = mgr.SpawnEnemyInFrontOfPlayer(enemyType);
                if (spawned != null)
                {
                    return $"Successfully spawned authoritative {enemyType} 2m in front of player.";
                }
                else
                {
                    return $"Failed to spawn {enemyType}. Check console/logs for details.";
                }
            }
        }

        private static class DmDespawn
        {
            public static readonly string name = "dm_despawn";
            public static readonly string description = "Despawns the last spawned enemy (or all with 'all') on Host/Server.";
            public static readonly string usage = "dm_despawn [all]";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.ShouldHandleSpawns)
                    return "Only the Host/Server can despawn authoritative enemies.";

                if (args != null && args.Length > 0 && args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    int count = mgr.DespawnAllSpawnedEnemies();
                    return $"Despawned {count} enemies.";
                }

                bool success = mgr.DespawnLastEnemy();
                return success ? "Successfully despawned last spawned enemy." : "No authoritative enemies found to despawn.";
            }
        }

        private static class GatherParty
        {
            public static readonly string name = "gather_party";
            public static readonly string description = "Teleports/synchronizes all connected party members to host's cell and location.";
            public static readonly string usage = "gather_party";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.IsHost)
                    return "Only the Host can issue the gather_party command.";

                mgr.GatherPartyToHost();
                return "Gather Party command sent to all peers.";
            }
        }

        private static class SetTime
        {
            public static readonly string name = "set_time";
            public static readonly string description = "Sets world time (Host only in multiplayer).";
            public static readonly string usage = "set_time <hour> [minute]";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                    return "Usage: " + usage;

                if (!int.TryParse(args[0], out int hour) || hour < 0 || hour > 23)
                    return $"Invalid hour '{args[0]}'. Must be between 0 and 23.";

                int minute = 0;
                if (args.Length > 1 && (!int.TryParse(args[1], out minute) || minute < 0 || minute > 59))
                    return $"Invalid minute '{args[1]}'. Must be between 0 and 59.";

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.IsHost && NetworkSession.IsActive)
                    return "Only the Host can set world time in multiplayer.";

                mgr.SetWorldTime(hour, minute);
                return $"World time set to {hour:D2}:{minute:D2}.";
            }
        }

        private static class SetWeather
        {
            public static readonly string name = "set_weather";
            public static readonly string description = "Sets world weather (Host only in multiplayer).";
            public static readonly string usage = "set_weather <sunny|cloudy|overcast|fog|rain|thunder|snow>";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                    return "Usage: " + usage;

                if (!Enum.TryParse<WeatherType>(args[0], true, out WeatherType weather))
                    return $"Invalid weather type '{args[0]}'. Valid options: sunny, cloudy, overcast, fog, rain, thunder, snow.";

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.IsHost && NetworkSession.IsActive)
                    return "Only the Host can set weather in multiplayer.";

                mgr.SetWorldWeather(weather);
                return $"World weather set to {weather}.";
            }
        }

        private static class TimeScale
        {
            public static readonly string name = "time_scale";
            public static readonly string description = "Sets world time scale (Host only in multiplayer).";
            public static readonly string usage = "time_scale <scale>";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                    return "Usage: " + usage;

                if (!float.TryParse(args[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale) || scale < 0f)
                    return $"Invalid time scale '{args[0]}'. Must be a non-negative number.";

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                if (!NetworkSession.IsHost && NetworkSession.IsActive)
                    return "Only the Host can set time scale in multiplayer.";

                mgr.SetWorldTimeScale(scale);
                return $"World time scale set to {scale}.";
            }
        }

        private static class NetLog
        {
            public static readonly string name = "net_log";
            public static readonly string description = "Configures or displays multiplayer console logging filters.";
            public static readonly string usage = "net_log [category (time|weather|entities|spawns|loot|transitions|all)] [on|off]";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                {
                    return $"Multiplayer Logging Filters:\n" +
                           $"  time:        {(DFUNetworkManager.LogTimeEnabled ? "ON" : "OFF")}\n" +
                           $"  weather:     {(DFUNetworkManager.LogWeatherEnabled ? "ON" : "OFF")}\n" +
                           $"  entities:    {(DFUNetworkManager.LogEntitiesEnabled ? "ON" : "OFF")}\n" +
                           $"  spawns:      {(DFUNetworkManager.LogSpawnsEnabled ? "ON" : "OFF")}\n" +
                           $"  loot:        {(DFUNetworkManager.LogLootEnabled ? "ON" : "OFF")}\n" +
                           $"  transitions: {(DFUNetworkManager.LogTransitionsEnabled ? "ON" : "OFF")}";
                }

                string cat = args[0].ToLowerInvariant();
                bool? setState = null;
                if (args.Length > 1)
                {
                    string val = args[1].ToLowerInvariant();
                    if (val == "on" || val == "true" || val == "1" || val == "enable")
                        setState = true;
                    else if (val == "off" || val == "false" || val == "0" || val == "disable")
                        setState = false;
                    else
                        return $"Invalid state '{args[1]}'. Use 'on' or 'off'.";
                }

                switch (cat)
                {
                    case "time":
                        DFUNetworkManager.LogTimeEnabled = setState ?? !DFUNetworkManager.LogTimeEnabled;
                        return $"Logging for 'time' is now {(DFUNetworkManager.LogTimeEnabled ? "ON" : "OFF")}.";

                    case "weather":
                        DFUNetworkManager.LogWeatherEnabled = setState ?? !DFUNetworkManager.LogWeatherEnabled;
                        return $"Logging for 'weather' is now {(DFUNetworkManager.LogWeatherEnabled ? "ON" : "OFF")}.";

                    case "entities":
                    case "entity":
                    case "puppets":
                    case "puppet":
                        DFUNetworkManager.LogEntitiesEnabled = setState ?? !DFUNetworkManager.LogEntitiesEnabled;
                        return $"Logging for 'entities' is now {(DFUNetworkManager.LogEntitiesEnabled ? "ON" : "OFF")}.";

                    case "spawns":
                    case "spawn":
                        DFUNetworkManager.LogSpawnsEnabled = setState ?? !DFUNetworkManager.LogSpawnsEnabled;
                        return $"Logging for 'spawns' is now {(DFUNetworkManager.LogSpawnsEnabled ? "ON" : "OFF")}.";

                    case "loot":
                        DFUNetworkManager.LogLootEnabled = setState ?? !DFUNetworkManager.LogLootEnabled;
                        return $"Logging for 'loot' is now {(DFUNetworkManager.LogLootEnabled ? "ON" : "OFF")}.";

                    case "transitions":
                    case "transition":
                        DFUNetworkManager.LogTransitionsEnabled = setState ?? !DFUNetworkManager.LogTransitionsEnabled;
                        return $"Logging for 'transitions' is now {(DFUNetworkManager.LogTransitionsEnabled ? "ON" : "OFF")}.";

                    case "all":
                        bool targetState = setState ?? !(DFUNetworkManager.LogTimeEnabled && DFUNetworkManager.LogWeatherEnabled && DFUNetworkManager.LogEntitiesEnabled && DFUNetworkManager.LogSpawnsEnabled && DFUNetworkManager.LogLootEnabled && DFUNetworkManager.LogTransitionsEnabled);
                        DFUNetworkManager.LogTimeEnabled = targetState;
                        DFUNetworkManager.LogWeatherEnabled = targetState;
                        DFUNetworkManager.LogEntitiesEnabled = targetState;
                        DFUNetworkManager.LogSpawnsEnabled = targetState;
                        DFUNetworkManager.LogLootEnabled = targetState;
                        DFUNetworkManager.LogTransitionsEnabled = targetState;
                        return $"All multiplayer logging categories are now {(targetState ? "ON" : "OFF")}.";

                    default:
                        return $"Unknown category '{args[0]}'. Valid categories: time, weather, entities, spawns, loot, transitions, all.";
                }
            }
        }

        private static class QuestGui
        {
            public static readonly string name = "quest_gui";
            public static readonly string description = "Toggles the Quest Creator / Test Menu overlay / TTRPG Quests Tab.";
            public static readonly string usage = "quest_gui";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available on GameManager.";

                if (mgr.showTtrpgSuite && mgr.ttrpgSelectedTab == 3)
                {
                    mgr.showTtrpgSuite = false;
                }
                else
                {
                    mgr.showTtrpgSuite = true;
                    mgr.ttrpgSelectedTab = 3;
                }
                return $"TTRPG Master Suite (Quests & Diálogos) is now {(mgr.showTtrpgSuite ? "OPEN" : "CLOSED")}.";
            }
        }

        private static class NetQuest
        {
            public static readonly string name = "net_quest";
            public static readonly string description = "Multiplayer quest management, testing, and debugging commands.";
            public static readonly string usage = "net_quest [menu|list|offer <name> [faction]|start <name> [faction]|end <name|uid|all>]";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                {
                    return "Usage: " + usage;
                }

                string sub = args[0].ToLowerInvariant();
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;

                switch (sub)
                {
                    case "menu":
                    case "gui":
                        if (mgr == null) return "DFUNetworkManager not available.";
                        if (mgr.showTtrpgSuite && mgr.ttrpgSelectedTab == 3)
                        {
                            mgr.showTtrpgSuite = false;
                        }
                        else
                        {
                            mgr.showTtrpgSuite = true;
                            mgr.ttrpgSelectedTab = 3;
                        }
                        return $"TTRPG Master Suite (Quests & Diálogos) is now {(mgr.showTtrpgSuite ? "OPEN" : "CLOSED")}.";

                    case "list":
                        if (QuestMachine.Instance == null) return "QuestMachine is not initialized.";
                        ulong[] active = QuestMachine.Instance.GetAllActiveQuests();
                        if (active == null || active.Length == 0)
                            return "No quests are currently active.";

                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        sb.AppendLine($"Active Quests ({active.Length}):");
                        foreach (ulong uid in active)
                        {
                            Quest q = QuestMachine.Instance.GetQuest(uid);
                            if (q != null)
                            {
                                string title = !string.IsNullOrEmpty(q.DisplayName) ? q.DisplayName : q.QuestName;
                                sb.AppendLine($" - [{q.QuestName}] {title} (UID: {uid})");
                            }
                        }
                        return sb.ToString().TrimEnd();

                    case "offer":
                        if (args.Length < 2) return "Usage: net_quest offer <questName> [factionId]";
                        string offerName = args[1];
                        int offerFaction = 0;
                        if (args.Length > 2) int.TryParse(args[2], out offerFaction);

                        if (mgr == null) return "DFUNetworkManager not available.";
                        bool offered = mgr.InitiatePartyQuestOffer(offerName, offerFaction);
                        return offered ? $"Initiated party quest offer for '{offerName}'." : $"Failed to offer quest '{offerName}'.";

                    case "start":
                        if (args.Length < 2) return "Usage: net_quest start <questName> [factionId]";
                        string startName = args[1];
                        int startFaction = 0;
                        if (args.Length > 2) int.TryParse(args[2], out startFaction);

                        try
                        {
                            QuestMachine.Instance.StartQuest(startName, startFaction);
                            return $"Started quest '{startName}' locally (Faction: {startFaction}).";
                        }
                        catch (Exception ex)
                        {
                            return $"Error starting quest '{startName}': {ex.Message}";
                        }

                    case "end":
                    case "stop":
                        if (args.Length < 2) return "Usage: net_quest end <name|uid|all>";
                        string target = args[1];
                        if (QuestMachine.Instance == null) return "QuestMachine not initialized.";

                        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            ulong[] allQuests = QuestMachine.Instance.GetAllActiveQuests();
                            int count = 0;
                            foreach (ulong uid in allQuests)
                            {
                                Quest q = QuestMachine.Instance.GetQuest(uid);
                                if (q != null)
                                {
                                    q.EndQuest();
                                    count++;
                                }
                            }
                            return $"Ended {count} active quest(s).";
                        }

                        if (ulong.TryParse(target, out ulong targetUid))
                        {
                            Quest q = QuestMachine.Instance.GetQuest(targetUid);
                            if (q != null)
                            {
                                q.EndQuest();
                                return $"Ended quest '{q.QuestName}' (UID: {targetUid}).";
                            }
                            return $"Quest with UID {targetUid} not found.";
                        }
                        else
                        {
                            ulong[] matching = QuestMachine.Instance.FindQuests(target, true);
                            if (matching == null || matching.Length == 0)
                                return $"No active quests found matching name '{target}'.";

                            foreach (ulong uid in matching)
                            {
                                Quest q = QuestMachine.Instance.GetQuest(uid);
                                if (q != null) q.EndQuest();
                            }
                            return $"Ended {matching.Length} quest(s) matching '{target}'.";
                        }

                    default:
                        return "Unknown subcommand. Usage: " + usage;
                }
            }
        }

        private static class DmInvis
        {
            public static readonly string name = "dm_invis";
            public static readonly string description = "Toggles DM True Invisibility on the local player.";
            public static readonly string usage = "dm_invis";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance != null ? GameManager.Instance.NetworkManager : null;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                mgr.ToggleDmInvisibility();
                return $"DM Invisibility is now {(mgr.isDmInvisible ? "ON" : "OFF")}.";
            }
        }

        private static class DmFreeCam
        {
            public static readonly string name = "dm_freecam";
            public static readonly string description = "Toggles Eagle Eye / FreeCam 3D flying observer mode with local character puppet.";
            public static readonly string usage = "dm_freecam";

            public static string Execute(params string[] args)
            {
                FreeCamController controller = FreeCamController.Instance;
                if (controller == null && DFUNetworkManager.Instance != null)
                {
                    controller = DFUNetworkManager.Instance.GetComponent<FreeCamController>() ??
                                 DFUNetworkManager.Instance.gameObject.AddComponent<FreeCamController>();
                }

                if (controller == null)
                    return "FreeCamController not available.";

                controller.ToggleFreeCam();
                return $"FreeCam is now {(controller.IsActive ? "ACTIVE" : "INACTIVE")}.";
            }
        }

        private static class DmTeleport
        {
            public static readonly string name = "dm_tp";
            public static readonly string description = "Teleports player locally to coordinates, first quest objective, or quest giver.";
            public static readonly string usage = "dm_tp <mapX> <mapY> | dm_tp quest | dm_tp questor";

            public static string Execute(params string[] args)
            {
                if (args == null || args.Length == 0)
                    return "Usage: " + usage;

                DFUNetworkManager mgr = GameManager.Instance != null ? GameManager.Instance.NetworkManager : null;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                string sub = args[0].ToLowerInvariant();
                if (sub == "quest")
                {
                    if (QuestMachine.Instance == null)
                        return "QuestMachine not available.";

                    ulong[] activeQuests = QuestMachine.Instance.GetAllActiveQuests();
                    if (activeQuests == null || activeQuests.Length == 0)
                        return "No active quests running.";

                    Quest q = QuestMachine.Instance.GetQuest(activeQuests[0]);
                    if (q == null)
                        return "Active quest not found.";

                    QuestResource[] places = q.GetAllResources(typeof(Place));
                    if (places == null || places.Length == 0)
                        return $"No Place resources found in quest '{q.QuestName}'.";

                    Place targetPlace = null;
                    foreach (var res in places)
                    {
                        Place p = res as Place;
                        if (p != null && !string.IsNullOrEmpty(p.SiteDetails.locationName))
                        {
                            if (p.SiteDetails.siteType == SiteTypes.Dungeon || targetPlace == null)
                                targetPlace = p;
                        }
                    }

                    if (targetPlace == null)
                        return $"Could not find a valid objective site for quest '{q.QuestName}'.";

                    SiteDetails site = targetPlace.SiteDetails;
                    DFLocation loc;
                    if (DaggerfallUnity.Instance != null &&
                        DaggerfallUnity.Instance.ContentReader != null &&
                        DaggerfallUnity.Instance.ContentReader.GetLocation(site.regionName, site.locationName, out loc))
                    {
                        DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel(loc.MapTableData.Longitude, loc.MapTableData.Latitude);
                        mgr.TeleportLocalPlayer(pos.X, pos.Y);
                        return $"Teleported to quest objective: {site.locationName} ({site.regionName}) [{site.siteType}] at ({pos.X}, {pos.Y}).";
                    }

                    return $"Could not resolve coordinates for '{site.locationName}' in '{site.regionName}'.";
                }
                else if (sub == "questor")
                {
                    if (QuestMachine.Instance == null)
                        return "QuestMachine not available.";

                    ulong[] activeQuests = QuestMachine.Instance.GetAllActiveQuests();
                    if (activeQuests == null || activeQuests.Length == 0)
                        return "No active quests running.";

                    Quest q = QuestMachine.Instance.GetQuest(activeQuests[0]);
                    if (q == null)
                        return "Active quest not found.";

                    QuestResource[] persons = q.GetAllResources(typeof(Person));
                    Person questorPerson = null;
                    if (persons != null)
                    {
                        foreach (var res in persons)
                        {
                            Person p = res as Person;
                            if (p != null && p.IsQuestor)
                            {
                                questorPerson = p;
                                break;
                            }
                        }
                        if (questorPerson == null && persons.Length > 0)
                            questorPerson = persons[0] as Person;
                    }

                    Place questorPlace = null;
                    if (questorPerson != null)
                    {
                        questorPlace = questorPerson.GetDialogPlace() ?? questorPerson.GetHomePlace();
                    }

                    if (questorPlace != null && !string.IsNullOrEmpty(questorPlace.SiteDetails.locationName))
                    {
                        SiteDetails site = questorPlace.SiteDetails;
                        DFLocation loc;
                        if (DaggerfallUnity.Instance != null &&
                            DaggerfallUnity.Instance.ContentReader != null &&
                            DaggerfallUnity.Instance.ContentReader.GetLocation(site.regionName, site.locationName, out loc))
                        {
                            DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel(loc.MapTableData.Longitude, loc.MapTableData.Latitude);
                            mgr.TeleportLocalPlayer(pos.X, pos.Y);
                            return $"Teleported to quest giver at {site.locationName} ({site.regionName}) at ({pos.X}, {pos.Y}).";
                        }
                    }

                    // Fallback to any town or building place in quest
                    QuestResource[] places = q.GetAllResources(typeof(Place));
                    if (places != null)
                    {
                        foreach (var res in places)
                        {
                            Place p = res as Place;
                            if (p != null && (p.SiteDetails.siteType == SiteTypes.Town || p.SiteDetails.siteType == SiteTypes.Building) && !string.IsNullOrEmpty(p.SiteDetails.locationName))
                            {
                                DFLocation loc;
                                if (DaggerfallUnity.Instance != null &&
                                    DaggerfallUnity.Instance.ContentReader != null &&
                                    DaggerfallUnity.Instance.ContentReader.GetLocation(p.SiteDetails.regionName, p.SiteDetails.locationName, out loc))
                                {
                                    DFPosition pos = MapsFile.LongitudeLatitudeToMapPixel(loc.MapTableData.Longitude, loc.MapTableData.Latitude);
                                    mgr.TeleportLocalPlayer(pos.X, pos.Y);
                                    return $"Teleported to quest giver settlement at {p.SiteDetails.locationName} ({p.SiteDetails.regionName}) at ({pos.X}, {pos.Y}).";
                                }
                            }
                        }
                    }

                    return $"Could not find quest giver location for quest '{q.QuestName}'.";
                }
                else if (args.Length >= 2 && int.TryParse(args[0], out int mapX) && int.TryParse(args[1], out int mapY))
                {
                    mgr.TeleportLocalPlayer(mapX, mapY);
                    return $"Teleported to coordinates ({mapX}, {mapY}).";
                }

                return "Usage: " + usage;
            }
        }

        private static class DmGod
        {
            public static readonly string name = "dm_god";
            public static readonly string description = "Toggles DM God Mode (invincibility, unlimited health/fatigue/magicka).";
            public static readonly string usage = "dm_god";

            public static string Execute(params string[] args)
            {
                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                mgr.ToggleDmGodMode();
                bool isGod = GameManager.Instance.PlayerEntity != null && GameManager.Instance.PlayerEntity.GodMode;
                return $"God Mode is now {(isGod ? "ENABLED" : "DISABLED")}.";
            }
        }

        private static class NetPvp
        {
            public static readonly string name = "net_pvp";
            public static readonly string description = "Toggles or sets friendly fire / PvP damage among party members (Host only).";
            public static readonly string usage = "net_pvp [on|off]";

            public static string Execute(params string[] args)
            {
                if (!NetworkSession.IsHost)
                    return "Only the Host can configure PvP / Friendly Fire settings.";

                DFUNetworkManager mgr = GameManager.Instance.NetworkManager;
                if (mgr == null)
                    return "DFUNetworkManager not available.";

                bool targetState = !DFUNetworkManager.isPvpAllowed;
                if (args != null && args.Length > 0)
                {
                    string sub = args[0].ToLowerInvariant();
                    if (sub == "on" || sub == "true" || sub == "1")
                        targetState = true;
                    else if (sub == "off" || sub == "false" || sub == "0")
                        targetState = false;
                }

                mgr.SetPvpAllowed(targetState);
                return $"PvP / Friendly Fire is now {(targetState ? "ENABLED" : "DISABLED")}.";
            }
        }
    }
}

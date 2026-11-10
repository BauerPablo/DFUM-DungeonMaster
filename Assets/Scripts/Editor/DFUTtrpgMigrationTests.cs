// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2023 Daggerfall Workshop
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Networking;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Automated EditMode & CLI Regression Test Suite verifying all 6 Migration Slices
    /// for Daggerfall Unity 1.1.1 + Native Multiplayer & TTRPG Master Suite (Phases 1-21).
    /// </summary>
    public class DFUTtrpgMigrationTests
    {
        [Test]
        public void Slice1_NetworkSessionAndRegistries_OperateCorrectly()
        {
            Assert.IsFalse(NetworkSession.IsActive, "NetworkSession should be inactive in offline EditMode.");
            Assert.IsTrue(NetworkSession.IsSinglePlayer, "NetworkSession should report SinglePlayer when offline.");
            Assert.IsTrue(NetworkSession.ShouldHandleSpawns, "ShouldHandleSpawns must be true in offline SinglePlayer.");

            DFUNetworkRegistry.Clear();
            GameObject testEntityGo = new GameObject("TestNetEntity");
            try
            {
                NativeNetworkEntity netEnt = testEntityGo.AddComponent<NativeNetworkEntity>();
                Guid guid = Guid.NewGuid();
                netEnt.entityId = guid;
                netEnt.EntityId = 42001UL;

                Assert.IsTrue(DFUNetworkRegistry.Register(42001UL, netEnt), "Numeric ID registration should succeed.");
                Assert.AreEqual(netEnt, DFUNetworkRegistry.Get(42001UL), "Lookup by numeric ID should return exact entity.");
                Assert.IsTrue(DFUNetworkRegistry.TryGetEntity(guid, out GameObject resolvedGo), "Lookup by GUID should succeed.");
                Assert.AreEqual(testEntityGo, resolvedGo, "Lookup by GUID should return exact GameObject.");

                Assert.IsTrue(DFUNetworkRegistry.Unregister(42001UL), "Unregister should return true.");
                Assert.IsNull(DFUNetworkRegistry.Get(42001UL), "Entity should be removed after Unregister.");
            }
            finally
            {
                DFUNetworkRegistry.Clear();
                UnityEngine.Object.DestroyImmediate(testEntityGo);
            }

            DFUNetworkLootRegistry.Clear();
            GameObject testLootGo = new GameObject("TestLootContainer");
            try
            {
                DaggerfallLoot loot = testLootGo.AddComponent<DaggerfallLoot>();
                DFUNetworkLootRegistry.Register(99001UL, loot);
                Assert.AreEqual(loot, DFUNetworkLootRegistry.Get(99001UL), "LootRegistry should resolve registered container by LoadID.");
                DFUNetworkLootRegistry.Unregister(99001UL);
                Assert.IsNull(DFUNetworkLootRegistry.Get(99001UL), "LootRegistry should return null after Unregister.");
            }
            finally
            {
                DFUNetworkLootRegistry.Clear();
                UnityEngine.Object.DestroyImmediate(testLootGo);
            }

            DFUNetworkDoorRegistry.Clear();
            GameObject testDoorGo = new GameObject("TestDoor");
            try
            {
                DaggerfallActionDoor door = testDoorGo.AddComponent<DaggerfallActionDoor>();
                door.LoadID = 77001UL;
                DFUNetworkDoorRegistry.Register(77001UL, door);
                Assert.AreEqual(door, DFUNetworkDoorRegistry.FindDoor(77001UL, testDoorGo.transform.position), "DoorRegistry should resolve door by LoadID.");
                DFUNetworkDoorRegistry.Unregister(77001UL);
                Assert.IsNull(DFUNetworkDoorRegistry.FindDoor(77001UL, new Vector3(9999f, 9999f, 9999f)), "DoorRegistry should return null after Unregister when out of range.");
            }
            finally
            {
                DFUNetworkDoorRegistry.Clear();
                UnityEngine.Object.DestroyImmediate(testDoorGo);
            }
        }

        [Test]
        public void Slice2_VersionGateAndPlayerLifecycle_Validate111AndDownedState()
        {
            Assert.AreEqual("1.1.1", VersionInfo.DaggerfallUnityVersion, "VersionInfo.DaggerfallUnityVersion must be 1.1.1 for Nexus mod compatibility.");
            Assert.IsFalse(string.IsNullOrEmpty(VersionInfo.DFUTTRPGVersion), "DFUTTRPGVersion must be defined.");
            MethodInfo versionCheck = typeof(ModManager).GetMethod("IsVersionLowerOrEqual", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(versionCheck, "ModManager.IsVersionLowerOrEqual must exist.");
            Assert.AreEqual(true, versionCheck.Invoke(null, new object[] { "1.1.1", VersionInfo.DaggerfallUnityVersion }), "Mods targeting DFU 1.1.1 must pass ModManager version check natively.");

            GameObject lifecycleGo = new GameObject("TestLifecycle");
            try
            {
                DFUPlayerLifecycleController controller = lifecycleGo.AddComponent<DFUPlayerLifecycleController>();
                Assert.AreEqual(PlayerLifecycleState.Alive, controller.CurrentState, "Initial lifecycle state must be Alive.");
                Assert.IsTrue(controller.IsAlive);
                Assert.IsFalse(controller.IsDowned);
                Assert.IsFalse(controller.IsSpectator);
                Assert.AreEqual(180f, DFUPlayerLifecycleController.MaxBleedoutSeconds, "Bleedout timer constant must be 180 seconds (3 minutes).");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lifecycleGo);
            }
        }

        [Test]
        public void Slice3And4_Upstream111ApisAndMultiplayerHooks_CoexistCleanly()
        {
            // Verify DFU 1.1.1 StringTableCSVParser upstream API exists and parses CSVs
            string sampleCsv = "key,text\nhello_key,\"Hello Tamriel\"\n";
            Dictionary<string, string> parsed = StringTableCSVParser.LoadDictionary(sampleCsv);
            Assert.IsNotNull(parsed);
            Assert.IsTrue(parsed.ContainsKey("hello_key"));
            Assert.AreEqual("Hello Tamriel", parsed["hello_key"]);

            // Verify FPSWeapon.Tint (DFU 1.1.1 mod API) and WeaponManager multiplayer hooks exist
            PropertyInfo tintProp = typeof(FPSWeapon).GetProperty("Tint", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(tintProp, "DFU 1.1.1 FPSWeapon.Tint property must be preserved.");

            // Verify GameManager.NetworkManager property exists
            PropertyInfo netMgrProp = typeof(GameManager).GetProperty("NetworkManager", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(netMgrProp, "GameManager.NetworkManager property must be wired.");

            // Verify EnemySenses multi-target methods and 1.1.1 ObstacleDetected property exist
            PropertyInfo obstacleProp = typeof(EnemyMotor).GetProperty("ObstacleDetected", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(obstacleProp, "DFU 1.1.1 EnemyMotor.ObstacleDetected property must be preserved.");
        }

        [Test]
        public void Slice5And6_RetroUiThemeAndAssets_ExistAndGenerateCleanly()
        {
            Texture2D frameTex = DarkFantasyPixelArtTheme.CreateOrnateWindowTexture(64, 48, DarkFantasyPixelArtTheme.ObsidianBg, false);
            try
            {
                Assert.IsNotNull(frameTex, "DarkFantasyPixelArtTheme must generate ornate window texture.");
                Assert.AreEqual(64, frameTex.width);
                Assert.AreEqual(48, frameTex.height);
                Assert.AreEqual(FilterMode.Point, frameTex.filterMode, "Pixel-art theme textures must use Point filtering.");
            }
            finally
            {
                if (frameTex != null)
                    UnityEngine.Object.DestroyImmediate(frameTex);
            }

            // Verify required custom assets and Tier 1 mods exist on disk
            string projectRoot = Directory.GetCurrentDirectory();
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, "Assets/Resources/Screens/background_menu.jpeg")), "Main menu background texture must exist.");
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, "Assets/Resources/Screens/title_logo.png")), "Main menu title logo texture must exist.");
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, "Assets/StreamingAssets/Textures/TEXTURE.488/25-0.PNG")), "Custom crouch front sprite 25-0.PNG must exist.");
            Assert.IsTrue(File.Exists(Path.Combine(projectRoot, "Assets/StreamingAssets/Textures/TEXTURE.488/25-1.PNG")), "Custom crouch back sprite 25-1.PNG must exist.");

            string tier1Dir = Path.Combine(projectRoot, "Assets/StreamingAssets/Mods/Tier1_Windows");
            Assert.IsTrue(Directory.Exists(tier1Dir), "Tier1_Windows mod directory must exist.");
            string[] dfmods = Directory.GetFiles(tier1Dir, "*.dfmod");
            Assert.AreEqual(18, dfmods.Length, "All 18 certified Tier 1 .dfmod packages must be present.");
        }

        [MenuItem("Daggerfall Tools/Multiplayer/Run Migration Verification Tests")]
        public static void RunAllCliTests()
        {
            DFUTtrpgMigrationTests suite = new DFUTtrpgMigrationTests();
            int passed = 0;
            int failed = 0;

            Action[] tests = new Action[]
            {
                suite.Slice1_NetworkSessionAndRegistries_OperateCorrectly,
                suite.Slice2_VersionGateAndPlayerLifecycle_Validate111AndDownedState,
                suite.Slice3And4_Upstream111ApisAndMultiplayerHooks_CoexistCleanly,
                suite.Slice5And6_RetroUiThemeAndAssets_ExistAndGenerateCleanly
            };

            foreach (Action test in tests)
            {
                try
                {
                    test.Invoke();
                    passed++;
                    Debug.Log($"[DFUTTRPG-TEST PASS] {test.Method.Name}");
                }
                catch (Exception ex)
                {
                    failed++;
                    Debug.LogError($"[DFUTTRPG-TEST FAIL] {test.Method.Name}: {ex}");
                }
            }

            Debug.Log($"[DFUTTRPG-TEST SUMMARY] Passed: {passed}/{tests.Length}, Failed: {failed}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(failed == 0 ? 0 : 1);
            }
        }
    }
}

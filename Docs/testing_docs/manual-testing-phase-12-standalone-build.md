# Manual Testing Guide: Legacy Mod Purge & Standalone Packaging (Phase 12)

## Overview
This testing guide covers the final architecture milestone (**Phase 12**):
1. **Complete Legacy Mod Purge:** Total removal of `Assets/Game/Mods/TanguyMultiplayer/` and all legacy hooks with zero compilation errors and zero broken references across core code.
2. **Standalone Windows Build Execution:** Verifying that a standalone Windows 64-bit production player build runs 100% out-of-the-box.
3. **End-to-End Multiplayer in Standalone:** Host/Client lifecycle, synchronization, and gameplay functioning completely outside the Unity Editor.

---

## Testing Status Dashboard

| Status | Total Cases | Cases |
| :--- | :---: | :--- |
| **READY FOR VERIFICATION** | **3** | Cases 1, 2, 3 |

---

## Build Reference & Automation

### Method 1: Unity Editor Menu (1-Click)
In the Unity Editor top menu bar, click:
`Daggerfall Tools` -> `Build Standalone Windows Player (x64)`

The build is automatically placed in:
`Builds/StandaloneWindows64/DaggerfallUnity.exe`

### Method 2: Headless CLI Build
Run the following PowerShell command:
```powershell
& "C:\Program Files\Unity\Hub\Editor\2019.4.40f1\Editor\Unity.exe" -quit -batchmode -projectPath "D:\11 Workspace\17 DFU-Multiplayer" -executeMethod DaggerfallWorkshop.Editor.StandaloneBuildHelper.BuildWindowsPlayer -logFile build.log
```

---

## Detailed Test Cases & Checklists

### [ ] Case 1: Core Independence & Zero Mod References Verification
- [ ] Run full project compilation in Unity Editor.
- [ ] Verify console reports **0 compilation errors** and **0 compilation warnings** related to deleted types.
- [ ] Verify `Assets/Game/Mods/` contains zero legacy scripts, prefabs, or archives (only standard DFU files remain).
- [ ] Verify `Assets/StreamingAssets/Textures/TEXTURE.488/` provides all required native puppet billboard textures (`25-0.PNG`, `25-1.PNG`).

---

### [ ] Case 2: Standalone Windows Player Build Generation
- [ ] Trigger standalone build via `Daggerfall Tools > Build Standalone Windows Player (x64)` or CLI batchmode.
- [ ] Check build log for successful compilation:
  - [ ] Both core scenes included (`DaggerfallUnityStartup.unity` and `DaggerfallUnityGame.unity`).
  - [ ] Build completes with `BuildResult.Succeeded`.
- [ ] Verify build output directory contains:
  - `DaggerfallUnity.exe`
  - `UnityPlayer.dll`
  - `DaggerfallUnity_Data/`
  - `DaggerfallUnity_Data/StreamingAssets/`

---

### [ ] Case 3: End-to-End Standalone Multiplayer Session
- [ ] Launch `Builds/StandaloneWindows64/DaggerfallUnity.exe` (Instance 1 - Host).
- [ ] In main menu, start or load game, press `Ctrl + F10` or open Network Menu, and click **`Host Server (KCP)`**.
- [ ] Launch a second instance (Instance 2 - Client):
  - [ ] Can be either Unity Editor or a second standalone executable instance.
  - [ ] Connect to `127.0.0.1` (or LAN/VPN IP).
- [ ] **Verify Core Multiplayer Features in Standalone:**
  - [ ] Puppet visual avatars spawn with correct race/class billboards and overhead name tags.
  - [ ] Movement and position sync smoothly between host and client.
  - [ ] Proximity **`F`** key opens player interaction window (Trade & Map Sharing).
  - [ ] Spellcasting animations, missiles, and impacts replicate across instances.
  - [ ] Combat damage and enemy sync function authoritatively with zero legacy mod dependencies.

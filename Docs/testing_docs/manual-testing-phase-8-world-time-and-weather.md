# Manual Testing Guide: Phase 8 World Time, Sky & Weather Sync

This document provides a comprehensive step-by-step verification protocol for **Phase 8 (World Time, Sky & Weather Sync)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity Editor.
2. **Clean Compilation Check**:
   - Verify that there are **0 compilation errors** in the Unity Console.
   - Verify that all Phase 8 network and game files compile cleanly:
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`WeatherManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/WeatherManager.cs)
     - [`DaggerfallRestWindow.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallRestWindow.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
     - [`DFUNetworkCommands.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkCommands.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Initial Connection Time & Weather Synchronization (Late-Join Handshake)
* **Objective**: Confirm that when a client connects to an active session, it immediately receives and applies the Host's current world time, time progression scale, and active weather state without local mismatch.
* **Steps**:
  1. On **Instance A (Host)**:
     - Start game in an exterior region (e.g. Daggerfall city or Privateer's Hold exterior).
     - Start multiplayer host via console command `net_host` (or press `F10` -> `Start Host`).
     - Open DM tool (`F11` or console `dm_gui`).
     - Set time to night: click **Midnight (00:00)** or run console command `set_time 0`.
     - Set weather to rain: click **Rain** or run console command `set_weather rain`.
     - Confirm Host shows dark night sky, rain particles falling, rain audio loops playing, and city lights on.
  2. On **Instance B (Client)**:
     - Start standalone client.
     - Connect via console `net_join 127.0.0.1` (or press `F10` -> `Connect Client`).
  3. Observe **Client** screen immediately upon spawn.
* **Expected Result**:
  - The client instantly renders nighttime (dark skybox, stars/moons, disabled directional sunlight).
  - The client immediately shows falling rain particles and plays rain ambient audio.
  - The client's clock (`DaggerfallUnity.Instance.WorldTime.Now`) matches the Host's exact hour, minute, and second.
  - Client console outputs: `[DFU Time] Client synced authoritative world time: ...` and `[DFU Weather] Client received authoritative weather: Rain`.

---

### Test Case 2: Continuous Real-Time Lockstep & Drift Threshold
* **Objective**: Confirm that the Host periodically broadcasts authoritative time (`MsgWorldTimeSync` every 3 seconds) and client time advances smoothly in lockstep with zero cumulative drift.
* **Steps**:
  1. Keep both Host and Client connected in an exterior cell.
  2. Let the game run for 2–3 minutes in real time.
  3. Check the in-game date and time on both instances (press `F11` on Host to view live time, or open character status / check console).
* **Expected Result**:
  - Both Host and Client clocks progress at the exact same rate (`TimeScale = 12.0f`).
  - No visual stutter, jitter, or rapid sky popping occurs on the Client during periodic 3-second sync heartbeats.
  - Client time remains within +/- 1 second of Host time continuously.

---

### Test Case 3: Day / Night Cycle, Skybox & Directional Sunlight Synchronization
* **Objective**: Confirm that cycling through day and night presets moves the sun/moon angle, swaps skybox textures, and synchronizes illumination across both peers simultaneously.
* **Steps**:
  1. On **Host**, press `F11` to open the DM Tool.
  2. Click **Dawn (06:00)** (or run `set_time 6 0`).
     - Observe both screens: sunrise color transition, dawn fog tint, sun cresting the horizon.
  3. Click **Noon (12:00)** (or run `set_time 12 0`).
     - Observe both screens: bright midday sun directly overhead, clear daylight shadows.
  4. Click **Dusk (18:00)** (or run `set_time 18 0`).
     - Observe both screens: sunset colors, lengthening shadows, fading daylight.
  5. Click **Midnight (00:00)** (or run `set_time 0 0`).
     - Observe both screens: full night sky, directional sunlight disabled, ambient night audio (crickets/wind).
* **Expected Result**:
  - Both players see the sun angle (`SunlightManager.myLight.transform.rotation`) and skybox orientation update in exact synchrony.
  - Shadow directions and daylight scales match identically on Host and Client.

---

### Test Case 4: City Street Lights & Building Light Windows
* **Objective**: Confirm that automated city street lanterns and building window illumination switch on/off simultaneously for all connected players.
* **Steps**:
  1. Gather both players in an exterior town or city (e.g. Daggerfall, Gothway Garden).
  2. On **Host**, set time to daytime: click **Noon (12:00)** in DM GUI (`F11`) or run `set_time 12`.
     - Verify on both screens: street lanterns are OFF; building windows have opaque/daytime textures.
  3. On **Host**, set time to night: click **Midnight (00:00)** in DM GUI (`F11`) or run `set_time 0`.
     - Verify on both screens: street lanterns turn ON with active point lights; building windows swap to illuminated warm night textures (`DaggerfallLocation.lastCityLightsFlag`).
* **Expected Result**:
  - Lights and window glow switch state on both machines simultaneously without requiring cell reload.

---

### Test Case 5: Dynamic Weather Transitions & Particle Systems
* **Objective**: Verify that changing weather on the Host propagates instantly to all clients, correctly configuring fog, sky styles, precipitation particles, and ambient audio loops.
* **Steps**:
  1. On **Host**, press `F11` to open the DM Tool (or use console commands).
  2. Test **Sunny**:
     - Click **Sunny** (or `set_weather sunny`).
     - Verify: Clear sky, minimal fog, sunny ambient audio, particles deactivated.
  3. Test **Overcast**:
     - Click **Overcast** (or `set_weather overcast`).
     - Verify: Muted sunlight scale, overcast sky, reduced shadows.
  4. Test **Fog**:
     - Click **Fog** (or `set_weather fog`).
     - Verify: Heavy volumetric fog density (`HeavyFogSettings`), reduced visibility.
  5. Test **Rain**:
     - Click **Rain** (or `set_weather rain`).
     - Verify: Falling rain particle system attached to player camera, rainy skybox style, rain ambient sound effects.
  6. Test **Thunder**:
     - Click **Thunder** (or `set_weather thunder`).
     - Verify: Storm rain, dark atmospheric lighting, thunder claps and storm ambient sound.
  7. Test **Snow**:
     - Click **Snow** (or `set_weather snow`).
     - Verify: Falling snowflakes particle system, snowy skybox style, snow lighting scale.
* **Expected Result**:
  - On every transition, the client updates its weather state immediately upon receiving `MsgWeatherSync`.
  - Weather audio and particles match the Host without lag or mismatch.

---

### Test Case 6: Time Progression Scaling (`time_scale`)
* **Objective**: Confirm that altering the world time progression rate synchronizes between Host and Client.
* **Steps**:
  1. On **Host**, run console command `time_scale 60` (5x acceleration) or click **Fast (60x)** in the DM GUI.
  2. Observe the sky and clock on both Host and Client for 30 seconds.
  3. On **Host**, run console command `time_scale 0` or click **Pause (0x)**.
  4. On **Host**, restore normal progression: `time_scale 12` or click **Norm (12x)**.
* **Expected Result**:
  - Under `time_scale 60`, both players observe rapid sun movement across the sky in lockstep.
  - Under `time_scale 0`, world time freezes completely on both instances.
  - Restoring `12x` returns normal Daggerfall time passage on all peers.

---

### Test Case 7: Rest Window Protection & Client Authority Guard
* **Objective**: Confirm that pure clients cannot unilaterally advance world time via the native Rest Window (`DaggerfallRestWindow`), preventing multiplayer time desynchronization.
* **Steps**:
  1. On **Instance B (Client)**: Press the rest hotkey (default: `R`).
  2. Observe the resulting UI feedback.
  3. On **Instance A (Host)**: Press the rest hotkey `R`.
  4. On **Host**: Select "Rest for a while" and rest for 4 hours.
  5. Observe the Client screen while Host is resting.
* **Expected Result**:
  - On the **Client**, pressing `R` triggers a pop-up notice: *"Resting is controlled by the Host in multiplayer."* and immediately cancels the window without advancing time.
  - On the **Host**, resting progresses normally. Because Host resting produces a sudden time jump (`deltaSec > 15`), the Host immediately broadcasts an authoritative `MsgWorldTimeSync (isImmediate = true)`.
  - The **Client** receives the time jump and advances its clock, sun angle, and lighting in sync with the Host's rest.

---

### Test Case 8: Exterior -> Interior -> Exterior Weather & Fog Persistence
* **Objective**: Confirm that transitioning between exterior and interior cells properly pauses precipitation inside buildings and restores authoritative weather/fog upon exiting.
* **Steps**:
  1. On **Host**, set weather to **Rain** (`set_weather rain`).
  2. Verify both players see rain outside.
  3. Have one or both players enter a shop or tavern door.
  4. Verify inside the interior:
     - Rain particles are stopped (`PlayerWeather.isInside == true`).
     - Interior fog is applied (`InteriorFogSettings`).
  5. Exit back to the street.
* **Expected Result**:
  - Upon stepping outside, rain particles resume immediately.
  - Exterior fog and authoritative weather are restored cleanly (`currentOutdoorFogSettings`).

---

## 3. Diagnostic & Troubleshooting Checklist

| Symptom | Diagnostic Step | Root Cause & Resolution |
| :--- | :--- | :--- |
| **Client sky remains at noon after connecting to nighttime Host** | Check Client console for `[DFU Time]` log message. | Handshake packet dropped or blocked. Verify KCP port 7777 UDP traffic and ensure `MsgWorldTimeSync` was sent in `OnServerConnect`. |
| **Client time slowly drifts out of sync** | Verify Host and Client console output every 3 seconds. | If delta exceeds 2 seconds, `OnClientReceiveWorldTimeSync` automatically snaps `FromSeconds(msg.timeInSeconds)`. Check that `worldTime.TimeScale` matches `msg.timeScale`. |
| **Weather particles don't appear on Client during rain** | Inspect Client `PlayerWeather` component in Unity hierarchy. | Ensure player camera has active `PlayerWeather` with valid `RainParticles` and `SnowParticles` references attached. |
| **Client rest opens normally and advances time** | Verify `NetworkSession.IsClient && !NetworkSession.IsHost` returns true on Client. | Client might be running in offline single-player mode. Ensure `net_join` was successful. |
| **Street lanterns don't turn on at midnight** | Check `DaggerfallUnity.Settings.AutomateCityLights` in settings. | Verify `DaggerfallLight.Update()` is running and `DaggerfallUnity.Instance.WorldTime.Now.IsCityLightsOn` evaluates to true (`Hour >= 17 || Hour < 8`). |

---

## 4. Phase 8 Verification Sign-off

When all test cases (TC-8.1 through TC-8.8) pass cleanly, Phase 8 is formally verified and ready to proceed to **Phase 9: Fast Travel & Unanimous Party Voting**.

# Manual Testing Guide: Phase 1 Native Networking & Session Lifecycle

This document provides a step-by-step verification protocol for **Phase 1** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Setup

1. **Unity Editor**: Open the project in Unity (`2019.4.x` / supported DFU version).
2. **Clean Compilation Check**:
   - Check the Unity **Console** tab.
   - Ensure there are **zero compilation errors** (`CS...`).
   - Verify that `Mirror` in `Assets/Mirror/` compiled cleanly alongside the native networking classes in `Assets/Scripts/Game/Networking/`.
3. **Execution Environments**:
   To test peer connections, you need two instances:
   - **Instance A (Host)**: Run directly inside the Unity Editor (`Play Mode`).
   - **Instance B (Client)**: A standalone Windows player build OR a second Unity Editor instance (using ParrelSync / local clone).
   - Alternatively, test lifecycle state changes (Single-Player -> Host -> Stop) locally within a single Unity Editor instance first.

---

## 2. Test Procedures

### Test Case 1: Initial Offline Baseline State
* **Objective**: Confirm that launching the game defaults to single-player mode and that native network singletons initialize cleanly.
* **Steps**:
  1. Open the scene `Assets/Scenes/DaggerfallUnityGame.unity` in Unity.
  2. Press **Play**.
  3. Press the backquote / tilde key (`~` or `\``) to open the native Daggerfall Unity console.
  4. Type `net_status` and press **Enter**.
* **Expected Result**:
  - Console outputs:
    ```text
    Session State: Single Player (Offline)
    IsServer: False
    IsClient: False
    IsHost: False
    IsSinglePlayer: True
    ShouldHandleSpawns: True
    ```
  - Zero errors or null reference warnings logged in the Unity Console.

---

### Test Case 2: In-Game Network GUI Overlay
* **Objective**: Verify that the built-in debug GUI overlay can be toggled and interacted with.
* **Steps**:
  1. In the console, type `net_gui` and press **Enter**.
* **Expected Result**:
  - The message `Network debug GUI is now ENABLED.` is displayed.
  - A GUI window titled **DFU Native Multiplayer** appears in the top-left corner showing:
    - Status: `Single Player (Offline)`
    - IP input field (default: `127.0.0.1`)
    - Port input field (default: `7777`)
    - Buttons: `Start Host (Server + Client)` and `Connect as Client`.
  2. Typing `net_gui` again hides the overlay.

---

### Test Case 3: Starting Authoritative Host (Instance A)
* **Objective**: Verify that the host can bind KCP UDP transport and transition into an authoritative server + local client state.
* **Steps**:
  1. In **Instance A**, open the console and type:
     ```text
     net_host 7777
     ```
     *(Alternatively, open `net_gui` and click `Start Host`).*
  2. Check the Unity Console logs.
  3. In the game console, type `net_status`.
* **Expected Result**:
  - Log outputs:
    - `[DFU Network] Starting Host on port 7777 (KCP Transport)...`
    - `[DFU Network] Authoritative Server started.`
    - `[DFU Network] Successfully connected to server at localhost.`
  - `net_status` outputs:
    ```text
    Session State: Host (Server + Client Connected)
    IsServer: True
    IsClient: False
    IsHost: True
    IsSinglePlayer: False
    ShouldHandleSpawns: True
    ```
  - `DFUNetworkManager` GameObject is persistent in the scene hierarchy (under `DontDestroyOnLoad`).

---

### Test Case 4: Client Connection over KCP (Instance B -> Instance A)
* **Objective**: Verify that a second instance connects via KCP over loopback (`127.0.0.1`) or LAN/VPN IP as a pure client.
* **Steps**:
  1. Ensure **Instance A** is hosting (from Test Case 3).
  2. Launch **Instance B** (Build or second instance).
  3. In **Instance B**, open the console and run:
     ```text
     net_join 127.0.0.1 7777
     ```
     *(If testing across separate PCs via Radmin VPN / LAN, replace `127.0.0.1` with Instance A's IPv4 address).*
  4. Check the console on both instances.
* **Expected Result**:
  - **On Instance B (Client)**:
    - Logs: `[DFU Network] Connecting to 127.0.0.1:7777 (KCP Transport)...`
    - Logs: `[DFU Network] Successfully connected to server at 127.0.0.1.`
    - `net_status` shows:
      ```text
      Session State: Connected Client
      IsServer: False
      IsClient: True
      IsHost: False
      IsSinglePlayer: False
      ShouldHandleSpawns: False
      ```
      *(CRITICAL: Note that `ShouldHandleSpawns` is `False` — this establishes the boundary for Phase 2).*
  - **On Instance A (Host)**:
    - Logs: `[DFU Network] Client connected to server: connectionId=1, address=127.0.0.1`.
    - `net_status` displays `Connected Clients: 2` (host local client + remote client).

---

### Test Case 5: Clean Client Disconnection
* **Objective**: Verify that a disconnecting client tears down cleanly without causing exceptions on the host.
* **Steps**:
  1. On **Instance B (Client)**, run in console:
     ```text
     net_disconnect
     ```
  2. Inspect both instances.
* **Expected Result**:
  - **On Instance B**:
    - Log: `[DFU Network] Disconnected from server.`
    - `net_status` returns to `Single Player (Offline)`.
  - **On Instance A (Host)**:
    - Log: `[DFU Network] Client disconnected from server: connectionId=1`.
    - Host remains active and unaffected (`IsServer` remains `True`).

---

### Test Case 6: Host Teardown
* **Objective**: Verify that shutting down the host stops the server and cleans up resources without memory leaks.
* **Steps**:
  1. On **Instance A (Host)**, run in console:
     ```text
     net_disconnect
     ```
  2. Check status via `net_status`.
* **Expected Result**:
  - Log: `[DFU Network] Server stopped.`
  - `net_status` returns to `Single Player (Offline)`:
    - `IsServer`: `False`
    - `IsActive`: `False`
    - `ShouldHandleSpawns`: `True`

---

## 3. Checklist & Sign-Off

| Checkpoint | Status | Notes |
| :--- | :--- | :--- |
| Zero Unity compilation errors with Mirror in `Assets/Mirror/` | [x] Pass / [ ] Fail | |
| `net_status` correctly reports `Single Player` initially | [x] Pass / [ ] Fail | |
| `net_gui` toggles debug window on screen | [x] Pass / [ ] Fail | |
| `net_host` starts authoritative server on port 7777 | [x] Pass / [ ] Fail | |
| `net_join` connects pure client over KCP | [x] Pass / [ ] Fail | |
| Pure client reports `ShouldHandleSpawns: False` | [x] Pass / [ ] Fail | |
| Client disconnects cleanly without host crash | [x] Pass / [ ] Fail | |
| Host shuts down cleanly returning to single-player | [x] Pass / [ ] Fail | |

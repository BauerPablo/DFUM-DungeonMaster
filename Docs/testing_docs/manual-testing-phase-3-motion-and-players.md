# Manual Testing Guide: Phase 3 Motion Streaming & Player Avatars

This document provides a step-by-step verification protocol for **Phase 3 (Motion Interpolation, Transform Streaming & Remote Player Avatars - Option B)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`Play Mode`).
2. **Clean Compilation Check**:
   - Verify there are **0 compilation errors** in the Unity Console.
   - Verify that all newly created and updated files compile cleanly:
     - [`NativeNetworkTransform.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkTransform.cs)
     - [`NativePlayerPuppet.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativePlayerPuppet.cs)
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`GameObjectHelper.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Utility/GameObjectHelper.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build or second Editor instance.

---

## 2. Test Procedures

### Test Case 1: Player Profile Handshake & Visual Avatar Instantiation
* **Objective**: Confirm that when a Client connects to the Host, both players immediately see each other's visual avatar with their overhead 3D name tag.
* **Steps**:
  1. On **Instance A (Host)**: Start game in Privateer's Hold, open console (`~`), and run:
     ```text
     net_host
     ```
  2. On **Instance B (Client)**: Start game in Privateer's Hold, open console, and run:
     ```text
     net_join 127.0.0.1
     ```
  3. Look at both screens.
* **Expected Result**:
  - **Host Console**:
    ```text
    [DFU Player] Server registered profile for '<ClientName>' (ConnId: ...)
    [DFU Player] Spawning remote player avatar for '<ClientName>' (ConnId: ...)
    ```
  - **Client Console**:
    ```text
    [DFU Player] Spawning remote player avatar for '<HostName>' (ConnId: 0)
    ```
  - Both players see the other player standing in the dungeon.
  - Above each remote player's head, a cyan 3D billboard shows their character's name.

---

### Test Case 2: Smooth Bidirectional Player Motion Streaming
* **Objective**: Confirm that moving either character streams smooth position, orientation, and walking billboard animations at 20 Hz without stuttering.
* **Steps**:
  1. With both Host and Client in line-of-sight:
  2. Walk forward, backwards, and turn in circles with **Client (Instance B)**.
  3. Watch **Host (Instance A)** screen simultaneously.
  4. Walk forward and turn with **Host (Instance A)**.
  5. Watch **Client (Instance B)** screen simultaneously.
* **Expected Result**:
  - The remote puppet glides smoothly across the floor via snapshot interpolation (`Vector3.Lerp` / `Quaternion.Slerp`).
  - When moving, the avatar plays the 8-directional walking sprite animation.
  - When stopping, the avatar seamlessly transitions back to the idle stance.
  - The overhead name tag follows the character smoothly and always faces the local camera.

---

### Test Case 3: Authoritative Monster Motion Streaming (`NativeNetworkTransform`)
* **Objective**: Confirm that authoritative server monsters move and animate smoothly on connected clients.
* **Steps**:
  1. On **Host (Instance A)**, spawn an enemy facing the player using the DM tool:
     ```text
     dm_spawn SkeletalWarrior
     ```
  2. Take a few steps back so the skeleton starts pathfinding and walking towards the Host.
  3. Observe **Client (Instance B)** screen.
* **Expected Result**:
  - The skeleton proxy on the Client moves across the room matching the server monster's path in real time.
  - The proxy sprite displays the walking animation while in motion and idle when stopped.
  - In the Unity Profiler on the Client, the proxy enemy runs **0 autonomous pathfinding or AI loops** (it is purely visual).

---

### Test Case 4: Remote Player Disconnection & Clean Teardown
* **Objective**: Confirm that when a player disconnects, their avatar is cleanly removed without leaving orphaned GameObjects in the scene.
* **Steps**:
  1. On **Client (Instance B)**, open console and run:
     ```text
     net_disconnect
     ```
  2. Observe **Host (Instance A)** console and scene.
* **Expected Result**:
  - **Host Console**:
    ```text
    [DFU Network] Client disconnected from server: connectionId=...
    [DFU Player] Despawning avatar for connection ...
    ```
  - The client's avatar and overhead name tag disappear immediately from the Host's screen.
  - No orphaned GameObjects remain in the scene hierarchy.

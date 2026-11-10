# Manual Testing Guide: Phase 2 Authoritative Spawner Pipeline

This document provides a step-by-step verification protocol for **Phase 2 (Authoritative Spawner Pipeline / The Death of Catchers)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`2019.4.x` / supported DFU version).
2. **Clean Compilation Check**:
   - Check the Unity **Console** tab.
   - Verify there are **zero compilation errors**.
   - Verify that all newly modified files (`RDBLayout.cs`, `GameObjectHelper.cs`, `FoeSpawner.cs`, `DFUNetworkManager.cs`, `DFUNetworkRegistry.cs`, `DFUNetworkCommands.cs`) compile cleanly into `Assembly-CSharp.dll`.
3. **Setup Two Instances**:
   - **Instance A (Host)**: Run inside the Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows player build or second Unity Editor instance.

---

## 2. Test Procedures

### Test Case 1: Offline Baseline (Single-Player Sanity Check)
* **Objective**: Confirm that single-player gameplay remains 100% normal and uninhibited by the networking layer.
* **Steps**:
  1. Open the game in **Play Mode** (offline, do not run `net_host`).
  2. Start a new character or load an existing save inside a dungeon (e.g., Privateer's Hold).
  3. Walk forward and encounter the initial rat / bat / skeletal warrior.
  4. Open the in-game console (`~`) and run:
     ```text
     net_status
     ```
* **Expected Result**:
  - `ShouldHandleSpawns: True`.
  - `Session State: Single Player (Offline)`.
  - Enemies spawn and behave normally with full AI, attacks, and audio.

---

### Test Case 2: Authoritative Host Dungeon Bootstrap & Entity Registration
* **Objective**: Verify that when the Host starts a multiplayer session, all pre-existing and newly spawned dungeon monsters are registered with deterministic GUIDs.
* **Steps**:
  1. In **Instance A (Host)**, inside a dungeon cell with enemies (e.g., Privateer's Hold), open console (`~`) and run:
     ```text
     net_host 7777
     ```
  2. Observe the in-game console and Unity Console logs.
  3. In console, type:
     ```text
     net_status
     net_entities
     ```
* **Expected Result**:
  - Console logs:
    ```text
    [DFU Network] Authoritative Server started.
    [DFU Spawn] Server registered X pre-existing scene entities.
    ```
  - `net_status` displays:
    ```text
    Session State: Host (Server + Client Connected)
    ShouldHandleSpawns: True
    Tracked Entities: X
    ```
  - `net_entities` prints every tracked monster with its GUID, name, and role:
    ```text
    Tracked Entities (X):
     - <guid-1>: DaggerfallEnemy [Rat] [Authoritative]
     - <guid-2>: DaggerfallEnemy [Bat] [Authoritative]
     ...
    ```
  - In Unity Editor Hierarchy, inspect any `DaggerfallEnemy`: it now has a `NativeNetworkEntity` component attached with its matching `entityId` and `isClientProxy = false`.

---

### Test Case 3: Client Connects & Synchronizes 1:1 Entity Proxies
* **Objective**: Verify that a joining client receives authoritative spawn messages for all existing dungeon entities and instantiates visual proxies with zero duplicate local spawns.
* **Steps**:
  1. Ensure **Instance A (Host)** is hosting inside the dungeon from Test Case 2.
  2. Launch **Instance B (Client)** and load/enter the game.
  3. In **Instance B**, open console (`~`) and connect:
     ```text
     net_join 127.0.0.1 7777
     ```
  4. Observe the console on both instances.
  5. In **Instance B**, run:
     ```text
     net_status
     net_entities
     ```
* **Expected Result**:
  - **On Host (Instance A)**:
    - Log: `[DFU Spawn] Synchronized X existing entities to client 1.`
  - **On Client (Instance B)**:
    - Log: `[DFU Spawn] Client received MsgSpawnEnemy: DaggerfallEnemy [...] (GUID: ...)` for each entity.
    - `net_status` shows:
      ```text
      Session State: Connected Client
      ShouldHandleSpawns: False
      Tracked Entities: X
      ```
    - `net_entities` shows the **exact same number of entities and identical GUIDs**, marked with `[Proxy]`:
      ```text
      Tracked Entities (X):
       - <guid-1>: DaggerfallEnemy [Rat] [Proxy]
       - <guid-2>: DaggerfallEnemy [Bat] [Proxy]
       ...
      ```
  - **Zero Duplicate Enemies**: Host and Client have a 1:1 count correspondence.

---

### Test Case 4: Client Gating Verification (Zero Autonomous Spawns)
* **Objective**: Verify that the client's local dungeon generator (`RDBLayout.cs`) and random encounter logic (`GameObjectHelper.cs`, `FoeSpawner.cs`) do NOT spawn autonomous monsters.
* **Steps**:
  1. While connected as Client in **Instance B**, move into an unexplored room or dungeon block.
  2. Pause the game or inspect the hierarchy on Instance B.
  3. Verify whether any local `DaggerfallEnemy` exists that was NOT created by the server.
* **Expected Result**:
  - All enemies present on Client have `NativeNetworkEntity` with `isClientProxy == true`.
  - There are **zero** unmanaged or autonomous local monsters (`ShouldHandleSpawns == false` completely bypasses `AddFixedEnemies`, `AddRandomEnemies`, and `CreateFoeSpawner`).
  - No autonomous monsters fight the player locally without server authority.

---

### Test Case 5: Client Proxy AI Stripping (Puppet Representation)
* **Objective**: Verify that the client-side entity representations are passive puppets with disabled local AI and kinematic physics.
* **Steps**:
  1. On **Instance B (Client)**, select any spawned proxy enemy in the Unity Hierarchy (or inspect via debug tools).
  2. Check its attached components:
     - `EnemyMotor`: **Disabled** (`enabled = false`)
     - `EnemySenses`: **Disabled** (`enabled = false`)
     - `EnemyAttack`: **Disabled** (`enabled = false`)
     - `CharacterController`: **Disabled** (`enabled = false`)
     - `Rigidbody`: **isKinematic = true**, **detectCollisions = false**
     - `NativeNetworkEntity`: **isClientProxy = true**
* **Expected Result**:
  - The client proxy does not autonomously pathfind, attack, or execute local physics collisions.
  - The visual representation (`MobileUnit`, billboard mesh, sprite textures) is rendered correctly in the scene.

---

### Test Case 6: Dynamic Server Spawning & Real-time Broadcast
* **Objective**: Verify that when the server spawns an enemy dynamically (via spawner, rest interruption, or console), it is immediately broadcast to connected clients in real time.
* **Steps**:
  1. With both Host and Client connected, trigger an enemy spawn on **Host (Instance A)**:
     - Rest in a dangerous dungeon area, or trigger a dungeon encounter.
  2. Observe console logs on both Host and Client.
* **Expected Result**:
  - **Host logs**:
    ```text
    [DFU Spawn] Server broadcasting spawn for DaggerfallEnemy [...] (GUID: <guid>)
    ```
  - **Client logs**:
    ```text
    [DFU Spawn] Client received MsgSpawnEnemy: DaggerfallEnemy [...] (GUID: <guid>)
    ```
  - The new enemy appears immediately on the Client's screen at the exact coordinates.
  - Running `net_entities` on both displays the new entity in both registries.

---

### Test Case 7: Authoritative Despawn & Entity Cleanup
* **Objective**: Verify that when an authoritative entity on the server is destroyed or despawned, clients remove their local proxies without orphaned GameObjects.
* **Steps**:
  1. On **Host (Instance A)**, destroy or kill an enemy.
  2. Check console on **Instance B (Client)**.
  3. Run `net_entities` on both instances.
* **Expected Result**:
  - Host broadcasts `MsgDespawnEnemy` on entity destruction.
  - Client logs:
    ```text
    [DFU Spawn] Client despawning proxy (GUID: <guid>)
    ```
  - The enemy GameObject disappears on Client.
  - `net_entities` confirms the entity is removed from `DFUNetworkRegistry` on both instances.

---

### Test Case 8: Client Clean Disconnection & Proxy Teardown
* **Objective**: Verify that disconnecting the client cleans up all proxy entities from the scene and memory.
* **Steps**:
  1. On **Instance B (Client)**, open console (`~`) and run:
     ```text
     net_disconnect
     ```
  2. Check hierarchy and console on Instance B.
* **Expected Result**:
  - Client destroys all active proxy enemies (`DestroyClientProxies()`).
  - `DFUNetworkRegistry.Count` resets to 0 on Client.
  - No leftover phantom enemies remain in the scene.
  - Zero memory leaks or null reference exceptions.

---

## 3. Summary Checklist

| # | Test Case | Success Criteria | Status |
|---|---|---|---|
| 1 | Offline Single Player | Normal gameplay, `ShouldHandleSpawns: True` | [x] |
| 2 | Host Dungeon Bootstrap | `net_entities` lists all enemies as `[Authoritative]` | [x] |
| 3 | Client 1:1 Sync | Exact 1:1 match of GUIDs and count on Client | [x] |
| 4 | Client Spawner Gating | 0 autonomous enemies spawned on pure Client | [x] |
| 5 | Puppet Component Stripping | `EnemyMotor`, `EnemySenses`, `EnemyAttack` disabled | [x] |
| 6 | Dynamic Server Spawn | Real-time broadcast and instant proxy creation | [x] |
| 7 | Authoritative Despawn | Proxy destroyed when server entity destroyed | [x] |
| 8 | Client Disconnection | All proxies destroyed cleanly, 0 leaks | [x] |

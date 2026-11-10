# Manual Testing Guide: Phase 4 Multi-Target Server AI

This document provides a step-by-step verification protocol for **Phase 4 (Multi-Target Server AI & Aggro System)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`Play Mode`).
2. **Clean Compilation Check**:
   - Verify there are **0 compilation errors** in the Unity Console.
   - Verify that all newly updated files compile cleanly:
     - [`EnemySenses.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/EnemySenses.cs)
     - [`EnemyMotor.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/EnemyMotor.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
     - [`GameObjectHelper.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Utility/GameObjectHelper.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Enemy Target Acquisition on Client (Nearest Player)
* **Objective**: Confirm that an authoritative enemy detects and chases the Client player when the Client is closer than the Host.
* **Steps**:
  1. On **Instance A (Host)**: Start session with `net_host`.
  2. On **Instance B (Client)**: Connect with `net_join 127.0.0.1`.
  3. On **Host**: Stand still or back up a few meters.
  4. On **Client**: Walk forward towards the middle of the room.
  5. On **Host**: Press `F11` (or use console `spawn_enemy rat` / `spawn_enemy skeleton`).
* **Expected Result**:
  - The spawned enemy evaluates both players.
  - Because Client is closer and in direct line-of-sight, the enemy turns toward Client (`RemotePlayer`) and walks towards Client.
  - On both screens, the enemy smoothly glides towards Client's position.

---

### Test Case 2: Target Switching with Hysteresis
* **Objective**: Confirm that the enemy dynamically switches target when another player moves significantly closer, without erratic flickering.
* **Steps**:
  1. While the enemy is pursuing the Client (Instance B):
  2. With **Host (Instance A)**, run towards the enemy until Host is closer to the enemy than the Client by more than 3 meters.
* **Expected Result**:
  - Once the Host crosses the hysteresis priority threshold, the enemy recalculates target priority, turns around, and begins pursuing the Host.
  - Movement is smooth with no rapid jittering between targets.

---

## 3. Success Criteria for Phase 4

- [x] Enemies can acquire either Host or Client as combat targets.
- [x] Nearest visible player receives target priority based on DFU scoring formula.
- [x] Hysteresis prevents target flickering when two players stand nearby.
- [x] Single-player mode continues to function with 100% fidelity.

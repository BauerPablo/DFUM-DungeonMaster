# Manual Testing Guide: Phase 5 Authoritative Combat & Hit Resolution

This document provides a step-by-step verification protocol for **Phase 5 (Authoritative Combat & Hit Resolution)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`Play Mode`).
2. **Clean Compilation Check**:
   - Verify there are **0 compilation errors** in the Unity Console.
   - Verify that all newly updated files compile cleanly:
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`GameObjectHelper.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Utility/GameObjectHelper.cs)
     - [`WeaponManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/WeaponManager.cs)
     - [`EnemyDeath.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/EnemyDeath.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Client Weapon Hit & Server Damage Resolution
* **Objective**: Confirm that a Client swinging a weapon hits an enemy proxy, routes damage to the server authoritatively, and displays blood/audio feedback.
* **Steps**:
  1. On **Instance A (Host)**: Start session with `net_host`.
  2. On **Instance B (Client)**: Connect with `net_join 127.0.0.1`.
  3. On **Host**: Spawn a test enemy (e.g. `spawn_enemy rat` or `spawn_enemy skeleton` via console or `F11` DM menu).
  4. On **Host**: In the Unity Inspector, select the spawned enemy's `DaggerfallEntityBehaviour` to monitor `Entity.CurrentHealth`.
  5. On **Client**: Equip a weapon (or fists) and strike the enemy.
* **Expected Result**:
  - The Client's weapon collider registers the hit on the client enemy proxy.
  - Client plays local swing/hit feedback.
  - Host console logs: `[DFU Combat] Client <connId> damaged entity <entityId> for <dmg> HP (Remaining: <hp>)`.
  - The enemy takes damage on the server and displays blood splash and grunt/pain sounds on both Host and Client.

---

### Test Case 2: Threat / Aggro Switching on Attack
* **Objective**: Confirm that when an enemy is pursuing the Host, an attack from the Client causes the enemy to immediately turn and target the Client.
* **Steps**:
  1. While an enemy is targeting and pursuing the Host (Instance A):
  2. Have the Client (Instance B) approach from behind or from the side and strike the enemy.
* **Expected Result**:
  - The enemy's aggro switches authoritatively to the Client puppet.
  - The enemy turns around and starts moving towards the Client.

---

### Test Case 3: Synchronized Enemy Death & Corpse Transition (The Frozen Monster Fix)
* **Objective**: Confirm that when an enemy's health reaches zero (killed by either Host or Client), the enemy collapses, plays the body fall sound, transitions to a corpse billboard marker, and does NOT freeze in place on the Client.
* **Steps**:
  1. Strike the enemy until its health reaches zero (test both killing from Host and killing from Client).
* **Expected Result**:
  - Host: The enemy deactivates, plays `BodyFall` sound, and leaves a lootable corpse marker on the ground.
  - Client: `MsgEntityDeath` is received. The proxy enemy is deactivated (does not freeze standing up!), the `BodyFall` audio clip plays at the enemy's position, and a matching corpse billboard marker is spawned on the floor.
  - Host logs: Clean unregistration of the dead entity ID.

---

## 3. Success Criteria for Phase 5

- [x] Client weapon strikes register collisions on client enemy proxies.
- [x] Client sends attack intents to the server without client-authoritative health hacking.
- [x] Server validates attack reach (<= 6m), metal effectiveness, bounds, and decrements health authoritatively.
- [x] Attacking an enemy raises aggro and switches target to the attacker.
- [x] Authoritative hit effects (blood splash and sounds) are synchronized across peers.
- [x] Enemies slain by either player collapse with death sounds and transition cleanly into corpses on all connected screens (resolving the previous frozen enemy bug).

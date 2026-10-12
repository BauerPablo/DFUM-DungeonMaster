# Manual Testing Guide: Authoritative Entity Management (Multiplayer)

This manual testing guide outlines the steps to verify the new server-authoritative entity management and client proxy system in the `TanguyMultiplayer` mod for Daggerfall Unity.

---

## Prerequisites

1. **Build / Unity Editor Environment**: Open the project in Unity (supporting Daggerfall Unity and Mirror).
2. **Instances**: You will need at least **two instances** running to test multiplayer replication:
   - **Host Instance**: Acts as both Server and Client (Host).
   - **Client Instance**: Connected to the Host via Mirror network.
3. **Scene**: Load into `DaggerfallUnityGame.unity` scene where enemies spawn (e.g., exterior wilderness or a dungeon cell).

---

## Test Cases

### Test Case 1: Server-Side Spawn Authority & Zero Client Duplication
* **Objective**: Verify that enemies are spawned exclusively by the server and replicated to clients without local client-side duplication or ghost entities.
* **Steps**:
  1. Start the game as **Host**.
  2. Start the second instance as a **Client** and connect to the Host.
  3. Navigate both players to an area with known enemy spawns (e.g., dungeons or exterior random encounters).
* **Expected Result**:
  - Enemies spawn only when triggered or scanned by the server's authoritative routine (`EntityCatcher`).
  - On the Client, `EntitySyncMarker` attaches to the instantiated proxy enemy.
  - No duplicate enemies exist at the same coordinates on the client and server.

### Test Case 2: Client Proxy Mode & AI Deactivation
* **Objective**: Verify that remote clients treat enemies as motion-interpolated dummies without running local AI (`EnemyMotor`) or physics conflicts.
* **Steps**:
  1. With Host and Client connected and an enemy visible to both:
  2. Observe the enemy's behavior on the **Client** screen.
* **Expected Result**:
  - The enemy on the client does **not** execute independent pathfinding or attack decisions (its `EnemyMotor` and character controller are disabled).
  - The enemy remains stationary or smoothly follows server movement updates without erratic local AI jitter.

### Test Case 3: Smooth Movement Interpolation
* **Objective**: Verify that enemy movement sent via `MsgEntityMove` interpolates smoothly on client proxies without snapping or rubberbanding.
* **Steps**:
  1. Move the Host player around an enemy or observe an enemy moving under server control.
  2. Watch the enemy's movement on the **Client** screen.
* **Expected Result**:
  - The enemy updates position smoothly every frame via `EntitySyncMarker.Update()` linear/spherical interpolation (`Vector3.Lerp` / `Quaternion.Slerp`).
  - No choppy frame-rate-dependent snaps or sudden teleports.

### Test Case 4: Server-Validated Combat & Health Sync
* **Objective**: Verify that attacking an enemy correctly routes damage through the server (`CmdAttackEntity`) and synchronizes death across all clients (`MsgEntityDeath`).
* **Steps**:
  1. As a **Client**, attack a spawned enemy.
  2. Observe the enemy's health reduction and death on both Host and Client screens.
* **Expected Result**:
  - Damage is registered on the server first via `CmdAttackEntity`.
  - When health drops to 0, `MsgEntityDeath` is broadcast to all clients, unregistering the entity from `EntityRegistry` and triggering the death state simultaneously across all connected players.

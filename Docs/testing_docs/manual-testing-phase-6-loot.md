# Manual Testing Guide: Phase 6 Synchronized World State, Containers & Loot

This document provides a step-by-step verification protocol for **Phase 6 (Synchronized World State, Containers & Loot)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`Play Mode`).
2. **Clean Compilation Check**:
   - Verify there are **0 compilation errors** in the Unity Console.
   - Verify that all newly updated files compile cleanly:
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`DFUNetworkLootRegistry.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkLootRegistry.cs)
     - [`DaggerfallLoot.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Internal/DaggerfallLoot.cs)
     - [`EnemyDeath.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/EnemyDeath.cs)
     - [`DaggerfallInventoryWindow.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/UserInterfaceWindows/DaggerfallInventoryWindow.cs)
     - [`PlayerActivate.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/PlayerActivate.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Corpse Loot Parity upon Enemy Death
* **Objective**: Confirm that an enemy slain by either Host or Client generates identical loot on both machines, eliminating the "the body has no treasure" error on the Client.
* **Steps**:
  1. On **Instance A (Host)**: Start session with `net_host`.
  2. On **Instance B (Client)**: Connect with `net_join 127.0.0.1`.
  3. Slay an enemy (e.g. humanoid enemy or monster carrying items).
  4. Both Host and Client walk up to the corpse billboard on the ground.
  5. On **Client**: Click the corpse to open the loot window.
* **Expected Result**:
  - The Client's inventory window opens with `LootTarget` displaying the enemy's authoritative items (gold pieces, weapons, ingredients, etc.).
  - The Client does not see "the body has no treasure" unless the enemy rolled zero loot on the server.

---

### Test Case 2: Real-Time Loot Window Synchronization & Anti-Duplication
* **Objective**: Confirm that when two players simultaneously inspect the same corpse or dungeon chest, taking an item on one machine immediately removes it from the other machine's open window.
* **Steps**:
  1. Have both **Host** and **Client** open the inventory window on the exact same corpse or chest marker.
  2. Verify that both windows display identical contents.
  3. On **Client**: Drag or click an item into the player inventory.
* **Expected Result**:
  - The item moves into Client's inventory.
  - Client sends `MsgLootTakeItem` to Server.
  - Server removes the item from the authoritative container and broadcasts `MsgLootSync`.
  - On **Host**: The open inventory window refreshes automatically; the taken item instantly disappears from Host's loot pane.
  - Neither player can duplicate the item.

---

### Test Case 3: Host Loot Transfer Synchronized to Client
* **Objective**: Confirm that when the Host takes an item from a shared container, the Client's open window updates in real-time.
* **Steps**:
  1. Both players open the same corpse/chest with multiple items remaining.
  2. On **Host**: Take an item from the container.
* **Expected Result**:
  - Host's transfer triggers an authoritative `MsgLootSync` broadcast.
  - On **Client**: The taken item instantly vanishes from the open loot window.

---

## 3. Success Criteria for Phase 6

- [x] Every loot container (corpse marker, chest, dropped loot) possesses a deterministic, synchronized `LoadID`.
- [x] Slain enemy corpses carry authoritative items serialized via native `SaveLoadManager` directly in `MsgEntityDeath`.
- [x] `DFUNetworkLootRegistry` tracks all active world containers automatically.
- [x] Taking an item broadcasts real-time updates and refreshes open inventory windows across connected peers.
- [x] Item duplication exploits are prevented by server-authoritative container ownership.

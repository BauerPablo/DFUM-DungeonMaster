# Manual Testing Guide: Phase 7 Remote Player Representation & World Sync

This document provides a comprehensive step-by-step verification protocol for **Phase 7 (Remote Player Representation & World Sync)** of the Daggerfall Unity Native Multiplayer architecture.

---

## 1. Prerequisites & Compilation Check

1. **Unity Editor**: Open the project in Unity (`Play Mode`).
2. **Clean Compilation Check**:
   - Verify there are **0 compilation errors** in the Unity Console.
   - Verify that all newly updated Phase 7 files compile cleanly:
     - [`NativeNetworkMessages.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativeNetworkMessages.cs)
     - [`NativePlayerPuppet.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/NativePlayerPuppet.cs)
     - [`DFUNetworkManager.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkManager.cs)
     - [`DFUNetworkCommands.cs`](file:///D:/11%20Workspace/17%20DFU-Multiplayer/Assets/Scripts/Game/Networking/DFUNetworkCommands.cs)
3. **Setup Two Instances**:
   - **Instance A (Host)**: Unity Editor (`Play Mode`).
   - **Instance B (Client)**: Standalone Windows Player build.

---

## 2. Test Procedures

### Test Case 1: Weapon Swing & Attack Animation Replication
* **Objective**: Confirm that when a player swings a weapon or shoots a bow, the other player immediately sees the attack animation and hears the hit/swing audio.
* **Steps**:
  1. On **Instance A (Host)**: Start session with `net_host` (or press `F10` -> Start Host).
  2. On **Instance B (Client)**: Connect with `net_join 127.0.0.1` (or press `F10` -> Connect Client).
  3. Walk both characters until they are facing each other.
  4. On **Host**: Equip a melee weapon (sword, axe, dagger) and swing.
  5. Check **Client** screen.
  6. On **Client**: Equip a melee weapon and swing.
  7. Check **Host** screen.
* **Expected Result**:
  - The observing player sees the remote puppet billboard execute `MobileStates.PrimaryAttack`.
  - The observing player hears the directional weapon swing sound (`SoundClips.SwingMediumPitch`) played at the puppet's position.
  - When the swing animation completes, the puppet returns to `Idle` or `Move`.

---

### Test Case 2: Bow & Projectile Attack Replication (Optional)
* **Objective**: Confirm that bow attacks trigger ranged animation and audio.
* **Steps**:
  1. Equip a bow and arrows on one of the players.
  2. Aim and release an arrow.
* **Expected Result**:
  - The other player sees the puppet play `MobileStates.RangedAttack1` and hears the bow release sound (`SoundClips.ArrowShoot`).

---

### Test Case 3: Smooth Idle <-> Move Transitions & Billboards Stability
* **Objective**: Confirm that moving and stopping immediately toggles between `Move` and `Idle` billboard animations without sprite scaling jitter or clipping.
* **Steps**:
  1. Move the player forward, backward, and strafe.
  2. Release movement keys and stand completely still.
* **Expected Result**:
  - While walking, the puppet plays its directional walk loop (`MobileStates.Move`).
  - When stopping, the puppet promptly reverts to resting stance (`MobileStates.Idle`).
  - Puppet sprite remains stable at its native billboard scale without flickering or popping between scales.

---

### Test Case 4: Crouching State & Custom Billboard Sprite Alignment
* **Objective**: Verify bidirectional crouching replication, overhead name tag positioning, and ground-aligned custom crouch sprite rendering.
* **Steps**:
  1. Have both characters facing each other.
  2. On **Host**: Press crouch (default: `C`).
  3. On **Client**: Observe the Host puppet.
  4. On **Host**: Walk behind the Client and crouch again.
  5. Repeat steps 2-4 with **Client** crouching and **Host** observing.
* **Expected Result**:
  - The observing screen switches from the default standing sprite to the custom crouching sprite (`TEXTURE.488`, `25-0` facing front, `25-1` facing back).
  - The sprite is properly grounded (feet touching floor at `Y = 0`, without floating or sinking into terrain/floor).
  - Overhead 3D name tag drops to crouched height (`1.45m`) and `CapsuleCollider` adjusts to `1.1m`.
  - Releasing crouch instantly restores normal standing sprite and dimensions.

---

### Test Case 5: Building Interiors & Dungeon Cell Flow
* **Objective**: Confirm that players can enter and exit buildings independently, see each other moving inside shared interiors, and explore dungeons together without mesh clipping or being stuck in walls.
* **Steps**:
  1. **Town Building Interior**:
     - Walk together outside a shop, tavern, or residence in a town.
     - Have Player A click the door to enter the building.
     - Observe Player B outside: Player A enters and disappears cleanly from the street (no phantom puppet in the exterior).
     - Have Player B click the same door to enter.
     - Inside the building: Both players should now see each other's puppet inside the room.
     - Have both players walk around the room: Verify that motion, rotation, and idle/move animations replicate smoothly.
     - Have Player A exit the building through the door:
       - Player A lands cleanly outside in the street without clipping into building meshes.
       - Player B remains inside browsing or resting without being kicked out.
     - Have Player B exit through the door: Player B lands cleanly outside on the street next to Player A.
  2. **Dungeon Entrance & Exploration**:
     - Walk together to a dungeon entrance (e.g. Privateer's Hold exterior entrance mound).
     - Enter the dungeon.
     - Verify both players land at the dungeon starting marker and can explore corridors together.
     - Click the dungeon exit door to return to the exterior.
* **Expected Result**:
  - Building interiors share authentic world positions and exterior door mappings (`TransitionToBuildingByKey`), preventing mesh clipping or backface transparency bugs.
  - Remote puppets automatically show or hide according to cell context (`SetCellVisibility`), keeping cell boundaries clean.
  - Exiting doors places players safely on the street/mound surface outside.

---

### Test Case 6: DM "Gather Party" Command & Late-Join Cell Sync
* **Objective**: Confirm that the Host can gather separated party members directly to the Host's position and cell context across Exterior, Buildings, and Dungeons.
* **Steps**:
  1. Separate the two players (e.g. Host inside a building or far away on the street, Client in the exterior or elsewhere).
  2. On **Host**: Open the DM GUI (`F11`) and click **"Gather Party to Host"** (or run `gather_party` in the console).
  3. Observe the Client screen.
* **Expected Result**:
  - The Client's local character controller is temporarily unlocked and teleported right beside the Host (with a small lateral offset to prevent collider overlaps).
  - If the Host was inside a building or dungeon, the Client automatically transitions into that exact same cell instance before teleporting.
  - Late-joining clients connecting while Host is inside a dungeon or building automatically synchronize into the Host's active cell on join.

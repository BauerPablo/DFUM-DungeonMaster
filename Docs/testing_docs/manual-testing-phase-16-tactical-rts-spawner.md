# Manual Testing Guide: Tactical RTS Spawner & Ghost Placement Preview (Phase 16)

## Overview
This testing guide covers all features introduced in **Phase 16 (v0.9.16)** of the DFUM TTRPG Master Suite:
1. **Raycast Surface Targeting & Height Alignment:**
   - Mouse-driven raycast hitting surfaces (terrain, dungeon stone floors, stairs, tables, rooftops) up to 150m.
   - Automatic surface normal and height offset to avoid clipping or floating.
2. **Local Ghost Puppet Preview (`DMRtsPlacementController`):**
   - Purely client-side preview object that follows the mouse cursor in real-time.
   - Zero network transmission or packet replication while previewing (100% invisible to remote clients).
3. **Dynamic Facing Rotation (Mouse Wheel):**
   - Rotating the mouse scroll wheel spins the entity's facing orientation in smooth increments prior to placement.
4. **Collision & Slope Validation (Green / Red Tint):**
   - Real-time `Physics.OverlapCapsule` bounds test.
   - **Green Tint (Valid):** Clear ground, no overlapping colliders, walkable slope angle.
   - **Red Tint (Invalid):** Overlapping an existing entity, wall collision, or excessive/unwalkable incline.
5. **Authoritative Spawn & Clean Cancellation:**
   - **Left Click:** Confirms placement, cleans up local ghost, and dispatches authoritative spawn on Host/Server (`MsgSpawnEnemy` or `MsgSpawnLoot`), replicating to all clients.
   - **Right Click / Esc:** Cancels RTS placement mode cleanly with zero network impact.
6. **Multi-Perspective Compatibility:**
   - Works seamlessly in First-Person Avatar view, Spectator FreeCam flight, and Cenital/Top-Down tactical camera.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **PASSED & APPROVED (Phase 16)** | **7** | Cases 1–7 | **100% Passed (Verified in Multiplayer Session)** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [x] Case 1: Enter RTS Placement Mode & Ghost Preview Instantiation
- [x] Launch game and host a multiplayer session as Dungeon Master.
- [x] Open Master Suite (`Ctrl + F11`) -> `[ Spawns & NPCs ]`.
- [x] Select any creature (e.g. *SkeletalWarrior*, *Daedroth*, or *Orc*).
- [x] Click **`✦ Colocación Táctica (Modo RTS Ghost) ✦`** (or press shortcut `Ctrl + R`).
- [x] Verify:
  - Master Suite modal minimizes or closes temporarily to give full view of the world.
  - A semitransparent Ghost Puppet matching the creature appears at the cursor location.
  - HUD displays: `"[RTS Spawner] MODO COLOCACIÓN ACTIVO: Rueda=Rotar | Click Izq=Colocar | Click Der/Esc=Cancelar"`.
  - On a connected client, verify NO entity or preview appears (0 packets sent).

---

### [x] Case 2: Surface Raycast & Ground Alignment
- [x] Move mouse cursor across varied terrain:
  - [x] Flat dirt/grass terrain in exterior.
  - [x] Dungeon flagstone floors and ramps.
  - [x] Stairs and elevated platforms.
  - [x] Rooftops in a settlement (e.g. Daggerfall City).
- [x] Verify:
  - Ghost puppet smoothly slides along the hit point without stutter.
  - Upright alignment remains perpendicular to gravity (does not tilt awkwardly sideways on slopes).
  - Entity base does not sink below ground geometry.

---

### [x] Case 3: Entity Rotation via Mouse Scroll Wheel
- [x] While ghost puppet is hovering over a valid surface:
  - [x] Scroll mouse wheel **UP**: verify the creature rotates clockwise.
  - [x] Scroll mouse wheel **DOWN**: verify the creature rotates counter-clockwise.
  - [x] Verify rotation indicator or facing direction arrow is clearly visible.

---

### [x] Case 4: Real-Time Collision & Placement Validation (Green / Red)
- [x] **Valid Placement (Green):**
  - Hover over an open, unobstructed floor.
  - Verify ghost puppet / placement ring renders in **GREEN** (`Valid`).
- [x] **Invalid Placement (Red):**
  - Hover directly over another NPC, player puppet, wall, or tight obstacle.
  - Verify ghost puppet / placement ring immediately tints **RED** (`Invalid: Obstruction Detected`).
  - Attempt to Left Click while Red: verify placement is blocked and HUD warns: `"[RTS Spawner] Ubicación obstruida. Busca un espacio despejado."`.

---

### [x] Case 5: Authoritative Spawn Confirmation & Network Replication
- [x] Position ghost puppet on a valid (Green) spot, set a specific rotation, and **Left Click**.
- [x] Verify:
  - Local ghost puppet disappears cleanly.
  - The actual creature spawns instantly at that exact coordinate facing the specified rotation.
  - Autonomous AI initializes properly (or obeys DM behavior state).
  - HUD confirms: `"[RTS Spawner] Invocado {Creature} en posición ({X}, {Y}, {Z})."`.
- [x] **Network Check:**
  - On a connected client, confirm the creature appears in real time at that exact position and facing direction.

---

### [x] Case 6: Clean Cancellation (`Right Click` / `Esc`)
- [x] Enter RTS Placement mode again.
- [x] Move ghost puppet around.
- [x] Press **`Right Click`** or **`Esc`**.
- [x] Verify:
  - Ghost puppet is immediately destroyed from scene.
  - No enemy is spawned.
  - Camera control and normal cursor mode restore cleanly without exceptions or memory leaks.

---

### [x] Case 7: RTS Placement from FreeCam & Tactical Cenital View
- [x] Enter **Spectator Spirit Mode** (`Ctrl + F8`) or activate **FreeCam**.
- [x] Fly high above the battlefield or toggle **Cenital / Top-Down View** (`F9`).
- [x] Press `Ctrl + R` (or select from Master Suite) to enter RTS Placement mode.
- [x] Verify:
  - Raycast works accurately from high altitudes (up to 150m).
  - You can place guards or ambushing monsters strategically from a commander perspective.
  - Left Click confirms and places the creatures into the dungeon/exterior scene.

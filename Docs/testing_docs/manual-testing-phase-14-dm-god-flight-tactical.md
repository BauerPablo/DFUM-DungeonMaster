# Manual Testing Guide: DM Dual Entity, God Flight & Tactical View (Phase 14)

## Overview
This testing guide covers all features introduced in **Phase 14 (v0.9.14)**:
1. **Pillar 1: DM Dual Entity (Spectator Spirit vs Physical Admin Avatar):**
   - **Spectator Spirit Mode (Zeus / Espíritu):** Intangible 6-DOF flight, complete invisibility to players, no mob aggro (`NoTargetMode`), vitals/fatigue/hunger frozen, no fall/environmental damage.
   - **Physical Admin Avatar Mode:** Physical presence in the 3D world with collider restored, prestigious overhead DM billboard nametag (`[DUNGEON MASTER]` in gold/purple), direct local chat speaker, god mode toggle.
   - **Instant 1-Key Hotkey Toggle:** Seamlessly switch between Spectator Spirit and Physical Avatar with `Ctrl + F8` (or dedicated button in Master Suite) with clear HUD status feedback.
2. **Pillar 2: God Flight & Tactical Cenital View:**
   - **6-DOF Noclip Flight:** Smooth WASD + Space/C flying with mouse look and mouse-scroll wheel speed scaling (2 to 150 m/s).
   - **Cenital / Top-Down Tactical View:** Overhead camera lock (top-down view) with smooth scroll zoom to inspect encounters, town squares, and dungeon rooms.
   - **Click-to-Teleport:** Shift + Left Click on any ground surface instantly teleports the DM or avatar to the targeted position.
3. **Pillar 3: Parametric Nametags & Floating Health Bars:**
   - Color-coded overhead billboard nametags: Gold/Purple for DM, Cyan for party members, Red for hostile foes, Green for neutral NPCs.
   - Distance falloff setting and optional health indicator.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **VERIFIED & READY (Pillars 1, 2, 3)** | **7** | Cases 1–4 Verified, 5–7 Ready to Retest | **Pillars 1, 2 & 3 Implemented** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [x] Case 1: Instant 1-Key DM Dual Entity Toggle (`Ctrl + F8` / Master Suite) - PASSED
- [x] Launch Daggerfall Unity and start or host a multiplayer session as Dungeon Master (Host or DM role).
- [x] In-game, press **`Ctrl + F8`** (or open Master Suite with `Ctrl + F11` -> `[ Mundo & Clima ]` / `[ Party & DM ]` and click the Dual Mode toggle button).
- [x] Verify HUD displays:
  - Entering Spectator: `"[DM DUAL MODE] Spectator Spirit (Zeus) ACTIVE - Invisible, Noclip Flight, No Aggro"`.
  - Exiting Spectator: `"[DM DUAL MODE] Physical Admin Avatar ACTIVE - Visible, Physical Collider Restored"`.
- [x] Verify smooth transition without crashes, console exceptions, or mouse cursor locks.

---

### [x] Case 2: Spectator Spirit Intangibility, Invisibility & Aggro Immunity - PASSED
- [x] While in **Spectator Spirit Mode**:
  - [x] **Flight & Intangibility:** Fly freely in 6 degrees of freedom through walls, doors, ceilings, and terrain using WASD, Space (ascend), and C (descend).
  - [x] **No Mob Aggro:** Fly close to hostile enemies (e.g., skeletons, guards, rats). Verify enemies do NOT detect, look at, or attack the DM (`NoTargetMode` active).
  - [x] **Vitals & Damage Immunity:** Confirm character takes no fall damage, fatigue does not deplete while flying/sprinting, and health remains untouched.
  - [x] **Network Invisibility:** On a connected client instance, look at the position where the DM is flying. Verify the DM's player puppet is completely invisible and no footsteps/sounds are generated.

---

### [x] Case 3: Physical Admin Avatar Manifestation & Golden Nametag - PASSED
- [x] Fly to a chosen location in Spectator Spirit Mode and press **`Ctrl + F8`** to manifest as **Physical Admin Avatar**:
  - [x] Verify the DM avatar materializes at the targeted ground location safely without falling into the void.
  - [x] Verify the character controller and physical collider are restored (avatar stands on ground, walks normally).
  - [x] On a connected client instance:
    - [x] Verify the DM puppet appears immediately in the 3D world.
    - [x] Verify the overhead 3D nametag displays `[DUNGEON MASTER]` in glowing gold/purple text above the character name.
  - [x] Open in-game chat or Master Suite and verify DM can interact with the party normally.

---

### [x] Case 4: 6-DOF God Flight Controls & Mouse Wheel Speed Scaling (Pillar 2) - PASSED
- [x] Enter flight mode.
- [x] Test standard movement controls:
  - `W / S`: Move camera forward / backward.
  - `A / D`: Strafe camera left / right.
  - `Space`: Ascend vertically.
  - `C`: Descend vertically.
  - `Shift`: Sprint boost (3.5x multiplier).
- [x] Scroll the mouse wheel up and down:
  - Verify flight speed scales smoothly between 2 m/s and 150 m/s.
  - Verify HUD displays real-time speed notification (e.g. `[FreeCam] Speed: 35 m/s`).

---

### [ ] Case 5: Tactical Cenital / Top-Down View with Orthogonal Zoom (Pillar 2)
- [ ] In flight mode, press **`B`** (or click `[Vista Cenital / Top-Down]` in Master Suite `Ctrl+F11` -> `[ Party & DM ]`).
- [ ] Verify camera smoothly rotates to look directly down (89.9° pitch) over the current area.
- [ ] Mouse cursor becomes visible and unlocked for RTS-style strategic panning/targeting.
- [ ] Verify scrolling the mouse wheel zooms the tactical camera altitude in and out smoothly (not flight speed).
- [ ] Use `WASD` to pan across the ground plane (North/South, East/West).
- [ ] Press **`B`** again to return to 6-DOF FreeCam flight mode.

---

### [ ] Case 6: Shift + Left Click Click-to-Teleport (Pillar 2)
- [ ] While in Cenital or FreeCam perspective, hold **`Shift`** and **Left Click** on any visible terrain, rooftop, or floor.
- [ ] Verify the DM (and local avatar) is instantly relocated to the clicked point with terrain elevation safeguard.
- [ ] *(Optional)* While in Physical Avatar walking mode, hold **`Ctrl + Shift`** and **Left Click** to instantly blink to the targeted surface.
- [ ] Confirm no clipping through terrain or falling into subterranean voids.

---

### [ ] Case 7: Parametric Overhead Nametags & Distance Fadeout (Pillar 3)
- [ ] Connect multiple players to the session (DM and Party Members).
- [ ] Verify overhead nametag color coding:
  - **Dungeon Master:** Golden / Purple (`[DUNGEON MASTER]`). **Always 100% visible across the entire scene, completely exempt from distance fadeout.**
  - **Party Members:** Bright Cyan with Player Name. Fades out smoothly with distance.
- [ ] In Master Suite (`Ctrl + F11` -> `[ Party & DM ]`), adjust the **Nametag Fadeout Distance** slider (5m to 100m).
- [ ] Verify player nametags fade out smoothly when moving beyond the configured distance threshold, while the DM nametag remains fully visible at all times.

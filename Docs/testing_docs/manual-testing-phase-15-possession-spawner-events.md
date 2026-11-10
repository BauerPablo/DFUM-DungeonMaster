# Manual Testing Guide: NPC Possession, Expanded Bestiary & World Events (Phase 15)

## Overview
This testing guide covers all features introduced in **Phase 15 (v0.9.15)** of the DFUM TTRPG Master Suite:
1. **Pillar 1: DM NPC & Monster Possession (`DMNpcController`):**
   - Direct possession of any living enemy or town civilian.
   - 3rd-person chase and 1st-person eye view toggle (`Tab`).
   - WASD movement, Sprint, Space jump, Left-click melee attacks.
   - Clean release (`Esc` or `F8`) with zero desync, restoring DM camera and state.
2. **Pillar 2: Complete Bestiary & Hierarchical Spawner:**
   - Categorized catalog: *No-Muertos*, *Monstruos*, *Daedra*, *Humanoides*, *Fauna*.
   - Real-time instant search filter across all Daggerfall `MobileTypes`.
   - Level & HP scaling override (1.0x to 5.0x multiplier).
   - Spawn in front of DM or spawn directly on targeted terrain/floor under crosshair.
3. **Pillar 3: Loot Forge:**
   - Craft custom weapons, armors, and gems by selecting type and material (Iron to Daedric).
   - Deposit directly into chosen player's inventory or drop on ground (network synchronized).
4. **Pillar 4: World Events Engine:**
   - Earthquake cataclysm (`Screen-Shake` effect + low subterranean rumble broadcasted to all players).
   - Real-time ambient audio triggers: Thunder, Monster Roar, Distant Howl, Scream, War Drums, Ghost Wail.
5. **Pillar 5: DM Economy & Rule Manipulation:**
   - Mint gold (+1,000, +10,000, +50,000 GP) with party member recipient selector.
   - Region-wide crime purge and pardon bounties with legal reputation restored to 50 (Honored) for target or party.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **PASSED & APPROVED (Pillars 1–5)** | **8** | Cases 1–8 | **100% Passed (Verified in Multiplayer Session)** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [x] Case 1: Inhabit / Possess an Enemy or NPC (Pillar 1)
- [x] Host or join as Dungeon Master (`Ctrl + F11` to open Master Suite).
- [x] In the `[ Spawns & NPCs ]` tab, spawn any enemy (e.g. *SkeletalWarrior* or *Orc*).
- [x] Aim your crosshair at the spawned creature.
- [x] Click **`✦ Poseer Objetivo Apuntado (Aim & Inhabit) [Ctrl+H] ✦`** in the menu, OR simply press **`Ctrl + H`** in real time while aiming at it.
- [x] Verify:
  - HUD displays: `"[DM Possession] POSSESSION ACTIVE: SkeletalWarrior..."`
  - Autonomous AI (`EnemyMotor`, `EnemyAttack`) freezes immediately.
  - Camera seamlessly docks behind the creature in 3rd-person chase mode.

---

### [x] Case 2: Control, Move, Attack & Perspective Toggle (Pillar 1)
- [x] While possessing the creature:
  - [x] **Movement:** Use `W / A / S / D` to walk. Hold `Shift` to sprint. Verify creature plays walking billboard animation.
  - [x] **Mouse Look:** Move mouse horizontally to turn the creature's facing direction.
  - [x] **Perspective Toggle:** Press **`Tab`** to toggle between 3rd-person chase camera and 1st-person eye view.
  - [x] **Melee Attack:** Left Click to attack. Verify creature plays primary attack animation and deals damage to nearby entities.

---

### [x] Case 3: Seamless Release of Possession (`Esc` / `F8` / `Ctrl+H`) (Pillar 1)
- [x] While controlling the creature, press **`Esc`**, **`F8`**, or **`Ctrl + H`** (or click `✦ Liberar Posesión ✦` in Master Suite).
- [x] Verify:
  - Creature's autonomous AI is restored.
  - DM camera detaches and returns smoothly to previous player/spectator position.
  - No camera clipping or soft-locks.

---

### [x] Case 4: Hierarchical Bestiary & Instant Search Filter (Pillar 2)
- [x] Open Master Suite (`Ctrl + F11`) -> `[ Spawns & NPCs ]`.
- [x] Switch between category tabs: `[Todos]`, `[No-Muertos]`, `[Monstruos]`, `[Daedra]`, `[Humanoides]`, `[Fauna]`.
- [x] Type in the search box (e.g., `"Lich"`, `"Daedra"`, `"Guard"`, `"Dragon"`).
- [x] Verify the list filters instantaneously.
- [x] Select an enemy and click **`✦ Spawn en Frente ✦`**. Verify the correct creature spawns.

---

### [x] Case 5: Custom HP Scaling & Ground Spawn (Pillar 2)
- [x] Adjust the **HP Multiplier** slider (e.g. set to `3.0x`).
- [x] Select a creature (e.g. *FrostDaedra*) and spawn it.
- [x] Attack or inspect the creature. Confirm its Max Health is multiplied 3x compared to normal.

---

### [x] Case 6: Loot Forge (Pillar 3)
- [x] Open Master Suite -> `[ Loot & Forja ]`.
- [x] Under **✦ FORJA DE OBJETOS PERSONALIZADOS (LOOT FORGE) ✦**:
  - [x] Select Weapon: *Dai-Katana*, Material: *Daedric*. Select Recipient (DM or party client) and click **`✦ Al Inventario ✦`**. Open inventory (`F6`) and verify a *Daedric Dai-Katana* is present on target.
  - [x] Select Armor: *Cuirass*, Material: *Ebony*. Click **`✦ En Suelo (Público) ✦`**. Verify an ebony cuirass loot container appears at feet and replicates to all connected clients.

---

### [x] Case 7: World Events Engine — Earthquake & Ambient SFX (Pillar 4)
- [x] Open Master Suite -> `[ Mundo & Eventos ]`.
- [x] Under **✦ EVENTOS DEL MUNDO & CATACLISMOS ✦**:
  - [x] Click **`✦ Provocar Terremoto (Earthquake) ✦`**.
  - [x] Verify camera shakes realistically for 4 seconds with subterranean audio across all clients simultaneously, then smoothly resets.
  - [x] Test ambient sound triggers:
    - Click `Trueno / Thunder` -> plays thunder crack on all clients.
    - Click `Rugido Monstruo` -> plays dragon/monster roar on all clients.
    - Click `Aullido Lejano` -> plays werewolf howl on all clients.
    - Click `Tambores de Guerra` -> plays war drums on all clients.

---

### [x] Case 8: DM Economy & Crime Pardon (Pillar 5)
- [x] Open Master Suite -> `[ Mundo & Eventos ]` / `[ Party & DM ]`.
- [x] Under **✦ ECONOMÍA & LEYES DM ✦**:
  - [x] Select target in Party Member Selector (`[Todos]`, `[DM]`, or client name).
  - [x] Click **`+10,000 Oro`**. Open inventory and verify gold increased by 10,000 for the selected target.
  - [x] Commit a crime in a town (e.g. attack a civilian or cast spell until guards shout "Halt!").
  - [x] Click **`✦ Perdonar Crímenes & Recompensas (Pardon Bounties) ✦`**.
  - [x] Verify guards cease hostility, legal reputation is restored to 50 (Honored), and crime state is cleared to `Crimes.None` for the selected recipient.

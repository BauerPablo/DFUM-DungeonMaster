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
   - Deposit directly into DM inventory or drop on ground.
4. **Pillar 4: World Events Engine:**
   - Earthquake cataclysm (`Screen-Shake` effect + low subterranean rumble).
   - Real-time ambient audio triggers: Thunder, Monster Roar, Distant Howl, Scream, War Drums, Ghost Wail.
5. **Pillar 5: DM Economy & Rule Manipulation:**
   - Mint gold (+1,000, +10,000, +50,000 GP).
   - Region-wide crime purge and pardon bounties with legal reputation restored to 50 (Honored).

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **READY TO TEST (Pillars 1–5)** | **8** | Cases 1–8 | **Awaiting User Verification** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [ ] Case 1: Inhabit / Possess an Enemy or NPC (Pillar 1)
- [ ] Host or join as Dungeon Master (`Ctrl + F11` to open Master Suite).
- [ ] In the `[ Spawns & NPCs ]` tab, spawn any enemy (e.g. *SkeletalWarrior* or *Orc*).
- [ ] Aim your crosshair at the spawned creature.
- [ ] Click **`✦ Poseer Objetivo Apuntado (Aim & Inhabit) ✦`** (or open tab and click the button).
- [ ] Verify:
  - HUD displays: `"[DM Possession] POSSESSION ACTIVE: SkeletalWarrior..."`
  - Autonomous AI (`EnemyMotor`, `EnemyAttack`) freezes immediately.
  - Camera seamlessly docks behind the creature in 3rd-person chase mode.

---

### [ ] Case 2: Control, Move, Attack & Perspective Toggle (Pillar 1)
- [ ] While possessing the creature:
  - [ ] **Movement:** Use `W / A / S / D` to walk. Hold `Shift` to sprint. Verify creature plays walking billboard animation.
  - [ ] **Mouse Look:** Move mouse horizontally to turn the creature's facing direction.
  - [ ] **Perspective Toggle:** Press **`Tab`** to toggle between 3rd-person chase camera and 1st-person eye view.
  - [ ] **Melee Attack:** Left Click to attack. Verify creature plays primary attack animation and deals damage to nearby entities.

---

### [ ] Case 3: Seamless Release of Possession (`Esc` / `F8`) (Pillar 1)
- [ ] While controlling the creature, press **`Esc`** or **`F8`** (or click `✦ Liberar Posesión ✦` in Master Suite).
- [ ] Verify:
  - Creature's autonomous AI is restored.
  - DM camera detaches and returns smoothly to previous player/spectator position.
  - No camera clipping or soft-locks.

---

### [ ] Case 4: Hierarchical Bestiary & Instant Search Filter (Pillar 2)
- [ ] Open Master Suite (`Ctrl + F11`) -> `[ Spawns & NPCs ]`.
- [ ] Switch between category tabs: `[Todos]`, `[No-Muertos]`, `[Monstruos]`, `[Daedra]`, `[Humanoides]`, `[Fauna]`.
- [ ] Type in the search box (e.g., `"Lich"`, `"Daedra"`, `"Guard"`, `"Dragon"`).
- [ ] Verify the list filters instantaneously.
- [ ] Select an enemy and click **`✦ Spawn en Frente ✦`**. Verify the correct creature spawns.

---

### [ ] Case 5: Custom HP Scaling & Ground Spawn (Pillar 2)
- [ ] Adjust the **HP Multiplier** slider (e.g. set to `3.0x`).
- [ ] Select a creature (e.g. *FrostDaedra*) and spawn it.
- [ ] Attack or inspect the creature. Confirm its Max Health is multiplied 3x compared to normal.

---

### [ ] Case 6: Loot Forge (Pillar 3)
- [ ] Open Master Suite -> `[ Loot & Forja ]`.
- [ ] Under **✦ FORJA DE OBJETOS PERSONALIZADOS (LOOT FORGE) ✦**:
  - [ ] Select Weapon: *Dai-Katana*, Material: *Daedric*. Click **`✦ Forjar en Inventario ✦`**. Open inventory (`F6`) and verify a *Daedric Dai-Katana* is present.
  - [ ] Select Armor: *Cuirass*, Material: *Ebony*. Click **`✦ Forjar y Soltar en Suelo ✦`**. Verify an ebony cuirass loot container appears at your feet.

---

### [ ] Case 7: World Events Engine — Earthquake & Ambient SFX (Pillar 4)
- [ ] Open Master Suite -> `[ Mundo & Eventos ]`.
- [ ] Under **✦ EVENTOS DEL MUNDO & CATACLISMOS ✦**:
  - [ ] Click **`✦ Provocar Terremoto (Earthquake) ✦`**.
  - [ ] Verify camera shakes realistically for 4 seconds with subterranean audio, then smoothly resets.
  - [ ] Test ambient sound triggers:
    - Click `Trueno / Thunder` -> plays thunder crack.
    - Click `Rugido Monstruo` -> plays dragon/monster roar.
    - Click `Aullido Lejano` -> plays werewolf howl.
    - Click `Tambores de Guerra` -> plays war drums.

---

### [ ] Case 8: DM Economy & Crime Pardon (Pillar 5)
- [ ] Open Master Suite -> `[ Mundo & Eventos ]` / `[ Party & DM ]`.
- [ ] Under **✦ ECONOMÍA & LEYES DM ✦**:
  - [ ] Click **`+10,000 Oro`**. Open inventory and verify gold increased by 10,000.
  - [ ] Commit a crime in a town (e.g. attack a civilian or cast spell until guards shout "Halt!").
  - [ ] Click **`✦ Perdonar Crímenes & Recompensas (Pardon Bounties) ✦`**.
  - [ ] Verify guards cease hostility, legal reputation is restored to 50 (Honored), and crime state is cleared to `Crimes.None`.

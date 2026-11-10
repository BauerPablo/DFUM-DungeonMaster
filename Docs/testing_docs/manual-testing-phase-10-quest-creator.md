# Manual Testing Guide: Quest Creator & Shared Quest System (Phase 10)

## Overview
This testing guide covers the **Quest Creator / Debug Menu**, multiplayer **Opt-In Quest Offering** infrastructure, **Dialogue Synchronization**, and DM exploration tools introduced for Phase 10 (Shared Quests & Dialogue Synchronization).

---

## Testing Status Dashboard

| Status | Total Cases | Cases |
| :--- | :---: | :--- |
| **PASSED / VERIFIED** | **17** | Cases 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 |
| **PENDING VERIFICATION** | **0** | None (Phase 10 Complete) |

- **Vote Timeout:** Configured to `60s` (previously 15s).
- **Dialogue Text:** Only overhead 3D bubbles appear; top HUD yellow notices removed.
- **FreeCam:** Full 6-DOF flying with WASD, mouse look (yaw/pitch), and 3rd-person character puppet rendering. Activated via **`Ctrl + F8`** or `dm_freecam`. Down key is **`C`** (Ctrl removed to prevent menu conflicts). Mouse wheel dynamically scales speed with HUD text.
- **Elevation Raycast & Ground Safeguard:** Raycast down from high altitude (1500m) smoothly snaps player to terrain + 1m, motor movement cancelled and fall damage suppressed (`ClearFallingDamage()`).
- **DM God Mode (Invincible):** Toggle in DM Tool (`Ctrl + F11`) or console `dm_god`.
- **Interior Gather:** Cross-pixel map sync prevents dark void spawns when gathering party into buildings.
- **DM Invisibility:** Replicated across network (hides puppet on remote clients), suppresses crime reporting, and pacifies AI targeting.
- **Quest Foe Synchronization:** Quest enemies spawned via quest triggers (`place foe`) synchronize cell context (`isInside`, `buildingKey`, `dungeon`), link `QuestResourceBehaviour` on clients, and advance kill counts for all party members upon death.

---

## Controls & Shortcuts Reference

- **`Ctrl + F7`**: Toggle Quest Creator & Quest Inspector menu.
- **`Ctrl + F11`**: Toggle DM Spawner & Teleport GUI (`dm_gui`). Includes God Mode & Invisibility toggles.
- **`Ctrl + F10`**: Toggle Multiplayer Network & Travel Status.
- **`Ctrl + F8`**: Toggle Eagle Eye / FreeCam flight mode.
- **Console (`~`) Commands:**
  - `quest_gui` or `net_quest menu`: Open Quest Creator.
  - `gather_party`: Host gathers all party members to host's current position/building.
  - `dm_freecam` / `freecam`: Toggle FreeCam flight mode.
  - `dm_invis`: Toggle DM True Invisibility.
  - `dm_god`: Toggle DM God Mode (Invincibility).
  - `dm_tp <mapX> <mapY>`: Teleport to map coordinates.
  - `dm_tp quest`: Teleport directly to active quest objective dungeon/town.
  - `dm_tp questor`: Teleport directly to active quest giver/turn-in site.

---

## Detailed Test Cases & Checklists

### [x] Case 1: Local Quest Start & Inspector Reset
- [x] Open Quest Creator with **`Ctrl + F7`**.
- [x] Verify presets show friendly names (e.g. `Fighters: Giant Hunt (M0B00Y00)`).
- [x] Verify Faction selector has friendly options (Commoner, Fighters Guild, Mages Guild, etc.).
- [x] Select a quest and click **Start Locally**.
- [x] Journal (**`L`**) lists the active quest.
- [x] Quest Creator lists active quest with UID under Section 4.
- [x] Click **End**: Quest clears immediately from journal and active list.

---

### [x] Case 2: Host Offers Quest to Party (Both Accept)
- [x] Connect Host and Client.
- [x] On Host, open **`Ctrl + F7`** and click **Offer to Party (Vote)**.
- [x] Host is auto-approved (`[x] Host (Yes)`) without redundant self-prompts.
- [x] Client receives Parchment popup window: `[ Quest Offer ]` with 60s countdown timer.
- [x] Client votes **Yes**.
- [x] Both receive HUD confirmation: `[Party Quest] Accepted quest!`.
- [x] Both players press **`L`** and see the exact same quest active in their journals.

---

### [x] Case 3: Client Proposes Quest to Party
- [x] On Client, open **`Ctrl + F7`** and click **Offer to Party (Vote)**.
- [x] Client is auto-marked as `Yes`.
- [x] Host receives Parchment vote prompt.
- [x] Host votes **Yes**.
- [x] Both players accept and start the quest synchronously.

---

### [x] Case 4: Opt-In Consensus: Host Declines / Client Accepts
- [x] Host offers quest to party via **`Ctrl + F7`**.
- [x] Host declines (or closes prompt), while Client clicks **Yes**.
- [x] Client accepts and compiles the quest successfully.
- [x] Client's journal (**`L`**) shows the quest active.
- [x] Host's journal remains empty.

---

### [x] Case 5: NPC Quest Giver in Town / Guild with Party Consensus
- [x] In town, approach a Guildmaster or quest-giving NPC.
- [x] Request a quest. The NPC dialogue popup appears with quest details.
- [x] Accept the quest.
- [x] The party vote popup triggers for all party members with a 60-second timer.
- [x] Initiator is marked as `Yes`.
- [x] Remote players who vote **Yes** receive the quest in their journal.
- [x] Remote players who vote **No** do not receive it.

---

### [x] Case 6: Console Command Testing (`net_quest`)
- [x] `net_quest menu`: Opens Quest Creator GUI.
- [x] `net_quest list`: Lists active running quests in console.
- [x] `net_quest offer M0B00Y00 41`: Initiates party vote for Giant Hunt.
- [x] `net_quest start M0B00Y00 41`: Starts Giant Hunt locally.
- [x] `net_quest end all`: Cancels all active quests.

---

### [x] Case 7: Overhead Dialogue Bubbles & Acoustic Falloff
- [x] Host talks to a town NPC near Client.
- [x] Client sees 3D overhead speech bubble directly above NPC's head.
- [x] Speech bubble scales text opacity with distance (close = crisp, far = faint, >16m = hidden).
- [x] **Fix Verification:** Dialogue text appears **only** overhead in 3D and does **NOT** spam yellow notices in the top HUD.

---

### [x] Case 8: Player Puppet Speech & Sequential Replacement
- [x] Client speaks with NPC.
- [x] Host sees player dialogue line in a speech bubble directly above Client's puppet head.
- [x] Host sees NPC answer above NPC's head.
- [x] New dialogue lines immediately replace older lines without overlap.

---

### [x] Case 9: Conversation End Linger & Smooth Fadeout
- [x] Client closes dialogue window (**`Esc`**).
- [x] Active speech bubbles remain visible for ~3.5 seconds.
- [x] Bubbles smoothly fade out over 1.5 seconds.

---

### [x] Case 10: Deterministic Quest Generation & Target Enemy Spawn
- [x] Deterministic seed verified: Host and Client journals (**`L`**) display identical target dungeon and time limits.
- [x] Objective teleport verified: Teleports party directly to the target location.
- [x] Quest Boss Spawn Mechanic: In DFU quests, foes assigned to dungeons/interiors spawn at designated interior markers upon entering the site, or on timer/proximity trigger.

---

### [x] Case 11: Quest Inspector `[View]` Details & Quick TP
- [x] Geolocation Mode 1 (Cercanía) tested and working.
- [x] In **`Ctrl + F7`**, under Section 4 (Active Running Quests), click **`[View]`** next to a quest.
- [x] Details expand: Quest Name, Title, UID, Faction, Objective site, Questor site.
- [x] Shows target dungeon/town name and map coordinates `(X, Y)`.
- [x] Click **`TP to Objective`**: Player teleports to objective site.
- [x] Click **`TP to Questor`**: Player teleports to quest giver site.

---

### [x] Case 12: Remote Province Browsing & Party Fast-Warp (Elevation & Road Offset Fix)
- [x] Remote province selector and tavern filtering tested and working.
- [x] Host teleports smoothly and lands on terrain surface.
- [x] Client fall-through resolved: Clients spawn aligned backwards along entrance road without colliding or falling into void.

---

### [x] Case 13: Complete Multiplayer Quest Loop (End-to-End)
- **Goal:** Validate full lifecycle from acceptance to boss fight and reward turn-in.
- [x] Offer quest to party (**`Ctrl + F7`** -> `Offer to Party`). Both accept.
- [x] Warp party to objective dungeon (`dm_tp quest` or `Ctrl + F11`).
- [x] Enter dungeon/house together: Quest monster (e.g. Giant Scorpion) spawns for **both** Host and Client.
- [x] Client proxy enemy correctly matches cell context and attaches `QuestResourceBehaviour`.
- [x] Defeat the quest monster in co-op: Kill count increments on both Host and Client quests.
- [x] Journal updates for all party members indicating objective completed.
- [x] Warp back to questor (`dm_tp questor`).
- [x] Talk to questor to turn in quest: reward (gold/reputation) is granted.
- [x] Clean journal closing & Tombstoning: Quest ends and clears from journal (**`L`**) on both Host and Client.
- [x] Reputation inspector & manual host adjustments verified across all party members.

---

### [x] Case 14: Eagle Eye / FreeCam Flight & 3rd-Person Puppet View (Fix Verification)
- **Goal:** Verify full 6-DOF flight controls and character puppet visibility.
- [x] Press **`Ctrl + F8`** (or execute **`dm_freecam`** in console).
- [x] **Camera Movement & Controls:**
  - [x] Camera decouples smoothly from current eye position without falling underground.
  - [x] First-person weapon model is hidden.
  - [x] Mouse look moves freely (pitch up/down, yaw left/right).
  - [x] **`W`**, **`A`**, **`S`**, **`D`** fly forward, left, backward, right.
  - [x] **`Space`** flies up, **`C`** flies down (**`Ctrl`** removed to prevent shortcut conflicts), **`Shift`** sprints.
  - [x] Mouse scroll wheel smoothly scales flight speed (`2` to `150` m/s) with HUD notification: `[FreeCam] Speed: <X> m/s`.
- [x] **Puppet Rendering:**
  - [x] You see your own character puppet standing at the original position with tag `[YourName] (You)`.
- [x] **Deactivation:**
  - [x] Press **`Ctrl + F8`** again: Camera snaps smoothly back to first-person inside player.

---

### [x] Case 15: DM Invisibility & AI Detection Bypass
- [x] Type **`dm_invis`** in console or click **`DM Invisibility: [OFF]`** in **`Ctrl + F11`**.
- [x] HUD displays: `[DM] Invisibility: ON (True Invisible)`.
- [x] Network visibility: On remote client's screen, host puppet is completely hidden (`activeSelf = false`).
- [x] AI & Crime bypass: Nearby monsters and town guards ignore player; no alerts or spawned watchmen.
- [x] FreeCam puppet integration: Local puppet is hidden when DM Invisibility is active.
- [x] Toggle DM invisibility OFF: Host puppet reappears on client screen and local FreeCam puppet reappears.

---

### [x] Case 16: DM Teleport Controls, Ground Elevation Raycast & God Mode
- [x] Press **`Ctrl + F11`** to open DM Spawner & Teleport GUI.
- [x] **`TP Me (X, Y)`**: Teleports local player to entered coordinates. High-altitude raycast (1500m) smoothly aligns to terrain surface + 1m, and fall damage is zeroed.
- [x] **`Warp Party (X, Y)`**: Teleports party together to coordinates with ground raycast.
- [x] **`TP Quest: <Name>`**: Teleports to active quest objective site.
- [x] **God Mode (Invincible)**: Click **`God Mode (Invincible): [OFF]`** button in **`Ctrl + F11`** or type **`dm_god`** in console to toggle invincibility on/off.
- [x] Console commands: `dm_tp <mapX> <mapY>`, `dm_tp quest`, `dm_tp questor`, `dm_god`.

---

### [x] Case 17: Gather Party into Building Interiors & Cross-Pixel Transitions
- [x] Host enters an interior building (e.g., a Tavern or General Store in Daggerfall).
- [x] Client stands outside in town or in a completely different map pixel/settlement.
- [x] Host executes `gather_party` (via console or **`Ctrl + F11`** button).
- [x] Client transitions across map pixels if needed.
- [x] Client transitions into the building interior next to Host without void desync.

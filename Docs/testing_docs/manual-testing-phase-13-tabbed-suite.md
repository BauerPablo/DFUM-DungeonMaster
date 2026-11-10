# Manual Testing Guide: Tabbed Pixel-Art UI Architecture & Main Menu Hub (Phase 13)

## Overview
This testing guide covers all features introduced and verified in **Phase 13 (v0.9.13)**:
1. **Multiplayer Main Menu Integration (`DaggerfallMultiplayerWindow`):** Native retro 320x200 pixel-virtual window directly accessible from the title screen (`DaggerfallStartWindow`) and pause menu.
2. **Tabs System in Main Menu:**
   - `[ UNIRSE (JOIN) ]`: Connect to servers via IP/Port, set player name, choose role (Player / DM).
   - `[ CREAR (HOST) ]`: Launch server session, set port, toggle friendly fire / PvP, select Host role.
   - `[ HISTORIAL ]`: Saved recent servers list with 1-click quick-fill.
   - `[ MODS ]`: Pre-game mod manifest check information.
3. **Unified In-Game Tabbed Master Suite (`DrawTtrpgMasterWindow`):** Single integrated window replacing cluttered debug overlays with Dark Fantasy pixel-art framing.
4. **Master Tabs System & DM Security:**
   - Tab 0: `Red & Sesión` (Connection, Host, Ping, Sync filters) — accessible to all connected players.
   - Tab 1: `Spawns & NPCs` (Enemy spawner, despawner, quick spawns) — DM only.
   - Tab 2: `Mundo & Clima` (World time, time scale, weather, DM teleportation, god mode, observer / freecam) — DM only.
   - Tab 3: `Quests & Diálogos` (Presets, custom quest compiler, geolocation, town search filter) — DM only.
   - Tab 4: `Loot & Forja` (Corpse spawning, chests, treasures, despawn loot) — DM only.
   - Tab 5: `Party & DM` (DM stealth/invisibility, gather party, PvP toggle, spell teaching / search) — DM only.
5. **Dark Fantasy Aesthetic & Layout Polish:**
   - High-resolution transparent pixel-art title logo banner (*"The Elder Scrolls II: Daggerfall Multiplayer TTRPG"*).
   - Compact 120px start menu positioned neatly to keep the campfire background scene fully visible.
   - 9-sliced IMGUI radio buttons eliminating horizontal stretch lines.
   - Widened quest action buttons (`Cancel` 72px, `Complete` 84px, `View` 52px).

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **VERIFIED & COMPLETED** | **6** | Cases 1, 2, 3, 4, 5, 6 | **ALL PASSED (v0.9.13)** |

---

## Detailed Test Cases & Results

### [x] Case 1: Start Menu "MULTIPLAYER / TTRPG" Button & Title Logo
- [x] Launch Daggerfall Unity to the title/start screen (`DaggerfallStartWindow`).
- [x] Verify the transparent pixel-art title banner appears with dragon, sword, and *"MULTIPLAYER TTRPG"* scroll.
- [x] Verify start menu container is compact (120px width), leaving the campfire adventurers and starry night sky unobstructed.
- [x] Click **`MULTIPLAYER LOBBY`**.
- [x] Verify `DaggerfallMultiplayerWindow` opens centered with classic 320x200 virtual resolution scaling.
- [x] Press `Esc` or click `SALIR`. Verify window closes back to start menu cleanly.

---

### [x] Case 2: Main Menu Hub Tabs Navigation
- [x] Open `DaggerfallMultiplayerWindow`.
- [x] Click on each tab header:
  - `[ UNIRSE (JOIN) ]`: Form shows IP, Port, Player Name, and Role button (`[ JUGADOR ]` vs `[ DUNGEON MASTER ]`).
  - `[ CREAR (HOST) ]`: Form shows Port (7777), Host Role toggle, PvP / Friendly fire button, and "INICIAR SERVIDOR".
  - `[ HISTORIAL ]`: Displays list of recently joined servers. Clicking an entry fills the IP/Port into Join tab.
  - `[ MODS ]`: Explains mod matching requirements and compatibility notes.
- [x] Verify tab switching is instant and visually distinct (active tab highlighted in gold).

---

### [x] Case 3: Hosting and Joining from the Main Menu Hub
- [x] On Host instance:
  - Open `MULTIPLAYER LOBBY` -> `[ CREAR (HOST) ]`.
  - Toggle PvP or Role if desired, then click **`INICIAR SERVIDOR (START HOST)`**.
  - Verify server initializes on port 7777 and window closes.
- [x] On Client instance:
  - Open `MULTIPLAYER LOBBY` -> `[ UNIRSE (JOIN) ]`.
  - Enter IP `127.0.0.1` and Port `7777`. Click **`CONECTAR A LA PARTIDA`**.
  - Verify client connects, character synchronizes, and IP is saved into `[ HISTORIAL ]`.

---

### [x] Case 4: Unified In-Game TTRPG Master Suite Navigation (`Ctrl + F11`)
- [x] In-game, press **`Ctrl + F11`**.
- [x] Verify the unified **DFU TTRPG MASTER SUITE** window opens with 6 top tabs:
  - `[ Red & Sesión ]`
  - `[ Spawns & NPCs ]`
  - `[ Mundo & Clima ]`
  - `[ Quests & Diálogos ]`
  - `[ Loot & Forja ]`
  - `[ Party & DM ]`
- [x] Verify log filter radio buttons have clean circular icons without horizontal strikethrough lines.
- [x] Under `Quests & Diálogos`, verify `Cancel` (72px) and `Complete` (84px) button labels fit cleanly without text cutoff.
- [x] Press **`Esc`** or click **`Cerrar (Esc / Ctrl+F11)`**; verify window closes and mouse cursor relocks.

---

### [x] Case 5: Direct Tab Hotkey Navigation (`Ctrl+F10`, `Ctrl+F12`, `Ctrl+F7`)
- [x] With Master Suite closed:
  - Press **`Ctrl + F10`**: Master Suite opens directly on **Red & Sesión** tab.
  - Press **`Ctrl + F10`** again: Master Suite closes.
  - Press **`Ctrl + F12`** (Host/DM): Master Suite opens directly on **Loot & Forja** tab.
  - Press **`Ctrl + F7`** (Host/DM): Master Suite opens directly on **Quests & Diálogos** tab.
  - With window open on **Quests & Diálogos**, press **`Ctrl + F12`**: Swaps tab directly to **Loot & Forja**.

---

### [x] Case 6: Tab Content Functionality & DM Security
- [x] On Pure Client (non-host / non-DM):
  - In Master Suite (`Ctrl + F11`), verify all 6 tabs are displayed in the header.
  - Verify the 5 DM tabs (`Spawns`, `Mundo`, `Quests`, `Loot`, `Party`) are disabled (`GUI.enabled = false`) and tagged with `(DM)`.
  - Verify only **`Red & Sesión`** is enabled and interactive for the client.
  - Verify pressing `Ctrl + F7` or `Ctrl + F12` as a client does not bypass security into DM tabs.
- [x] On Host / DM:
  - In `[ Spawns & NPCs ]`: Spawn enemies and civilians; verify despawn works.
  - In `[ Mundo & Clima ]`: Adjust time and weather; verify synchronization across peers.
  - In `[ Loot & Forja ]`: Spawn chests and containers; verify party can loot.
  - In `[ Party & DM ]`: Toggle DM Invisibility; verify player becomes invisible to enemies and network peers.

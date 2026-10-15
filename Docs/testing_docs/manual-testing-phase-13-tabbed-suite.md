# Manual Testing Guide: Tabbed Pixel-Art UI Architecture & Main Menu Hub (Phase 13)

## Overview
This testing guide covers all features introduced in **Phase 13**:
1. **Multiplayer Main Menu Integration (`DaggerfallMultiplayerWindow`):** Native retro 320x200 pixel-virtual window directly accessible from the title screen (`DaggerfallStartWindow`) and pause menu.
2. **Tabs System in Main Menu:**
   - `[ UNIRSE (JOIN) ]`: Connect to servers via IP/Port, set player name, choose role (Player / DM).
   - `[ CREAR (HOST) ]`: Launch server session, set port, toggle friendly fire / PvP, select Host role.
   - `[ HISTORIAL ]`: Saved recent servers list with 1-click quick-fill.
   - `[ MODS ]`: Pre-game mod manifest check information.
3. **Unified In-Game Tabbed Master Suite (`DrawTtrpgMasterWindow`):** Single integrated window replacing cluttered debug overlays.
4. **Master Tabs System:**
   - Tab 0: `Red & Sesión` (Connection, Host, Ping, Sync filters)
   - Tab 1: `Spawns & NPCs` (Enemy spawner, despawner, quick spawns)
   - Tab 2: `Mundo & Clima` (World time, time scale, weather, DM teleportation, god mode, observer / freecam)
   - Tab 3: `Quests & Diálogos` (Presets, custom quest compiler, geolocation, town search filter)
   - Tab 4: `Loot & Forja` (Corpse spawning, chests, treasures, despawn loot)
   - Tab 5: `Party & DM` (DM stealth/invisibility, gather party, PvP toggle, spell teaching / search)
5. **Keyboard Shortcuts & Hotkey Navigation:**
   - `Ctrl + F11`: Toggles the Master Suite.
   - `Ctrl + F10`: Opens or toggles `Red & Sesión`.
   - `Ctrl + F12`: Opens or toggles `Loot & Forja`.
   - `Ctrl + F7`: Opens or toggles `Quests & Diálogos`.
   - `Esc`: Closes the active Master Suite window.
   - Console command `ttrpg_gui [0-5]`: Toggles or jumps to a specific tab.

---

## Testing Status Dashboard

| Status | Total Cases | Cases |
| :--- | :---: | :--- |
| **READY FOR TESTING** | **6** | Cases 1, 2, 3, 4, 5, 6 |

---

## Detailed Test Cases & Checklists

### [ ] Case 1: Start Menu "MULTIPLAYER / TTRPG" Button
- [ ] Launch Daggerfall Unity to the title/start screen (`DaggerfallStartWindow`).
- [ ] Verify the button **`MULTIPLAYER / TTRPG`** appears between "Start New Game" and "Exit Game" with retro gold border and text.
- [ ] Click **`MULTIPLAYER / TTRPG`**.
- [ ] Verify `DaggerfallMultiplayerWindow` opens centered with classic 320x200 virtual resolution scaling.
- [ ] Press `Esc` or click `CANCELAR / SALIR`. Verify window closes back to start menu cleanly.

---

### [ ] Case 2: Main Menu Hub Tabs Navigation
- [ ] Open `DaggerfallMultiplayerWindow`.
- [ ] Click on each tab header:
  - `[ UNIRSE (JOIN) ]`: Form shows IP, Port, Player Name, and Role button (`[ JUGADOR ]` vs `[ DUNGEON MASTER ]`).
  - `[ CREAR (HOST) ]`: Form shows Port (7777), Host Role toggle, PvP / Friendly fire button, and "INICIAR SERVIDOR".
  - `[ HISTORIAL ]`: Displays list of recently joined servers. Clicking an entry fills the IP/Port into Join tab.
  - `[ MODS ]`: Explains mod matching requirements and compatibility notes.
- [ ] Verify tab switching is instant and visually distinct (active tab highlighted in gold).

---

### [ ] Case 3: Hosting and Joining from the Main Menu Hub
- [ ] On Host instance:
  - Open `MULTIPLAYER / TTRPG` -> `[ CREAR (HOST) ]`.
  - Toggle PvP or Role if desired, then click **`INICIAR SERVIDOR (START HOST)`**.
  - Verify server initializes on port 7777 and window closes.
- [ ] On Client instance:
  - Open `MULTIPLAYER / TTRPG` -> `[ UNIRSE (JOIN) ]`.
  - Enter IP `127.0.0.1` and Port `7777`. Click **`CONECTAR A LA PARTIDA`**.
  - Verify client connects, character synchronizes, and IP is saved into `[ HISTORIAL ]`.

---

### [ ] Case 4: Unified In-Game TTRPG Master Suite Navigation (`Ctrl + F11`)
- [ ] In-game, press **`Ctrl + F11`**.
- [ ] Verify the unified **DFU TTRPG MASTER SUITE** window opens with 6 top tabs:
  - `[ Red & Sesión ]`
  - `[ Spawns & NPCs ]`
  - `[ Mundo & Clima ]`
  - `[ Quests & Diálogos ]`
  - `[ Loot & Forja ]`
  - `[ Party & DM ]`
- [ ] Click each tab; verify content refreshes smoothly inside the scroll view.
- [ ] Press **`Esc`** or click **`Cerrar (Esc / Ctrl+F11)`**; verify window closes and mouse cursor relocks.

---

### [ ] Case 5: Direct Tab Hotkey Navigation (`Ctrl+F10`, `Ctrl+F12`, `Ctrl+F7`)
- [ ] With Master Suite closed:
  - Press **`Ctrl + F10`**: Master Suite opens directly on **Red & Sesión** tab.
  - Press **`Ctrl + F10`** again: Master Suite closes.
  - Press **`Ctrl + F12`**: Master Suite opens directly on **Loot & Forja** tab.
  - Press **`Ctrl + F7`**: Master Suite opens directly on **Quests & Diálogos** tab.
  - With window open on **Quests & Diálogos**, press **`Ctrl + F12`**: Swaps tab directly to **Loot & Forja**.

---

### [ ] Case 6: Tab Content Functionality & DM Security
- [ ] On Pure Client (non-host):
  - In `[ Spawns & NPCs ]` and `[ Mundo & Clima ]`, verify informative banner: "Spawns / controls are only allowed on the Host/Server (or DM)."
- [ ] On Host:
  - In `[ Spawns & NPCs ]`: Click "Spawn Rat (2m Front)", verify enemy spawns. Click "Despawn Last", verify enemy disappears.
  - In `[ Mundo & Clima ]`: Click "Noon (12:00)" and "Thunder", verify time and weather adjust.
  - In `[ Loot & Forja ]`: Click "Spawn Dungeon Chest", verify chest appears.
  - In `[ Party & DM ]`: Toggle "DM Invisibility", verify player becomes invisible to enemies and network peers.

# Manual Testing Guide: Mods Catcher & Configuration Replication (Phase 18)

## Overview
This testing guide covers all features introduced in **Phase 18 (v0.9.18)** of the DFUM TTRPG Master Suite:
1. **1:1 Mod Manifest Handshake:**
   - Client and Server exchange full active `.dfmod` lists with assembly versions and cryptographic SHA-256 hashes.
   - Server evaluates 1:1 parity and blocks launch until all clients match the Host's exact loadout.
2. **Conflict & Mismatch Diagnostic Modal (`DFUModConflictModalWindow`):**
   - In Pre-Game Lobby (`Tab 2`), when a parity mismatch occurs:
     - The **Ready** button locks with crimson badge: `[!] CONFLICTO MODS (VER)`.
     - Clicking opens a pixel-art Dark Fantasy modal showing:
       - **Mods Faltantes:** Active on Host, missing on Client.
       - **Mods Sobrantes:** Active on Client, not allowed on Host.
       - **Versiones o Archivos en Conflicto:** Checksum SHA-256 or version differences.
3. **Authoritative `modsettings.json` Live Sync:**
   - Upon verified parity, the Host broadcasts its active mod settings (e.g. *Climates & Calories* survival parameters) to clients via `MsgModSettingsSync`.
   - Client creates a temporary session backup (`modsettings.mp_backup.json`) and live-applies the Host's settings in memory via `mod.LoadSettings()`.
4. **Session Disconnect Rollback:**
   - Disconnecting from the multiplayer session automatically restores local client configs from `.mp_backup.json` and deletes temporary backups with zero data loss.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **READY TO TEST (Phase 18)** | **6** | Cases 1–6 | **Implemented & Awaiting Verification** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [ ] Case 1: Vanilla vs. Vanilla Handshake (Zero Mods Parity)
- [ ] **Host Setup:**
  - Launch DFU instance 1 (no mods enabled in Enhancements/Mods menu).
  - Open Multiplayer Window -> **HOST** tab -> click `CREATE SERVER & OPEN LOBBY`.
- [ ] **Client Setup:**
  - Launch DFU instance 2 (no mods enabled).
  - Open Multiplayer Window -> **JOIN** tab -> connect to `127.0.0.1:7777`.
- [ ] **Verification:**
  - [ ] Client transitions into Pre-Game Lobby (`Tab 2`).
  - [ ] Roster list shows: `Mods: OK | Estado: [WAIT]` for both Host and Client.
  - [ ] Client's Ready button displays `MARK AS READY` (gold/emerald), NOT locked by conflict.
  - [ ] Tab 4 (Mods Inspector) shows `--- Local Active Mods (0) --- (Vanilla DFU)`.
  - [ ] Clicking `✦ VERIFICAR PARIDAD 1:1 DE MODS ✦` in Tab 4 opens modal stating: `✦ ¡100% PARIDAD DE MODS VERIFICADA! ✦`.

---

### [ ] Case 2: Missing Mod Detection (Host has mod, Client lacks it)
- [ ] **Host Setup:**
  - Place a `.dfmod` file (e.g. `climates-and-calories.dfmod` or any test `.dfmod`) in Host's `StreamingAssets/Mods/` directory and enable it in the Mods menu.
  - Start Host and open Lobby.
- [ ] **Client Setup:**
  - Client does NOT have this `.dfmod` installed (or it is disabled).
  - Client connects to Host.
- [ ] **Verification:**
  - [ ] In Client's Pre-Game Lobby, the roster displays:
    `Mods: MISMATCH (Faltan: 1)` in bright red text.
  - [ ] Client's bottom Ready button is **Locked** with crimson texture and text: `[!] CONFLICTO MODS (VER)`.
  - [ ] Clicking the Ready button opens the **Mod Parity Conflict Diagnostics** modal.
  - [ ] Modal lists:
    `--- MODS FALTANTES (1) - Debes instalar: ---`
    `  [FALTA] climates-and-calories (vX.X)`
  - [ ] On Host's screen: Host launch button remains disabled (`WAITING FOR PARTY...`). Host cannot start the session while client is in conflict.

---

### [ ] Case 3: Extra Mod Detection (Client has unapproved mod)
- [ ] **Host Setup:**
  - Host runs clean vanilla (or has a specific mod set).
- [ ] **Client Setup:**
  - Client has an additional mod enabled that the Host does not have.
  - Client connects to Host.
- [ ] **Verification:**
  - [ ] Client's roster row displays: `Mods: MISMATCH (Extra: 1)` in red.
  - [ ] Ready button displays: `[!] CONFLICTO MODS (VER)`.
  - [ ] Clicking opens Diagnostic Modal listing:
    `--- MODS SOBRANTES (1) - Debes desactivar: ---`
    `  [EXTRA] {ExtraModName}`
  - [ ] Client cannot mark Ready until the extra mod is disabled in the main menu Mods list.

---

### [ ] Case 4: Version & SHA-256 Checksum Mismatch Detection
- [ ] **Setup:**
  - Place two `.dfmod` bundles with the same name on Host and Client, but with different internal versions or edited file contents (altering SHA-256 checksum).
- [ ] **Verification:**
  - [ ] When Client connects, the SHA-256 hash comparison flags the difference.
  - [ ] Diagnostics modal displays under `--- VERSIONES O ARCHIVOS EN CONFLICTO ---`:
    `  [MISMATCH] {ModName} (Checksum SHA-256 Diferente / Versión: ...)`
  - [ ] Prevents players from joining with modified, cheated, or outdated mod files.

---

### [ ] Case 5: Authoritative `modsettings.json` Replication
- [ ] **Host Setup:**
  - Host has a mod with configurable settings (e.g. *Climates & Calories*).
  - Host alters a distinct setting (e.g. Thirst Rate, Temperature Multiplier) in `modsettings.json`.
- [ ] **Client Setup:**
  - Client has matching `.dfmod` with its own default `modsettings.json`.
  - Client connects to Host. 1:1 parity is verified.
- [ ] **Verification:**
  - [ ] As parity is validated, Host automatically transmits `MsgModSettingsSync`.
  - [ ] Client logs confirm: `[DFU ModSync] Applied X authoritative mod configs from Host.`.
  - [ ] HUD displays green message: `[MOD CATCHER] Sincronizadas X reglas de mods autoritativas del Host.`.
  - [ ] Inspect Client's mod configuration directory:
    - Verify `modsettings.mp_backup.json` has been created containing the client's original values.
    - Verify `modsettings.json` now reflects the Host's custom values.
    - If in game, verify `mod.LoadSettings()` updated the live mod variables.

---

### [ ] Case 6: Automatic Non-Destructive Rollback on Disconnect
- [ ] **Prerequisite:** Complete Case 5 (Client is connected with replicated Host settings and active `.mp_backup.json`).
- [ ] **Procedure:**
  - In Client's Lobby (or pause menu), click **DISCONNECT**.
- [ ] **Verification:**
  - [ ] Client disconnects cleanly.
  - [ ] Check client's mod configuration directory:
    - `modsettings.json` has been restored to the original pre-session values.
    - `modsettings.mp_backup.json` has been deleted.
    - Client logs confirm: `[DFU ModSync] Restored X local mod configurations from backup.`.
    - Local single-player settings remain completely intact and unaltered.

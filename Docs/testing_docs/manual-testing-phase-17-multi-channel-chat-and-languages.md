# Manual Testing Guide: Multi-Channel Chat, Whispers & RPG Language Decryption (Phase 17)

## Overview
This testing guide covers all features introduced in **Phase 17 (v0.9.17)** of the DFUM TTRPG Master Suite:
1. **Channel Architecture & Commands:**
   - `/say` (or default): Local acoustic chat (< 25m) with automatic 3D overhead dialogue bubble.
   - `/party` (or `/p`): Party-wide broadcast reaching all players across dungeons and wilderness.
   - `/w [player]` (or `/whisper`): Bilateral private messages between two players.
   - `/dm [player]` (or `/gm`): Secret channel between DM and player for private notes, hallucinations, and passive perception checks.
   - `/ooc`: Out-of-character chat room for meta discussions.
   - `/roll [dice]` (e.g. `1d20+5`, `3d6`, `1d100`): Authoritative dice engine with RNG breakdown.
2. **Daggerfall Language Skill Decryption:**
   - Active language selector: *Tamrielic (Common)*, *Orcish*, *Dragonish*, *Daedric*, *Giantish*, *Harpy*, *Impish*, *Centaurian*, *Nymph*, *Spriggan*.
   - Recipient skill check based on native DFU character skills (`DFCareer.Skills`):
     - **Skill ≥ 50:** 100% clear translation.
     - **Skill 20–49:** Partially deciphered (comprehends key syllables).
     - **Skill < 20:** Scrambled into authentic guttural/exotic syllables.
     - **Dungeon Master:** Always receives translated text with `[Language -> DM]` tag.
3. **Retro Dark Fantasy Chat Window (`DMChatController`):**
   - Opened with `Enter` (or `T`).
   - Clean scrollable log with channel color-coding, autoscroll to bottom, and command autocomplete.
4. **Visual D20 Tray & Baldur's Gate 3-Style DC Challenge (`DMDiceTrayController`):**
   - Top-center triangular puller tab (`▼ D20 ▼`) unfolding a pixel-art rolling tray.
   - 1.8-second animated suspense die tumble with cycling faces and audio rattle before landing.
   - DM-initiated DC challenge with custom target number, reason, and hidden/visible DC toggle.
   - First client roll resolves challenge; Nat 20 (Golden Critical Success) & Nat 1 (Red Critical Failure) broadcasted party-wide.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **READY TO TEST (Phase 17)** | **9** | Cases 1–9 | **Awaiting Implementation & Verification** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [ ] Case 1: Chat HUD (`Y` / `Esc`), Auto-Fadeout, Large HUD Offset & DM Chord Blocking (`Ctrl+Shift`)
- [ ] Launch game and start or join a multiplayer session.
- [ ] **Player Controls & Cursor Verification:**
  - [ ] Press **`Enter`** while exploring: verify `Actions.ActivateCursor` toggles the mouse cursor normally. Chat does NOT open.
  - [ ] Move with **`WASD`**: verify character moves freely.
  - [ ] Hold **`L.Shift` + `W`**: verify sprinting works smoothly with no key blocks.
  - [ ] Hold **`L.Ctrl` + `W` / `A` / `D`**: verify sliding / crouching movement works with zero hindrance.
- [ ] **Chat Activation (`Y`):**
  - [ ] Press **`Y`**:
  - [ ] Verify chat window opens at bottom-left with translucent dark slate & bronze filigree border.
  - [ ] Text input field immediately receives keyboard focus.
  - [ ] Player movement and mouse look freeze cleanly so typing letters (`W`, `A`, `S`, `D`, `T`, `I`, etc.) never moves the character or opens vanilla menus.
- [ ] **DM Chord Blocking Verification (`Ctrl + Shift + [Key]`):**
  - [ ] Press **`Ctrl + Shift + F10`**: verify TTRPG Master Suite opens cleanly.
  - [ ] Press **`Ctrl + Shift + R`**: verify Tactical RTS Spawner activates WITHOUT opening the vanilla Rest (`R`) window.
  - [ ] Press **`Ctrl + Shift + H`**: verify NPC Possession activates WITHOUT triggering the Switch Hand (`H`) weapon animation.
  - [ ] Press **`Ctrl + Shift + F8`**: verify FreeCam / DM Entity Mode toggles WITHOUT taking a vanilla screenshot (`F8`).
- [ ] **Auto-Fadeout Verification:**
  - [ ] Type a message in chat and press **`Enter`** (or press **`Esc`** to cancel):
  - [ ] Input line closes, movement restores.
  - [ ] Chat log remains visible at full opacity for ~7 seconds, then smoothly fades out over ~2.5 seconds to 100% transparency.
  - [ ] When fully faded out, the screen is completely unobstructed.
  - [ ] Pressing **`Y`** or receiving any new message immediately wakes the chat up at 100% opacity.
- [ ] **Large HUD Docking Offset Verification:**
  - [ ] In game settings, enable docked **Large HUD**.
  - [ ] Verify chat box automatically elevates above the Large HUD bezel without covering health bars, compass, or inventory buttons.

---

### [ ] Case 2: Local Chat (`/say`) & Overhead Speech Bubble (< 25m)
- [ ] With two connected characters standing within 10 meters of each other:
  - [ ] Player A types: `"¡Cuidado con la trampa del pasillo!"` and presses Enter.
  - [ ] Verify message appears in local chat log in Ivory/White text.
  - [ ] Verify 3D overhead speech bubble appears above Player A's puppet.
- [ ] Have Player B walk further than 30 meters away:
  - [ ] Player A sends another `/say` message.
  - [ ] Verify Player B does NOT receive the message in their log (acoustic distance falloff verified).

---

### [ ] Case 3: Party Broadcast (`/party` / `/p`)
- [ ] Player A and Player B are in different dungeon blocks or exterior cells.
- [ ] Player A types: `/party Nos encontramos en la posada` (or `/p ...`).
- [ ] Verify:
  - Message appears in **Cyan** `[Party] [Player A]: Nos encontramos en la posada`.
  - Player B receives the message instantly regardless of world distance.

---

### [ ] Case 4: Private Whispers (`/w [player]`) & Overhead Nametag Verification
- [ ] **Nametag & Identity Check:**
  - [ ] Verify that neither player has the engine default `"Nameless"` displayed over their head.
  - [ ] The overhead 3D billboard reflects their actual character or lobby name (or `[DUNGEON MASTER] \n <Name>` for the host DM).
- [ ] **Whisper Dispatch:**
  - [ ] Player A types: `/w PlayerB Tengo una poción extra para ti` (or `/w dm ...` to whisper the host directly).
  - [ ] Supports full character name, prefix matching (e.g. `/w Play ...`), or connection ID.
  - [ ] Verify sent log on Player A: `<color=#FF77DD><b>[Susurro a PlayerB]:</b> Tengo una poción extra para ti</color>`.
  - [ ] Verify received log on Player B: `<color=#FF77DD><b>[Susurro de PlayerA]:</b> Tengo una poción extra para ti</color>`.
  - [ ] Verify message arrives **exactly once** (no duplicate echoes).
  - [ ] Third-party players receive 0 messages (privacy verified).

---

### [ ] Case 5: Secret DM Notes & Perception Checks (`/dm`)
- [ ] **Player to DM:**
  - [ ] Player A types: `/dm DM, creo que escuché pasos detrás del muro`.
  - [ ] Verify DM receives in **Amber/Gold**: `[DM de PlayerA]: creo que escuché pasos detrás del muro` (**delivered exactly once**).
  - [ ] Verify Player A sees: `[Secreto al DM]: creo que escuché pasos detrás del muro` (**delivered exactly once**).
- [ ] **DM to Player:**
  - [ ] DM replies: `/dm PlayerA Superas la tirada: sientes un ligero olor a azufre`.
  - [ ] Verify DM sees: `[DM a PlayerA]: Superas la tirada: sientes un ligero olor a azufre`.
  - [ ] Verify Player A receives: `[Secreto del DM]: Superas la tirada: sientes un ligero olor a azufre`.
  - [ ] Other players see nothing.

---

### [ ] Case 6: Authoritative Dice Roller (`/roll`)
- [ ] In chat, test various dice formulas:
  - [ ] `/roll 1d20` -> HUD displays: `[Tirada] PlayerA tira 1d20: [14] = 14`.
  - [ ] `/roll 1d20+5` -> displays: `[Tirada] PlayerA tira 1d20+5: [18] + 5 = 23 (¡Éxito Crítico!)`.
  - [ ] `/roll 3d6` -> displays: `[Tirada] PlayerA tira 3d6: [4, 6, 2] = 12`.
  - [ ] `/roll 1d100` -> displays percentage roll `1-100`.
- [ ] Verify dice rolls are evaluated on the host/server to prevent client RNG manipulation.

---

### [ ] Case 7: Daggerfall RPG Language Decryption & Cursor Availability
- [ ] **Mouse Cursor Verification:**
  - [ ] Press **`Y`** to open chat.
  - [ ] Verify mouse cursor unlocks immediately (`CursorLockMode.None`), is fully visible, and free to click all UI controls.
  - [ ] Click the Language button to cycle through languages (*Común*, *Orco*, *Dragón*, *Daédrico*, etc.).
  - [ ] Click the Channel button (*Party*, *Say*, *OOC*, etc.).
- [ ] **Language Decryption:**
  - [ ] Select **Orco (Orcish)**.
  - [ ] Player A (fluent in Orcish) types: `"¡Por Malacath, ataquen!"`.
  - [ ] **Recipient with Low Orcish Skill (< 20):**
    - Receives scrambled guttural text: `[Orco] "Ghor lagash mog durat!"`.
  - [ ] **Recipient with High Orcish Skill (≥ 50):**
    - Receives translated text: `[Orco] "¡Por Malacath, ataquen!"`.
  - [ ] **Dungeon Master:**
    - Regardless of character stats, the DM always sees: `[Orco -> Traductor DM] "¡Por Malacath, ataquen!"`.

---

### [ ] Case 8: Interactive D20 Tray & Physical Tumbling Animation
- [ ] Look at the top-center of the screen:
  - Verify downward triangular puller tab `[ ▼ D20 ▼ ]` is visible.
- [ ] Click the triangular tab:
  - Mouse cursor unlocks smoothly.
  - Rolling tray unfolds smoothly with dark fantasy borders.
  - Displays a detailed procedural icosahedron D20 die graphic and a **`✦ ¡LANZAR 1D20! ✦`** button.
- [ ] Click **`✦ ¡LANZAR 1D20! ✦`**:
  - Verify the die does NOT cycle numbers like a slot machine.
  - The die physically rotates (angular deceleration), hops with damped bounces across the tray, and squashes/stretches on floor impact.
  - While spinning, a tumbling sparkle (`✦`) is visible inside the moving facet.
  - Realistic clatter sounds decelerate from rapid rattles to a final solid landing thud.
  - On landing, the die settles upright at 0° with an impact punch and reveals the final number prominently centered in the front face:
    - **Natural 20:** Radiant Golden die texture, gold number `#FFDF78`, and gold coins victory sound.
    - **Natural 1:** Blood-red die texture, crimson number `#FF5555`.
    - **Regular (2–19):** Dark bronze/obsidian die texture, ivory number `#EDE2C8`.
- [ ] Click the triangular tab again:
  - Verify the tray retracts smoothly and mouse-look re-engages seamlessly.

---

### [ ] Case 9: DM DC Challenge (Baldur's Gate 3 Style)
- [ ] **Setup by DM:**
  - In Master Suite (or D20 Tray DM section), DM sets:
    - Razón: `"Forzar Cerradura Antigua"`
    - Dificultad (DC): `14`
    - Checkbox: `[x] Mostrar Dificultad a Jugadores`
  - DM clicks **`✦ Lanzar Desafío a la Party (DC 14) ✦`**.
- [ ] **Client Experience:**
  - The D20 tray automatically pops open on all players' screens with dramatic audio cue.
  - Header displays: `[DESAFÍO DEL DM: Forzar Cerradura Antigua]` y `[DIFICULTAD OBJETIVO: 14]`.
- [ ] **First Player to Roll:**
  - Player A clicks **`✦ ¡Lanzar Dado de Prueba! ✦`**.
  - Die tumbles with suspense for ~1.8 seconds.
  - **Regular Success (Roll ≥ 14):** Displays Emerald/Green banner `[¡PRUEBA SUPERADA! 16 ≥ 14]`.
  - **Regular Failure (Roll < 14):** Displays Crimson/Red banner `[PRUEBA FALLIDA: 8 < 14]`.
  - **Natural 20 (Éxito Crítico):** Radiant Gold banner `[✦ ¡ÉXITO CRÍTICO! NAT 20 ✦]` with immediate triumph fanfare!
  - **Natural 1 (Fallo Crítico):** Dark Red banner `[☠ ¡FALLO CRÍTICO! NAT 1 ☠]` with critical blunder sound!
- [ ] **Hidden DC Mode:**
  - DM unchecks `[ ] Mostrar Dificultad` and launches another challenge (e.g. DC 16).
  - Clients see: `[DIFICULTAD OBJETIVO: Desconocida / Oculta por el DM]`.
  - When the roll finishes, it displays whether it passed or failed without revealing the exact DC number.
- [ ] **Party Synchronization:**
  - All connected clients see the outcome banner simultaneously. Subsequent players cannot overwrite the first roll for that challenge.

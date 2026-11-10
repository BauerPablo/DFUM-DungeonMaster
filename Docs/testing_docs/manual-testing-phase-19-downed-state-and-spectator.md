# Manual Testing Guide: Downed State, Cooperative Revive & FreeCam Spectator (Phase 19)

## Overview
This testing guide covers all features introduced in **Phase 19 (v0.9.19)** of the DFUM TTRPG Master Suite:
1. **Unconscious / Downed State Interception (Anti-Desync Lifecycle):**
   - Intercepts player death at `<= 0 HP` when connected to a multiplayer session.
   - Completely suppresses vanilla DFU's destructive `TitleMenuFromDeath` reset sequence and scene unloads.
   - Pins health at 0, cancels motor movement/attacks, lowers the camera to floor level, displays a pulsing red vignette, plays an audible heartbeat pulse, and shows an onscreen countdown timer (`[Inconsciente] Desangrándote: 03:00`).
   - Chat (`/say`, `/party`, `/ooc`) remains 100% functional while unconscious so the player can call for help.
2. **Synchronized Prone Billboard & Overhead Nametag:**
   - Remote party puppets switch immediately to a prone corpse billboard and lower the overhead nametag to ground level with crimson text: `[INCONSCIENTE]`.
3. **Cooperative Resuscitation & Healing Mechanics:**
   - **First Aid (< 2.5m):** Approaching a fallen player and pressing **'F'** opens the interaction menu with `✦ REANIMAR / PRIMEROS AUXILIOS ✦`, restoring 15% HP.
   - **Healing Potion:** If the helper has a potion in inventory, `✦ ADMINISTRAR POCIÓN DE CURACIÓN ✦` appears, consuming 1 potion and restoring 30% HP.
   - **Healing Magic:** Casting any targeted restoration or healing spell on the fallen player authoritatively revives them with health equal to the spell's magnitude.
   - **DM Divine Resurrect:** In the DM Suite (`Ctrl + F10` -> Tab 5: Party), the DM can select any party member and click `✦ Revivir / Resucitar Divino ✦` to instantly restore them to full health.
4. **Bleedout Expiration & Seamless FreeCam Spectator Mode:**
   - If the 3-minute timer reaches 00:00 without resuscitation, `MsgPlayerPermaDeath` is dispatched.
   - A permanent DFU corpse marker is spawned at the player's death position.
   - The player seamlessly transitions into FreeCam Spectator mode (invisible, noclip 6-DOF flight, no collision) while maintaining connection, synchronization loops, and full chat participation.
5. **Zero Regression Guarantee:**
   - In offline single-player mode, classic DFU death behavior (`TitleMenuFromDeath`, fading, reset) remains 100% unchanged.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **VERIFIED (Phase 19)** | **7** | Cases 1–3, 5–7 | **6 Aprobados, 1 Diferido a Fase 20 (Caso 4)** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [x] Case 1: Interception of 0 HP Death & Bleedout Countdown (Anti-Desync Lifecycle)
- [ ] **Host Setup:**
  - Launch DFU Host, load or create a character, and start server via Multiplayer Window (`Ctrl+F10`).
- [ ] **Client Setup:**
  - Launch DFU Client, connect to `127.0.0.1:7777`.
- [ ] **Trigger Death on Client:**
  - Client takes fatal damage (e.g. attacked by an enemy, jumping from high cliff, or console command `tgm` off and damage).
- [ ] **Verification:**
  - [ ] Client's health reaches 0 HP, but the game **DOES NOT** unload the scene or return to the main title menu (`TitleMenuFromDeath`).
  - [ ] Client does **NOT** disconnect; NetworkClient remains connected and healthy.
  - [ ] Client's camera drops smoothly to floor level (~0.25m).
  - [ ] Screen displays a subtle pulsing crimson border (vignette).
  - [ ] Audible heartbeat pulse plays periodically through the audio source.
  - [ ] An onscreen banner appears at the top:
    `[INCONSCIENTE] Desangrándote: 03:00`
    `Pide auxilio por el chat (/party o Enter). Un aliado puede reanimarte con Primeros Auxilios, Poción o Hechizo.`
  - [ ] Client cannot walk or jump (`PlayerMotor.CancelMovement = true`), and the first-person weapon is lowered.
  - [ ] Pressing `Enter` opens the chat bar; client can type `/party ¡Ayuda, caí en la mazmorra!` and send it to the party.

---

### [x] Case 2: Visual Prone Pose & Synchronized Nametag on Remote Peers
- [ ] **Setup:**
  - With Client 1 downed in front of Host (Client 2):
- [ ] **Verification on Peer's Screen:**
  - [ ] Remote puppet for Client 1 collapses from upright position to prone corpse billboard on the floor.
  - [ ] Overhead 3D name tag moves down to ground level (~0.40m).
  - [ ] Name tag text changes to crimson/red:
    `[INCONSCIENTE]`
    `{PlayerName}`
  - [ ] Host's chat window shows system announcement:
    `[Party] ¡{PlayerName} ha caído inconsciente y necesita reanimación!`

---

### [x] Case 3: Cooperative First Aid & Potion Resuscitation ('F' Interaction)
- [ ] **Setup:**
  - Client 1 is downed.
  - Client 2 (or Host) approaches within proximity (< 2.5 meters) facing Client 1.
- [ ] **First Aid Execution:**
  - Client 2 presses **'F'** to open the Interaction Menu.
  - Verify window displays:
    `<b>[¡JUGADOR INCONSCIENTE!]</b>`
    `Este aliado se está desangrando. ¡Necesita reanimación urgente!`
    `✦ REANIMAR / PRIMEROS AUXILIOS ✦ (+15% HP)`
  - Client 2 clicks `✦ REANIMAR / PRIMEROS AUXILIOS ✦`.
- [ ] **Verification:**
  - [ ] Client 1's downed state clears immediately:
    - Camera restores smoothly to normal standing eye height.
    - Crimson vignette overlay disappears.
    - Heartbeat audio stops.
    - Movement, jumping, and weapon controls are restored.
    - Health is restored by +20 HP (15% baseline).
    - HUD shows: `¡Has sido reanimado por {HealerName}! (+20 HP)`.
  - [ ] On Client 2's screen:
    - Puppet stands back up into normal upright idle pose.
    - Name tag returns to eye level and bright cyan color.
  - [ ] Party chat displays:
    `[Party] ¡{Client1Name} ha sido reanimado por {Client2Name}! (+20 HP)`
- [ ] **Potion Resuscitation Variant:**
  - If Client 2 possesses any healing potion in inventory, the button `✦ ADMINISTRAR POCIÓN: {PotionName} ✦ (+30% HP)` also appears.
  - Clicking it consumes 1 potion from Client 2's inventory and restores 40 HP (30% baseline) to Client 1.

---

### [~] Case 4: Magic Spell Healing Resuscitation (Restoration Spell) — [Diferido a Fase 20]
- [ ] **Setup:**
  - Client 1 drops to 0 HP and enters Downed state.
  - Client 2 has a healing spell (e.g. *Heal Health*, *Heal*).
- [ ] **Action:**
  - Client 2 casts the restoration/healing spell targeting Client 1.
- [ ] **Verification:**
  - [ ] As soon as the healing effect packet arrives (`MsgApplySpellBundle` / `HealHealth`), Client 1's health increases above 0.
  - [ ] Client 1 instantly wakes up from the Downed state.
  - [ ] Normal camera, HUD, and motor controls are restored.
  - [ ] Party chat logs:
    `[Party] ¡{Client1Name} ha sido reanimado por Magia / Curación! (+{Magnitude} HP)`
  - [ ] Remote puppet stands back up across all peers.

---

### [x] Case 5: DM Divine Resurrect (DM Suite Ctrl+F10)
- [ ] **Setup:**
  - Client 1 is downed.
  - Host/DM opens the DM Suite via **Ctrl + F10** -> navigates to **Tab 5 (Party & DM)**.
- [ ] **Execution:**
  - Under party target selector, select Client 1.
  - Status indicator displays: `Estado: [INCONSCIENTE]`.
  - Click the radiant button: `✦ Revivir / Resucitar Divino ✦`.
- [ ] **Verification:**
  - [ ] Client 1 is instantly revived to 100% full Max Health.
  - [ ] Downed overlay and restrictions clear immediately.
  - [ ] Party chat displays:
    `[Party] ¡{Client1Name} ha sido reanimado por Dungeon Master! (+9999 HP)`
  - [ ] HUD displays: `[DM] ¡{Client1Name} ha sido resucitado divinamente!`.

---

### [x] Case 6: Bleedout Timeout Expiration & Seamless FreeCam Spectator Mode
- [ ] **Setup:**
  - Client 1 is downed and deliberately not resuscitated.
- [ ] **Execution:**
  - Allow the 3-minute timer to tick down from `03:00` to `00:00` (or test with reduced timer).
- [ ] **Verification upon 00:00:**
  - [ ] Timer expires without disconnecting or crashing to the title menu.
  - [ ] Client 1's physical body becomes a permanent ground corpse marker.
  - [ ] Client 1 immediately enters **FreeCam Spectator Mode**:
    - Camera flies freely in 3D using **WASD, Space, C, Sprint (Shift)** and mouse look.
    - Invisible to scene enemies and peers; no collision with world colliders.
    - Top banner shows: `✦ MODO ESPECTADOR (VUELO LIBRE) ✦ | Chat /party disponible`.
    - Chat remains 100% active; player can continue typing in `/party` and `/say`.
    - Network connection remains connected and scene state remains synchronized.
  - [ ] On other peers' screens:
    - A permanent corpse marker remains on the floor at the location of death.
    - Party chat logs: `[Party] ¡{Client1Name} ha sucumbido a sus heridas y ahora es un espectador!`.

---

### [x] Case 7: Offline Single-Player Safety & Regression Test
- [ ] **Setup:**
  - Launch DFU in offline single-player mode (no multiplayer hosting or joining).
  - Enter any game world with a normal character.
- [ ] **Execution:**
  - Take fatal damage until health reaches `<= 0`.
- [ ] **Verification:**
  - [ ] Single-player death sequence executes exactly as in vanilla DFU:
    - Camera tilts down to floor.
    - Screen fades to black over `FadeDuration`.
    - After 3 seconds, `StartGameBehaviour.StartMethods.TitleMenuFromDeath` triggers and brings the player back to the title menu.
  - [ ] No multiplayer downed overlay or spectator mode is triggered offline.

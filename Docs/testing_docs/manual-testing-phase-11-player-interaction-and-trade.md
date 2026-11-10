# Manual Testing Guide: Magic Spells, Player Interaction & Bilateral Trade (Phase 11)

## Overview
This testing guide covers all features introduced in **Phase 11**:
1. **Direct Bilateral Player-to-Player Trading** (Items & Gold).
2. **In-World Character Proximity Interaction** & **Map Discovery Sharing**.
3. **Spell Effect & Projectile Replication** (Touch, Missile, AOE Particles & Audio).
4. **Supportive Magic Routing** (Healing and beneficial buffs applied to party members).
5. **Offensive Magic Routing & PvP Toggle** (`net_pvp on/off`).
6. **Wandering Town Civilian NPCs Synchronization** (Deterministic / Server-authoritative population sync).

---

## Testing Status Dashboard

| Status | Total Cases | Cases |
| :--- | :---: | :--- |
| **PASSED / VERIFIED** | **11** | Cases 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 |

---

## Controls & Interaction Reference

- **Avatar Proximity Interaction:** Walk within 3.5m - 4.0m of another player's puppet avatar and press **`F`** (replaces click-activation to avoid accidental triggers during movement/combat). Pressing **`F`** or **`Esc`** closes the interaction menu.
- **Player Interaction Menu:**
  - `Compartir Mapa y Descubrimientos`: Transfers discovered towns and interior buildings to partner's automap.
  - `Iniciar Comercio Bilateral (Trade)`: Sends trade invitation.
  - `Cerrar`: Dismisses menu.
- **Escape Key (`Esc`):** Cancels active trade session or closes prompt.
- **Distance Safeguard:** If players separate > 6.0 meters, trade auto-cancels with HUD notification.
- **PvP Command:** In game console (`~`), run `net_pvp on` or `net_pvp off` (Host only) or toggle in DM Spawner overlay (`Ctrl + F11`).

---

## Detailed Test Cases & Checklists

### [x] Case 1: In-World Trade Invitation & Response Flow
- [x] Position Host and Client within 3.5 meters of each other.
- [x] Player A approaches Player B and presses **`F`**.
- [x] Interaction menu opens showing Player B's name, class, and distance.
- [x] Player A clicks **`Iniciar Comercio Bilateral (Trade)`**.
- [x] Player A receives HUD notice: `[Comercio] Solicitud enviada a [Nombre]. Esperando respuesta...`.
- [x] Player B receives popup prompt: `[Solicitud de Comercio]` with 25s countdown timer.
- [x] **Acceptance test:** Player B clicks **`Aceptar`**.
- [x] Both players see the 2-column Bilateral Trade Window open simultaneously.
- [x] **Decline test:** Repeat invite and click **`Rechazar`**; verify Player A is notified and no trade window opens.

---

### [x] Case 2: Gold Offering & Live Real-Time Preview
- [x] With trade window open, Player A clicks gold buttons (`+10`, `+100`, `+1000`, `Max`, `0`).
- [x] Player A's gold offer updates under **Tu Oferta**.
- [x] In real-time on Player B's screen, the right column (**Oferta de [Nombre]**) reflects the exact same gold amount.
- [x] Verify gold cannot exceed player's available gold (`PlayerEntity.GoldPieces`).

---

### [x] Case 3: Inventory Item Picker & Stack Transfer
- [x] Under **Tu Oferta**, Player A clicks **`▼ + Agregar Ítems del Inventario`**.
- [x] Sub-scrollview displays unequipped, non-quest inventory items with name, weight, and stack count.
- [x] Player A clicks an item (e.g. an Iron Dagger, potion, or arrows):
  - [x] Item moves to Player A's offered list.
  - [x] Total weight updates.
  - [x] Player B immediately sees the item appear on the right column with weight and stack count.
- [x] Player A clicks **`X`** next to the offered item:
  - [x] Item is removed from the offer.
  - [x] Item disappears from Player B's view in real-time.

---

### [x] Case 4: Offer Lock & Anti-Scam Protection
- [x] Player A sets gold and adds an item, then clicks **`Bloquear Oferta`**.
  - [x] Status updates to: `<color=#88FF88>[x] OFERTA BLOQUEADA</color>`.
  - [x] On Player B's screen, Player A's status shows: `[x] OFERTA BLOQUEADA`.
- [x] **Anti-Scam Verification:**
  - [x] Player B locks their offer as well.
  - [x] Player B clicks **`Aceptar Intercambio`**.
  - [x] Player A clicks **`Desbloquear Oferta`** and changes an item or gold.
  - [x] **Verify:** Player B's acceptance is immediately cleared to prevent stealth modifications.

---

### [x] Case 5: Bilateral Mutual Accept & Authoritative Swap Execution
- [x] Both players lock their offers (`Bloquear Oferta`).
- [x] Both players click **`Aceptar Intercambio`**.
- [x] Server receives both commitments, executes atomic swap:
  - [x] Player A's offered items & gold are removed from Player A and added to Player B.
  - [x] Player B's offered items & gold are removed from Player B and added to Player A.
- [x] Both players receive HUD text: `[Comercio] ¡Intercambio completado con éxito!`.
- [x] Trade window closes cleanly on both screens.
- [x] Both players open inventory (`F1`): verify received items and updated gold balance.

---

### [x] Case 6: Distance & Cancellation Safeguards
- [x] Open trade window between Host and Client.
- [x] One player walks away > 6.0 meters.
- [x] Trade window closes immediately with notification: `[Comercio] Cancelado: El compañero se alejó demasiado.`.
- [x] Reopen trade window and press **`Esc`**: window closes cleanly and partner is notified without items lost.

---

### [x] Case 7: In-World Map Discovery Sharing
- [x] Have Player A discover multiple shops, taverns, or guilds in town (automap shows labeled buildings).
- [x] Player A approaches Player B (< 3.5m) and presses **`F`**.
- [x] Player A clicks **`Compartir Mapa y Descubrimientos`**.
- [x] Player A receives HUD notice: `[Mapa] Descubrimientos compartidos con [Nombre] (X ubicaciones, Y edificios).`.
- [x] Player B receives HUD notice: `[Mapa] ¡[Nombre] compartió sus descubrimientos contigo! (X ubicaciones, Y edificios agregados).`.
- [x] Player B opens local automap (`M`) and exterior automap:
  - [x] Verify newly shared shops, guilds, and temples now appear discovered with correct building names and colors on Player B's map.

---

### [x] Case 8: Spell Effect & Projectile Replication (Touch, Missile, AOE)
- [x] Position Host and Client within line-of-sight in an outdoor or dungeon area.
- [x] **Missile Spell Test (e.g. Fireball, Wizard's Fire, Shock):**
  - [x] Player A casts a ranged missile spell.
  - [x] Verify Player B sees Player A's puppet play the casting animation.
  - [x] Verify Player B hears the element casting sound originating from Player A.
  - [x] Verify Player B sees the 3D projectile fly through the air matching Player A's trajectory.
  - [x] When projectile strikes a surface or entity, verify Player B sees the impact explosion billboard/particles and hears the impact sound.
- [x] **Touch Spell Test (e.g. Shocking Touch):**
  - [x] Player A casts a touch spell within melee range.
  - [x] Verify Player B sees the sparkle/touch impact effect at the point of contact.

---

### [x] Case 9: Supportive Magic Routing on Party Members
- [x] Have Player B take non-fatal damage (e.g. fall damage or enemy strike) so their health is reduced.
- [x] Player A equips a healing spell (e.g. *Heal*, *Cure Poison*, or *Balyna's Antidote*).
- [x] Player A approaches Player B and casts the spell targeting Player B.
- [x] **Verify:**
  - [x] Player B receives the healing effect bundle.
  - [x] Player B's health bar increases by the spell amount.
  - [x] Player B sees the healing visual effect/sparkles on their HUD or screen.

---

### [x] Case 10: Offensive Magic Routing & PvP Friendly Fire Toggle (`net_pvp`)
- [x] **Step 1: Friendly Fire Disabled (`net_pvp off`):**
  - [x] Host runs `net_pvp off` in console (or checks DM overlay).
  - [x] Player A casts an offensive ranged spell (e.g. Fireball) directly at Player B.
  - [x] Projectile impacts Player B:
    - [x] Impact particles and explosion play.
    - [x] **Verify:** Player B takes **0** damage; health remains intact.
- [x] **Step 2: Friendly Fire Enabled (`net_pvp on`):**
  - [x] Host runs `net_pvp on` in console.
  - [x] Player A casts Fireball at Player B.
  - [x] **Verify:**
    - [x] Player B takes damage according to spell formula and magic resistance.
    - [x] Player B's health decreases and hurt sound plays.
    - [x] Player B's health bar updates across network party status.

---

### [x] Case 11: Wandering Town Civilian NPCs Synchronization
- [x] Host and Client fast travel to the same town (e.g. Daggerfall city or Gothway Garden) during daytime.
- [x] Both players meet at a central street or town square.
- [x] Observe the wandering civilian NPCs (`MobilePersonNPC`) spawned in the streets:
  - [x] **Verify:** Host and Client see the exact same civilian NPCs (same gender, same race/clothing billboard).
  - [x] **Verify:** Civilian NPC positions and walking directions are synchronized in real-time.
  - [x] **Verify:** Neither player sees "phantom" civilians that only exist on their own screen.
  - [x] Left-click on a civilian NPC: verify dialogue window opens normally and overhead chat bubble appears above them for nearby party members.

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
| **READY FOR TESTING** | **11** | Cases 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 |

---

## Controls & Interaction Reference

- **Avatar Proximity Interaction:** Walk within 3.5m of another player's puppet avatar and left-click on them.
- **Player Interaction Menu:**
  - `Compartir Mapa y Descubrimientos`: Transfers discovered towns and interior buildings to partner's automap.
  - `Iniciar Comercio Bilateral (Trade)`: Sends trade invitation.
  - `Cerrar`: Dismisses menu.
- **Escape Key (`Esc`):** Cancels active trade session or closes prompt.
- **Distance Safeguard:** If players separate > 6.0 meters, trade auto-cancels with HUD notification.
- **PvP Command:** In game console (`~`), run `net_pvp on` or `net_pvp off` (Host only) or toggle in DM Spawner overlay (`Ctrl + F11`).

---

## Detailed Test Cases & Checklists

### [ ] Case 1: In-World Trade Invitation & Response Flow
- [ ] Position Host and Client within 3.5 meters of each other.
- [ ] Player A clicks on Player B's avatar puppet.
- [ ] Interaction menu opens showing Player B's name, class, and distance.
- [ ] Player A clicks **`Iniciar Comercio Bilateral (Trade)`**.
- [ ] Player A receives HUD notice: `[Comercio] Solicitud enviada a [Nombre]. Esperando respuesta...`.
- [ ] Player B receives popup prompt: `[Solicitud de Comercio]` with 25s countdown timer.
- [ ] **Acceptance test:** Player B clicks **`Aceptar`**.
- [ ] Both players see the 2-column Bilateral Trade Window open simultaneously.
- [ ] **Decline test:** Repeat invite and click **`Rechazar`**; verify Player A is notified and no trade window opens.

---

### [ ] Case 2: Gold Offering & Live Real-Time Preview
- [ ] With trade window open, Player A clicks gold buttons (`+10`, `+100`, `+1000`, `Max`, `0`).
- [ ] Player A's gold offer updates under **Tu Oferta**.
- [ ] In real-time on Player B's screen, the right column (**Oferta de [Nombre]**) reflects the exact same gold amount.
- [ ] Verify gold cannot exceed player's available gold (`PlayerEntity.GoldPieces`).

---

### [ ] Case 3: Inventory Item Picker & Stack Transfer
- [ ] Under **Tu Oferta**, Player A clicks **`▼ + Agregar Ítems del Inventario`**.
- [ ] Sub-scrollview displays unequipped, non-quest inventory items with name, weight, and stack count.
- [ ] Player A clicks an item (e.g. an Iron Dagger, potion, or arrows):
  - [ ] Item moves to Player A's offered list.
  - [ ] Total weight updates.
  - [ ] Player B immediately sees the item appear on the right column with weight and stack count.
- [ ] Player A clicks **`X`** next to the offered item:
  - [ ] Item is removed from the offer.
  - [ ] Item disappears from Player B's view in real-time.

---

### [ ] Case 4: Offer Lock & Anti-Scam Protection
- [ ] Player A sets gold and adds an item, then clicks **`Bloquear Oferta`**.
  - [ ] Status updates to: `<color=#88FF88>[x] OFERTA BLOQUEADA</color>`.
  - [ ] On Player B's screen, Player A's status shows: `[x] OFERTA BLOQUEADA`.
- [ ] **Anti-Scam Verification:**
  - [ ] Player B locks their offer as well.
  - [ ] Player B clicks **`Aceptar Intercambio`**.
  - [ ] Player A clicks **`Desbloquear Oferta`** and changes an item or gold.
  - [ ] **Verify:** Player B's acceptance is immediately cleared to prevent stealth modifications.

---

### [ ] Case 5: Bilateral Mutual Accept & Authoritative Swap Execution
- [ ] Both players lock their offers (`Bloquear Oferta`).
- [ ] Both players click **`Aceptar Intercambio`**.
- [ ] Server receives both commitments, executes atomic swap:
  - [ ] Player A's offered items & gold are removed from Player A and added to Player B.
  - [ ] Player B's offered items & gold are removed from Player B and added to Player A.
- [ ] Both players receive HUD text: `[Comercio] ¡Intercambio completado con éxito!`.
- [ ] Trade window closes cleanly on both screens.
- [ ] Both players open inventory (`F1`): verify received items and updated gold balance.

---

### [ ] Case 6: Distance & Cancellation Safeguards
- [ ] Open trade window between Host and Client.
- [ ] One player walks away > 6.0 meters.
- [ ] Trade window closes immediately with notification: `[Comercio] Cancelado: El compañero se alejó demasiado.`.
- [ ] Reopen trade window and press **`Esc`**: window closes cleanly and partner is notified without items lost.

---

### [ ] Case 7: In-World Map Discovery Sharing
- [ ] Have Player A discover multiple shops, taverns, or guilds in town (automap shows labeled buildings).
- [ ] Player A approaches Player B (< 3.5m) and left-clicks Player B's avatar.
- [ ] Player A clicks **`Compartir Mapa y Descubrimientos`**.
- [ ] Player A receives HUD notice: `[Mapa] Descubrimientos compartidos con [Nombre] (X ubicaciones, Y edificios).`.
- [ ] Player B receives HUD notice: `[Mapa] ¡[Nombre] compartió sus descubrimientos contigo! (X ubicaciones, Y edificios agregados).`.
- [ ] Player B opens local automap (`M`) and exterior automap:
  - [ ] Verify newly shared shops, guilds, and temples now appear discovered with correct building names and colors on Player B's map.

---

### [ ] Case 8: Spell Effect & Projectile Replication (Touch, Missile, AOE)
- [ ] Position Host and Client within line-of-sight in an outdoor or dungeon area.
- [ ] **Missile Spell Test (e.g. Fireball, Wizard's Fire, Shock):**
  - [ ] Player A casts a ranged missile spell.
  - [ ] Verify Player B sees Player A's puppet play the casting animation.
  - [ ] Verify Player B hears the element casting sound originating from Player A.
  - [ ] Verify Player B sees the 3D projectile fly through the air matching Player A's trajectory.
  - [ ] When projectile strikes a surface or entity, verify Player B sees the impact explosion billboard/particles and hears the impact sound.
- [ ] **Touch Spell Test (e.g. Shocking Touch):**
  - [ ] Player A casts a touch spell within melee range.
  - [ ] Verify Player B sees the sparkle/touch impact effect at the point of contact.

---

### [ ] Case 9: Supportive Magic Routing on Party Members
- [ ] Have Player B take non-fatal damage (e.g. fall damage or enemy strike) so their health is reduced.
- [ ] Player A equips a healing spell (e.g. *Heal*, *Cure Poison*, or *Balyna's Antidote*).
- [ ] Player A approaches Player B and casts the spell targeting Player B.
- [ ] **Verify:**
  - [ ] Player B receives the healing effect bundle.
  - [ ] Player B's health bar increases by the spell amount.
  - [ ] Player B sees the healing visual effect/sparkles on their HUD or screen.

---

### [ ] Case 10: Offensive Magic Routing & PvP Friendly Fire Toggle (`net_pvp`)
- [ ] **Step 1: Friendly Fire Disabled (`net_pvp off`):**
  - [ ] Host runs `net_pvp off` in console (or checks DM overlay).
  - [ ] Player A casts an offensive ranged spell (e.g. Fireball) directly at Player B.
  - [ ] Projectile impacts Player B:
    - [ ] Impact particles and explosion play.
    - [ ] **Verify:** Player B takes **0** damage; health remains intact.
- [ ] **Step 2: Friendly Fire Enabled (`net_pvp on`):**
  - [ ] Host runs `net_pvp on` in console.
  - [ ] Player A casts Fireball at Player B.
  - [ ] **Verify:**
    - [ ] Player B takes damage according to spell formula and magic resistance.
    - [ ] Player B's health decreases and hurt sound plays.
    - [ ] Player B's health bar updates across network party status.

---

### [ ] Case 11: Wandering Town Civilian NPCs Synchronization
- [ ] Host and Client fast travel to the same town (e.g. Daggerfall city or Gothway Garden) during daytime.
- [ ] Both players meet at a central street or town square.
- [ ] Observe the wandering civilian NPCs (`MobilePersonNPC`) spawned in the streets:
  - [ ] **Verify:** Host and Client see the exact same civilian NPCs (same gender, same race/clothing billboard).
  - [ ] **Verify:** Civilian NPC positions and walking directions are synchronized in real-time.
  - [ ] **Verify:** Neither player sees "phantom" civilians that only exist on their own screen.
  - [ ] Left-click on a civilian NPC: verify dialogue window opens normally and overhead chat bubble appears above them for nearby party members.

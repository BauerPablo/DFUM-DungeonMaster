# Manual Testing Guide: Player Interaction & Bilateral Trade (Phase 11)

## Overview
This testing guide covers the **Bilateral Player-to-Player Trading System** and in-world character interactions introduced in Phase 11.

---

## Testing Status Dashboard

| Status | Total Cases | Cases |
| :--- | :---: | :--- |
| **READY FOR TESTING** | **6** | Cases 1, 2, 3, 4, 5, 6 |

---

## Controls & Interaction Reference

- **Raycast Activation:** Walk within 3.5m of another player's puppet avatar and left-click on them.
- **Player Interaction Menu:**
  - `Compartir Mapa y Descubrimientos`: Transfers discovered towns and buildings.
  - `Iniciar Comercio Bilateral (Trade)`: Sends trade invitation.
  - `Cerrar`: Dismisses menu.
- **Escape Key (`Esc`):** Cancels active trade session or closes prompt.
- **Distance Safeguard:** If players separate > 6.0 meters, trade auto-cancels with HUD notification.

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

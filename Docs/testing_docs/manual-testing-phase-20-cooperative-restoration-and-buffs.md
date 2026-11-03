# Manual Testing Guide: Cooperative Party Restoration, Shared Buffs & Non-Magical First Aid (Phase 20)

## Overview
This testing guide covers all features introduced in **Phase 20 (v0.9.20)** of the DFUM TTRPG Master Suite:
1. **Party Smart-Support Spell Adapter:**
   - Overcomes classic DFU's `TargetTypes.CasterOnly` constraint for restoration, curing, and buff spells.
   - When aiming crosshair directly at an allied party member (`NativePlayerPuppet`) within < 2.0m, beneficial spells are intelligently re-routed to the targeted ally over Mirror network RPCs (`MsgApplySpellBundle`).
   - Standard self-casting is 100% preserved when not aiming at an ally.
   - Works seamlessly on unconscious / downed allies at 0 HP, authoritatively reviving them when health rises above 0 (fulfilling **Case 4 from Phase 19**).
2. **Area of Effect (AoE) Aura Synchronization:**
   - Spells configured with `TargetTypes.AreaAroundCaster` or `TargetTypes.AreaAtRange` intelligently broadcast to all party puppets within the radius (~15m).
   - Beneficial effects (Group Heal, Mass Cure, Protection) apply to the entire group.
   - Harmful effects respect the `net_pvp` friendly fire toggle (allies are immune if PvP is disabled).
3. **Non-Magical First Aid & Consumable Injection (For Non-Caster Classes):**
   - In the Player Interaction Menu ('**F**' proximity interaction), players can directly administer beneficial potions from inventory (Healing, Stamina/Vigor, Antidotes) to live or downed allies.
   - Consumes exactly 1 potion from the stack (`RemoveOne`) and delivers the effect via `MsgPartySupportAction`.
4. **Field Medic (Medical Skill):**
   - Characters with Medical skill >= 15% can apply Field Medic treatment to restore +15 HP and +35 Fatigue with a 30-second tactical cooldown.

---

## Testing Status Dashboard

| Status | Total Cases | Cases | Result |
| :--- | :---: | :---: | :---: |
| **READY TO TEST (Phase 20)** | **7** | Cases 1–7 | **Implemented & Awaiting In-Game Verification** |

---

## Detailed Test Cases & Step-by-Step Instructions

### [ ] Case 1: Targeted Healing Spell Resuscitation on Downed Ally (Phase 19 Case 4 Resolution)
- [ ] **Setup:**
  - Client 1 is in Downed state (unconscious, 0 HP, red vignette, bleedout timer ticking).
  - Client 2 is a spellcaster possessing a healing spell (e.g. *Heal*, *Balm of Healing*).
- [ ] **Execution:**
  - Client 2 aims crosshair at Client 1's prone billboard on the floor (< 2.0 metros).
  - Client 2 casts the healing spell.
- [ ] **Verification:**
  - [ ] Client 2's HUD displays: `[Soporte] ¡Has lanzado '{SpellName}' sobre {Client1Name}!`.
  - [ ] Client 2's chat logs: `[Party] {Client2Name} lanzó '{SpellName}' sobre {Client1Name}.`.
  - [ ] Client 1 receives `MsgApplySpellBundle`:
    - Health increases above 0 HP (by the spell's rolled magnitude).
    - Client 1 instantly awakens from the Downed state: camera ascends to eye height, vignette clears, heartbeat stops, and controls are restored.
    - Remote puppet stands back up across all peers.
    - HUD confirms: `¡Has sido reanimado por Magia / Curación! (+{HP} HP)`.

---

### [ ] Case 2: Smart-Support Targeted Buff on Live Ally
- [ ] **Setup:**
  - Both Client 1 and Client 2 are alive and in normal state.
  - Client 2 readies a beneficial spell with `TargetTypes.CasterOnly` or touch (e.g. *Levitate*, *Invisibility*, *Water Walking*, *Resist Fire*, or a cure spell).
- [ ] **Execution:**
  - Client 2 aims crosshair directly at Client 1 (< 2.0m) and releases the spell.
- [ ] **Verification:**
  - [ ] Client 2's Magicka is deducted for the cast cost, and spellcast animation/sound plays.
  - [ ] The spell DOES NOT apply to Client 2; it is successfully redirected to Client 1.
  - [ ] Client 1's screen displays: `[Magia] {Client2Name} aplicó sobre vos: {SpellName}`.
  - [ ] Client 1 receives the buff/effect in their `PlayerEffectManager` (e.g. levitation starts, invisibility triggers).

---

### [ ] Case 3: Classic Self-Casting Preservation (Zero Regression)
- [ ] **Setup:**
  - Client 2 possesses the same *Heal* or *Buff* spell.
  - Client 2 aims away from all players (into open sky or empty dungeon corridor).
- [ ] **Execution:**
  - Client 2 casts the spell.
- [ ] **Verification:**
  - [ ] Because no allied puppet is in the crosshair, the Smart-Support adapter does not intercept.
  - [ ] The spell applies directly to Client 2 (self-cast) exactly as in classic single-player DFU.

---

### [ ] Case 4: Area of Effect (AoE) Party Aura (`AreaAroundCaster`)
- [ ] **Setup:**
  - Host and Clients stand grouped together in a room (< 15 meters apart).
  - One player readies a beneficial AoE spell (e.g. custom spell made in Spellmaker with `Target: Area Around Caster` or *Mass Healing*).
- [ ] **Execution:**
  - Cast the AoE spell.
- [ ] **Verification:**
  - [ ] Caster receives HUD notification: `[Aura de Grupo] ¡Imbuiste a {Count} aliados con {SpellName}!`.
  - [ ] Party chat displays: `[Party] {CasterName} activó un aura de grupo: '{SpellName}' ({Count} aliados afectados).`.
  - [ ] All party members within 15 meters receive the spell payload bundle simultaneously.

---

### [ ] Case 5: Friendly Fire Protection & Harmful Spell Handling
- [ ] **Setup:**
  - Ensure `net_pvp` is disabled (default).
  - Client 2 casts a harmful AoE or missile spell (e.g. *Fireball*, *Shock*) hitting Client 1 or exploding in their radius.
- [ ] **Verification:**
  - [ ] Client 1 takes **NO** damage and negative effects do not apply.
  - [ ] Client 2's HUD indicates: `[PvP] Fuego amigo desactivado (net_pvp está en OFF).`.
  - [ ] If Host/DM enables PvP (`net_pvp 1`), verify spells can damage party members as intended.

---

### [ ] Case 6: Non-Magical Consumable Injection via Interaction Menu ('F')
- [ ] **Setup:**
  - Client 2 (Warrior / Rogue without spells) has a Healing Potion, Stamina Potion, or Antidote in inventory.
  - Client 1 has taken physical damage or lost fatigue.
- [ ] **Execution:**
  - Client 2 approaches Client 1 (< 2.5 meters) and presses **'F'**.
  - Interaction menu displays buttons for available consumables:
    `✦ Aplicar Poción: {PotionName} ✦ (+40 HP)`
    `✦ Aplicar Poción: {StaminaPotionName} ✦ (+50 Fatiga)`
  - Client 2 clicks one of the support potion buttons.
- [ ] **Verification:**
  - [ ] Client 2 loses exactly 1 potion from inventory (stack count decrements by 1; stack is not wiped out).
  - [ ] Client 1 receives HUD notification: `[Soporte] ¡{Client2Name} te administró una poción de curación! (+40 HP)`.
  - [ ] Client 1's health/fatigue increases, and potion drink sound plays.
  - [ ] Party chat logs: `[Party] {Client2Name} aplicó Poción ({PotionName}) a {Client1Name}.`.

---

### [ ] Case 7: Field Medic Skill Action
- [ ] **Setup:**
  - Client 2 has a character with Medical skill >= 15%.
  - Approach Client 1 and press **'F'**.
- [ ] **Execution:**
  - Click `✦ Primeros Auxilios (Skill Médico: {Skill}%) ✦ (+15 HP, +35 Fatiga)`.
- [ ] **Verification:**
  - [ ] Client 1 gains +15 HP and +35 Fatigue.
  - [ ] Client 2's button enters a 30-second cooldown: `(Espera {Seconds}s)`.
  - [ ] Attempting to click before the cooldown expires is disabled until the timer elapses.

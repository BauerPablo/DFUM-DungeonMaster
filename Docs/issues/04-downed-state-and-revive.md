# Issue 4: Downed Player State and Party Revive Mechanism

**Title:** `feat(combat): Downed Player State and Party Revive Mechanism`  
**Labels:** `enhancement`, `combat`, `ttrpg`

## Problem Description
In vanilla DFU, reaching 0 health triggers immediate character death, loading the save game screen. In a multiplayer co-op / TTRPG session, a single player's death shouldn't abruptly force a reload or leave that player out of the game.

## Proposed Solution
1. **Downed State:**
   - When player health reaches 0 while in a multiplayer session, instead of invoking `EntityDeath`, set health to 1, apply crawl/ragdoll animation on the puppet, freeze normal standing motor, and start a 60-second bleedout countdown.
2. **Party Revive:**
   - An ally can approach the downed player and hold `E` (interact) for 4 seconds to stabilize/revive them, restoring 15% health.
3. **Tabletop Death Saves (Optional DM rule):**
   - Alternatively, allow the player to roll death saving throws (d20 >= 10 is success, 3 successes to stabilize, 3 failures to perma-die).

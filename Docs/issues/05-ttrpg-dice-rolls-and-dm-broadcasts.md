# Issue 5: Shared Dice Rolls and DM Narrative Broadcasts

**Title:** `feat(ttrpg): Shared Dice Rolls and DM Narrative Broadcasts`  
**Labels:** `enhancement`, `ttrpg`, `ui`

## Problem Description
To truly function as a Tabletop RPG Virtual Tabletop (TTRPG), players and DMs need transparent dice roll mechanics (skill checks, saving throws, attack rolls) and the DM needs a way to narrate scenes without typing through standard speech bubbles.

## Proposed Solution
1. **Slash Command Dice Rolling:**
   - Command `/roll <count>d<sides>+<modifier>` (e.g. `/roll 1d20+4`, `/roll 2d6`).
   - Broadcasts outcome to all party members in gold text: `[Dice] <Player> rolled 1d20+4: [17] + 4 = 21`.
2. **DM Narrative Banner:**
   - Button in `Ctrl + F11` or command `dm_announce <text>`.
   - Displays a cinematic title banner in the center of all players' screens with dramatic font and chime audio, ideal for scene introductions and DM storytelling.

# Issue 3: Synchronize Door Opening and Closing Across Clients

**Title:** `fix(world): Synchronize Door Opening and Closing Across Clients`  
**Labels:** `bug`, `world`, `networking`

## Problem Description
When a player clicks on an interactive door (`DaggerfallActionDoor`) in town or inside buildings/dungeons, the door animates open or closed locally. Other connected players in the vicinity do not see the door move and can walk through what appears to them as a closed door (or get blocked visually).

## Proposed Solution
1. **Network Message (`MsgDoorState`):**
   - Transmit `loadID` or unique scene path of the clicked door, and target state (`IsOpen`).
2. **Door Action Hook:**
   - In `DaggerfallActionDoor.ToggleDoor()`, broadcast `MsgDoorState` when online.
   - Remote clients receiving `MsgDoorState` trigger `SetDoorState()` smoothly matching the network state without audio duplication if far away.

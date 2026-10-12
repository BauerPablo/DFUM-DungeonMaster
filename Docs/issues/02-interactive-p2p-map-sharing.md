# Issue 2: P2P Map Exploration Sharing via Character Click

**Title:** `feat(maps): P2P Map Exploration Sharing via Character Click`  
**Labels:** `enhancement`, `world`, `ui`

## Problem Description
Exploration maps (automap in dungeons and exterior town discovery in `PlayerGPS`) are strictly stored per-client. When one player discovers a shop, guildhall, or dungeon path, other party members still see it undiscovered in the dark.

## Proposed Solution
1. **Character Click Interaction:** When aiming and clicking (or holding `E`) on a remote player avatar within 3 meters, open a radial/contextual prompt with options (e.g. `Share Map`, `Trade`).
2. **Fog of War & Discovery Replication:**
   - Send `MsgShareMap` containing discovered buildings (`discoveredBuildings` list) in the current town, or automap geometry bitmasks if inside a dungeon.
   - The recipient player's map immediately updates with the sender's discoveries.
   - Show HUD notification: `[Map] <PlayerName> shared their map discoveries with you!`.

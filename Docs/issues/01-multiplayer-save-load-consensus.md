# Issue 1: Multiplayer Save and Load Consensus System

**Title:** `feat(save-load): Multiplayer Save and Load Consensus System`  
**Labels:** `enhancement`, `save-load`, `multiplayer`

## Problem Description
Currently, Daggerfall Unity saving and loading (`SaveLoadManager`) operates purely in single-player mode. When the host or a client loads a saved game, there is no protocol defining how other connected peers synchronize or if they must agree to reload a previous state.

## Proposed Solution
1. **Host Save Broadcast:** When the Host saves the game, broadcast a snapshot of world state (world time, active party quests, weather, seed). Clients locally save their own inventory and character progress.
2. **Load Consensus Voting:** When the Host attempts to load a saved game while clients are connected:
   - A Parchment voting prompt appears for all connected players: *"The DM is loading a save game: [SaveName]. Accept reload?"*
   - Upon consensus approval, all clients transition cleanly into the loaded world coordinates and synchronize state.
3. **Save Game Compatibility:** Maintain backward compatibility with standard DFU `.txt` / `.json` save structures.

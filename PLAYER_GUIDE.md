# DFUM: Dungeon Master — Player Quick Start Guide

Welcome, adventurer! This guide will get you connected to a **DFUM: Dungeon Master** cooperative session in just a few minutes.

---

## 🛠️ Step 1: Requirements

1. **DFUM Game Build:** Download the latest compiled game release provided by your Dungeon Master / Host.
2. **Daggerfall Game Data:** Daggerfall's original game assets are free. If you don't already have them, download the Daggerfall Game Files from [Daggerfall Workshop](https://www.dfworkshop.net/) and unzip them into a folder (e.g. `C:\Games\Daggerfall`).

---

## 🎮 Step 2: First Launch & Character Setup

1. Launch `DaggerfallUnity.exe`.
2. On the first launch, browse and point to your Daggerfall game data directory.
3. Click **Start**, then create your character (race, class, custom attributes) or load an existing adventurer.

---

## 🌐 Step 3: Joining the Host (LAN & Radmin VPN)

DFUM supports peer-to-peer multiplayer via local LAN or virtual LAN (Radmin VPN):

### Option A: Via Radmin VPN (Recommended for Internet Play)
1. Download and install [Radmin VPN](https://www.radmin-vpn.com/) (free, no registration).
2. **Host:** Click **Network -> Create New Network**, choose a name and password, and share them with the party.
3. **Clients:** Click **Network -> Join an existing network**, enter the Host's network credentials.
4. **Clients:** In Radmin VPN, right-click the Host's computer name and click **Copy IP Address** (an address starting with `26.x.x.x`).

### Option B: Via Local LAN (Same Wi-Fi / Router)
1. **Host:** Open Command Prompt (`cmd`) and type `ipconfig` to find your IPv4 Address (e.g. `192.168.1.X`).
2. **Clients:** Use that local IPv4 Address to connect.

### In-Game Connection Steps:
1. Once inside the game world, press **`Ctrl + F10`** (or press `~` and type `net_gui`).
2. In the **DFU Native Multiplayer** window:
   - **IP:** Enter the Host's IP (Radmin `26.x.x.x` or LAN `192.168.1.X`).
   - **Port:** Keep default `7777`.
3. Click **Connect as Client**.
4. You will see a green status message: `Connected as Client` and instantly synchronize with the Host's session!

---

## ⚔️ Player Controls & Co-op Features

- **Quest Proposals:** When the DM or another player proposes a quest, you will see a parchment pop-up window with a 60-second timer. Click **Yes** to join the adventure, or **No** to decline.
- **Tavern Rest & Healing:** When the party rests or loiters at a tavern, you will enter a shared blackout screen and automatically recover health, fatigue, and magicka based on Daggerfall formulas.
- **Speech Bubbles:** Talk to NPCs or other players to see overhead 3D speech bubbles that scale with distance.
- **Shared Looting:** Defeated monsters spawn corpses visible to everyone. Multiple players can inspect containers and take loot simultaneously.

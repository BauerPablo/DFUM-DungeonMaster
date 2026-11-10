# DFU Native Multiplayer: Console & DM Command Reference

This document provides a comprehensive reference for all in-game console commands, graphical overlays, and keyboard shortcuts implemented in the Daggerfall Unity Native Multiplayer system.

To open the in-game console, press the console hotkey (`~` or backtick `` ` `` / `\` depending on your keyboard layout).

---

## Quick Reference Table

| Command | Shortcut | Target Role | Description |
| :--- | :---: | :---: | :--- |
| [`net_host`](#net_host) | — | Host | Starts an authoritative server and local host client on KCP transport. |
| [`net_join`](#net_join) | — | Client | Connects to a remote host server as a pure client. |
| [`net_disconnect`](#net_disconnect) | — | Any | Gracefully shuts down active session and clears proxies. |
| [`net_status`](#net_status) | — | Any | Displays active session state, role flags, and entity count. |
| [`net_entities`](#net_entities) | — | Any | Lists all registered network entities and their proxy/authoritative status. |
| [`net_gui`](#net_gui) | `Ctrl + F10` | Any | Toggles the on-screen Network Debug GUI window. |
| [`dm_gui`](#dm_gui) | `Ctrl + F11` | Host / DM | Toggles DM Tools GUI (Spawns, Teleport, Weather, Time, Invisibility, God Mode). |
| [`loot_gui`](#loot_gui) | `Ctrl + F12` | Host / DM | Toggles the on-screen DM Loot Tool GUI (containers and corpses). |
| [`quest_gui`](#quest_gui) / [`net_quest`](#net_quest) | `Ctrl + F7` | Any | Toggles Quest Creator & Inspector GUI and party consensus voting. |
| [`dm_spawn`](#dm_spawn) | — | Host / DM | Spawns an authoritative enemy 2 meters in front of the player. |
| [`dm_despawn`](#dm_despawn) | — | Host / DM | Despawns the last spawned enemy (or all) and broadcasts cleanup. |
| [`gather_party`](#gather_party) | — | Host / DM | Authoritatively gathers all party members to the host's position/building. |
| [`set_time`](#set_time) | — | Host / DM | Authoritatively sets world time (0-23 hours, 0-59 minutes). |
| [`set_weather`](#set_weather) | — | Host / DM | Authoritatively changes world weather across all peers. |
| [`timescale`](#timescale) | — | Host / DM | Adjusts world time progression speed (0x to 60x). |
| [`net_log`](#net_log) | — | Any | Configures selective logging filters for diagnostics. |
| [`dm_invis`](#dm_invis) | — | Any | Toggles DM True Invisibility (pacifies AI, bypasses crime, hides puppet). |
| [`dm_god`](#dm_god) | — | Any | Toggles DM God Mode (invincibility, unlimited health/fatigue/magicka). |
| [`dm_freecam`](#dm_freecam) / `freecam` | `Ctrl + F8` | Any | Toggles 6-DOF Eagle Eye flight mode with local 3rd-person puppet. |
| [`dm_tp`](#dm_tp) | — | Any | Teleports player to coordinates, quest objective, or quest giver. |

---

## 1. Network Session Commands

### `net_host`
* **Syntax:** `net_host [port]`
* **Default Port:** `7777`
* **Authority:** Host (Server + Local Player)
* **Description:** Binds KCP transport to the specified UDP port, starts authoritative Mirror server, enables authoritative dungeon and world spawners, registers pre-existing scene entities, and connects local host player.
* **Example:** `net_host 7777`

### `net_join`
* **Syntax:** `net_join <ip> [port]`
* **Default Port:** `7777`
* **Authority:** Client
* **Description:** Connects to a running DFU host session. Automatically disables local dungeon and world spawning, purges pre-existing offline local enemies to prevent duplicates, and requests state synchronization from host.
* **Example:** `net_join 127.0.0.1 7777`

### `net_disconnect`
* **Syntax:** `net_disconnect`
* **Authority:** Any
* **Description:** Disconnects active session. If on Host, stops server and disconnects peers. If on Client, destroys all proxies and returns safely to Single Player.

### `net_status`
* **Syntax:** `net_status`
* **Authority:** Any
* **Description:** Prints detailed diagnostic information regarding session state, peer connections, and authority flags.

### `net_entities`
* **Syntax:** `net_entities`
* **Authority:** Any
* **Description:** Dumps all tracked network entities in `DFUNetworkRegistry`. Shows GUID, GameObject name, and whether Authoritative or Proxy.

---

## 2. In-Game GUI Overlays & Hotkeys

### `net_gui` (Hotkey: `Ctrl + F10`)
* **Syntax:** `net_gui`
* **Description:** Toggles draggable on-screen GUI window for connection management, role status, and logging filter toggles.

### `dm_gui` (Hotkey: `Ctrl + F11`)
* **Syntax:** `dm_gui`
* **Description:** Toggles **Dungeon Master (DM) Tool** overlay for testing dynamic spawning, world time, weather, teleportation, invisibility, and god mode.
* **Features:**
  * **Role Guard:** Displays warning box on pure clients.
  * **World Time:** Quick buttons for Dawn (06:00), Noon (12:00), Dusk (18:00), Midnight (00:00), and Timescales (Pause, 1x, 12x, 60x).
  * **Weather Control:** Instant buttons for Sunny, Cloudy, Overcast, Fog, Rain, Thunder, Snow.
  * **Enemy Spawner:** Monster carousel and 2m front spawn button, plus quick spawns (Rat, Bat, Skeleton, Thief, Zombie, Bear).
  * **Authoritative Despawn:** Despawn Last and Despawn All buttons.
  * **DM Teleportation:** Input fields for Map X/Y, `TP Me`, `Warp Party`, and quick teleport to active quest objective.
  * **DM Observer & Tools:**
    * **DM Invisibility:** Toggle true invisibility on/off.
    * **God Mode (Invincible):** Toggle invincible mode on/off.
    * **Eagle Eye / FreeCam:** Toggle 6-DOF flight camera on/off.
    * **Gather Party to Host:** Instantly teleport all party members to host's location.

### `loot_gui` (Hotkey: `Ctrl + F12`)
* **Syntax:** `loot_gui`
* **Description:** Toggles DM Loot Tool overlay for authoritative loot container creation and testing.

### `quest_gui` (Hotkey: `Ctrl + F7`)
* **Syntax:** `quest_gui` or `net_quest menu`
* **Description:** Toggles Quest Creator & Inspector window.
* **Features:**
  * **Section 1 (Local Quest Start):** Choose quest template, select faction from friendly dropdown/buttons, and start locally.
  * **Section 2 (Multiplayer Quest Offer):** Propose quest to party with 60-second opt-in consensus voting.
  * **Section 3 (Fast Travel & Settlement Browser):** Browse provinces, filter settlements with taverns, and initiate fast warp or party travel votes.
  * **Section 4 (Active Running Quests):** Inspect running quests with `[View]` button showing quest title, target dungeon/town coordinates, turn-in NPC details, and quick `TP to Objective` / `TP to Questor` test buttons.

---

## 3. Dungeon Master (DM) & Spawning Commands

### `dm_spawn`
* **Syntax:** `dm_spawn [enemyType]`
* **Default Type:** `Rat`
* **Authority:** Host / Server only
* **Description:** Spawns authoritative enemy 2 meters in front of the player, aligns to ground, registers into `DFUNetworkRegistry`, and broadcasts spawn to all peers.
* **Examples:** `dm_spawn Rat`, `dm_spawn SkeletalWarrior`, `dm_spawn Werewolf`

### `dm_despawn`
* **Syntax:** `dm_despawn [all]`
* **Authority:** Host / Server only
* **Description:** Destroys most recently spawned enemy on server (or all if passing `all`), unregisters GUID, and broadcasts cleanup to clients.

### `gather_party`
* **Syntax:** `gather_party`
* **Authority:** Host / Server only
* **Description:** Teleports all connected clients to host's position, synchronizing exterior/interior/dungeon cell context.

---

## 4. World Environment & Time Commands

### `set_time`
* **Syntax:** `set_time <hour> [minute]`
* **Authority:** Host / Server only
* **Description:** Authoritatively changes world time (0-23 hours, 0-59 minutes) and syncs immediately to all clients.
* **Examples:** `set_time 12`, `set_time 6 30`, `set_time 0 0`

### `set_weather`
* **Syntax:** `set_weather <weatherType>`
* **Authority:** Host / Server only
* **Description:** Changes world weather across all peers.
* **Supported Types:** `Sunny`, `Cloudy`, `Overcast`, `Fog`, `Rain`, `Thunder`, `Snow`
* **Example:** `set_weather Thunder`

### `timescale`
* **Syntax:** `timescale <scale>`
* **Authority:** Host / Server only
* **Description:** Sets time flow multiplier (0 = paused, 1 = realtime, 12 = default DFU, 60 = fast).
* **Example:** `timescale 12`

---

## 5. DM Observer, Navigation & God Mode Commands

### `dm_invis`
* **Syntax:** `dm_invis`
* **Authority:** Any
* **Description:** Toggles DM True Invisibility.
  * Hides player puppet on all remote clients.
  * Sets `NoTargetMode` and pacifies nearby enemy AI.
  * Suppresses crime reporting from town guards and citizens.
  * Hides local FreeCam puppet if active.

### `dm_god`
* **Syntax:** `dm_god`
* **Authority:** Any
* **Description:** Toggles DM God Mode (Invincibility).
  * Prevents all damage to health, fatigue, and magicka.
  * Allows unlimited casting and running without encumbrance limits.

### `dm_freecam` / `freecam` (Hotkey: `Ctrl + F8`)
* **Syntax:** `dm_freecam` or `freecam`
* **Authority:** Any
* **Description:** Detaches camera from player body into 6-DOF Eagle Eye flight mode.
* **Controls:**
  * **`W`**, **`A`**, **`S`**, **`D`**: Fly forward, left, backward, right.
  * **`Space`**: Fly straight up.
  * **`C`**: Fly straight down.
  * **`Shift`**: Sprint flight boost.
  * **Mouse Look**: Aim camera yaw and pitch freely.
  * **Mouse Scroll Wheel**: Dynamically scale flight speed (from 2 m/s to 150 m/s) with HUD notification.
  * **3rd-Person Puppet:** Renders your own character avatar standing in the world marked with `[YourName] (You)` (hidden if DM Invisibility is active).

### `dm_tp`
* **Syntax:**
  * `dm_tp <mapX> <mapY>`: Teleport locally to specific map pixel coordinates.
  * `dm_tp quest`: Teleport locally directly to active quest objective dungeon or town.
  * `dm_tp questor`: Teleport locally to active quest giver / turn-in NPC site.
* **Features:**
  * **High-Altitude Raycast (1500m):** Automatically finds ground surface and smoothly positions player ~1m above ground.
  * **Fall Damage Suppression:** Clears falling distance and zeroed motor during transition so player does not take lethal damage.
  * **Cell Safeguard:** Transitions out of interior/dungeon if moving to exterior coordinates.

---

## 6. Quest & Party Consensus Commands

### `net_quest`
* **Syntax:** `net_quest <subcommand> [args]`
* **Subcommands:**
  * `net_quest menu`: Opens Quest Creator GUI.
  * `net_quest list`: Lists active running quests with UIDs and factions.
  * `net_quest offer <questName> [factionId]`: Initiates party vote for specified quest (e.g. `net_quest offer M0B00Y00 41`).
  * `net_quest start <questName> [factionId]`: Starts quest locally on the caller.
  * `net_quest end <all|uid>`: Cancels and terminates running quest(s).

---

## 7. Diagnostics & Logging Commands

### `net_log`
* **Syntax:** `net_log <category> [on|off]`
* **Categories:** `all`, `combat`, `entities`, `loot`, `movement`, `network`, `quest`, `spawn`, `transitions`, `weather`
* **Description:** Enables or disables fine-grained debug logging in the Unity console.
* **Examples:**
  * `net_log quest on`: Shows detailed quest replication logs.
  * `net_log combat on`: Shows authoritative combat hit/damage logs.
  * `net_log movement off`: Quiets high-frequency transform streaming logs.

# Changelog

All notable changes to the **DFUM: Dungeon Master** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.9.14] - In Development (Phase 14)

### Planned
- **Dual-Mode DM Entity:** Seamless switching between Spectator Spirit (intangible, zero physics, invisible) and Physical Admin Avatar.
- **God Flight & Tactical Cameras:** Enhanced 6-DOF noclip flight controls, orthogonal/cenital top-down tactical battlefield camera with zoom.
- **Click-to-Teleport:** Shift+Click on world terrain to instantly move the DM or party members.
- **Parametric Nametags & Overhead Health Bars:** Configurable billboard tags for party members and enemies with role and distance falloff.

---

## [0.9.13] - 2026-10-16 (Phase 13)

### Added
- **Native Multiplayer Main Menu Hub (`DaggerfallMultiplayerWindow`):**
  - Integrated directly into Daggerfall's opening title screen with retro 320x200 pixel-virtual scaling.
  - Sub-menu tabs for Join (direct connect), Host (server setup & PvP settings), History (recent server auto-fill), and Mod Compatibility checking.
- **Pre-Game Multiplayer Lobby:**
  - Real-time player roster synchronization before entering the 3D world.
  - Live character selection: choose to create a brand new character via the classic wizard or select an existing saved character with full name, level, race, and class previews.
  - Host-to-client readiness and synchronized world launch.
- **Unified In-Game TTRPG Master Suite (`Ctrl + F11`):**
  - Consolidated all disparate debug windows into a unified tabbed suite with Dark Fantasy pixel-art framing.
  - 6 dedicated master tabs: *Red & Sesión*, *Spawns & NPCs*, *Mundo & Clima*, *Quests & Diálogos*, *Loot & Forja*, and *Party & DM*.
- **Role-Based DM Security Enforcement:**
  - Non-DM clients opening the Master Suite have full visibility of all tabs, but all DM-specific tabs are locked (`GUI.enabled = false` with `(DM)` indicators).
  - Clients can only interact with the *Red & Sesión* tab.
  - Keyboard shortcuts (`Ctrl+F7`, `Ctrl+F12`) are strictly restricted to the Host/DM.
- **Dark Fantasy Pixel-Art UI Aesthetic:**
  - Custom pixel-art title logo banner (*"The Elder Scrolls II: Daggerfall Multiplayer TTRPG"*) with dragon, dagger, and ribbon crest extracted with clean transparency.
  - Compact 120px start menu positioned to keep the campfire scene and adventurers completely visible.
  - Custom 9-sliced beveled borders, gold/crimson/emerald/iron button styles, and custom radio button textures.

### Fixed
- **Radio Button Horizontal Line:** Resolved an IMGUI 9-slice issue where radio button textures were stretched across label text, replacing it with a 28x18 texture with a transparent stretch region.
- **Quest Action Button Widths:** Widened *Cancel* (72px), *Complete* (84px), and *View/Hide* (52px) buttons in the Quests tab to prevent text truncation.
- **Client Auto-Movement & Puppet Collision:** Resolved continuous movement on client connect by making remote puppet colliders triggers, ignoring them in player motor controllers, and preventing self-puppet duplication.
- **Remote Puppet Dependency Error:** Resolved `Can't remove CharacterController because EnemyMotor depends on it` by stripping puppet components in reverse dependency order.
- **QuickLoad Shortcut Collision:** Prevented `Ctrl + F11` from unintentionally triggering DFU's default QuickLoad.
- **EnemyDeath NullReferenceException:** Added safe fallback for `EnemyMotor` references in enemy death logic.

---

## [0.9.12] - 2026-10-15 (Phase 12)

### Added
- **Core Standalone Decoupling:** Fully decoupled multiplayer systems from legacy TanguyMultiplayer mod dependencies, integrating them directly into native DFU engine namespaces.
- **Automated Standalone Build Helper:** Integrated build script for compiling production standalone clients with embedded multiplayer assets.

---

## [0.9.11] - 2026-10-14 (Phase 11)

### Added
- **Proximity Player Interaction & Bilateral Trading:**
  - Press `F` near other players to open the interaction menu (inspect profile, invite to trade, whisper).
  - Two-way synchronized trading window with offer validation, locking, and confirmation.

---

## [0.9.10] - 2026-10-13 (Phase 10)

### Added
- **Multiplayer Quest Consensus System:**
  - 60-second opt-in voting parchment popup for party quest offers.
  - Deterministic quest site, dungeon, and NPC turn-in synchronization.
  - Synchronized boss battle encounters and party-wide objective tracking.

---

## [0.9.9] - 2026-10-11 (Phase 9)

### Added
- **3D Overhead Speech Bubbles & Acoustic Dialogue:**
  - Dynamic floating text bubbles positioned over speaking NPCs and player puppets.
  - Natural volume/opacity falloff based on acoustic distance (< 3.5m full opacity, fading out past 16m).

---

## [0.9.8] - 2026-10-09 (Phase 8)

### Added
- **Authoritative Combat & Loot Synchronization:**
  - Server-authoritative AI targeting, damage calculation, and metal weapon hit immunities (silver/mithril/daedric).
  - Multi-target AI aggro across party members.
  - Synchronized corpse containers and concurrent inventory looting.

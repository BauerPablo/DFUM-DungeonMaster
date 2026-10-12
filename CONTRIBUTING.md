# Contributing to DFUM: Dungeon Master

Thank you for your interest in contributing to **DFUM: Dungeon Master**! This project aims to bring native, robust cooperative multiplayer and Tabletop RPG (TTRPG) mechanics to Daggerfall Unity.

---

## 🏗️ Architectural Guidelines

1. **Native Authoritative Architecture:**
   - The Server (Host) is authoritative over enemy spawning, AI targeting, damage calculation, world time, and weather.
   - Remote clients only render proxies and interpolate transforms (`NativeNetworkTransform`).
   - Never implement autonomous AI loops or enemy instantiation on pure clients without checking `NetworkSession.ShouldHandleSpawns`.

2. **Deterministic Elevation & Streaming:**
   - Terrain chunks in Daggerfall Unity generate dynamically on a 1000x500 pixel grid.
   - Any cross-pixel warp or teleport must ensure `AcrobatMotor.ClearFallingDamage()` is called and invoke `EnsurePlayerGroundedCoroutine()` with high-altitude raycasting to prevent lethal falling damage during streaming transitions.

3. **Cell Context Awareness:**
   - Game objects and enemy proxies must maintain awareness of their active cell: Exterior, Interior Building (`buildingKey`), or Dungeon Interior (`isInsideDungeon`).

---

## 💻 Code Standards & Conventions

- **Language:** Code, comments, identifiers, variable names, and git commits must always be in **English**.
- **Conventional Commits:** Use standard conventional commit prefixes (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`).
- **Clean Commits:** Never include AI attribution or automated co-author trailers in commit messages.
- **Null Safety:** Always defensively check singleton references (`GameManager.Instance`, `DaggerfallUI.Instance`, `PlayerEnterExit`, `PlayerGPS`).

---

## 🧪 Testing Checklist

Before opening a PR, ensure all manual testing cases pass according to [manual-testing-phase-10-quest-creator.md](Docs/testing_docs/manual-testing-phase-10-quest-creator.md):
- [ ] Compiles with 0 warnings/errors in Unity 2019.4 LTS.
- [ ] Host-to-Client transform replication remains smooth.
- [ ] No null reference exceptions when clients transition between exterior and buildings.
- [ ] Dialogue speech bubbles display properly without cluttering the HUD.

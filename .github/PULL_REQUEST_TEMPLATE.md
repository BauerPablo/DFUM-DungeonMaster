## Description
Briefly describe the purpose of this change and the problem it solves.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change adding new functionality)
- [ ] Refactor / Performance enhancement
- [ ] Documentation update

## Multiplayer Testing Checklist
- [ ] Tested in multi-instance environment (Host + Client).
- [ ] Verified that client enemy proxies and player puppets synchronize correctly.
- [ ] Cell transitions (Exterior <-> Interior <-> Dungeon) tested without void or elevation falls.
- [ ] Verified that authoritative server logic is guarded with `NetworkSession.ShouldHandleSpawns`.
- [ ] Follows conventional commits without AI attribution.

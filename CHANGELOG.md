# Changelog

All notable changes to this project will be documented in this file.

## [0.1.2] - 2026-04-21

### Added
- **Skeletal Hitbox System**: Implemented a 13-part compound collider system attached to the character's metarig bones (Head, Spine, Arms, Legs).
- **Physical Solidification**: Transitioned hitboxes from triggers to solid physical objects for realistic environmental interaction.

### Changed
- **Symmetric Mirroring**: Programmatically mirrored left-side limb colliders to the right side to ensure perfect physical symmetry.
- **Movement Speed Tuning**: Reduced movement speed to `3.0` to improve controllability while testing the new skeletal physics.
- **Legacy Cleanup**: Reorganized the skeletal hierarchy to remove obsolete trial colliders.

## [0.1.1] - 2026-04-21

### Changed
- **Control Remapping**: Migrated Grab/Throw functionality from the `F` key to the **Left Mouse Button (LMB)** for a more intuitive mouse-focused experience.
- **Physics Normalization**: Standardized throw forces to a range of `3.0` (Min) to `10.0` (Max).
- **Trajectory Update**: Adjusted throw direction to be flatter (reduced vertical lift) for better gameplay accuracy.

### Removed
- **Pull Mechanic**: Removed the "Pull" (E key) magnetic feature to focus on purely physical "Party Animals" style gameplay.

### Fixed
- **Inspector Overrides**: Resolved an issue where high serialized values in the Inspector (`300` - `1200`) were overriding code defaults, causing objects to fly too far.
- **Compilation Errors**: Fixed redundant variable definitions in `PlayerMovement.cs` and obsolete references in `GameplayTest.cs`.

## [0.1.0] - 2026-04-21

### Added
- **Input System Integration**: Traditional legacy input replaced with the new Unity Input System.
- **Automated Test Suite**: Added `GameplayTest.cs` and `TestRunner` to simulate player actions automatically.
- **Refactored Gameplay Logic**: Encapsulated `Punch`, `Grab`, and `Throw` into public methods to support external testing and AI calls.

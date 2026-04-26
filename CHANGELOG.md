# Changelog

All notable changes to this project will be documented in this file.

## [0.4.0] - 2026-04-25

### Fixed
- **Animation Stalling**: Removed incorrect `LateUpdate` snapping in `ActiveRagdollBone.cs` that was preventing the Animator from moving the master rig.
- **Movement Jitter**: Forced `RigidbodyInterpolation.Interpolate` on both the root Player capsule and physics rigidbodies to ensure smooth camera and character movement.
- **Physics Stability**: Tuned `ActiveRagdollBalance` tracker joint with higher damping (100) and spring (1500) to reduce balance oscillations.

### Improved
- **Automatic Slerp Configuration**: `ActiveRagdollBone` now automatically sets `RotationDriveMode.Slerp` and configures `JointDrive` parameters on `Start()`, reducing manual setup errors in the Inspector.
- **Auto Skin Re-mapping**: `ActiveRagdollInitialiser` now automatically rebinds the `SkinnedMeshRenderer` bones to the `physicRig` at runtime, ensuring the visual mesh follows the physical ragdoll.
- **Code Cleanup**: Simplified `ActiveRagdollBone` logic to focus purely on delta rotation following.


## [0.3.0] - 2026-04-23

### Added
- **Active Ragdoll System**: Full physics-driven character rig using `physicRig` with Rigidbody + ConfigurableJoint on each bone.
- **Dual-Rig Architecture**: `metarig` (animation driver) + `physicRig` (physics puppet) working in tandem.
- **ActiveRagdollBalance**: Virtual spring joint that tethers the physics spine to the Player capsule, keeping the character upright with tunable balance strength.
- **ActiveRagdollBone**: Per-bone script that reads `metarig` rotation and applies it to `physicRig` via SlerpDrive. Exposes `slerpDriveSpring` and `slerpDriveDamper` fields for per-bone Inspector tuning.
- **SkinTransferTool**: Editor utility (`Tools → Transfer Skin to Physics Rig`) that rebinds the character's `SkinnedMeshRenderer` from `metarig` to `physicRig` so all mesh deformation is physics-driven.
- **ActiveRagdollInitialiser**: Editor utility that automates setup — removes physics components from `metarig`, attaches `ActiveRagdollBalance`, and assigns the pelvis reference.
- **RagdollJointLimitTool**: Editor utility (`Tools → Set Ragdoll Joint Limits`) that sets angular joint limits on all `physicRig` bones based on their type (thigh, forearm, spine, head, etc.).
- **RagdollMirrorTool**: Editor utility that mirrors CapsuleCollider parameters from `.L` bones to corresponding `.R` bones.
- **Self-Collision Ignore**: `ActiveRagdollBalance` automatically disables collisions between the Player capsule and all `physicRig` bone colliders to prevent jitter.

### Changed
- **Jump Force**: `PlayerMovement` now dynamically calculates jump force based on the total mass of all `physicRig` Rigidbodies, ensuring consistent jump height regardless of ragdoll weight.
- **Physics Architecture**: Transitioned from single-capsule hitbox to full ragdoll physics simulation.

### Fixed
- **Bones Disappearing on Play**: Removed `pelvis.parent = null` from `ActiveRagdollBalance.Start()` which was unparenting the spine and causing all child bones to vanish from the `physicRig` hierarchy during play mode.
- **Physics Explosion**: Reduced `angularDrive.positionSpring` from 15,000 to a stable range to prevent NaN errors and bones flying to infinity on the first physics frame.

## [0.2.0] - 2026-04-22

### Added
- **Ragdoll Converter**: `RagdollConverter.cs` tool to convert Unity's auto-generated `CharacterJoint` components to `ConfigurableJoint` for finer control.
- **Collider Mirror Tool**: Programmatic mirroring of left-side collider sizes and offsets to right-side bones.

### Changed
- **Collider Setup**: Replaced auto-generated ragdoll colliders with manually tuned CapsuleColliders fitted to the panda model's actual geometry.

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

# Physics Battle Prototype

A physics-based action game prototype inspired by the mechanics of *Party Animals* and *Gang Beasts*. Features an **Active Ragdoll** character system where the character's body is fully physics-driven, reacting naturally to forces, impacts, and movement.

## 🎮 Controls

| Action | Input |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` |
| **Sprint** | `Left Shift` (Hold) |
| **Jump** | `Space` |
| **Punch** | `Right Mouse Button` |
| **Grab / Pick Up** | `Left Mouse Button` (Hold when near object) |
| **Charge & Throw** | `Left Mouse Button` (Hold to charge, Release to throw) |

## ✨ Features

- **Active Ragdoll System**: The character's physical body (`physicRig`) is fully driven by Unity physics (Rigidbody + ConfigurableJoint). The character mesh deforms based on real physics forces, not keyframe animation.
- **Dual-Rig Architecture**: Two skeletons exist simultaneously — `metarig` (animation driver) and `physicRig` (physics puppet). The `physicRig` follows the `metarig` via spring forces, creating naturally "soft" movement.
- **Physics Balance**: A virtual spring joint (`ActiveRagdollBalance`) tethers the physics rig to the player capsule, keeping the character upright. Balance strength is tunable — high values give stiff, upright posture; low values allow natural falling.
- **Automatic Skin Mapping**: The character mesh is automatically rebound from the animation rig to the physics rig at runtime by `ActiveRagdollInitialiser`, ensuring the visual mesh follows the physical puppet perfectly.
- **Interpolation & Smoothness**: Root and bone rigidbodies are set to `Interpolate` mode, eliminating physics jitter during movement.
- **Per-Bone Spring Tuning**: Each ragdoll bone has an `ActiveRagdollBone` component with exposed `slerpDriveSpring` and `slerpDriveDamper` values, allowing precise per-limb tuning in the Inspector.
- **Physics-Based Combat**: All punches and interactions use Unity Physics (`AddForce`/`Impulse`) for dynamic behavior.
- **Grab & Throw System**: Context-sensitive grabbing that allows players to pick up objects or opponents and throw them with variable force.

- **Automated Testing**: Built-in test runner (`GameplayTest.cs`) to verify physics stability and input responsiveness.

## 🏗 Architecture

```
Player (Capsule Collider + Rigidbody + PlayerMovement)
└── panda
    ├── meshes[0]  (SkinnedMeshRenderer → skinned to physicRig)
    ├── metarig    (Animation rig — drives physicRig via ActiveRagdollBone)
    └── physicRig  (Physics rig — Rigidbody + ConfigurableJoint on each bone)
        └── spine
            ├── pelvis.L / pelvis.R
            ├── thigh.L / thigh.R
            └── ...
```

### Key Scripts

| Script | Location | Purpose |
| :--- | :--- | :--- |
| `ActiveRagdollBalance` | Scripts/ | Tethers physicRig spine to Player capsule via spring joint |
| `ActiveRagdollBone` | Scripts/ | Per-bone: copies metarig rotation to physicRig via SlerpDrive |
| `ActiveRagdollInitialiser` | Scripts/ | One-time setup: removes metarig colliders, attaches balance script |
| `SkinTransferTool` | Scripts/Editor/ | Editor tool: rebinds SkinnedMeshRenderer bones to physicRig |
| `RagdollJointLimitTool` | Scripts/Editor/ | Editor tool: sets angular joint limits on all physicRig bones |
| `PlayerMovement` | Scripts/ | Handles WASD movement and jumping; accounts for ragdoll total mass |

### Balance Tuning (`ActiveRagdollBalance.cs`)

| Parameter | Effect |
| :--- | :--- |
| `positionSpring` (drive) | How tightly physicRig follows Player position. Lower = lags behind |
| `angularDrive.positionSpring` | Uprighting force. 10000 = stiff, 200 = falls easily like Gang Beasts |

### Per-Bone Tuning (`ActiveRagdollBone.cs`)

| Parameter | Effect |
| :--- | :--- |
| `slerpDriveSpring` | Speed of following animation. 1000–3000 = natural, >5000 = snappy |
| `slerpDriveDamper` | Reduces oscillation/wobble. Usually 100–200 |

## 🛠 Technical Details

- **Unity Version**: 6000.x (Universal Render Pipeline)
- **Input System**: `com.unity.inputsystem` package
- **Physics**: ConfigurableJoint-based Active Ragdoll with per-bone spring drives

## 🚀 Getting Started

1. Open the project in Unity 6.
2. Load the `SampleScene`.
3. Press **Play**.
4. Use **WASD** to move and **Left Mouse Button** to grab/throw!

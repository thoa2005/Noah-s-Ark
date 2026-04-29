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
- **Physics Balance**: A virtual spring joint (`ActiveRagdollBalancer`) tethers the physics rig to the player capsule, keeping the character upright. Balance strength is tunable — high values give stiff, upright posture; low values allow natural falling.
- **Re-Setup Tool**: Includes a "Re-Setup Ragdoll" context menu on the controller to quickly bind bones, apply muscle forces, and configure collision filters.
- **Ignore Collision**: Automatically configures limbs to ignore collisions with the main body, preventing physical jitter and "self-entanglement" while maintaining solid collision with the environment.
- **Automatic Skin Mapping**: The character mesh is automatically rebound from the animation rig to the physics rig at runtime, ensuring the visual mesh follows the physical puppet perfectly.
- **Interpolation & Smoothness**: Root and bone rigidbodies are set to `Interpolate` mode, eliminating physics jitter during movement.

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
| `ActiveRagdollBalancer` | Scripts/ | Tethers physicRig spine to Player capsule via spring joint |
| `ActiveRagdollBone` | Scripts/ | Per-bone: copies metarig rotation to physicRig via SlerpDrive |
| `ActiveRagdollController` | Scripts/ | Main controller managing the rig, bones, and balance lifecycle |
| `PlayerMovement` | Scripts/ | Handles WASD movement and jumping; accounts for ragdoll total mass |

### Balance Tuning (`ActiveRagdollBalancer.cs`)

| Parameter | Effect |
| :--- | :--- |
| `balanceSpring` | Uprighting force. 10000 = very stiff, 1500 = more natural |
| `balanceDamper` | Reduces oscillation/wobble. Usually 50–200 |
| `balanceOffset` | Manual rotation offset to fix leaning issues |

### Per-Bone Tuning (`ActiveRagdollBone.cs`)

| Parameter | Effect |
| :--- | :--- |
| `controller.muscleSpring` | Speed of following animation. 1000–3000 = natural, >5000 = snappy |
| `controller.muscleDamper` | Reduces oscillation/wobble. Usually 10–50 |

## 🛠 Technical Details

- **Unity Version**: 6000.x (Universal Render Pipeline)
- **Input System**: `com.unity.inputsystem` package
- **Physics**: ConfigurableJoint-based Active Ragdoll with per-bone spring drives

## 🚀 Getting Started

1. Open the project in Unity 6.
2. Load the `SampleScene`.
3. Press **Play**.
4. Use **WASD** to move and **Left Mouse Button** to grab/throw!
5. **Pro Tip**: If the character looks broken, Right-Click `ActiveRagdollController` and select **Re-Setup Ragdoll**.

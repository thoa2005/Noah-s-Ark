# Physics Battle Prototype

A physics-based action game prototype inspired by the mechanics of *Party Animals*. This project features character movement, physical combat, and interactive object manipulation.

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

- **Skeletal Hitbox System**: Replaced the single capsule collider with a 13-part compound system (Head, Torso, Arms, Legs) attached to the character's skeleton for accurate hit detection and environment interaction.
- **Physics-Based Combat**: All punches and interactions use Unity Physics (AddForce/Impulse) for dynamic behavior.
- **Grab & Throw System**: Context-sensitive grabbing system that allows players to pick up objects or opponents and throw them with variable force.
- **AI Bots**: Basic AI opponents that navigate and interact with the player using the same physics rules.
- **Automated Testing**: Built-in test runner (`GameplayTest.cs`) to verify physics stability and input responsiveness.

## 🛠 Technical Details

- **Unity Version**: 6000.x (Universal Render Pipeline)
- **Input System**: Uses the new `com.unity.inputsystem` package for flexible remapping.
- **Physics Tuning**: Throw forces are calibrated between `3.0` and `10.0` units to ensure realistic trajectories without objects flying out of bounds.

## 🚀 Getting Started

1. Open the project in Unity 6.
2. Load the `SampleScene`.
3. Press **Play**.
4. Use **Left Mouse Button** to interact with the bots and environment!

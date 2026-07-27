[Русский](README.md) | **English**

# Micro-Surgery Universe — A "Quantum Field Surgeon" WPF Game

A particle-physics arcade game built in C# and WPF: a swarm of colored particles and their antiparticles moves around the play field, and the player manages their destruction and gravitational interactions, scoring points while trying not to run out of lives.

## Gameplay

- A swarm of particles moves across the field simultaneously. Each particle has a color and a type — a regular particle or an antiparticle.
- Particles of the same color attract one another; particles of different types (particle vs. antiparticle) sharing the same color **annihilate** on collision — both disappear, and the player loses a life (unless a shield is active).
- **Left-click** on a particle removes it from the field manually, for +10 points.
- **Right-click** anywhere on the field creates a temporary gravity well that attracts or repels nearby particles.
- The speed slider in the top panel controls the overall game pace: gravity strength, maximum particle speed, and spawn frequency.
- Golden bonus particles appear periodically, each granting a random effect.
- The game ends when the player runs out of lives (starting with 3, replenishable via a bonus).

## Bonuses

| Bonus | Icon | Effect |
|---|---|---|
| Slow motion | 🐢 | Reduces the overall game speed by 60% for the duration |
| Double points | ⭐ | Doubles all points earned |
| Shield | 🛡️ | Prevents losing a life on the next annihilation |
| Mega well | 🌀 | Instantly creates a powerful gravity well at the center of the field |
| Health | 💚 | Restores one life (up to a maximum of 5) |

The active bonus and its remaining duration are shown in the bottom panel.

## Physics

- Every pair of particles experiences a gravitational force inversely proportional to the square of the distance between them (similar to the law of universal gravitation); whether the force attracts or repels depends on whether the particles share the same color and type.
- Particles are also affected by temporary gravity wells (created by right-clicking or via the "Mega Well" bonus), which always attract.
- Collisions with the field boundaries are handled as elastic bounces with some speed loss.
- Particle speed is capped by a maximum value that scales with the speed slider.
- Physics is recalculated on every `DispatcherTimer` tick (roughly 60 times per second).

## Interface

- Top panel: score, remaining lives, particle count on the field, game-speed slider, "New Game" button.
- An indicator for the active bonus and a countdown timer for it.
- A bottom panel with control hints (left/right click) and the annihilation rules.
- Visual effects: an animated flash on annihilation, a shield effect, sparks scattering when a bonus is collected, and a pulsing gravity well with a fading halo as it expires.

## Repository contents

| File | Purpose |
|---|---|
| `MainWindow.xaml` | Window layout: game canvas, top stats panel, bottom hints panel |
| `MainWindow.xaml.cs` | All game logic: particle physics, gravity wells, bonuses, collisions/annihilation, effects, HUD |
| `App.xaml` / `App.xaml.cs` | WPF application entry point |
| `App.config` | Application configuration |
| `WpfApp1.csproj` / `WpfApp1.sln` | Visual Studio project/solution files |
| `Properties/` | Assembly metadata |

## Requirements

- Windows
- .NET Framework and Visual Studio (the ".NET desktop development" workload)

## Running the project

1. Open `WpfApp1.sln` in Visual Studio.
2. Build and run the project.
3. Left-click particles to remove them from the field and earn points; right-click to create gravity wells and steer the swarm.
4. Watch your lives: avoid unnecessary same-color particle/antiparticle annihilations while no shield is active.
5. Collect the golden bonus particles for temporary advantages.

## Technologies

- C#, WPF: `Canvas`, `Shapes` (`Rectangle`, `Ellipse`), `Media` (gradients, `DropShadowEffect`), `Animation` (`DoubleAnimation`, `ScaleTransform`)
- `System.Windows.Threading.DispatcherTimer` — the game loop, particle spawning, and the bonus timer

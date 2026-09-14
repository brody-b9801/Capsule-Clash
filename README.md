# Capsule Clash

A 3D multiplayer first-person shooter built in Unity 6. Fight other players, build cover on the fly, and travel between dimensions to feed capsule essence to a being called the Server.

Everything is written in C# and hand-written HLSL on top of [Fish-Net](https://fish-networking.gitbook.io/docs) networking.

> **Status:** in active development.

## Gameplay

- **Shoot and build.** Place floors, walls, and ramps on a grid mid-fight. Structures that lose their connection to the ground collapse.
- **Move fast.** Quake-style acceleration and friction, sprinting, dashing, and ground detection that sticks to slopes.
- **Grow stronger.** Kills earn capsule essences and upgrade points, which you spend on nine upgrades (speed, jump, dash force, dash regen, health regen, stamina regen, fire rate, damage, and reload speed) at four levels each.
- **Explore dimensions.** Portals lead to areas with their own rules:
  - **Desert:** the starting area
  - **Maze:** building, dashing, and jump upgrades are disabled
  - **Space:** lower gravity, higher jumps, and faster dash cooldowns
  - **Tundra:** slippery ground, and shooting pushes you forward
- **Feed the Server.** Find the Server in each dimension and feed it capsules to earn a key. Collect all three keys to unseal the locked portal.

### Controls

| Action | Default key |
| --- | --- |
| Move | `W` `A` `S` `D` |
| Jump / dash | `Space` |
| Sprint | `Shift` |
| Shoot | Left mouse |
| Aim | Right mouse or `Ctrl` |
| Reload | `R` |
| Switch weapon | `E` |
| Build floor / wall / ramp | `X` / `Z` / `C` |
| Break build | `V` |
| Inventory | `I` |
| Buy upgrade | `1`–`9` |
| Respawn (when dead) | `R` |

Build keys can be rebound in the settings menu.

## Technical highlights

### Server-authoritative combat

The dedicated server owns everything that decides a fight:

- **Bullets** are spawned by the server. Each tick, `BulletManager` raycasts from a bullet's previous position to its current one, so fast bullets can't tunnel through thin geometry between frames.
- **Health, spawn protection, and damage multipliers** live in server-owned `SyncVar`s (`DamageControl`), so clients can't edit them.
- **Build pieces** are spawned and despawned only by the server (`ObjectSpawner`).

Clients send inputs and requests through `ServerRpc`s, and results are broadcast back with `ObserversRpc`s.

### Building and structural collapse

`ObjectSpawner` snaps pieces to a 5-unit grid and checks for overlaps before placing them. After any change, a support pass runs a breadth-first search:

1. Seed the search with every piece resting on solid ground.
2. Walk outward through touching neighbors, marking each one supported.
3. Collapse every piece the search never reached, one after another.

### Movement

`PlayerMovement` is a custom character controller rather than Unity's physics:

- Separate ground and air acceleration
- Friction, with extra friction on sharp turns to keep direction changes snappy
- Sprinting and dashing
- Sphere-cast ground detection
- Input projected onto the floor's surface normal, so slopes feel smooth

### Rendering

All custom shaders are in `Assets/Custom Shaders`:

| Shader | Purpose |
| --- | --- |
| `CustomCel-Outline` | Comic-book cel shading with an inverted-hull outline pass |
| `CustomCel-Terrain` | Cel shading and outlines for terrain |
| `ComicBookPostProcess` | Full-screen comic-book post-processing |
| `RetroDither` | Four-pass retro dithering post-process |
| `CameraMotionBlur`, `FogShader` | Motion blur and fog post-processing |
| `UI_ComicBook`, `UI_RetroDither` | Matching effects for UI elements |

Their runtime controllers are in `Assets/Scripts/PPE_Shader_Controllers`. Early shader experiments are kept in `Unused-EarlyIterations` for reference.

## Getting started

### Requirements

- **Unity 6000.3.17f1** (Unity 6)
- **[Git LFS](https://git-lfs.com/)**, because large assets are stored with LFS

### Clone and open

```bash
git lfs install
git clone https://github.com/brody-b9801/Capsule-Clash.git
```

Then:

1. Open the folder in Unity Hub with the version above.
2. Open `Assets/Scenes/CombatScene.unity`.

### Playing locally

The client connects to the address and port set on the `RoomMenu` component (default `localhost:7770`).

- **Testing in the Editor:** enable **Host Locally** on `RoomMenu`. Pressing Start then launches a server alongside the client.
- **Playing against a dedicated server:** point `serverAddress` at the machine running a server build.

## Building

From the Unity menu bar, choose **Build → Windows Client Server** (`Assets/Editor/PostBuild.cs`). This produces:

| Output | Location |
| --- | --- |
| Windows client | `Builds/WindowsClient/` |
| Linux dedicated server | `Builds/LinuxServer/`, also zipped to `Builds.zip` |

Whichever target is already active is built first, which saves a platform switch.

### CI

`.github/workflows/gameci.yml` builds the Windows client with [GameCI](https://game.ci/) on a self-hosted Windows runner (label `unity_windows`). It checks out LFS assets and uploads the build as an artifact. The workflow runs manually from the Actions tab.

It needs these repository secrets:

- `RUNNER_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

`test.yml` is a quick check that the runner is online.

## Project structure

| Folder | Contents |
| --- | --- |
| `Assets/Scripts/Movement and Player` | Character controller, stamina, upgrades, and the Server NPC |
| `Assets/Scripts/Bullets and Guns` | Shooting, the bullet simulation, and damage |
| `Assets/Scripts/Building Mechanic` | Placement, build health, and structural support |
| `Assets/Scripts/Menu and UI` | Menus, connection flow, settings, HUD, and leaderboard |
| `Assets/Scripts/PPE_Shader_Controllers` | Post-processing effect controllers |
| `Assets/Scripts/Procedural Effects and Animations` | Camera shake, particles, and procedural animation |
| `Assets/Scripts/Helpers` | Editor and level-building utilities, including a maze generator |
| `Assets/Custom Shaders` | Hand-written HLSL shaders |
| `Assets/Editor` | Build script and custom inspectors |
| `Assets/FishNet` | Fish-Net networking library (third party) |

## Roadmap

- [ ] Client-side prediction and reconciliation for responsive movement at high ping
- [ ] Dedicated server builds in CI
- [ ] Automated Steam deploys
- [ ] Boss scene

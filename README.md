# Double Life

> A two-player online co-op mobile runner where you share one life between you.

## What Is It?

Double Life is a cooperative multiplayer mobile game for two players. Both players share a single procedurally generated run — when one falls, the other takes over from the exact same spot. If both fail, the run is over. Push as far as you can together and climb the duo leaderboard.

## How to Play

1. One player creates a room; the other joins via matchmaking.
2. Player 1 controls the rolling ball. Player 2 watches in real time.
3. When Player 1 dies, a brief dramatic pause plays — then Player 2 takes control from that exact position and momentum.
4. If Player 2 also dies, the run ends. Your score is the total combined distance.
5. Rematch or exit from the game over screen.

There is no finish line. The goal is distance.

## Key Features

- Procedural chunk-based course generation synced via shared seed
- Real-time spectator view for the waiting player
- Seamless handoff — Player 2 inherits Player 1's exact position and momentum
- Shared duo leaderboard
- Emoji/ping react system for the spectating player
- Dramatic handoff moment with screen flash and audio cue

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity (Android + iOS) |
| Networking | Photon PUN 2 |
| Procedural Gen | Chunk-based prefab spawning, deterministic shared seed |
| Physics | Rigidbody rolling ball |
| Platform Sync | Seed distributed via Photon on room creation |

## Project Setup

### Prerequisites

- Unity (LTS recommended)
- Photon PUN 2 package (import via Unity Asset Store or Package Manager)
- Android Build Support and/or iOS Build Support modules installed

### Getting Started

1. Clone the repository:
   ```
   git clone https://github.com/your-org/VGDC-Game-Jam-Spring-26.git
   ```
2. Open the project in Unity.
3. Import Photon PUN 2 and enter your App ID in `PhotonServerSettings`.
4. Open the main scene and hit Play to test in editor (two editor instances or Unity Multiplayer Play Mode for co-op testing).

## Build

- **Android:** File > Build Settings > Android > Build
- **iOS:** File > Build Settings > iOS > Build, then open Xcode and archive

## Team

VGDC Game Jam — Spring 2026

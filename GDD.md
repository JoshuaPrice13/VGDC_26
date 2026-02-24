# Game Design Document — Double Life

**Working Title:** Double Life
**Genre:** Cooperative Multiplayer Procedural Runner
**Platform:** Mobile (Android / iOS)
**Engine:** Unity
**Team:** VGDC Game Jam — Spring 2026
**Document Status:** Draft

---

## 1. Overview

Double Life is a two-player online co-op mobile game where both players share a single run. A procedurally generated course is created from a shared random seed, ensuring both players see an identical world. Players take turns controlling a rolling ball — when one falls, the other inherits their exact position and momentum. The run ends when both players have exhausted their turn. Score is total distance traveled.

The game is built around co-op tension: both players are always invested, even when one is watching.

---

## 2. Core Loop

```
Room created → Seed generated and shared → World generated on both clients
    → Player 1 runs → Player 1 dies → Dramatic handoff → Player 2 runs
    → Player 2 dies → Game Over → Score recorded → Rematch or Exit
```

1. A shared random seed generates the same procedural course on both clients.
2. Player 1 controls the ball while Player 2 watches via real-time spectator view.
3. When Player 1 falls or hits an obstacle, a dramatic pause plays.
4. Player 2 takes control from Player 1's exact last position and momentum state.
5. If Player 2 also fails, the run ends.
6. The duo sees their total distance/score. A rematch option keeps the pair together.

---

## 3. Win / Lose Condition

- **No finish line.** The objective is maximum distance.
- The run ends when both players have used their single turn.
- Score = total distance traveled across both players' turns combined.
- Scores are recorded to a shared duo leaderboard.

---

## 4. Game Flow

| Step | Description |
|---|---|
| 1 | Player opens the app and creates or joins a room via matchmaking |
| 2 | Room host generates a random seed; Photon shares it to both clients |
| 3 | Both clients generate the identical course locally using the seed |
| 4 | Player 1 is designated active; Player 2 enters spectator mode |
| 5 | Player 1 runs until death — handoff event broadcast via Photon |
| 6 | Dramatic handoff moment (screen flash, audio cue) |
| 7 | Player 2 takes control from Player 1's last position |
| 8 | Player 2 runs until death |
| 9 | Game over screen shown to both players with total distance score |
| 10 | Rematch (same duo, new seed) or exit |

---

## 5. Player Controls

- **One thumb / one finger** — swipe or tap to steer the ball left/right
- Input should feel immediate and responsive — no complex gestures
- Spectating player has access to emoji/ping react system only

---

## 6. Key Features

### 6.1 Deterministic Procedural Generation
- Course is built from a library of chunk prefabs
- A shared seed is passed through Photon on room creation
- Both clients generate the world locally — no world state is synced over the network
- This is the highest-risk system and should be built and verified for sync early in development

### 6.2 Real-Time Spectator View
- Waiting player watches the active player's run live
- Camera follows the active ball
- Spectator sees the same world they will soon inherit

### 6.3 Seamless Handoff System
- On Player 1 death, Photon broadcasts a handoff event with last known position and velocity
- Player 2's ball is spawned at that exact transform with matching Rigidbody momentum state
- A short screen flash and audio sting punctuate the transition

### 6.4 Emoji / Ping Reacts
- Spectating player can send a small set of emoji reactions visible to both players
- Reactions float above the active ball briefly
- Keeps the waiting player engaged without affecting gameplay

### 6.5 Duo Leaderboard
- Score belongs to the pair, not the individual
- Leaderboard displays duo names and combined distance

---

## 7. Design Pillars

| Pillar | What It Means |
|---|---|
| Co-op tension | Both players are always invested — the watcher cares as much as the runner |
| Shared ownership | The score belongs to the duo; success and failure are mutual |
| Simple to pick up | One-thumb control, no tutorials needed |
| High replayability | Procedural generation means every run is different |

---

## 8. Technical Stack

| System | Technology |
|---|---|
| Engine | Unity (Mobile — Android + iOS) |
| Networking | Photon PUN 2 |
| Procedural Generation | Chunk-based prefab spawning with deterministic shared seed |
| Physics | Rigidbody rolling ball with physics material |
| Platform Sync | Seed passed via Photon room properties; world generated client-side |

---

## 9. Scope Notes

- **Minimum viable scope:** deterministic proc-gen sync, handoff system, basic spectator camera, game over screen with score.
- Art and UI should remain minimal — the mechanic is the focus.
- Emoji react system is a stretch feature if core systems are solid.
- Duo leaderboard can use Photon's built-in or a simple backend; defer until core loop is stable.
- Build chunk prefabs first and test seed sync before any other networking work.

---

## 10. Open Questions

- [ ] How many distinct chunk prefabs are needed for the run to feel varied?
- [ ] What is the difficulty curve — does the course get harder over distance?
- [ ] How does matchmaking handle odd player counts (solo player waiting for partner)?
- [ ] What happens if a player disconnects mid-run?

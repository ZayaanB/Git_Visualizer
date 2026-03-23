# Git Visualizer

> **Note:** This is an experimental game built with heavy use of AI assistance.

A Unity 3D multiplayer game that visualizes GitHub repository commit history as an interactive 3D graph. Play **Merge Conflict Hunt** with friends over LAN or solo.

## Merge Conflict Hunt — Gameplay

Explore a 3D commit graph generated from a real GitHub repository. Five random nodes are marked as "conflicts." Find them, hold on each for 3 seconds to resolve, and resolve all five before the 5‑minute timer runs out to win. If time runs out, it's game over. Work together in Co-op mode or compete solo.

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download) (recommended) or Unity 6 Editor
- Git (for version control)

### GitHub Personal Access Token

The game fetches repository data from the GitHub API. To avoid rate limits and load private repos:

1. Create a token at [GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens).
2. In Unity, select the **GameLoopManager** object in `Scene_MainGame`.
3. In the Inspector, set **Play Again (Host)** → **Repo Token** to your token (leave empty for public repos only).
4. Optionally set **Repo Owner** and **Repo Name** for the repository to load (default: `ZayaanB` / `Git_Visualizer`).

The graph loads automatically when the Host starts a game. It uses the repo and token configured on **GameLoopManager**.

### Local LAN Co-op

1. **Host:** Main Menu → **Play Co-op (LAN)** → **Host Game**.
2. **Client:** Main Menu → **Play Co-op (LAN)** → enter the Host's IP address → **Join Game**.

Ensure both machines are on the same network. Default port: **7777** (configurable on NetworkBootstrap).

### Steps to Run

1. **Open the project** — Unity Hub → Add → select the `Git_Visualizer` folder.
2. **Setup** — Run **Git Visualizer → Setup Networking (Scene_MainGame)** and **Setup Post-Processing (Scene_MainGame)** in the Editor.
3. **Open the scene** — `Assets/Scenes/Scene_MainGame.unity`.
4. **Press Play** and start a Solo or Co-op game.

### Controls

| Action        | Input                |
|---------------|----------------------|
| Pan           | W / A / S / D        |
| Zoom          | Scroll wheel         |
| Orbit         | Right-click + drag   |
| View commit   | Left-click on a node |
| Resolve conflict | Hold left-click on red node (3 sec) |

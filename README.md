# Git Visualizer

A Unity 3D app that visualizes GitHub repository commit history as an interactive 3D graph.

## How to Run

### Prerequisites

- [Unity Hub](https://unity.com/download) (recommended) or Unity 6 Editor
- Git (for version control)

### Steps

1. **Open the project**
   - Launch Unity Hub → Add → select the `Git_Visualizer` folder
   - Or open Unity Editor and use File → Open Project → select this folder

2. **Open the scene**
   - In the Project window, go to `Assets/Scenes/`
   - Double-click `MainScene.unity`

3. **Press Play**
   - Click the Play button in the Unity Editor toolbar

4. **Load repository data** (requires wiring in AppManager)
   - The graph is populated by calling `GraphRenderer.SpawnGraph()` with data from `GitHubService.FetchRepoData()`
   - Wire this in `AppManager.Start()` or from a UI button

### Controls

| Action | Input |
|--------|-------|
| Pan | W / A / S / D |
| Zoom | Scroll wheel |
| Orbit | Right-click + drag |
| View commit | Left-click on a node |

### Optional: GitHub Personal Access Token

For higher API rate limits, create a token at [GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens) and pass it to `FetchRepoData()`.

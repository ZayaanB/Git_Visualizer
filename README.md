# Git Visualizer

> Experimental — built with a lot of help from AI. — Zayaan

Load a **GitHub** repo, see its commits as a **3D graph**, and play **Merge Conflict Hunt**: five random commits are “conflicts” (red). Hold each for **3 seconds** to clear it. Clear all **five** before the **5-minute** timer ends. Play alone or on **LAN** (host on port **7777**; the joiner gets the same graph and live game state).

**Needs:** JDK 21, Maven 3.9+

```bash
mvn test
mvn javafx:run
```

Set **`GITHUB_TOKEN`** (or paste a token in the app) so GitHub’s API is less likely to rate-limit you. Default repo in the UI is `ZayaanB` / `Git_Visualizer`—change owner/name as you like.

**Controls:** right-drag orbit, scroll zoom, **WASD** pan, click a node for details, hold a **red** node ~3s to resolve. Click the 3D view first if **WASD** does nothing (keyboard focus). If the app won’t start, run commands from this folder and use JDK 21.

Maven writes build output to `target/`; it’s gitignored and safe to delete anytime.

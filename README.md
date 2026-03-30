# PuzzleUp
# Match-3 Core Engine - Data-Oriented Architecture

This project is a high-performance and scalable Match-3 puzzle game core developed using the Unity game engine. 

Instead of the classic "everything is a GameObject" (heavy OOP-based structure) approach, it is built using the industry-standard **Data-Oriented Design**. This provides a robust infrastructure capable of simulating thousands of moves per second without performance loss, which is essential for future Artificial Intelligence (AI) level-testing simulations.

## 🚀 Key Features

* **Data and Visual Decoupling:** The core game logic (Data) and the visual presentation (View) are completely isolated from each other.
* **Advanced Match Algorithm:** Instead of using costly Flood-Fill algorithms, it uses **Linear Scan** and **Set Union** to perfectly detect straight lines, L-shapes, T-shapes, and 5-tile matches.
* **Command-Based Timeline (Command Pattern):** While data updates happen at lightning speed, all visual actions (Swap, Destroy, Gravity, Spawn) are queued and managed via Coroutines to keep animations and player experience perfectly synchronized.
* **Smart Power-Ups:**
  * **Horizontal Rocket (RocketHorizontal):** Formed by vertical 4-tile matches; destroys the entire row.
  * **Vertical Rocket (RocketVertical):** Formed by horizontal 4-tile matches; destroys the entire column.
  * **TNT (Bomb):** Formed by L or T-shaped matches; explodes a 3x3 area.
  * **Color Bomb (ColorBomb):** Formed by 5-tile matches; clears all tiles of the swapped color from the entire board.
* **Invalid Move Reversion (Revert System):** Swaps that do not result in a match are visually played but rejected in the data layer, smoothly returning the tiles to their original positions (deterministic control).

---

## 🏗️ System Architecture

The project is built upon 5 main pillars:

### 1. Data Layer (Model)
A pure data layer that is completely independent of Unity scenes and `MonoBehaviour`s, taking up only a few bytes in RAM.
* `TileType` (Enum): Numerical values representing objects on the board (0=None, 1=Red, 7=Rocket, etc.).
* `BoardModel`: The main data grid (`TileType[,] grid`) that keeps track of the board's current state.

### 2. Rules Layer (Systems)
The "brains" of the game. These systems only read data and perform mathematical calculations without interacting with the screen.
* `MatchSystem`: Scans the board, calculates matches, and determines which special tile should spawn and where.
* `GravitySystem`: Detects empty spaces after explosions and calculates the fall path for the tiles above.
* `SpawnSystem`: Fills the empty slots at the top of the board with random base colors.
* `PowerUpSystem`: Calculates the Area of Effect (AoE) for special tiles when they are triggered.

### 3. Command Layer (Commands)
Instead of applying the systems' decisions immediately, this layer queues them up. All actions inherit from the `ICommand` interface (e.g., `SwapCommand`, `DestroyCommand`).

### 4. Visual Layer (View)
The "dumb but stylish" layer. It knows nothing about game rules or matches; it simply reads the data from `BoardModel` and renders it on the screen. 

[Image of Model View Controller software architecture diagram]

* `TileView`: Handles Sprite assignments and smooth positional transitions (Lerp).
* `BoardView`: Manages camera sizing (Orthographic Size calculations) and controls the `TileView` objects.

### 5. The Conductor (Controller)
* `GameController`: Receives player inputs (`InputSystem`) and orchestrates the entire lifecycle (Swap -> Match -> Destroy -> Gravity -> Spawn) using `IEnumerator` (Coroutines) to maintain proper timing.

---

## 🧠 Technical Details & Edge Cases

* **Why Vector2Int instead of Vector2?** Since the game is built on a deterministic grid (rows/columns), mathematically precise integer vectors (`Vector2Int`) are used to locate tiles instead of floating-point values (`Vector2`), preventing floating-point errors.
* **Preventing Power-Up Auto-Matches (`IsBaseColor`):** Special tiles are filtered out during the `MatchSystem` scan. Even if 3 TNTs align side-by-side, they will not match. They are only triggered by direct player swaps or by getting caught in another explosion's AoE.
* **Avoiding Costly Instantiation:** Special tiles are not created out of thin air via `Instantiate()`. The system simply updates the `TileType` (Enum) integer at that specific coordinate, and the visual layer swaps the Sprite. This prevents Garbage Collection spikes and provides a massive performance boost on mobile devices.

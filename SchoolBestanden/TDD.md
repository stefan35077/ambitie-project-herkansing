# Technical Design Document (TDD)
## Stellar Spheres

**Product Owners (Client):** Luc / Jorrit / Gloria  
**Document Type:** Technical Design Document  
**Related Document:** Functional Design Document (FDD)  
**Genre:** Match-Three Puzzle Game  
**Inspiration:** Zuma  

---

## 1. Introduction

This Technical Design Document describes the technical implementation of the game **Stellar Spheres**, based on the Functional Design Document. It outlines the internal systems, architecture, data structures, and technical logic required to implement the gameplay mechanics, progression, and interactions.

The purpose of this document is to provide a clear technical blueprint for development.

---

## 2. Technical Overview

### 2.1 Architecture Style
The game uses a **component-based architecture** where systems are separated by responsibility. A central Game Manager controls the global game state while individual systems handle gameplay features.

### 2.2 Core Systems
- Game Manager
- Orb System
- Pathway System
- Shooting System
- Match Detection System
- Power-up System
- Obstacle System (Black Holes)
- Level System
- User Interface System

---

## 3. Game State Management

### 3.1 Game States
The game progresses through the following states:

- Main Menu  
- Level Start  
- Playing  
- Paused  
- Win  
- Lose  
- Level End  

### 3.2 Game Manager Responsibilities
The Game Manager is responsible for:
- Tracking the current game state
- Managing win and lose conditions
- Pausing and resuming gameplay
- Triggering level completion
- Calculating star ratings

---

## 4. Orb System

### 4.1 Orb Data Structure
Each orb is represented by the following data:

- ColorType (enum)
- IsPowerUp (boolean)
- PowerUpType (enum)
- PathIndex (integer)
- WorldPosition (vector)
- IsMoving (boolean)

### 4.2 Orb Colors
Possible orb colors:
- Red
- Blue
- Green
- Yellow
- Purple

If a color is fully eliminated during a level, it will no longer spawn.

### 4.3 Orb Lifecycle
1. Orb spawns in the pathway
2. Orb moves along the path
3. Orb is hit by player projectile
4. Orb is inserted into the pathway
5. Match detection is triggered
6. Orb is destroyed or remains
7. Pathway gaps are resolved

---

## 5. Pathway System

### 5.1 Path Definition
- Each level contains one or more predefined paths
- Paths are static and authored per level
- Orbs move along the path using normalized positions (0–1)

### 5.2 Movement Logic
- Constant forward movement
- Speed can increase per level
- Movement can be frozen by power-ups

### 5.3 Gap Filling
When orbs are destroyed:
- Remaining orbs slide together
- New matches can be triggered, causing chain reactions

---

## 6. Shooting System

### 6.1 Aiming
- Player controls a stationary spaceship
- Aiming direction follows mouse position
- Optional visual aiming indicator

### 6.2 Shooting Logic
- Left mouse click fires an orb
- The projectile moves in a straight line
- Projectile checks for:
  - Pathway collision
  - Black hole influence
  - Screen bounds

### 6.3 Projectile Impact
On impact:
- Orb is inserted at the nearest valid position in the path
- Orb locks into the path order
- Match detection is triggered

---

## 7. Match Detection System

### 7.1 Match Rules
- A match requires **three or more orbs** of the same color
- Matches only trigger when:
  - The player inserts an orb
  - Chain reactions connect orb groups

### 7.2 Detection Algorithm
1. Start from the inserted orb
2. Check adjacent orbs left and right
3. Count consecutive orbs of the same color
4. If the count is three or more, destroy the group

### 7.3 Chain Reactions
- After destruction, the path collapses
- Newly connected orbs are checked again
- The process repeats until no new matches occur

---

## 8. Power-up System

### 8.1 Power-up Types

| Power-up | Description |
|--------|-------------|
| Freeze | Temporarily stops pathway movement |
| Rainbow | Matches with any orb color |
| Black Hole Orb | Destroys one orb on impact |

### 8.2 Power-up Spawning
- Power-ups appear randomly when loading a new orb
- Spawn chance increases with difficulty
- Only one power-up orb can be active at a time

### 8.3 Power-up Execution
- Power-ups override default orb behavior
- Effects are managed by the Power-up System

---

## 9. Black Hole Obstacle System

### 9.1 Black Hole Zones
Each black hole has two zones:

- **Core Zone**
  - Destroys player projectiles on contact

- **Gravity Zone**
  - Applies force to the projectile trajectory

### 9.2 Gravity Calculation
- Force strength depends on distance to the black hole
- Projectile trajectory curves while maintaining forward velocity

### 9.3 Collision Rules
- Player projectiles are destroyed in the core
- Pathway orbs are not affected

---

## 10. Level System

### 10.1 Level Data
Each level defines:
- Pathway shape
- Allowed orb colors
- Total number of orbs
- Black hole positions
- Orb movement speed
- Star rating thresholds

### 10.2 Win Condition
- All orbs are cleared from the pathway

### 10.3 Lose Condition
- Any orb reaches the end of the pathway

---

## 11. Progression and Difficulty

### 11.1 Difficulty Scaling
Difficulty increases by:
- Increasing orb speed
- Adding complex pathways
- Introducing more black holes
- Reducing power-up frequency

### 11.2 Star Rating System
Levels are rated using a three-star system:

- **3 Stars:** High efficiency, few mistakes
- **2 Stars:** Moderate efficiency
- **1 Star:** Level barely completed

Metrics include:
- Missed shots
- Time taken
- Power-ups used

---

## 12. User Interface System

### 12.1 HUD Elements
- Current orb indicator
- Next orb preview
- Active power-up indicator
- Remaining orb count
- Pause button

### 12.2 Player Feedback
- Visual pop effects
- Sound effects
- Subtle screen shake
- Chain reaction indicators

---

## 13. Input System

### 13.1 Controls

| Action | Input |
|------|------|
| Aim | Mouse movement |
| Shoot | Left mouse click |
| Pause | Escape key |

### 13.2 Accessibility
- Mouse-only controls
- Suitable for relaxed, one-handed gameplay

---

## 14. Performance Considerations

- Object pooling for orbs
- Minimal physics calculations
- Preloaded level data
- Deterministic match detection logic

---

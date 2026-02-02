# Stellar Spheres

**Product owners (client): Luc / Jorrit / Gloria**

Stellar Spheres is a match-three puzzle game inspired by the classic
game \'Zuma\' (Walkthrough:
<https://www.youtube.com/watch?v=QwpuOhuVnnQ>). Set against a cosmic
backdrop, players battle cosmic anomalies in the form of star orbs and
harness power-ups to clear challenging levels.

![Zuma Deluxe for Windows - Download it from Uptodown for
free](Media/Img1.jpg)

## Important Features

<ins>Shooting Mechanism</ins>: Utilize a stationary spaceship to
shoot star orbs. After shooting a star orb, you get a new star orb of a
random colour.

<ins>Orb Matching</ins>: Match three or more star orbs of the same
colour to eliminate them.

<ins>Orb pathways</ins>: Orbs move in pathways in the shape of the
orbits of planets or the shape of constellations, slowly approaching an
end point.

<ins>Gravity Wells</ins>: Obstacles with gravitational forces that
can alter or stop the movement of orbs.

<ins>Power-ups</ins>: Power-ups that can appear as balls that you
gain. A star orb that deletes whatever orb it hits, a star orb that is a
rainbow colour and can be used to match any colour and a freeze star orb
that freezes all the orbs in the pathway.

<ins>Progression System</ins>: Multiple levels with different orb
pathways, with a three-star rating system.

## User Stories

As a player, I want to be challenged so that I can test my skills across
various levels.

As a player, I want the option to use power-ups when levels get tough so
that I can overcome them.

As a player, I want to have relaxing gameplay to pass the time.

As a player, I want satisfying chain reactions.

As a player, I want simple mouse-only controls.

## Mechanics

Shooting Mechanism: Players control a stationary spaceship to aim and
shoot star orbs. Shooting a star orb at the orb pathway causes the orb
to get stuck in the orb pathway in the location where it hit. After
shooting an orb, you ready a new random coloured orb. If a certain orb
colour is eliminated from the game, it no longer spawns for the player.\
Example: See Zuma walkthrough.

Power-ups: The player can randomly get a power-up star orb.\
- Freeze: Freezes the movement of the pathway for a set duration.\
- Rainbow: A star orb that can be used as any colour orb.\
- Black hole: A star orb that destroys whatever orb it hits. This will
only destroy 1 orb.

Orb Matching: Mechanism to detect and eliminate groups of three or more
matching orbs. The orb pathway can contain three matching orbs, but they
will only pop after the player adds one themselves.\
Example: See Zuma walkthrough.

Black holes: These are obstacles that can be present in a level. The
centre of the black hole deletes star orbs that the player shoots at it.
The edges of the black hole pull the orbs of the player in, causing it
the curve towards the black hole but still fly past.\
Example:

![](Media/Img2.png)\
In the picture to the left you can see the black hole. The grey arrow is
the player. The dotted red line indicates how the shot orbs should fly.
The transparent green circle shows the path the shot orb actually makes
because of the pull of the black hole, displayed as the black/red
circle.

Pathway Navigation: Star orbs move along preset pathways, with a
constant speed and direction. Gaps in the pathway are filled back. If
one orb reaches the end of the pathway the player loses. Only a set
amount of orbs spawn each level, clearing them all finishes the level.\
Example: See Zuma walkthrough.

Chain reactions: It is possible for orbs to spawn in sets of 3 or more
from the beginning by random chance. These will only trigger once
another one of the same colour is added to it, or if they connect to
another one of the same colour by popping orbs that were in between the
set and a similar coloured orb.\
Example: See this timestamp
<https://youtu.be/QwpuOhuVnnQ?si=eY476Q0dEJSdLVsn&t=1645>

## Dynamics

Level Progression: As players clear levels, they progress to harder
challenges.

Power-up Usage: Power-ups introduce dynamic changes, altering gameplay
temporarily.

Black holes: Present obstacles that make it more difficult to hit the
orbs you want to at any given time.

Shooting an orb in line that doesn't work towards a set of three causes
more work for the future.

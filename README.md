# Spatial Navigator

Octree-based volumetric navigation for flying agents in Unity, by acostar.

**Version 0.1.0 - experimental.** Targets Unity 2021.3 LTS and newer. Unity compilation,
Play Mode and player-build validation have not yet been performed for this package.
The core uses Unity Physics and does not depend on a render pipeline, navigation
package, custom tag, third-party priority queue or game manager.

## Scope and limitations

- Static, axis-aligned cubic navigation volume. Rotation and scale of the volume object
  do not rotate or scale the octree; only its world position and Root Size are used.
- Endpoints outside the volume or inside blocked cells are projected to clear octants.
  `CreatePath` returns cell centers, not exact endpoints. An empty result means no route
  or exhausted search budget. The agent only approaches the exact target if clearance permits.
- Cell occupancy does not include agent radius. Narrow corridors or cell transitions
  may reject movement even when a graph route exists. This is not a complete finite-radius
  planner, Rigidbody controller, crowd avoidance or dynamic obstacle avoidance system.
- Movement modifies the Transform. Do not combine it with an independently moving
  non-kinematic Rigidbody. The legacy Center and Approach Height fields are retained
  for serialization compatibility; clearance checks use the Transform position and
  exact movement segment, and those two fields have no effect.
- Build and A* run synchronously on the main thread. A* uses a linear frontier scan and
  a 10,000-expansion limit. Depth is clamped to 0-6; high depth/dense geometry can still
  be costly. Profile before deploying with many agents.
- Graph routes are not a guarantee of shortest continuous-space paths or collision-free
  motion for every radius. This initial package needs Unity validation before release.


## License and attribution

MIT - see [LICENSE](LICENSE). Retain the copyright and license notice when
redistributing copies or substantial portions. A public link credit is appreciated:
"Spatial Navigator by acostar - https://github.com/acostar/SpatialNavigator".
This credit suggestion does not add a condition to the MIT license.

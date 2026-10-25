# Spatial Navigator

Octree-based volumetric navigation for flying agents in Unity, by acostar.

**Version 0.1.0 — experimental.** Targets Unity 2021.3 LTS and newer. Unity compilation,
Play Mode and player-build validation have not yet been performed for this package.
The core uses Unity Physics and does not depend on a render pipeline, navigation
package, custom tag, third-party priority queue or game manager.

## Install from GitHub

After these files are committed to the repository, open Unity's **Window > Package
Manager**, click **+ > Add package from git URL** (or **Install package from Git URL**),
and paste:

```text
https://github.com/acostar/SpatialNavigator.git
```

Git must be installed and available on PATH. The package manifest is at the repository
root, so no `?path=` is necessary. For a reproducible release, after creating the tag:

```text
https://github.com/acostar/SpatialNavigator.git#v0.1.0
```

Alternatively, add this entry to your project's existing `Packages/manifest.json`
`dependencies` object, keeping all other dependencies:

```json
"com.acostar.spatial-navigator": "https://github.com/acostar/SpatialNavigator.git"
```

To test before publishing: extract the ZIP, then use **Add package from disk** and
select `SpatialNavigator/package.json`. This is a UPM package, not a `.unitypackage`.

## Try the demo

1. Select **Spatial Navigator** in Package Manager.
2. Expand **Samples** and import **Flying Agent Demo**.
3. Open `Assets/Samples/Spatial Navigator/0.1.0/Flying Agent Demo/FlyingAgentDemo.unity`.
4. Press Play. The cyan agent flies around a central obstacle between green and yellow markers.

The scene builds its geometry, camera and light at runtime; it is intentionally mostly
empty before Play. No input setup or custom layers are needed. The scene uses Default
for obstacles and Ignore Raycast for non-colliding visuals. It is intended for Editor
Play Mode. Its dynamically located unlit shader must be retained in Graphics Settings
if you include this sample in a player build. Custom render pipelines may need a
replacement demo material. See the sample README for debugging instructions.

## Use in your scene

1. Add `SpatialNavigationMesh` to an empty object at the center of your navigation area.
2. Set **Root Size** (world-space cube side length), **Max Layer** (subdivision depth),
   and **Obstacle Layer Mask**. The smallest cell has side `rootSize / 2^maxLayer`.
3. Give your static obstacles 3D colliders and put them on the selected obstacle layers.
   Triggers are ignored. Exclude the agent's own collider from the obstacle mask.
4. Add `SpatialAgent` to the flying object and assign its **Navigation Mesh** explicitly.
   If unset, it finds a scene volume automatically; always assign it in multi-volume scenes.
5. Set the agent's **Collision Layers** to the same mask and **Navigation Radius** to
   match the clearance needed. Call `Move` every frame from your movement controller.

```csharp
using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [SerializeField] private SpatialAgent agent;
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 3f;

    private void Update()
    {
        if (agent != null && target != null)
            agent.Move(target.position, speed);
    }
}
```

`Move(position, speed)` expects speed in world units per second and applies delta time
internally. The Inspector Target field is only for the manual debug path request;
it does not automatically chase that target. Keep **Move Directly** off to demonstrate
routing; enable it to bypass pathfinding when a direct sphere cast is clear. **Simplify
Path** removes visible intermediate waypoints. Normal movement also checks segment
clearance. Navigation builds on first use or Start. Call `Build()` after obstacle or
volume changes; call `Configure(...)` again on agents to discard cached routes immediately.

If your game scripts use an assembly definition, add `Acostar.SpatialNavigator` to its
references. The supplied global type names are retained for source compatibility.

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
  a 10,000-expansion limit. Depth is clamped to 0–6; high depth/dense geometry can still
  be costly. Profile before deploying with many agents.
- Graph routes are not a guarantee of shortest continuous-space paths or collision-free
  motion for every radius. This initial package needs Unity validation before release.

## Migrating loose scripts

Back up your Unity project. Remove the old loose `SpatialAgent.cs` and
`SpatialNavigationMesh.cs` before installing this package, to avoid duplicate types.
The attachment contained no Unity `.meta` files; existing scene/prefab component script
references therefore cannot be preserved automatically. To preserve those references,
copy your original script GUIDs into the matching Runtime `.meta` files **before first
publishing**, or reattach/reconfigure the components. Do not change GUIDs after release.
`Octant_v2` was missing in the supplied source; the agent now uses the supplied `Octant`.

## Publish and validate

See [Publishing](Documentation~/publishing.md) and [Validation](Documentation~/validation.md).

## License and attribution

MIT — see [LICENSE.md](LICENSE.md). Retain the copyright and license notice when
redistributing copies or substantial portions. A public link credit is appreciated:
“Spatial Navigator by acostar — https://github.com/acostar/SpatialNavigator”.
This credit suggestion does not add a condition to the MIT license.

## Unity references

- https://docs.unity3d.com/Manual/upm-git.html
- https://docs.unity3d.com/Manual/cus-layout.html
- https://docs.unity3d.com/Manual/cus-samples.html

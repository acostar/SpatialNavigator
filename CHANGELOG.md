# Changelog

## 0.1.0 - 2026-10-25

Initial UPM packaging of the supplied Spatial Navigator source. Experimental; Unity
validation pending.

- Added root package manifest, runtime assembly, metadata, importable flying demo,
  Edit Mode tests, structural validation workflow and publication instructions.
- Retained MIT license and existing public global component/type names.
- Replaced undefined Octant_v2 with Octant; removed GameManager and missing priority
  queue dependencies. A* now accumulates route cost and returns empty for no route.
- Guarded fully blocked volumes and non-transitable neighbour recursion; corrected
  blocked-neighbour sentinel. Added explicit Build and Configure APIs.
- Removed required NavigationMesh tag and preserved manually assigned volume references.
- Added usable defaults, zero-length cast protection, failed-route cooldown/reset,
  final-target movement and movement segment clearance checks.
- Fixed StopSphereChecks recording flag and corrected radius gizmo origin.
- Retained legacy center/approachHeight serialized fields but checks now use exact
  movement segments from the Transform position.

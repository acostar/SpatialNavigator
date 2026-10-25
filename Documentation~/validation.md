# Validation and release checklist

## Completed during packaging

- JSON parsing and package name/version/sample path checks.
- Assembly-reference checks and Editor-only test assembly configuration.
- Unique Unity GUIDs, scene script GUID resolution, and asset metadata coverage.
- Source checks for removed missing dependencies and project-specific calls.
- ZIP integrity and exclusion of uploaded Git history.

No Unity Editor, Unity compiler, .NET compiler or player build was available during
packaging. The C# and scene have **not** been compiled, imported or executed here.

## Required in Unity before publishing a stable release

1. Use a clean Unity 2021.3 LTS project, then repeat in your intended Unity 6 version.
2. Install locally from `package.json`; check the Console for compilation/import errors.
3. Import the sample, open its scene and Play. Confirm the cyan cube makes repeated
   round trips around the central block and reaches both markers without exceptions.
4. In your own scene, check masks, obstacle colliders, small and large agent radii,
   moving targets, multiple volumes and no-path cases. Test blocked corridors.
5. Install Unity Test Framework. In the project's `Packages/manifest.json`, add a
   top-level `"testables": ["com.acostar.spatial-navigator"]`, merging any existing list.
   Run the five `SpatialNavigationTests` Edit Mode tests in Test Runner. These test an
   empty volume, a fully blocked volume, disconnected regions, a detour and rebuilding.
6. Make a player build with an actual game scene that uses the package. Runtime assembly
   must compile without NUnit or UnityEditor references. The demo's dynamic shaders need
   explicit retention if included in the build; verify rendering in your pipeline.
7. Push and repeat installation from the Git URL in a clean project; then test the tag URL.

The test assembly requires Unity Test Framework only when running tests; it is not a
runtime dependency. The GitHub workflow runs structural checks only.

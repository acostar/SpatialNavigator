# Publish to GitHub

The origin in the uploaded archive is `https://github.com/acostar/SpatialNavigator.git`.
The package metadata uses that repository and the existing MIT copyright owner.

1. Extract the ZIP. Copy the **contents** of its `SpatialNavigator` folder into your
   existing local repository root. `package.json` must sit beside `README.md`, not
   inside another nested `SpatialNavigator` directory.
2. Remove the two old root-level `.cs` files; their replacements are under `Runtime/`.
   The existing `LICENSE` can be removed in favor of the identical `LICENSE.md`.
   Preserve your checkout's `.git` directory. This ZIP intentionally excludes Git history.
3. Follow the validation checklist before publishing. Check the diff and commit every
   supplied `.meta` file along with its matching asset.
4. Commit and push to your default branch using GitHub Desktop or Git. No GitHub upload,
   commit, push, release or tag has been performed by the package preparation process.
5. Test a clean project installation using the plain Git URL. Confirm sample import and
   player compilation. Once validated, create and push tag `v0.1.0` at that commit.
6. Users can then use the pinned `...SpatialNavigator.git#v0.1.0` URL. Creating a GitHub
   Release is optional; release ZIP assets are not needed by Unity Package Manager.

Example commands from your existing checkout, after copying and reviewing files:

```sh
git add -A
git commit -m "Package Spatial Navigator for Unity Package Manager"
git push origin main
# Only after validating that commit in Unity:
git tag v0.1.0
git push origin v0.1.0
```

For future releases, update `package.json` and `CHANGELOG.md`, validate, and create a new
version tag. Never move a published version tag. Existing projects lock Git dependencies
to a revision; changing the remote branch does not force automatic updates in them.

No npm publication, registry server, GitHub Action, release binary, or full Unity project
is required for Git URL installation. `.github/workflows/package-check.yml` checks the
archive layout and metadata only; it does not claim to compile Unity code.

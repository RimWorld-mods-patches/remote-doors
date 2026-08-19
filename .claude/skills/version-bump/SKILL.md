---
name: version-bump
description: Bump the Remote Doors mod version in both places it lives, choosing the semver level from what actually changed. Use before committing mod changes to main, before pushing a branch, or whenever the version guard blocks a commit or push asking for a bump.
---

# Bumping the mod version

The version lives in **two files** that must always agree. Changing one and not the
other is the failure this skill exists to prevent.

| File | Field | Who reads it |
|---|---|---|
| `Source/RemoteDoors/RemoteDoors.csproj` | `<Version>` | The .NET SDK, which bakes it into `RemoteDoors.dll` as AssemblyVersion, FileVersion and InformationalVersion |
| `About/About.xml` | `<modVersion>` | `Verse.ModMetaData`, the in-game mod list, and mod managers such as RimPy |

RimWorld does not read the DLL version for anything - it loads mod assemblies by path
with no version check - so `<modVersion>` is the one players actually see. The DLL
version is for tracing bug reports back to source. Both still get bumped together.

## Choosing the level

Judge by the effect on **a player with an existing save**, not by how much code moved.

**MAJOR** - existing saves break or silently change meaning:
- Renaming or removing a `defName` (a save referencing `RemoteDoor` finds nothing and
  the building disappears from the colony)
- Changing `thingClass` to a type that does not load the old scribed data
- Removing a building, comp or field that was previously scribed

**MINOR** - new capability, existing saves unaffected:
- A new building, def, comp or research project
- Adding a `supportedVersions` entry for a new RimWorld release
- A new gizmo or player-facing option

**PATCH** - no new content, no save impact:
- Bug fixes, including behaviour corrections to existing buildings
- Balance changes to costs, work amounts or power draw
- Textures, labels, descriptions, translations
- Comments, refactors, build tooling, skills, hooks

When a change spans levels, take the highest one that applies.

Pre-1.0 rules do not apply here - the mod is already at 1.0.0, so a breaking change is
a MAJOR bump, not a MINOR one.

## Doing the bump

1. Read the current version from `About/About.xml` (`<modVersion>`).
2. Decide the level from the rules above, based on what is staged.
3. Set the **same** new version in both files.
4. Rebuild so the DLL carries it - see the `build-mod` skill. A commit whose
   `About.xml` and `.csproj` disagree with the built DLL is worse than no bump.
5. State the level chosen and why, in one line, when reporting the commit.

## When the guard fires

`.claude/hooks/require-version-bump.sh` runs before every Bash tool call and checks
`git commit` and `git push`. It only cares about changes under `Source/`, `1.6/` or
`Languages/` - the shipped content. Where it enforces depends on the branch:

| Situation | Enforced? | Baseline compared against |
|---|---|---|
| `git commit` on `main` | Yes | The previous commit - every content commit on main carries its own bump |
| `git commit` on a branch | No | Branches iterate freely; the gate is at push |
| `git push` | Yes | The upstream ref if the branch has one, else the merge base with `main` - one bump covers the whole branch |

So on a branch you bump once, whenever it suits, as long as it happens before the push.
On main every content commit needs its own.

The guard also rejects a commit or push where the two files disagree, independently of
whether anything was bumped.

`--amend` and `--no-verify` both bypass it. Use `--no-verify` when a change genuinely
ships no player-visible difference and you have decided it needs no version.

Note the repo currently has **no git remote**, so the push branch of this logic will not
fire until one is added.

## Notes

- The two values must be byte-identical. `1.1.0` in one file and `1.1` in the other
  counts as a mismatch even though both parse.
- `<modVersion>` is free text to RimWorld - it will not complain about a malformed
  value, so nothing but this skill catches a typo.
- Commits that touch only `.claude/`, `README`, or `.gitignore` do not need a bump, and
  the commit-guard hook does not ask for one.

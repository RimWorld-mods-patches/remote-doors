---
name: build-mod
description: Build the Remote Doors RimWorld mod assembly using Docker. Use whenever the C# under Source/ changes, when asked to build/compile/rebuild the mod, or before testing in game. Also covers cleaning build output and installing the mod into RimWorld.
---

# Building Remote Doors

The host has **no .NET SDK** — all compilation goes through Docker. Do not try `dotnet`,
`msbuild` or `mono` directly; they are not installed and are not expected to be.

The build is hermetic: RimWorld's reference assemblies come from the `Krafs.Rimworld.Ref`
NuGet package, so nothing is mounted from the Steam install and the build works on any
machine with Docker.

## Prerequisite

The Docker daemon must be running. Check with:

```bash
docker version --format '{{.Server.Version}}'
```

If that fails with "failed to connect to the docker API", Docker Desktop is not started.
Ask the user to start it — do not attempt to start it or work around it.

## Build

Run from the repo root:

```bash
docker run --rm \
  -v "$PWD":/mod \
  -v rimworld-nuget:/root/.nuget/packages \
  -w /mod/Source/RemoteDoors \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet build -c Release
```

Output lands in `1.6/Assemblies/RemoteDoors.dll`. The `rimworld-nuget` named volume
persists the NuGet cache, so the first build downloads ~100MB of reference assemblies
and later builds are fast.

## Clean

```bash
rm -rf Source/RemoteDoors/bin Source/RemoteDoors/obj
```

To also discard the cached packages: `docker volume rm rimworld-nuget`.

## Install into RimWorld

Symlink the repo into the game's Mods folder once, so every build is picked up on the
next game start with no copy step:

```bash
ln -s "$PWD" "$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/RemoteDoors"
```

## Reading failures

- **`Assembly-CSharp` / `Verse` / `RimWorld` type not found** — the `Krafs.Rimworld.Ref`
  package failed to restore, or the pinned version was changed to one that does not exist.
  Installed game build is **1.6.4871**; the package is pinned to match in
  `Source/RemoteDoors/RemoteDoors.csproj`.
- **`net472` targeting pack errors** — `Microsoft.NETFramework.ReferenceAssemblies` did not
  restore. Almost always a network problem on first build; retry.
- **Runtime errors in game**, not build errors, surface in
  `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`.

## Rules

- Never commit `bin/` or `obj/` — both are gitignored.
- Never let RimWorld or Unity DLLs reach `1.6/Assemblies/`. Only `RemoteDoors.dll` belongs
  there; shipping `Assembly-CSharp.dll` beside a mod DLL breaks the game on load. The
  csproj prevents this with `PrivateAssets="all" ExcludeAssets="runtime"` — do not remove.

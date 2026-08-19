---
name: build-mod
description: Build the Remote Doors RimWorld mod assembly using Docker, and decompile vanilla RimWorld classes to check their real source. Use whenever the C# under Source/ changes, when asked to build/compile/rebuild the mod, before testing in game, or when you need to know what a vanilla type actually does - accessibility, signatures, or behaviour.
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

## Decompiling vanilla RimWorld code

Never guess at what a vanilla type does, and never infer accessibility from symbol names -
`Building_Door.DoorOpen` reads as public in a string dump but is `protected`, which decided
the whole architecture of this mod. Decompile and read the real thing.

The `decompile/` folder beside this file is a small console app wrapping
`ICSharpCode.Decompiler`. Run it against the installed game assembly:

```bash
MANAGED="$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed"
docker run --rm \
  -v "$MANAGED":/rw:ro \
  -v "$PWD/.claude/skills/build-mod/decompile":/decompile \
  -v rimworld-nuget:/root/.nuget/packages \
  -w /decompile \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run -c Release -- /rw/Assembly-CSharp.dll RimWorld.Building_Door
```

Swap the last argument for any fully-qualified type. Pipe to a file and grep it - these
types run to hundreds of lines.

Notes:
- **Namespaces are not guessable.** `Building_Door` is in `RimWorld`, not `Verse`, despite
  most `Building_*` types living in `Verse`. A wrong namespace gives
  "Could not find type definition ... in type system"; try the other one.
- **Do not use `ilspycmd`.** The NuGet id is shadowed by an unrelated squatted 1.0.0 package
  with no `DotnetToolSettings.xml`, so `dotnet tool install -g ilspycmd` fails at any version.
  The library approach above avoids it entirely.
- Decompile the **game's** `Assembly-CSharp.dll`, not `Krafs.Rimworld.Ref` - the NuGet
  package ships reference assemblies with method bodies stripped.

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

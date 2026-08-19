#!/usr/bin/env bash
# PreToolUse/Bash guard: refuse a git commit or push that ships mod changes without a
# correct version. See the version-bump skill.
#
#   commit on main    -> must bump against the previous commit; main carries releases
#   commit on a branch -> not checked, so a branch can iterate freely
#   push (any branch)  -> the pushed range must contain a bump, checked against the
#                         upstream if there is one, else the merge base with main
#
# Either way both files must agree with each other.
set -uo pipefail

MAIN_BRANCH=main
ABOUT=About/About.xml
CSPROJ=Source/RemoteDoors/RemoteDoors.csproj
CONTENT_RE='^(Source/|1\.6/|Languages/)'

input=$(cat)
cmd=$(printf '%s' "$input" | jq -r '.tool_input.command // ""')

case "$cmd" in
  *"git commit"*) event=commit ;;
  *"git push"*)   event=push ;;
  *) exit 0 ;;
esac

case "$cmd" in
  *--amend*|*--no-verify*) exit 0 ;;
esac

root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
cd "$root" || exit 0
git rev-parse HEAD >/dev/null 2>&1 || exit 0

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)

about_version_at()  { git show "$1$ABOUT"  2>/dev/null | sed -n 's/.*<modVersion>\([^<]*\)<\/modVersion>.*/\1/p' | head -1; }
csproj_version_at() { git show "$1$CSPROJ" 2>/dev/null | sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p'       | head -1; }

if [ "$event" = commit ]; then
  # A branch bumps once, enforced at push time - let its commits through.
  [ "$branch" = "$MAIN_BRANCH" ] || exit 0

  git diff --cached --name-only | grep -qE "$CONTENT_RE" || exit 0
  version_rev=":"                       # the index: what this commit will contain
  baseline_rev="HEAD:"
  baseline_desc="the previous commit on $MAIN_BRANCH"
  retry="commit again"
else
  # Prefer what the remote already has; fall back to this branch's start point.
  if upstream=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null); then
    baseline=$upstream
    baseline_desc="$upstream"
  elif baseline=$(git merge-base HEAD "$MAIN_BRANCH" 2>/dev/null) && [ -n "$baseline" ]; then
    baseline_desc="where this branch left $MAIN_BRANCH"
  else
    exit 0                              # nothing meaningful to compare against
  fi

  git diff --name-only "$baseline" HEAD 2>/dev/null | grep -qE "$CONTENT_RE" || exit 0
  version_rev="HEAD:"                   # push ships commits, not the index
  baseline_rev="$baseline:"
  retry="push again"
fi

new_about=$(about_version_at "$version_rev")
new_csproj=$(csproj_version_at "$version_rev")
base_about=$(about_version_at "$baseline_rev")

deny() {
  jq -n --arg r "$1" '{hookSpecificOutput: {hookEventName: "PreToolUse",
    permissionDecision: "deny", permissionDecisionReason: $r}}'
  exit 0
}

if [ -z "$new_about" ] || [ -z "$new_csproj" ]; then
  deny "Could not read the mod version: <modVersion> in $ABOUT and/or <Version> in $CSPROJ is missing or unparseable. Fix the version fields, then $retry."
fi

if [ "$new_about" != "$new_csproj" ]; then
  deny "The two version fields disagree: $ABOUT says <modVersion>$new_about</modVersion> but $CSPROJ says <Version>$new_csproj</Version>.

They must be byte-identical. Invoke the version-bump skill, set both to the same value, rebuild, then $retry."
fi

if [ "$new_about" = "$base_about" ]; then
  deny "This $event ships mod content but the version is still $new_about, unchanged from $baseline_desc.

Invoke the version-bump skill: pick the semver level from what changed, set the same new version in BOTH $ABOUT and $CSPROJ, rebuild so the DLL carries it, then $retry.

If this $event genuinely should not carry a bump, re-run with --no-verify."
fi

exit 0

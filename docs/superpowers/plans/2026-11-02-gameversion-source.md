# Game Version Source Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make `gameversion.txt` the single source of truth for the last verified SWYF game version shown on the website and in GitHub releases.

**Architecture:** Store one plain-text version at repository root. Pages copies `docs/` to a staging directory and replaces a `__GAME_VERSION__` placeholder before upload. Release metadata reads the same file. README links to the source file instead of duplicating the value.

**Tech Stack:** GitHub Actions, PowerShell, static HTML.

**Spec:** Approved in chat on 2026-11-02.

## Global Constraints

- Repository: `Nereziel/swyf-mody` only.
- Current verified game version: `0.1.95-swyf`.
- Pages auto-deploy only for `docs/**` or `gameversion.txt` changes.
- Releases remain manual.

## Review Focus

- Empty or whitespace-only `gameversion.txt` must fail workflows.
- Website artifact must not contain an unresolved `__GAME_VERSION__` placeholder.
- Release notes must use the version captured from the same commit that was built.
- Non-web/non-version commits must not trigger Pages.
- README must not keep a second hard-coded game version.

---

### Task 1: Add canonical game version
- Create `gameversion.txt` with `0.1.95-swyf`.
- Remove duplicate version value from README and link to the canonical file.

### Task 2: Generate Pages artifact from canonical version
- Change `docs/index.html` to use `__GAME_VERSION__`.
- Trigger Pages on `docs/**` and `gameversion.txt`.
- Stage `docs/` into `_site`, validate version, replace placeholder, verify no placeholder remains, upload `_site`.

### Task 3: Generate release notes from canonical version
- Capture `gameversion.txt` in build metadata.
- Pass it as a build output to publish.
- Use it in release body instead of a hard-coded version.

### Task 4: Verify and clean up
- Confirm only `gameversion.txt` contains `0.1.95-swyf` as configuration data.
- Confirm Pages and release workflows reference `gameversion.txt`.
- Remove this temporary plan before merge.

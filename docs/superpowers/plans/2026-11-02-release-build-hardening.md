# Release and Build Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make build, release, package verification, game-version metadata and website version display self-consistent and harder to publish incorrectly.

**Architecture:** Add focused PowerShell validators for release metadata and package contents, call them from one reusable self-hosted build workflow, and make the manual build/release workflows thin callers. Pages will render game and mod versions from repository metadata at deploy time. Existing release reruns will update release metadata while preserving generated changelog text.

**Tech Stack:** GitHub Actions, PowerShell 7, .NET ZIP APIs, static HTML.

**Spec:** User-approved improvements from chat on 2026-11-02.

## Global Constraints

- Repository writes only to `Nereziel/swyf-mody`.
- Public mods remain exactly CzechSpeechEnhanced, CzechTTSFix and CzechTranslationFix.
- PrivacyTelemetry must compile in `All` but must never appear in public packages.
- Build and release entry workflows remain manual-only.
- Pages auto-deploy only when web inputs change.
- `gameversion.txt` remains the single source of the verified game version.
- Current game version is `0.1.95-swyf` and release tag convention is `vYYYY.MM.DD-X.Y.Z`.

## Review Focus

- A plugin version differing from its `mod.json` version must fail before packaging.
- Invalid/multi-line game versions or release tags for another game version must fail.
- Packages with unexpected entries, wrong embedded metadata or PrivacyTelemetry content must fail.
- Re-running an existing release must update title/body/prerelease without losing generated changelog suffix text.
- Pages must render all version placeholders and trigger when any displayed version source changes.

---

### Task 1: Metadata validation

**Files:**
- Create: `tools/validate-release-metadata.ps1`
- Create: `tests/Hardening/Test-Metadata.ps1`

**Interfaces:**
- Consumes: repository root and optional release tag.
- Produces: validation success/failure for `gameversion.txt`, public mod versions and release-tag compatibility.

- [ ] Write tests for valid metadata, plugin/manifest mismatch, malformed game version, multi-line game version, malformed tag and tag/game mismatch.
- [ ] Run tests and verify RED because validator is missing.
- [ ] Implement validator.
- [ ] Run tests and verify GREEN.

### Task 2: Package-content validation

**Files:**
- Create: `tools/verify-packages.ps1`
- Create: `tests/Hardening/Test-Packages.ps1`

**Interfaces:**
- Consumes: repository root and `dist` directory.
- Produces: validation that exactly three public `.swyfmod` files contain only expected content and matching embedded manifests.

- [ ] Write synthetic ZIP tests for a valid set, unexpected file, metadata mismatch and PrivacyTelemetry leakage.
- [ ] Run tests and verify RED because verifier is missing.
- [ ] Implement verifier.
- [ ] Run tests and verify GREEN.

### Task 3: Reusable build and release hardening

**Files:**
- Create: `.github/workflows/build-mods-reusable.yml`
- Modify: `.github/workflows/build-self-hosted.yml`
- Modify: `.github/workflows/release-self-hosted.yml`

**Interfaces:**
- Consumes: artifact name and optional release tag.
- Produces: build metadata outputs plus a verified package artifact.

- [ ] Move shared tests/build/package checks into `workflow_call` workflow.
- [ ] Add game DLL SHA256 fingerprint logging.
- [ ] Keep manual Build as a thin caller.
- [ ] Keep manual Release as a thin caller plus GitHub-hosted publish job.
- [ ] Validate release tag in the called build.
- [ ] PATCH existing releases with current title/body/prerelease while preserving generated suffix text.
- [ ] Run branch CI/self-hosted build and verify GREEN.

### Task 4: Website version rendering

**Files:**
- Modify: `.github/workflows/pages.yml`
- Modify: `docs/index.html`
- Modify: `README.md`

**Interfaces:**
- Consumes: `gameversion.txt` and three public `mod.json` files.
- Produces: deployed HTML containing current game and mod versions with no unresolved placeholders.

- [ ] Add version placeholders to the three mod cards.
- [ ] Render all four version values during Pages deployment.
- [ ] Trigger Pages for `docs/**`, `gameversion.txt` and the three public `mod.json` files only.
- [ ] Fail deployment if any version placeholder remains unresolved.
- [ ] Document metadata/tag conventions briefly.
- [ ] Verify Pages deployment succeeds.

### Task 5: Final review and cleanup

- [ ] Run hardening tests and full self-hosted reusable build.
- [ ] Review branch diff for unintended runtime changes.
- [ ] Remove temporary branch-only CI and this plan file before merge.
- [ ] Merge only after verification evidence is green.

# Repository Audit & Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Improve correctness, reproducibility, and maintenance without redesigning the mod architecture or changing `.swyfmod` compatibility.

**Architecture:** Keep the existing three public mods plus experimental PrivacyTelemetry. Isolate pure audio preparation for testing, strengthen localization-reference validation, and make build/release paths more reproducible while preserving manual self-hosted execution.

**Tech Stack:** C#/.NET Framework 4.7.2, .NET 8 test consoles, PowerShell, GitHub Actions, Python/UnityPy.

**Spec:** User delegated audit/optimization decisions; conservative changes only.

## Global Constraints

- Write only to `Nereziel/swyf-mody`.
- Preserve CzechTTSFix and PrivacyTelemetry.
- Do not add automatic triggers to self-hosted workflows.
- Keep public release at exactly three `.swyfmod` packages.
- Avoid broad Harmony/runtime rewrites without direct evidence.

## Review Focus

- Fallback speech recognition must receive original, unmodified microphone samples.
- Game localization export updates must not require changing a magic row count in test code.
- PrivacyTelemetry must continue compiling even though it is excluded from public packaging.
- External model/runtime inputs must be pinned and hash-verified where possible.
- Repeated release dispatches for the same tag must not race.

---

### Task 1: Preserve original audio for Whisper fallback

- [ ] Add a failing pure .NET test for normalized-copy behavior.
- [ ] Extract audio preparation from Harmony patches into `AudioProcessing.cs`.
- [ ] Pass normalized copy to Parakeet and original samples to fallback.
- [ ] Bump CzechSpeechEnhanced to 1.3.2 and update README version.
- [ ] Pin stock Parakeet download URL to the commit that introduced the verified GGUF.

### Task 2: Make localization reference checks self-describing

- [ ] Replace hard-coded `3821` with `manifest.json` `total_keys` and table counts.
- [ ] Remove unused `GameDir` from the translation test project.
- [ ] Store relative source paths in future localization manifests and clean the current manifest.
- [ ] Pin UnityPy in `tools/requirements.txt`.

### Task 3: Harden build and release maintenance

- [ ] Move the NeMo build cache outside the checked-out repository by default.
- [ ] Compile PrivacyTelemetry during `-Mod All` as validation without packaging it.
- [ ] Disable redundant artifact compression for already-zipped `.swyfmod` files.
- [ ] Serialize release runs by tag with GitHub Actions concurrency.

### Task 4: Final review and verification

- [ ] Run branch tests/build where available.
- [ ] Review diff for unintended public-package or trigger changes.
- [ ] Remove temporary branch-only TDD workflow and planning artifact before merge.
- [ ] Merge only after review; report anything that still needs a manual self-hosted run.

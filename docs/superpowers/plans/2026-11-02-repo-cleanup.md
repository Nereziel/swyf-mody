# Repository Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Zjednodušit strukturu repozitáře bez odstranění PrivacyTelemetry nebo CzechTTSFix a bez změny runtime chování veřejných modů.

**Architecture:** Každý mod bude soběstačný ve vlastní složce. Lokalizace bude mít jeden kanonický zdrojový export (`localization/original/en-cs.json`) plus manifest; runtime balíček bude nadále obsahovat jen `overrides*.json`. GitHub Actions zůstane jedinou release cestou.

**Tech Stack:** C# / .NET Framework 4.7.2, PowerShell, GitHub Actions, Python UnityPy.

**Spec:** uživatelský požadavek v aktuální konverzaci.

## Global Constraints

- Zachovat `PrivacyTelemetry`.
- Zachovat `CzechTTSFix` jako veřejný mod.
- Neměnit ID ani verze modů.
- Commitovat pouze do `Nereziel/swyf-mody`.
- Runtime `CzechTranslationFix` dál používá pouze `overrides*.json`.

## Tasks

- [ ] Přesunout volné C# soubory pod složky příslušných modů a upravit `.csproj`.
- [ ] Přepnout TranslationFix testy z `translations.json` na `localization/original/en-cs.json` a zachovat kontrolu runtime overrideů.
- [ ] Upravit exportní skript a dokumentaci na jediný kanonický export + manifest.
- [ ] Odstranit odvoditelné lokalizační kopie a `src/CzechTranslationFix/translations.json`.
- [ ] Odstranit duplicitní `runner-smoke-test.yml`, `release.cmd` a zastaralý `prepare-runner-libs.cmd`.
- [ ] Aktualizovat README/workflow reference a staticky ověřit výsledný strom.
- [ ] Po dokončení odstranit tento dočasný plán z pracovního stromu.

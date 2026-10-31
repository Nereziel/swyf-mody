# SWYF mody

Monorepo neoficiálních modů pro **Scam With Your Friends** a SWYF Mod Hub. Autorem je **Nereziel**. ASR část vychází z práce na modu Lepší čeština od Dex.

Repo je rozdělené na čtyři samostatné mody, které se sestavují a instalují nezávisle:

| Mod | Projekt | Co obsahuje |
|---|---|---|
| **CzechSpeechEnhanced** | `src/SpeechEnhanced/` | Parakeet TDT v3, Parakeet Ultra a Nemotron Unified |
| **CzechSpeechEnhancedCanary** | `src/CzechSpeechEnhancedCanary/` | Canary 1B v2 přes ONNX Runtime |
| **CzechTTSFix** | `src/CzechTTSFix/` | oprava českého textu pro Pocket TTS |
| **PrivacyTelemetry** | `src/PrivacyTelemetry/` | blokování známé analytiky a omezení diagnostiky feedbacku |

Společné ASR zdrojáky (`Plugin.cs`, `Patches.cs`, `ParakeetBridge.cs`, `TranscriptCleaner.cs`) zůstávají v `src/` a oba speech projekty je linkují při kompilaci. TTS a Privacy mají vlastní plugin entry point a do ASR DLL se nekompilují.

## Lokální sestavení

Potřebuješ:

- .NET SDK,
- nainstalovanou hru,
- BepInEx základ s Harmony v adresáři hry,
- pro Canary také Python 3.14 s `pip` při prvním sestavení runtime balíčku.

### Nejjednodušší: `build.cmd`

Spusť v kořeni repozitáře dvojklikem:

```text
build.cmd
```

Skript se zeptá na cestu ke hře a co chceš sestavit:

```text
All
Speech
Canary
TTS
Privacy
```

Výchozí volba je `All`. `build.cmd` automaticky spustí `build-package.ps1` přes PowerShell 7 (`pwsh`), pokud je dostupný, jinak použije Windows PowerShell. Použije `ExecutionPolicy Bypass`, takže není potřeba měnit systémovou PowerShell Execution Policy.

Lze ho spustit i s parametry bez interaktivních dotazů:

```cmd
build.cmd -GameDir "D:\Hry\Steam\steamapps\common\Scam With Your Friends" -Mod All
build.cmd -GameDir "D:\Hry\Steam\steamapps\common\Scam With Your Friends" -Mod Speech
build.cmd -GameDir "D:\Hry\Steam\steamapps\common\Scam With Your Friends" -Mod Canary
build.cmd -GameDir "D:\Hry\Steam\steamapps\common\Scam With Your Friends" -Mod TTS
build.cmd -GameDir "D:\Hry\Steam\steamapps\common\Scam With Your Friends" -Mod Privacy
```

### Přímé spuštění PowerShellu

Všechny čtyři mody:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

Jen konkrétní mod:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Speech
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Canary
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod TTS
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

Výstupy vzniknou v `dist/` jako samostatné `.swyfmod` soubory.

Řešení pro Visual Studio je `swyf-mody.sln`.

## CzechSpeechEnhanced

Používá jeden mod pro tři volitelné enginy:

- **Parakeet TDT v3**
- **Parakeet Ultra**
- **Nemotron Unified**

Model se volí v SWYF Mod Hubu. Parakeet/Ultra používají NeMo-Speech.cpp runtime, Nemotron používá `parakeet.cpp`. Modely nejsou součástí repozitáře; jejich URL, velikost a SHA-256 jsou v `src/SpeechEnhanced/mod.json` a Hub je stáhne zvlášť.

## CzechSpeechEnhancedCanary

Samostatný Canary 1B v2 mod. Používá ONNX převod a lokální helper server. Modelové soubory nejsou v repozitáři a Hub je stahuje podle `src/CzechSpeechEnhancedCanary/mod.json`.

Canary a CzechSpeechEnhanced se nemají zapínat současně, protože oba patchují stejnou herní speech pipeline.

## CzechTTSFix

Malý samostatný mod bez ASR modelu. Opravuje preprocessing českého Pocket TTS: zachová Unicode/diakritiku, normalizuje mezery a doplní ukončovací interpunkci tam, kde chybí.

## PrivacyTelemetry

Samostatný privacy mod. Aktuálně:

- blokuje známý `Kolkata.Analytics.AnalyticsCollector`,
- může omezit diagnostická metadata a recent logs ve feedback snapshotu,
- neblokuje AI backend, multiplayer ani všechen síťový provoz,
- explicitně přiložené nahrávky ve feedbacku zůstávají zachované.

Konfigurace:

```text
BepInEx/config/nereziel.swyf.privacytelemetry.cfg
```

## Struktura

```text
swyf-mody/
├─ swyf-mody.sln
├─ build.cmd
├─ build-package.ps1
├─ licenses/
└─ src/
   ├─ SpeechEnhanced/
   │  ├─ SpeechEnhanced.csproj
   │  └─ mod.json
   ├─ CzechSpeechEnhancedCanary/
   │  ├─ CzechSpeechEnhancedCanary.csproj
   │  └─ mod.json
   ├─ CzechTTSFix/
   │  ├─ CzechTTSFix.csproj
   │  ├─ Plugin.cs
   │  └─ mod.json
   ├─ PrivacyTelemetry/
   │  ├─ PrivacyTelemetry.csproj
   │  ├─ Plugin.cs
   │  └─ mod.json
   ├─ Plugin.cs
   ├─ Patches.cs
   ├─ ParakeetBridge.cs
   ├─ TranscriptCleaner.cs
   ├─ CzechPocketText.cs
   ├─ PrivacyPatches.cs
   ├─ canary_server.py
   └─ canary-requirements.txt
```

## Licence a třetí strany

- Původní mod: Dex / `swyf-mods`.
- NeMo-Speech.cpp: NVIDIA, Apache-2.0; oznámení jsou ve `licenses/`.
- NVIDIA Parakeet: model distribuovaný samostatně pod CC BY 4.0.
- Canary 1B v2: NVIDIA, model CC BY 4.0; použitý ONNX převod od istupakov.
- Parakeet Ultra: Moondream, GGUF převod od dawsonvosburga, CC BY 4.0.
- Nemotron Unified: NVIDIA / převod pro parakeet.cpp; OpenMDW 1.1 a související oznámení jsou ve `licenses/`.

Před veřejnou distribucí odvozeného kódu z původního modu ověř licenci původního repozitáře.

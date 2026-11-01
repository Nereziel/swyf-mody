# Scam With Your Friends mody (SWYF) – čeština, slovenština a TTS

Dva neoficiální mody pro **Scam With Your Friends** zaměřené na české a slovenské hráče.

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Co mody dělají

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Nahrazuje herní Whisper při přepisu mikrofonu modelem NVIDIA Parakeet a zlepšuje rozpoznávání češtiny a slovenštiny. |
| **CzechTTSFix** | Opravuje český text pro herní TTS, aby NPC lépe zachovala diakritiku, interpunkci a českou výslovnost. |

**Parakeet** je model pro převod mluvené řeči na text. Hra standardně používá Whisper; `CzechSpeechEnhanced` použije místo něj Parakeet TDT v3 nebo Parakeet Ultra.

> **Pozor:** tyto mody nejsou kompatibilní s modem **Lepší čeština**. Nepoužívej je současně.

## Instalace

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) stáhni požadovaný soubor `.swyfmod`.
3. Nainstaluj ho přes SWYF Mod Hub.
4. Restartuj hru.

U `CzechSpeechEnhanced` si v Mod Hubu zvolíš **Parakeet TDT v3** nebo **Parakeet Ultra**. Potřebný model se stáhne automaticky.
V Mod Hubu také nastavíš pauzu před dokončením věty: **1,3 až 3 sekundy**. Delší pauza dovolí přestávky v řeči, ale oddálí odeslání přepisu. Po změně restartuj hru.

Konfigurace přepisu je v `BepInEx/config/nereziel.swyf.czechspeechenhanced.cfg`:

- `[General] Enabled`: zapnutí modu.
- `[Recognition] Engine`, `ComputeDevice`, `LivePreview`: model, CPU/GPU a průběžný náhled (`on`/`off`).
- `[Audio] NormalizeVolume`, `SpeechThreshold`, `EndSilenceSeconds`: hlasitost, práh řeči a délka ticha před odesláním věty (1,3–3 s).
- `[Debug] LogTranscripts`: zápis rozpoznaného textu do logu.

Staré volby Whisperu, čističe a nesouvisející sekce Privacy se při načtení automaticky odstraní. Zbývající nastavení se zachovají; staré `LivePreview = auto` se převede na `on`.

## Lokální sestavení

Potřebuješ `.NET SDK` a nainstalovanou hru **Scam With Your Friends**.

Spusť:

```text
build.cmd
```

Běžné volby:

```text
All
Speech
TTS
```

`All` vytvoří v `dist/` dva veřejné balíčky:

```text
CzechSpeechEnhanced-1.3.1.swyfmod
CzechTTSFix-1.0.0.swyfmod
```

Přímo přes PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

## Vytvoření GitHub Release

Pro automatický release je potřeba [GitHub CLI](https://cli.github.com/). Poprvé se přihlas:

```text
gh auth login
```

Potom například:

```text
release.cmd 1.3.1
```

`release.cmd` sestaví oba veřejné `.swyfmod` balíčky a nahraje je do GitHub Release.

## Pokročilé

### Vlastní opravy českého překladu

Samostatný mod **CzechTranslationFix** načítá uživatelské opravy z UTF-8 souboru
`Mods/CzechTranslationFix/translations.json`. Balíček obsahuje 3821 klíčů s anglickým
a původním českým textem. Doplň pouze pole `text`; `null` a prázdné řetězce se ignorují.
Po změně restartuj hru. Před aktualizací modu si vlastní soubor zálohuj.
Podrobný postup: [návod](src/CzechTranslationFix/README.txt).

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Translation
```

Vznikne `dist/CzechTranslationFix-1.0.0.swyfmod`. Tento samostatný balíček není součástí
`All` ani automatického veřejného release. Mod mění české lokalizační tabulky v paměti;
vlastní opravené překlady neobsahuje. Funkčnost je nutné ověřit v běžící hře.

Test načítání souboru a šablony:

```powershell
dotnet run --project tests/CzechTranslationFix -- src/CzechTranslationFix/translations.json
```

V repozitáři zůstává experimentální `PrivacyTelemetry`, ale není součástí běžného buildu `All` ani veřejného release. Ruční build:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.

# Scam With Your Friends mody (SWYF) – čeština, slovenština a TTS

Dva neoficiální mody pro **Scam With Your Friends** zaměřené na české a slovenské hráče.

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Co mody dělají

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Nahrazuje herní Whisper při přepisu mikrofonu modelem NVIDIA Parakeet a zlepšuje rozpoznávání češtiny a slovenštiny. |
| **CzechTTSFix** | Opravuje český text pro herní TTS, aby NPC lépe zachovala diakritiku, interpunkci a českou výslovnost. |

**Parakeet** je model pro převod mluvené řeči na text. Hra standardně používá Whisper; `CzechSpeechEnhanced` použije místo něj Parakeet TDT v3 nebo Parakeet Ultra.

## Instalace

### Doporučeno – `.swyfmod`

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) stáhni požadovaný soubor `.swyfmod`.
3. Nainstaluj ho přes SWYF Mod Hub.
4. Restartuj hru.

U `CzechSpeechEnhanced` Mod Hub stáhne vybraný model Parakeet automaticky.

### Ruční instalace – `.zip`

ZIP je určený pro uživatele, kteří nechtějí instalovat samotný mod přes `.swyfmod`.

1. Hra už musí mít připravený základ pro mody – **BepInEx + SWYF Mod Manager**. Nejjednodušší je jednou nainstalovat SWYF Mod Hub.
2. Stáhni ZIP požadovaného modu ze stránky [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest).
3. ZIP rozbal.
4. Uvnitř najdeš jednu složku, například `CzechSpeechEnhanced`.
5. Celou tuto složku zkopíruj do:

```text
Scam With Your Friends\Mods\
```

Výsledek musí vypadat například takto:

```text
Scam With Your Friends\Mods\CzechSpeechEnhanced\mod.json
```

6. Restartuj hru.

Každý ruční ZIP obsahuje také `INSTALACE.txt` se stejným postupem. ZIP pro `CzechSpeechEnhanced` už obsahuje výchozí **Parakeet TDT v3** model, takže po zkopírování nic dalšího stahovat nemusíš. **Parakeet Ultra** v ručním ZIPu není; nejjednodušší instalace Ultra je přes `.swyfmod` a SWYF Mod Hub.

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

Build pro každý zvolený mod vytvoří v `dist/` oba formáty:

```text
CzechSpeechEnhanced-1.3.1.swyfmod
CzechSpeechEnhanced-1.3.1.zip
CzechTTSFix-1.0.0.swyfmod
CzechTTSFix-1.0.0.zip
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

`release.cmd` sestaví oba veřejné mody a nahraje jejich `.swyfmod` i ruční `.zip` varianty.

## Pokročilé

V repozitáři zůstává experimentální `PrivacyTelemetry`, ale není součástí běžného buildu `All` ani veřejného release. Ruční build:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.

Model **NVIDIA Parakeet TDT 0.6B v3** je licencovaný pod **CC BY 4.0**. Attribution a licenční informace jsou v `licenses/Parakeet-ATTRIBUTION.txt` a `licenses/Parakeet-LICENSE.txt`. Tyto soubory se přibalují do speech balíčků společně s licencemi NeMo-Speech.

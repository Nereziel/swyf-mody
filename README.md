# Scam With Your Friends mody (SWYF) – čeština, slovenština a TTS

Dva neoficiální mody pro **Scam With Your Friends** zaměřené na české a slovenské hráče.

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Co mody dělají

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Nahrazuje herní Whisper při přepisu mikrofonu modelem NVIDIA Parakeet a zlepšuje rozpoznávání češtiny a slovenštiny. |
| **CzechTTSFix** | Opravuje český text pro herní TTS, aby NPC lépe zachovala diakritiku, interpunkci a českou výslovnost. |

**Parakeet** je model pro převod mluvené řeči na text. Hra standardně používá Whisper; `CzechSpeechEnhanced` použije místo něj Parakeet TDT v3 nebo Parakeet Ultra.

## Co stáhnout

### CzechSpeechEnhanced

Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) najdeš tři varianty:

| Soubor | Pro koho je |
|---|---|
| `CzechSpeechEnhanced-...swyfmod` | **Doporučeno.** Instalace přes SWYF Mod Hub, který stáhne vybraný model automaticky. |
| `CzechSpeechEnhanced-...-with-model.zip` | Ruční instalace. Obsahuje už **Parakeet TDT v3**, takže jen rozbalíš a zkopíruješ složku do `Mods`. |
| `CzechSpeechEnhanced-...-no-model.zip` | Menší ruční balík bez GGUF modelu. Model si stáhneš sám z odkazů níže. |

### CzechTTSFix

Můžeš použít `.swyfmod` přes Mod Hub nebo obyčejný `.zip` pro ruční instalaci.

## Instalace přes `.swyfmod` – nejjednodušší

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) stáhni požadovaný `.swyfmod`.
3. Nainstaluj ho přes SWYF Mod Hub.
4. Restartuj hru.

U `CzechSpeechEnhanced` Mod Hub stáhne vybraný model Parakeet automaticky.

## Ruční instalace ZIPu

1. Hra už musí mít připravený základ pro mody – **BepInEx + SWYF Mod Manager**. Nejjednodušší je jednou nainstalovat SWYF Mod Hub.
2. Stáhni ZIP požadovaného modu.
3. ZIP rozbal.
4. Uvnitř najdeš jednu složku, například `CzechSpeechEnhanced`.
5. Celou tuto složku zkopíruj do:

```text
Scam With Your Friends\Mods\
```

Výsledek má vypadat například takto:

```text
Scam With Your Friends\Mods\CzechSpeechEnhanced\mod.json
```

6. Restartuj hru.

Každý ruční ZIP obsahuje také `INSTALACE.txt` se stejným postupem.

## Ruční stažení modelu Parakeet

Toto potřebuješ jen pokud použiješ `CzechSpeechEnhanced-...-no-model.zip`.

### Parakeet TDT v3 – doporučeno

**Stáhnout Q8 GGUF (~714 MB):**  
https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3/resolve/main/parakeet-tdt-0.6b-v3.q8_0.gguf

Stažený soubor dej sem a pojmenuj ho přesně `parakeet.q8.gguf`:

```text
Scam With Your Friends\Mods\CzechSpeechEnhanced\Parakeet\parakeet.q8.gguf
```

### Parakeet Ultra – volitelné

**Stáhnout Q8 GGUF (~714 MB):**  
https://huggingface.co/dawsonvosburg/parakeet-ultra-gguf/resolve/bb2ab0a6c9ba509e28abb1d258bb74e65ec93512/parakeet-ultra.q8_0.gguf

Stažený soubor dej sem a pojmenuj ho přesně `parakeet-ultra.q8.gguf`:

```text
Scam With Your Friends\Mods\CzechSpeechEnhanced\Parakeet\parakeet-ultra.q8.gguf
```

Pokud používáš Ultra ručně, po prvním spuštění otevři:

```text
Scam With Your Friends\BepInEx\config\nereziel.swyf.czechspeechenhanced.cfg
```

a v sekci `[Recognition]` nastav:

```ini
Engine = ultra
```

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

`Speech` vytvoří:

```text
CzechSpeechEnhanced-1.3.1.swyfmod
CzechSpeechEnhanced-1.3.1-with-model.zip
CzechSpeechEnhanced-1.3.1-no-model.zip
```

`TTS` vytvoří:

```text
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

`release.cmd` sestaví oba veřejné mody a nahraje všechny jejich veřejné varianty.

## Pokročilé

V repozitáři zůstává experimentální `PrivacyTelemetry`, ale není součástí běžného buildu `All` ani veřejného release. Ruční build:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.

Model **NVIDIA Parakeet TDT 0.6B v3** je licencovaný pod **CC BY 4.0**. Attribution a licenční informace jsou v `licenses/Parakeet-ATTRIBUTION.txt` a `licenses/Parakeet-LICENSE.txt`. Tyto soubory se přibalují do speech balíčků společně s licencemi NeMo-Speech.

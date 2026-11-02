# SWYF CZ/SK mody

Jednoduché mody pro **Scam With Your Friends**, které zlepšují hraní v češtině a slovenštině.

**Poslední ověřená verze hry:** viz [`gameversion.txt`](gameversion.txt).

Pokud ti hra špatně rozumí, české hlasy čtou text divně nebo jsou v menu chybné překlady, vyber si jen mod, který řeší právě tvůj problém. **Nemusíš instalovat všechny tři.**

**Web projektu:** https://nereziel.github.io/swyf-mody/

## Který mod potřebuji?

| Když tě trápí… | Stáhni | Co se změní |
|---|---|---|
| Hra špatně přepisuje, co říkáš do mikrofonu | **CzechSpeechEnhanced** | Hra lépe rozpozná češtinu a slovenštinu. |
| NPC čtou český text špatně a používáš Pocket TTS | **CzechTTSFix** | Český text se před přečtením správně připraví, včetně diakritiky. |
| V menu jsou chybné, nepřirozené nebo chybějící české texty | **CzechTranslationFix** | Opraví české popisky a překlady v rozhraní hry. |

`CzechTranslationFix` opravuje například špatné popisky přepínačů, slepené věty a další viditelné chyby v českém menu.

> **Pozor:** tyto mody nejsou kompatibilní s modem **Lepší čeština**. Nepoužívej je současně.

## Instalace

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Na stránce [Stažení](https://github.com/Nereziel/swyf-mody/releases/latest) stáhni požadovaný soubor `.swyfmod`.
3. Otevři ho v [SWYF Mod Hubu](https://dexhauls.github.io/swyf-mods/) a nainstaluj.
4. Restartuj hru.

Model **Parakeet TDT v3** nebo **Parakeet Ultra** nejprve stáhni přes [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/). Od `CzechSpeechEnhanced 1.4.0` ho pak vybíráš přímo v herním **nastavení rozpoznávání řeči**, vedle Whisperu a Moonshine. Přepnutí nepotřebuje restart hry; první přepis novým modelem může trvat déle kvůli jeho načtení.

Při první inicializaci integrace mod automaticky vybere kompletně stažený Parakeet (respektuje dosavadní volbu TDT/Ultra, pokud je dostupná). Další spuštění zachovají tvoji volbu, i když se vrátíš na Whisper nebo Moonshine. Chybějící nebo neúplný model se zobrazí jako nedostupný. Mod kontroluje potřebné runtime soubory, velikost modelu a hlavičku GGUF; nejde o kontrolní součet celého modelu. Nově stažený model se zpřístupní při dalším otevření nabídky modelů.

## Pro vývojáře a pokročilé

<details>
<summary>Zobrazit technické informace</summary>

### Lokální sestavení

Potřebuješ `.NET SDK` a nainstalovanou hru **Scam With Your Friends**.

```text
build.cmd
```

Běžné volby:

```text
All
Speech
TTS
Translation
```

`All` vytvoří v `dist/` všechny tři veřejné balíčky a navíc ověří, že experimentální `PrivacyTelemetry` stále jde zkompilovat. PrivacyTelemetry se do veřejného `dist/` nepřidává.

```text
CzechSpeechEnhanced-1.4.0.swyfmod
CzechTTSFix-1.0.0.swyfmod
CzechTranslationFix-1.0.3.swyfmod
```

Přímo přes PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

### GitHub Actions

Workflow **Build mods on self-hosted runner** se spouští pouze ručně přes `Actions → Run workflow`. Workflow **Build and release mods** je také pouze ruční. Oba používají stejný reusable self-hosted build, takže testy, validační pravidla a sestavení se nemohou mezi Build a Release rozjet.

Společný build kontroluje shodu verzí mezi C# pluginem a jeho `mod.json`, spouští Speech/Parakeet/Translation testy, sestaví tři veřejné mody, ověří kompilaci PrivacyTelemetry a následně otevře výsledné `.swyfmod` jako ZIP. Kontroluje jejich embedded `mod.json`, očekávanou DLL, povolený obsah a výslovně odmítne PrivacyTelemetry ve veřejném balíčku.

Self-hosted build také vypisuje SHA256 fingerprinty klíčových herních knihoven (`ScriptsAssDef.dll`, `UnityEngine.CoreModule.dll`, `BepInEx.dll` a `0Harmony.dll`). Po update hry tak lze z logu určit, proti jakým DLL byl konkrétní build ověřen.

Soubor [`gameversion.txt`](gameversion.txt) je jediný zdroj poslední ověřené verze hry. Musí obsahovat právě jeden řádek ve formátu `X.Y.Z-swyf`. Release tag používá formát `vYYYY.MM.DD-X.Y.Z` a jeho koncová verze musí odpovídat `gameversion.txt`; například `0.1.95-swyf` odpovídá tagu `v2026.11.02-0.1.95`.

Verze veřejných modů se berou z jejich `src/.../mod.json`. Pages je při deployi automaticky doplní na web spolu s verzí hry. Pages se automaticky spouští jen při změně webových souborů nebo těchto zdrojů zobrazených verzí.

Při opakovaném spuštění release pro stejný tag a stejný commit se aktualizuje název, popis, prerelease stav i `.swyfmod` assety; automaticky vygenerovaný changelog na konci popisu zůstává zachovaný. Pokud tag ukazuje na jiný commit, release se zastaví.

Jeden společný release je záměr: stránka `releases/latest` tak obsahuje všechny aktuální veřejné mody na jednom místě, i když mají každý vlastní verzi.

### CzechTranslationFix

Runtime překlady jsou v `src/CzechTranslationFix/overrides*.json`. Kanonický referenční export původní anglické a české lokalizace je v `localization/original/en-cs.json`; za běhu se nenačítá a do `.swyfmod` se nepřibaluje.

Podrobnosti jsou v [návodu k CzechTranslationFix](src/CzechTranslationFix/README.txt).

Test parseru, referenčního exportu a všech runtime override souborů:

```powershell
dotnet run --project tests/CzechTranslationFix -- localization/original/en-cs.json src/CzechTranslationFix
```

Test odmítne duplicitní kombinace `table/key` napříč různými `overrides*.json` a kontroluje strukturu referenčního exportu proti `localization/original/manifest.json`. Referenční export může být starší než některé nové runtime override klíče, proto se mezi nimi nevynucuje úplná shoda.

### PrivacyTelemetry

Experimentální `PrivacyTelemetry` zůstává v repozitáři. `All` ověřuje jeho kompilaci, ale nevytváří pro něj veřejný balíček. Samostatný ruční balíček lze stále sestavit přes:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

</details>

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.

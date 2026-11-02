# SWYF CZ/SK mody

Jednoduché mody pro **Scam With Your Friends**, které zlepšují hraní v češtině a slovenštině.

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

U `CzechSpeechEnhanced` si v [SWYF Mod Hubu](https://dexhauls.github.io/swyf-mods/) můžeš vybrat **Parakeet TDT v3** nebo **Parakeet Ultra**. Potřebný model se stáhne automaticky, takže ho nemusíš hledat ani instalovat ručně.

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

`All` vytvoří v `dist/` všechny tři veřejné balíčky:

```text
CzechSpeechEnhanced-1.3.2.swyfmod
CzechTTSFix-1.0.0.swyfmod
CzechTranslationFix-1.0.3.swyfmod
```

Přímo přes PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

### GitHub Actions

Workflow **Build mods on self-hosted runner** se spouští pouze ručně přes `Actions → Run workflow`. Na self-hosted runneru otestuje překladové podklady, sestaví všechny tři veřejné mody s oprávněním pouze pro čtení repozitáře a uloží `.swyfmod` jako workflow artifact.

Workflow **Build and release mods** se také spouští pouze ručně. Build proběhne na self-hosted runneru bez zapisovacích práv k repozitáři; samotné vytvoření GitHub Release a upload balíčků pak proběhne na GitHub-hosted runneru. Pro novější build použij nový release tag.

Jeden společný release je záměr: stránka `releases/latest` tak obsahuje všechny aktuální veřejné mody na jednom místě, i když mají každý vlastní verzi.

### CzechTranslationFix

Runtime překlady jsou v `src/CzechTranslationFix/overrides*.json`. Kanonický referenční export původní anglické a české lokalizace je v `localization/original/en-cs.json`; za běhu se nenačítá a do `.swyfmod` se nepřibaluje.

Podrobnosti jsou v [návodu k CzechTranslationFix](src/CzechTranslationFix/README.txt).

Test parseru, referenčního exportu a všech runtime override souborů:

```powershell
dotnet run --project tests/CzechTranslationFix -- localization/original/en-cs.json src/CzechTranslationFix
```

Test odmítne duplicitní kombinace `table/key` napříč různými `overrides*.json` a samostatně kontroluje strukturu referenčního exportu. Referenční export může být starší než některé nové runtime override klíče, proto se mezi nimi nevynucuje úplná shoda.

### PrivacyTelemetry

Experimentální `PrivacyTelemetry` zůstává v repozitáři, ale není součástí běžného buildu `All` ani veřejného release. Ruční build:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod Privacy
```

</details>

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods). Použité externí runtime a modely mají vlastní licence.

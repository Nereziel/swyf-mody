# SWYF mody

Neoficiální mody pro **Scam With Your Friends**.

## Nejdřív nainstaluj SWYF Mod Hub

Pro tyto mody je potřeba **SWYF Mod Hub od Dexe**:

**https://dexhauls.github.io/swyf-mods/**

Mod Hub nainstaluje potřebný **BepInEx** a umí instalovat balíčky `.swyfmod`.

## Co jednotlivé mody dělají

| Mod | Funkce |
|---|---|
| **CzechSpeechEnhanced** | Lepší rozpoznávání češtiny/slovenštiny pomocí Parakeet TDT v3 nebo Parakeet Ultra. |
| **CzechSpeechEnhancedCanary** | Alternativní rozpoznávání řeči pomocí NVIDIA Canary 1B v2. |
| **CzechTTSFix** | Opravuje zpracování českého textu pro herní TTS, hlavně diakritiku a interpunkci. |
| **PrivacyTelemetry** | Blokuje známou analytiku hry a omezuje diagnostická data odesílaná ve feedbacku. |

**CzechSpeechEnhanced a CzechSpeechEnhancedCanary nezapínej současně.** Oba upravují stejnou část rozpoznávání řeči. TTS Fix a PrivacyTelemetry lze používat nezávisle.

Speech mody čekají přibližně **1,3 sekundy ticha** před ukončením věty, aby krátká pauza při mluvení nerozdělila jednu větu na dvě.

## Instalace

1. Nainstaluj [SWYF Mod Hub](https://dexhauls.github.io/swyf-mods/).
2. Stáhni požadovaný `.swyfmod` balíček.
3. Nainstaluj ho přes SWYF Mod Hub.
4. U speech modu případně vyber model a způsob výpočtu.
5. Restartuj hru.

## Lokální sestavení

Potřebuješ `.NET SDK`, nainstalovanou hru a BepInEx. Pro sestavení Canary balíčku je navíc potřeba Python 3.14 s `pip`.

Nejjednodušší je spustit v kořeni repozitáře:

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

Výsledné balíčky vzniknou v `dist/` jako `.swyfmod`.

Lze použít i přímo PowerShell:

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Mod All
```

## Licence

Kód vytvořený v tomto repozitáři je dostupný pod MIT licencí v rozsahu uvedeném v souboru `LICENSE`.

Část speech kódu vychází z projektu [dexhauls/swyf-mods](https://github.com/dexhauls/swyf-mods) a MIT licence tohoto repozitáře nepřelicencovává cizí nebo odvozený kód. Použité externí runtime/modely mají vlastní licence.

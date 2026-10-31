# CzechSpeechEnhanced

Samostatný neoficiální mod pro **Scam With Your Friends** a SWYF Mod Hub. Autorem je **Nereziel**. Projekt byl postaven na základech modu [Lepší čeština od Dex](https://github.com/dexhauls/swyf-mods).

- Dva samostatné mody: CzechSpeechEnhanced nabízí Parakeet TDT v3, Parakeet Ultra a Nemotron Unified; CzechSpeechEnhancedCanary nabízí Canary 1B v2 INT8.
- Výběr zařízení `auto`, `cpu` nebo `gpu`. Modely Parakeet a Nemotron používají Vulkan; Canary používá NVIDIA CUDA. Při selhání akcelerace se mod pokusí o CPU.
- Zachování české diakritiky pro český Pocket TTS; výslovnost a kompatibilitu je třeba ověřit ve hře.
- Nastavitelná prodleva ukončení řeči a volby pro známý analytický sběrač a diagnostiku zpětné vazby. Nejde o úplnou blokaci síťového provozu.

## Instalace

V SWYF Mod Hubu nainstaluj buď `CzechSpeechEnhanced-1.3.0.swyfmod`, nebo `CzechSpeechEnhancedCanary-1.3.0.swyfmod`, a vyber soubory požadovaného modelu. Pro Canary jsou potřeba všechny čtyři soubory (dohromady asi 1 GB). V kartě modu vyber zařízení, vypni druhý mod i původní Lepší češtinu a restartuj hru. Oba mody se z bezpečnostních důvodů při současném zapnutí deaktivují. Mod vyžaduje 64bit Windows; GPU potřebuje Vulkan nebo u Canary NVIDIA CUDA 13 a kompatibilní ovladač. Canary používá i CPU operace. Chybějící soubory nebo opakovaná chyba vedou k použití herního Whisperu.

Při aktualizaci ze společného balíčku 1.2.0 Hub zachovává staré soubory, včetně `Mods/CzechSpeechEnhanced/Canary`. Po úspěšném přechodu je můžeš ručně odstranit; nový Canary používá vlastní složku `Mods/CzechSpeechEnhancedCanary/Canary` a nový konfigurační soubor. Pokud měla základní verze vybrán Canary, přepne se na Parakeet.

## Sestavení balíčku

Potřebuješ .NET SDK, Python 3.14 s pipem, nainstalovanou hru a její BepInEx základ s Harmony. Herní knihovny nejsou součástí projektu.

```powershell
.\build-package.ps1 -GameDir 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' -Flavor Both
```

Skript vytvoří oba balíčky do `dist`. Parametr `-Flavor Core` sestaví pouze Parakeet/Nemotron a nepotřebuje Python; `-Flavor Canary` sestaví pouze Canary. Stahované runtime skript ověřuje SHA-256. Balíček Canary s CUDA knihovnami je velký. Modely se nestahují do repozitáře; jejich adresy a kontrolní součty obsahují manifesty pro Hub.

## Nastavení

Po spuštění hry najdeš konfiguraci v `BepInEx/config/nereziel.swyf.czechspeechenhanced.cfg`. `BlockAnalytics` a `BlockFeedbackDiagnostics` řídí oddělené ochrany; blokace diagnostiky se vztahuje na známou cestu formuláře zpětné vazby, ne na všechny služby nebo lokální logy. Přiložené nahrávky zůstávají součástí ručně odeslané zpětné vazby. AI backend nadále dostává text potřebný pro odpovědi.

Základní mod používá `nereziel.swyf.czechspeechenhanced.cfg`, Canary používá vlastní `nereziel.swyf.czechspeechenhancedcanary.cfg`. Základní `Recognition/Engine` přijímá `parakeet`, `ultra` nebo `nemotron`; Canary volí vždy svůj model. U obou `Recognition/ComputeDevice` přijímá `auto`, `cpu` nebo `gpu`. Po změně restartuj hru. Server Nemotronu zatím rozpoznává jazyk automaticky, na rozdíl od Canary nerespektuje vynucený jazyk v konfiguraci. Přesnost jednotlivých modelů na konkrétním hlase je třeba porovnat na stejných větách.

## Zdroje a licence

Původní mod: Dex, [swyf-mods](https://github.com/dexhauls/swyf-mods). NeMo-Speech.cpp: NVIDIA, Apache-2.0; oznámení jsou ve složce `licenses`. Model [NVIDIA Parakeet](https://huggingface.co/nvidia/parakeet-tdt-0.6b-v3) je distribuován samostatně pod CC BY 4.0. Licenci původního modu před veřejnou distribucí ověř u autora; v jeho repozitáři není výslovně uvedena.

Canary: [NVIDIA Canary 1B v2](https://huggingface.co/nvidia/canary-1b-v2), ONNX převod od [istupakova](https://huggingface.co/istupakov/canary-1b-v2-onnx), model CC BY 4.0. Runtime používá [onnx-asr](https://github.com/istupakov/onnx-asr) a ONNX Runtime; jejich licence jsou přibaleny v balíčcích Pythonu.

Parakeet Ultra: [Moondream](https://huggingface.co/moondream/parakeet-ultra), převod GGUF od [dawsonvosburga](https://huggingface.co/dawsonvosburg/parakeet-ultra-gguf), CC BY 4.0. Nemotron Unified: [NVIDIA](https://huggingface.co/nvidia/nemotron-labs-unified-asr-0.6b), převod [mudler](https://huggingface.co/mudler/parakeet-cpp-gguf); licence OpenMDW 1.1 a oznámení jsou ve složce `licenses`. Runtime [parakeet.cpp](https://github.com/mudler/parakeet.cpp) má vlastní přiloženou licenci.

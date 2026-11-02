Czech Translation Fix 1.0.3
===========================

Instalace: otevři CzechTranslationFix-1.0.3.swyfmod v SWYF Mod Hubu.

Mod doplňuje chybějící české texty a opravuje vybrané chybné nebo slepené
překlady. Původní herní soubory se nemění; opravy se aplikují pouze v paměti
a pouze pro českou lokalizaci.

Runtime překlady jsou rozdělené do souborů overrides*.json ve stejné složce
jako plugin. Všechny tyto soubory se při startu načítají společně.

Kanonický referenční export původní anglické a české lokalizace je v repozitáři
v localization/original/en-cs.json. Slouží pouze pro kontrolu dalších aktualizací
hry, za běhu se nenačítá a do .swyfmod se nepřibaluje. Může být starší než
některé novější runtime override klíče.

Každá aktivní položka obsahuje:
  table   název lokalizační tabulky
  key     přesný klíč textu
  text    český text, který mod použije

Zachovej původní placeholdery ({0}, {name}, smart-string výrazy), značky
<size>, <color>, <sprite> a zalomení řádků. Neznámé klíče mod přeskočí a
zapíše do BepInEx/LogOutput.log.

Pokud dva override soubory obsahují stejnou kombinaci table/key, mod překlady
při startu raději vypne a chybu zapíše do logu. CI stejnou duplicitu kontroluje
ještě před sestavením veřejného balíčku. Referenční export se kontroluje
samostatně, ale nevynucuje se úplná shoda jeho klíčů s runtime override soubory.

Mod mění pouze české StringTable. Neopravuje texty v obrázcích, pevně zapsané
texty mimo lokalizační tabulky ani generované dialogy NPC.

Sestavení a statické ověření nejsou potvrzením funkčnosti v běžící hře.

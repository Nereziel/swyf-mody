Czech Translation Fix 1.0.0
===========================

Instalace: otevři CzechTranslationFix-1.0.0.swyfmod v SWYF Mod Hubu.
Mod obsahuje připravenou šablonu. Žádné vlastní překlady nejsou vyplněné.

Úpravy: po instalaci otevři soubor
  Mods/CzechTranslationFix/translations.json
v editoru, který umí UTF-8 (např. VS Code).

Každá položka obsahuje:
  table        název tabulky; neměň
  key          klíč textu; neměň
  original_en  anglický originál, pouze pomůcka
  original_cs  původní čeština, pouze pomůcka
  text         tvůj překlad

Například změň u menu.play:
  "text": null
na:
  "text": "HRÁT"

Text null nebo "" se ignoruje. Nevynucuje prázdný text.
Nemusíš vyplnit všechny položky. Nepotřebné položky můžeš smazat.
Mod čte jen table, key a text. original_en a original_cs neupravují hru.
Zachovej původní placeholdery ({0}, {name}, smart-string výrazy) a značky.
Uvnitř JSON řetězce zapisuj nový řádek jako \n, uvozovku jako \" a lomítko jako \\.
Soubor ulož v UTF-8 a restartuj hru. Ve hře vyber češtinu.

Mod mění pouze české tabulky v paměti. Funguje i pro klíče, které v české
tabulce zatím chybějí, pokud existují ve sdílených klíčích hry.
Neznámé klíče přeskočí a zapíše do logu. Neopravuje texty v obrázcích,
pevně zapsané texty mimo lokalizační tabulky ani generované dialogy NPC.
Neplatný JSON, typ textu nebo duplicitní aktivní klíče vypnou přepisování
pro dané spuštění; podrobnosti najdeš v BepInEx/LogOutput.log.
Při startu log uvádí počet načtených oprav, při použití tabulky počet aplikovaných.

Před přeinstalací nebo aktualizací modu si translations.json zálohuj;
instalátor může soubor přepsat. Ostatní překladové mody mohou přepisovat
stejné texty. Současně používej jen jeden mod měnící tyto položky.

Sestavení a statické ověření nejsou potvrzením funkčnosti v běžící hře.

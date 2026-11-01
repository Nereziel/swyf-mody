# Původní anglické a české texty SWYF

Export z lokální instalace hry ze dne 2026-11-01. Texty nejsou opravené ani normalizované.

- `original/en-cs.txt`: čitelný anglický a český text vedle sebe, s klíčem a ID.
- `original/en-cs.json`: stejné dvojice ve strukturované podobě.
- `original/en.json`, `original/cs.json`: samostatné jazyky se stejnými klíči.
- `original/manifest.json`: zdrojové soubory, SHA-256 a počty položek.

| Tabulka | Anglické položky | České položky | Chybějící české položky |
|---|---:|---:|---:|
| Menu | 3462 | 3416 | 46 |
| Controls | 296 | 0 | 296 |
| Commands | 39 | 0 | 39 |
| DemoStringTable | 24 | 0 | 24 |
| Celkem | 3821 | 3416 | 405 |

`null` v JSON znamená chybějící položku jazyka, nikoli prázdný řetězec. TXT ji označuje `[CHYBÍ]`.
Žádný chybějící český text nebyl automaticky doplněn angličtinou. To, co hra skutečně
zobrazí při chybějícím překladu, závisí na jejím nastavení fallbacku.

Export pokrývá StringTable objekty v anglickém a českém lokalizačním balíčku.
Nezahrnuje text v obrázcích, pevně zapsané řetězce ve skriptech ani generované dialogy NPC.
Číselná ID jsou uložená jako řetězce kvůli zachování přesnosti.
Diakritika, zalomení řádků, značky formátování a placeholdery zůstávají původní.
Herní soubory se při exportu nemění. Texty pocházejí ze hry a nejsou autorským kódem tohoto repozitáře.

## Opakování exportu

Python s balíčkem `UnityPy==1.25.4`:

```powershell
python tools/extract-localization.py 'D:\Hry\Steam\steamapps\common\Scam With Your Friends' localization/original
```

Skript ověřuje shodu všech exportovaných jazykových položek se zdrojem po opětovném
načtení JSON a kontroluje, že se SHA-256 zdrojových balíčků nezměnilo.

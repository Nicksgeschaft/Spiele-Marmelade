# Spiele Marmelade – Anleitung: Level aus den 4 Bausteinen bauen

Diese Anleitung ist für alle gedacht, die während der Jam ein Level bauen wollen –
auch ohne Programmiererfahrung. Alle Werkzeuge findest du in Unity oben in der
Menüleiste unter **Tools**, sortiert in Game Creation / Level Creation / Prefab
Creation / Utilities.

## Schnellstart – in 3 Schritten loslegen

1. **Tools → Game Creation → New Game...** öffnen, Namen eingeben, Bewegungsstil wählen
   (z. B. "2.5D Platformer"), **Create Game** klicken.
2. **Play** drücken – Spieler, Kamera und ein kleines Starter-Level sind schon da.
3. Eigenes Level bauen: **Tools → Level Creation → Asset Builder** öffnen und drauflos
   malen (siehe unten).

Alles Weitere in dieser Anleitung ist Hintergrundwissen, falls du mehr Kontrolle willst.

## Die 4 Bausteine

Jedes Level besteht aus genau 4 einfachen 1x1-Bausteinen:

| Baustein | Aussehen | Verhalten |
|---|---|---|
| **Boden-Platte** | flach, eckig | strukturell – stapelbar, landet im fertigen Spiel |
| **Wand-Baustein** | hoch, eckig (3× so hoch wie die Platte) | strukturell – stapelbar, landet im fertigen Spiel |
| **Runde Platte** | flach, rund | reine Deko – bleibt nur in der Szene |
| **Runder Baustein** | hoch, rund | reine Deko – bleibt nur in der Szene |

Farbe kommt nicht von der Textur, sondern vom **Material**, das du beim Malen
auswählst – dieselben 4 Bausteine sehen je nach Material völlig anders aus
(Stein, Metall, Neon, Eis, Lava, …).

Die runden Bausteine sind bewusst nur Deko (z. B. Säulen, Buttons, Verzierungen):
Sie werden nicht mit exportiert, wenn ein Spiel zur Laufzeit Wände verändern
können soll (siehe "Export to WorldData" unten).

## Asset Builder – ein Level malen

Öffnen über **Tools → Level Creation → Asset Builder**.

1. **Baustein wählen** – oben im Fenster einen der 4 Bausteine anklicken (der Tooltip
   beim Draufhalten erklärt kurz, was er tut).
2. **Material/Farbe wählen** – in der Palette weiter unten anklicken.
3. **Start Painting** klicken – das Fenster zeigt jetzt "PAINTING ACTIVE".
4. Im **Scene-View** (nicht im Game-View!) mit **Linksklick** malen. Ziehen malt
   mehrere Zellen hintereinander.
5. Zum Löschen oben auf **Erase** umschalten, dann wieder anklicken.

Weitere Einstellungen:

- **Stacking**: `Stack` baut einfach nach oben weiter, `Only` ersetzt nur wenn schon
  etwas da ist, `Stack N` ersetzt die obersten N Teile auf einmal.
- **Replace** (nur bei Brush Shape `Single`): zielt exakt auf den Brick unter der
  Maus, nicht auf den obersten in der Spalte. Der anvisierte Brick wird beim Hovern
  **pink gehighlighted**. Zeigt die Maus auf leeren Boden, wird dort ganz normal ein
  neuer Brick platziert. Bei `Rect`/`Circle`-Pinsel bleibt Replace beim alten
  Spalten-Verhalten.
- **Erase** (Brush Shape `Single`) zielt genauso exakt auf den Brick unter der Maus
  (rot gehighlighted) und löscht nur den.
- **Brush Shape**: `Single` = ein Feld, `Rect`/`Circle` = größere Flächen auf einmal
  malen (Radius-Regler), `Line` = eine gerade Reihe von N Bausteinen entlang X, Y
  (stapelt übereinander) oder Z ab dem Klickpunkt — z. B. eine 10er-Mauer mit einem
  Klick.
- **Mauer erhöhen** (dritter Brush-Modus neben Paint/Erase): Klick auf eine Mauer
  findet alle direkt verbundenen Bausteine auf derselben Höhe (grün markiert) und
  packt auf jeden davon eine weitere Reihe. Verweigert die Aktion, wenn ein Baustein
  in der Gruppe komplett umschlossen ist (keine dünne Mauer, sondern eine volle Fläche).
- **Parent Object**: Wohin die gemalten Bausteine einsortiert werden. Ohne Angabe
  "Create Root" klicken, dann landet alles sauber unter einem `LevelRoot`-Objekt.

Level fertigstellen, drei Wege je nachdem was gebraucht wird:

- **Bake to Mesh** – kombiniert alle gemalten Bausteine zu wenigen fertigen Meshes
  (schnell, gut für die Performance). Erzeugt standardmäßig auch einen `MeshCollider`
  ("Add MeshCollider to baked mesh") – exakte Kollision, aber inklusive der Noppen
  auf jedem Baustein, was sich beim Laufen leicht holprig anfühlen kann.
- **Generate Merged Box Colliders** – Alternative nur für die Kollision: führt
  benachbarte Bausteine auf derselben Höhe zu möglichst wenigen, komplett flachen
  Box-Collidern zusammen (keine Noppen, viel glatteres Laufgefühl). Landet unter einem
  eigenen `Colliders_Baked`-Objekt – kombinierbar mit Bake to Mesh (dort dann "Add
  MeshCollider" abschalten) oder mit den sichtbaren Original-Bricks.
- **Export to WorldData Asset** – exportiert das Level in ein `WorldData`-Asset,
  das zur Laufzeit von einem `BrickWorld`-Objekt geladen und verändert werden kann
  (z. B. zerstörbare Wände bei einem Bomberman-artigen Spiel). Nur Boden-Platte und
  Wand-Baustein werden dabei mitgenommen, runde Deko-Teile bleiben absichtlich außen vor.

Egal welcher Weg: **Original-Bricks nur ausblenden, wenn die gewählte Alternative auch
wirklich eine eigene Kollision mitbringt** – sonst fällt der Spieler durch den Boden
(genau das ist die häufigste Ursache für "ich kann nicht richtig laufen").

Der Asset Builder ist nicht nur für ganze Level gedacht — auch kleinere Zusammenbauten
(z. B. eine einfache Figur aus ein paar Bricks) lassen sich damit über **Parent Object**
+ Stack/Replace/Line/Mauer erhöhen zusammenstellen; als eigenes Prefab dann über das
Objekt selbst speichern (Rechtsklick im Project-Fenster → "Prefab erstellen" bzw. das
`LevelRoot`-Objekt in den Project-Ordner ziehen).

## Brick Text Generator – Texte, Logos & Buttons aus Bricks

Öffnen über **Tools → Prefab Creation → Brick Text Generator**. Baut Text komplett aus
Wand-Bausteinen (wie im "BRICKROT"/"PLAY"/"EXIT"-Stil): jeder Buchstabe ist 5 Bricks hoch
und 3 Bricks breit, mit 1 Brick Abstand zwischen Buchstaben. Die ganze Fläche wird als
durchgehende Wand gefüllt — die Buchstaben-Pixel bekommen ein Material, der Rest ein
anderes, sodass der Kontrast die Buchstaben erkennbar macht. Unterstützt werden A-Z, 0-9,
Leerzeichen sowie `! ? . -`. Kein Scene-View-Klicken nötig — rein deterministisch, ein Klick
auf **Generate** erzeugt sofort ein fertiges Prefab unter `Shared/Prefabs/Text/`.

- **Text** eintippen (wird automatisch groß geschrieben).
- **Buchstaben-Material** / **Hintergrund-Material** aus der Liste wählen.
- **Als Button nutzbar**: fügt einen passend großen Collider + `BrickTextButton` hinzu — damit
  ist das Ergebnis per Mausklick als echter 3D-Button im Spiel klickbar (`OnClicked`-Event im
  Inspector verdrahten), unabhängig vom Menu-Flow-System.

**Wichtig für dich als Regel:** Wenn künftig Menüs oder Buttons gebaut werden (auch von mir),
werden sie mit diesem Tool aus Bricks gebaut — kein normaler UI-Text.

## Icon Painter – Pixel Art aus Bricks

Öffnen über **Tools → Prefab Creation → Icon Painter**. Baut ein leeres Gitter aus Wand-Bausteinen
(gleiche Größe wie im Brick Text Generator, damit Icons und Text-Schilder zueinander passen)
und lässt dich einzelne Bricks im Scene-View anklicken, um sie umzufärben — Pixel Art, nur
mit Bricks statt Pixeln.

1. **Breite/Höhe** einstellen (in Bricks, z. B. 8x8).
2. **Hintergrund-Farbe** wählen — das ist die Startfarbe aller Bricks.
3. **Build Grid** klicken — baut das Gitter (nochmaliges Klicken baut es mit neuer
   Größe/Farbe neu auf, vorhandene Bemalung geht dabei verloren).
4. **Paint-Farbe** wählen, **Start Painting** klicken, dann im Scene-View auf einzelne
   Bricks klicken oder ziehen, um sie in der Paint-Farbe umzufärben.
5. Wenn das Icon fertig ist: Namen eingeben, **Save as Prefab** — landet unter
   `Shared/Prefabs/Icons/`.

Die Zuordnung "welches Icon gehört zu welchem Spiel" (z. B. in der Hub-Spieleauswahl) ist noch
manuell — automatische Verknüpfung ist ein möglicher späterer Schritt.

## Hub-Hauptmenü – Spieleauswahl vor allen Spielen

Anders als das Menü-Flow-System (das pro Minigame ein 2D-uGUI-Overlay für Pause/Options baut),
ist das Hub-Hauptmenü eine **3D-Welt mit klickbaren Brick-Text-Schildern** — genau wie im
"PLAY"/"EXIT"-Beispielbild. Grund: ein 2D-Canvas und echte 3D-Brick-Meshes lassen sich nicht
sinnvoll ineinander verschachteln, deshalb ein eigener, einfacherer Ansatz nur für dieses eine
Menü.

**Aufbau (einmalig in `Hub.unity` einzurichten):**

1. Mit dem **Brick Text Generator** vier Schilder bauen, z. B. Titel ("GAMEJAM UNIVERSE" o. ä.,
   *nicht* als Button), sowie "PLAY", "SETTINGS", "QUIT" (jeweils **Als Button nutzbar**
   anhaken).
2. Die Schilder in der Szene anordnen, alle vier unter ein gemeinsames leeres Eltern-Objekt
   packen (das wird gleich das `mainMenuGroup`).
3. Ein leeres `GameSelectContainer`-Objekt anlegen (wird zur Laufzeit automatisch mit der
   Spieleliste befüllt — keine manuelle Pflege nötig, neue Spiele tauchen automatisch auf).
4. Ein Options-Panel bereitstellen — am einfachsten: im **Menu Flow Editor** kurz einen
   Ein-Screen-Graphen mit nur einem `Options`-Screen erzeugen und generieren; das Ergebnis
   enthält schon eine funktionierende `OptionsPanelController`-Komponente.
5. `HubMenuController` auf ein neues Objekt packen, im Inspector zuweisen: `mainMenuGroup`,
   `gameSelectContainer`, `brickPrefab` (`Shared/Prefabs/Bricks/Brick.prefab`), zwei Materialien
   für die Spieleliste, `optionsPanel`.
6. An den drei Schildern `BrickTextButton.OnClicked` verdrahten: Play → `ShowGameSelect()`,
   Settings → `ShowOptions()`, Quit → `QuitApp()`.
7. Das alte `HubUIController`-Tab-System bleibt vorerst unangetastet — du entscheidest später,
   ob es ersetzt wird.

Icons pro Spiel in der Auswahl kommen später über ein noch zu bauendes "Objekte malen"-Tool.

## Neues Minigame anlegen

Zwei Werkzeuge stehen zur Wahl, je nachdem wie viel du direkt fertig haben willst:

### Create New Minigame (leeres Gerüst)

Über **Tools → Game Creation → Create New Minigame...** – dupliziert die Vorlage
(`_Template`) in einen neuen Ordner mit eigenem Skript, eigener Szene und eigener
Metadaten-Datei. Danach einmal **Tools → Game Creation → Game Registry** öffnen und
synchronisieren, damit das neue Spiel im Hub auftaucht.

### New Game (mit Spieler, Kamera & Start-Level)

Über **Tools → Game Creation → New Game...** – macht dasselbe wie oben, fragt aber
zusätzlich:

- **Bewegungsstil** – alle 4 sind fertig eingerichtet:
  - **2.5D Platformer (Jump'n'Run)**: WASD + Leertaste zum Springen, seitliche
    Kamera. Starter-Level: Laufstrecke mit einer Stufe zum Draufspringen.
  - **Top-Down Grid (Bomberman-Style)**: Bewegung entlang einer Achse,
    Querachse rastet sanft aufs Brick-Raster ein. Starter-Level: kleine Arena
    mit Wänden aus Bausteinen ringsum.
  - **Top-Down Free (Zelda / Twin-Stick)**: freie 8-Richtungs-Bewegung ohne
    Raster. Starter-Level: offene Fläche mit Deko.
  - **Free 3D / Third-Person**: volle 3D-Bewegung relativ zur Kamera, inkl.
    Sprung; Kamera lässt sich per Maus/rechtem Stick umschauen (Yaw/Pitch).
    Starter-Level: offene Fläche mit Deko.
- **Starter-Level einfügen**: baut automatisch ein passendes kleines Level aus
  den 4 Bricks für den gewählten Bewegungsstil.

Danach ist die Szene direkt bereit für **Play**: Spieler und passende Kamera
sind schon eingerichtet, das Win/Lose-Test-UI aus der Vorlage bleibt
zusätzlich erhalten. Am Ende synct der Wizard automatisch die Game Registry –
der manuelle Sync-Schritt entfällt hier.

Der Wizard legt außerdem automatisch ein Start-/Pause-/Options-Menü an (siehe
nächster Abschnitt) – mit einem sinnvollen Standard-Flow (Main Menu → Play →
Game, plus Options/Credits/Quit). Das lässt sich danach im Menu Flow Editor
anpassen.

## Menu Flow Editor – Start-/Pause-/Options-Menü gestalten

Öffnen über **Tools → Game Creation → Menu Flow Editor**. Hier legst du fest, welche
Menü-Screens es gibt und welcher Button wohin führt – als Diagramm, das du dir
zusammenklickst, statt es zu programmieren.

- **Screens** sind Boxen auf der Fläche links: verschiebbar, farblich nach Art
  markiert (Options = blau, Pause = orange, Game = grün – das ist kein Panel,
  sondern "hier beginnt das eigentliche Spiel").
- Screen anklicken öffnet rechts den **Inspector**: Titel, optionaler Text,
  Buttons hinzufügen/entfernen, pro Button ein Ziel-Screen oder eine
  Spezial-Aktion (Quit App / Resume Game / Restart Game) wählen.
- Die **Kurven** zwischen den Boxen zeigen automatisch, welcher Button wohin
  führt.
- **+ Screen** legt einen neuen Screen an, **Neu...** erstellt einen ganz neuen
  Graphen (z. B. für ein zweites Minigame).
- **Generate** baut daraus die echte Canvas/UI in der aktuell geöffneten
  Minigame-Szene – inklusive eines funktionierenden Options-Screens
  (Lautstärke-Regler + Vollbild-Schalter) und, sofern ein `Pause`-Screen im
  Graphen existiert, automatischem Pausieren per **Escape** während des Spiels.

**Buttons UND Titel sind Brick-Text-Schilder**, kein normales UI-Text mehr dafür
(nur noch Bodytext-Absätze und die Options-Regler-Beschriftungen bleiben uGUI,
da Brick-Text weder Fließtext noch live nachgeführte Werte kann). Der Titel
sitzt automatisch oberhalb des Button-Stapels auf derselben `MenuCamera`-Bühne.
Dafür wird automatisch eine eigene, feste `MenuCamera` gebaut, die unabhängig
von der Gameplay-Kamera zuverlässig auf Titel/Buttons zeigt – wichtig bei
Pause, wo die Gameplay-Kamera ja mitten im Spiel irgendwohin zeigen kann.
Farbe/Material: `MenuFlowGraph`-Asset im Inspector öffnen, `Button Letter
Material`/`Button Background Material` setzen (leer lassen für einen
automatischen Fallback — gilt für Titel und Buttons gleichermaßen). Schon
bestehende Menüs (z. B. Brick Dungeon) einmal neu **Generate** klicken, um
die Brick-Titel zu bekommen.

Das Options-Menü funktioniert auch dann, wenn du die Minigame-Szene ganz allein
öffnest und direkt Play drückst (ohne über den Hub zu gehen) – in dem Fall
werden die Einstellungen lokal gespeichert, statt im großen Spielstand.

Wichtig: Player/Kamera/Level müssen unter einem Objekt namens **GameplayRoot**
liegen, damit der Game-Screen sie ein-/ausblenden kann – der New Game Wizard
richtet das automatisch so ein.

## Balancing & Hooks – Achievements, Sound, Stats, Brick-VFX ohne neuen Code

Diese vier Bausteine sind reine **Verdrahtungs-Werkzeuge**: du packst eine Komponente auf ein
GameObject, trägst ein paar Werte ein und hängst sie im Inspector an ein bestehendes
**UnityEvent** (z. B. `Health.OnDamaged`, `Health.OnDeath`, `LevelExitTrigger.OnPlayerReached`,
einen Button-Klick). Kein neuer Code nötig, egal wie viele Achievements/Sounds/Effekte du
später ergänzt.

### Achievement-Hooks (an jedem Event "hooken")

1. Neue **Achievement Definition** anlegen: Rechtsklick im Project-Fenster →
   `Create → Spiele Marmelade → Achievement Definition`.
2. `Stat Key` auf **CustomEvent** stellen, `Event Key` z. B. `"EnemyKilled"` eintragen,
   `Target Value` z. B. `10` (= "10 Slimes besiegt").
3. Auf das Objekt, das das Event auslöst (z. B. `Enemy_Slime`), die Komponente
   **`AchievementEventHook`** packen, `Event Key` genauso eintragen (`"EnemyKilled"`).
4. Im Inspector das passende UnityEvent (z. B. `Health` → `On Death ()`) auf
   `AchievementEventHook.Report()` legen.
5. Fertig – jeder Slime-Tod zählt jetzt automatisch hoch, ganz ohne C#-Code. Neue
   Achievements für neue Ereignisse funktionieren genau so: neue Definition + neuer
   `Event Key`, irgendwo im Spiel einmal `Report()`/`ReportAmount(float)` dranhängen.

### Sound-Wiring (Sounds ohne Code anschließen)

1. **`SfxTrigger`**-Komponente auf ein beliebiges GameObject packen.
2. `Sfx Id` eintragen (muss in der `AudioLibrary` als Eintrag existieren) und den
   passenden `Channel` wählen (Sfx/Ui/Music/Ambient).
3. Im Inspector ein UnityEvent auf `SfxTrigger.Play()` legen (z. B. `LevelExitTrigger` →
   `On Player Reached ()`, oder ein Button → `On Click ()`).
4. Funktioniert auch, wenn die Minigame-Szene direkt geöffnet wird (ohne Hub/Boot) –
   dann passiert einfach nichts, statt dass es crasht.

### Stat-Blöcke & Modifikatoren (Balancing)

1. Neuen **Stat Block** anlegen: `Create → Spiele Marmelade → Stat Block`, z. B.
   `Stats_Slime` – darin eine Liste mit `Type` (MaxHealth/MoveSpeed/Damage/Armor/
   AttackSpeed/JumpForce) + `Base Value` pro Zeile.
2. **`CharacterStats`**-Komponente auf das Enemy/Player-Prefab packen, den Stat
   Block im Feld `Base Stats` zuweisen.
3. Sobald `CharacterStats` auf demselben Objekt sitzt wie `Health` bzw.
   `SwordAttack`, überschreiben die Werte aus dem Stat Block automatisch die
   bisherigen festen Felder (`Max Health`/Schwert-Schaden) – ohne `CharacterStats`
   verhält sich alles exakt wie vorher.
4. Buffs/Debuffs zur Laufzeit aus Code: `characterStats.AddModifier(new StatModifier
   { type = StatType.MoveSpeed, mode = StatModifierMode.PercentAdd, value = 0.5f,
   duration = 5f, sourceId = "SpeedPotion" })` – läuft nach `duration` Sekunden
   automatisch ab, oder vorzeitig entfernbar über
   `characterStats.RemoveModifiersFromSource("SpeedPotion")`.

### Brick-VFX (Effekte aus Bricks)

1. **Tools → Prefab Creation → Brick VFX Builder** öffnen.
2. Fragment-Anzahl/-Größe, Kraft- und Drehimpuls-Bereich sowie Lebensdauer
   einstellen (**Vorschau** zeigt nur Anzahl/Farbe/Größe, keine echte Physik –
   die simuliert erst im Play Mode).
3. **Als Prefab speichern** klickt ein `BrickShatterEffect`-Prefab unter
   `Shared/Prefabs/VFX` zusammen.
4. Komponente `BrickShatterEffect` auf ein Enemy/Player-Prefab packen (Farbe der
   Fragmente wird automatisch vom eigenen Renderer übernommen – Slime zerfällt
   grün, Spieler in Spielerfarbe), im Inspector `Health.OnDeath` auf
   `BrickShatterEffect.Shatter()` legen.

## Free-3D-Kampfgefühl – Lock-On, Kombo, Block/Parry, Rolle, Sprungangriff

Gilt für den **Free 3D/Third-Person**-Archetyp (`Player_ThirdPerson`-Prefab) und ist Phase 1
eines größeren Action-Kampf-Fahrplans (Zelda/Dark-Souls-Lite/Minecraft-Dungeons-Gefühl statt
Shooter). Weitere Phasen (Fähigkeiten/Schnellitems, Klettern, Inventar/Charakter/Karte) folgen
später, sobald sich dieser Kern gut anfühlt.

**Steuerung (PC):**

| Taste | Aktion |
|---|---|
| WASD | Bewegen (leicht ans Brick-Raster "eingerastet", ohne dass es sich eckig anfühlt) |
| Maus | Kamera drehen (zieht sich automatisch von Wänden zurück, clippt nicht mehr durch) |
| Linke Maustaste | Angriff – 3-Schlag-Kombo, dritter Treffer macht mehr Schaden |
| Rechte Maustaste (halten) | Blocken – reduziert Schaden; **rechtzeitig antippen** = Parry (100 % Schaden negiert) |
| Mittlere Maustaste | Lock-On – anvisiert den nächsten Gegner vor der Kamera, nochmal drücken löst es |
| Leertaste | Springen; Angriff in der Luft löst einen stärkeren Sprungangriff statt der Boden-Kombo aus |
| Shift (halten) | Sprint – **nur ohne Lock-On** |
| Shift (antippen, während Lock-On aktiv) | Ausweichrolle mit kurzer Unverwundbarkeit |

**Was neu dazugekommen ist:**

- **`LockOnController`** (neue Komponente auf `Player_ThirdPerson`): sucht beim Drücken der
  mittleren Maustaste den nächstgelegenen Gegner mit `Health`-Komponente vor der Kamera. Bricht
  automatisch ab, wenn das Ziel stirbt oder zu weit weg läuft.
- **`MeleeCombatController`** (ersetzt das bisherige `SwordAttack`): Kombo-Zähler, Block/Parry
  über die neue `Block`-Eingabe, eigener Sprungangriffs-Pfad. Nutzt weiterhin `MeleeHitbox` und,
  falls vorhanden, den `Damage`-Stat aus `CharacterStats` als Basiswert für die Kombo-Multiplikatoren.
- **`FreeThirdPersonMovement`**: dreht sich während Lock-On zum Ziel statt zur Laufrichtung
  (Bewegung wird zum Strafe), hat ein einstellbares, subtiles Brick-Raster-Bewegungsgefühl
  (`Grid Snap Strength` im Inspector) und die neue Ausweichrolle.
  `ThirdPersonOrbitCameraRig`: Wand-Kollisionsvermeidung + automatisches Kamera-Framing
  während Lock-On.
- **`Health`**: neues `IsInvulnerable` (für die Rollen-i-Frames) und `DamageMultiplier`
  (für Block/Parry) – beides von außen setzbar, für eigene Erweiterungen wiederverwendbar.

Alle neuen Werte (Kombo-Fenster, Block-Reduktion, Parry-Fenster, Dodge-Speed, Grid-Snap-Stärke,
Kamera-Kollisionspuffer, Lock-On-Reichweite/-Winkel) stehen als Inspector-Felder auf den
jeweiligen Komponenten und lassen sich ohne Code anpassen.

**Bewusst noch nicht Teil davon:** Ziel-Wechsel per Maus während Lock-On, Waffen-Wechsel-System
(Ausrüstung/Inventar). Waffentyp/schwerer Angriff und Lock-On-Sichtlinie sind mittlerweile
nachgezogen, siehe unten.

### Waffentyp & schwerer Angriff

`MeleeCombatController` hat jetzt ein `Weapon Type`-Feld (`One Handed`/`Two Handed`):

- **One Handed** (Standard, wie bisher): Rechte Maustaste blockt/pariert.
- **Two Handed**: Rechte Maustaste blockt nicht mehr, sondern löst einen **schweren
  Einzelschlag** aus (deutlich mehr Schaden, längerer Cooldown, eigene Schwungdauer – alles als
  Inspector-Felder unter "Schwerer Angriff" einstellbar). Die linke Maustaste (Kombo) verhält
  sich unverändert. Kein separates Waffen-Item nötig – einfach am `Player_ThirdPerson`-Prefab
  (oder einem eigenen Charakter mit `MeleeCombatController`) auf `Two Handed` umstellen.

### Lock-On-Sichtlinie

`LockOnController` prüft jetzt per Raycast, ob wirklich freie Sicht zum Ziel besteht:

- Man kann sich nicht mehr auf einen Gegner locken, der gerade hinter einer Wand steht.
- Ein bereits anvisiertes Ziel, das hinter Deckung läuft, bricht das Lock-On automatisch ab –
  mit einer kurzen Karenzzeit (`Los Break Grace`, Standard 0.3s), damit kurzes Verdecken durch
  eine Ecke nicht sofort stört.

## Fähigkeiten-Slots & Schnellitems (Q/R/F, 1-4)

Phase 2 des Action-Kampf-Fahrplans (nach dem Free-3D-Kampfgefühl oben). Sieben Slots, jeder
kann mit einer beliebigen "Usable"-Komponente belegt werden – kein Code nötig, um ein neues
Item/eine neue Fähigkeit in einen Slot zu stecken.

| Taste | Slot |
|---|---|
| Q | Ability1 |
| R | Ability2 |
| F | AbilitySpecial |
| 1 | QuickSlot1 – ab Werk: **Heiltrank** (`HealthPotionUsable`) |
| 2 | QuickSlot2 – ab Werk: **Buff-Trank** (`StatBuffUsable`, +50% Lauftempo für 5s) |
| 3 | QuickSlot3 – frei |
| 4 | QuickSlot4 – frei |

**So funktioniert's:** `Player_ThirdPerson` trägt 7× die Komponente `AbilitySlot` (je eine pro
Taste, über `Slot Key` im Inspector unterschieden). Jeder Slot hat ein `Usable Behaviour`-Feld –
zieh eine beliebige Komponente rein, die `IUsable` implementiert (z. B. `HealthPotionUsable`,
`StatBuffUsable`), und der Slot ruft sie beim Tastendruck auf (mit eigenem Cooldown).

**Eigene Fähigkeiten/Items ergänzen:** neue Komponente schreiben, die `IUsable` implementiert
(`CanUse(GameObject)`, `Use(GameObject)`), aufs Spieler-Objekt packen, in einen freien
`AbilitySlot` ziehen – fertig. Kein Umbau an `AbilitySlot`/`PlayerInputReader` nötig. Bomben,
Wurfmesser & Co. aus dem Original-Design brauchen zusätzlich ein Projektil-System, das es noch
nicht gibt – das ist ein guter erster eigener `IUsable`-Baustein.

**Hinweis:** Die Tasten 1/2 haben vorher ein generisches "Slot-Zyklen" (Previous/Next) gemacht,
das nirgends verdrahtet war – das ist jetzt komplett durch die Schnellitem-Slots ersetzt.

## Brick-Klettern (Strg)

Phase 3 des Action-Kampf-Fahrplans. Jede Wand aus Bricks kann kletterbar gemacht werden – ganz
ohne neue Bausteine, nur eine Marker-Komponente.

1. Auf das GameObject einer gewünschten Wand (ein einzelner platzierter Brick, z. B. ein
   "Wand-Baustein" aus dem Asset Builder) die Komponente **`ClimbableSurface`** packen.
2. Im Spiel: davor stehen, **Strg gedrückt halten** → der Spieler dreht sich zur Wand und
   klettert. **WASD** bewegt hoch/runter/seitlich an der Wand entlang.
3. **Leertaste** springt von der Wand ab (kleiner Schub weg von der Wand + normaler Sprung).
4. Strg loslassen oder das Ende der Wand erreichen beendet das Klettern automatisch.

Nicht jede Wand wird automatisch kletterbar – der Nutzer entscheidet gezielt, welche
`ClimbableSurface` bekommen (z. B. nur bestimmte Vorsprünge/Kletterwände im Dungeon, nicht jede
Mauer). Aktuell nur per Tastatur (Strg) – die Gamepad-Belegung ist nach Phase 1+2 bereits mit
allen Tasten/Triggern/Sticks/Dpad voll.

## Inventar, Charakter & Karte (Tab/I/M)

Phase 4, letzter Teil des Action-Kampf-Fahrplans. Anders als Pause (Escape) halten diese drei
Overlays das Spiel **nicht** an – Tab/I/M lassen sich unabhängig voneinander öffnen, während
weitergespielt wird.

| Taste | Screen |
|---|---|
| Tab | **Inventar** – Liste aller Items (Name x Anzahl) |
| I | **Charakter** – Health + alle konfigurierten Stats aus `CharacterStats` |
| M | **Karte** – aktuell nur ein Platzhalter ("Karte kommt, sobald es mehrere Räume gibt") |

**Items ins Spiel bringen:**

1. Neues **Item**-Asset anlegen: `Create → Spiele Marmelade → Item`, `Display Name`/`Description`
   eintragen, `Stackable`/`Max Stack` je nach Bedarf.
2. Ein GameObject mit **`ItemPickup`** in den Level stellen (Collider wird automatisch zum
   Trigger gemacht), das Item-Asset zuweisen, `Count` setzen.
3. Fertig – läuft der Spieler rein, landet das Item automatisch im Inventar (kein Interact-
   Tastendruck nötig, funktioniert wie `LevelExitTrigger`).

**Technisch:** `Player_ThirdPerson` trägt jetzt `Inventory` (reiner Laufzeit-Bestand, nicht im
Spielstand gespeichert – wie `Health`/`CharacterStats` auch) und `PlayerHudScreensController`
(baut sich sein eigenes Canvas + die drei Panels beim Start selbst, keine manuelle UI-Arbeit
nötig). Schnellslots (Q/R/F/1-4 aus Phase 2) sind bewusst noch **nicht** ans Inventar gekoppelt –
sie rufen weiterhin feste `IUsable`-Komponenten auf, unabhängig vom Item-Bestand.

## Brick Bar Builder – Lebensbalken & Slider aus Bricks

Öffnen über **Tools → Prefab Creation → Brick Bar Builder**. Baut eine Reihe/einen Stapel aus N Bricks
(Typ/Material/Anzahl/Ausrichtung frei wählbar) als Prefab – reines Formular, kein Malen im
Scene-View nötig.

**Modus `Bar`** – Segmente, die sich einzeln ein-/ausblenden lassen:

1. Brick-Typ (z. B. `PlateRound`), Material (z. B. Rot), Anzahl (z. B. 10), Ausrichtung `Row`
   wählen, **Als Prefab speichern**.
2. Prefab in die Szene ziehen (z. B. unter die Kamera, oben im Sichtfeld – für ein HUD gibt es
   keinen automatischen Schritt dafür, das Prefab muss von Hand passend platziert werden).
3. Komponente **`BrickHealthBarView`** draufpacken, `Health` (z. B. vom Spieler) zuweisen –
   fertig. Nimmt der Spieler Schaden, verschwindet automatisch der letzte Brick.
4. Eigene Zähler (nicht Health-basiert): direkt `BrickBar.SetFilledCount(int)`/`SetValue01(float)`
   aus eigenem Code aufrufen.

**Modus `Slider`** – dieselbe Schiene + ein Griff-Brick zum Ziehen:

1. Wie oben, aber Modus `Slider`, optional ein eigenes Griff-Material wählen (sonst
   Fallback-Weiß).
2. Nach dem Speichern am `BrickSliderTrack` im Inspector **`Raycast Camera`** zuweisen (z. B.
   die `MenuCamera` aus dem Menü-Flow-System) – ohne das fällt es auf `Camera.main` zurück, was
   bei einer eigenen Menü-Bühne nicht das Richtige trifft.
3. Griff-Brick anklicken und seitlich ziehen → `OnValueChanged` feuert mit dem aktuellen Wert
   (`Min Value`/`Max Value` im Inspector einstellbar, z. B. 0-1 für Lautstärke).

Noch nicht Teil davon: die eigentlichen Options-Lautstärke-Regler im Menu-Flow-System laufen
weiterhin über normale uGUI-Slider – dieses Tool liefert nur den wiederverwendbaren Baustein,
falls du sie später auf Brick-Slider umstellen willst.

## Sound-Felder überall – direkt eingebaut, kein Verdrahten nötig

Zusätzlich zum generischen `SfxTrigger` (siehe "Balancing & Hooks") haben die wichtigsten
Gameplay-Komponenten jetzt **eigene Sound-ID-Felder direkt im Inspector** – einfach eine ID
eintragen (muss in einer `AudioLibrary` existieren), fertig, kein UnityEvent-Wiring nötig. Leer
gelassen = stumm, ändert nichts am Verhalten.

| Komponente | Felder |
|---|---|
| `Health` | `Hit Sfx Id`, `Death Sfx Id` |
| `MeleeCombatController` | `Attack Sfx Id`, `Block Start Sfx Id` |
| `MeleeHitbox` | `Hit Sfx Id` (Treffer-Impact, separat vom Schwung) |
| `LockOnController` | `Lock On Sfx Id`, `Lock Off Sfx Id` |
| `SlimeEnemyAI` | `Hop Sfx Id`, `Contact Sfx Id` |
| `ItemPickup` | `Pickup Sfx Id` |
| `AbilitySlot` | `Use Sfx Id` |
| `LevelExitTrigger` | `Sfx Id` |
| `BrickTextButton` | `Click Sfx Id` |
| `BrickSliderTrack` | `Drag Start Sfx Id` |
| `MenuFlowController` | `Pause Sfx Id`, `Resume Sfx Id` |
| `AchievementDefinition`-Asset | `Unlocked Sfx Id` |
| `PlayerHudScreensController` | `Toggle Sfx Id` (Tab/I/M) |
| `FreeThirdPersonMovement` | `Jump Sfx Id`, `Dodge Sfx Id`, `Climb Start Sfx Id` |

Technisch: neue statische Klasse `SfxPlayer` (`Shared/Scripts/Audio/`) kapselt den
Dual-Mode-sicheren `AudioManager`-Zugriff – `SfxTrigger` nutzt sie jetzt intern auch. Bei
`AchievementDefinition`/`AchievementManager` (die liegen in der Core-Assembly, die `Shared`
nicht referenziert) läuft der Aufruf direkt über `GameManager`, gleiches Muster, nur ohne den
Helfer.

## Weitere Werkzeuge

- **Tools → Game Creation → Game Registry** – zeigt alle registrierten Minigames, synct
  neue Spiele in die Liste, die der Hub anzeigt.
- **Tools → Utilities → Save Inspector** – zeigt den aktuellen Spielstand, kann ihn
  öffnen oder löschen.
- **Tools → Utilities → Generate Material Presets** – erzeugt die Stil-Materialien
  (Stein, Metall, Neon, …) neu, falls mal eines fehlt.

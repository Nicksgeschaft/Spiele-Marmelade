# JumpBrickScale – Szenen-Setup

Kurzanleitung, um eine eigene Level-Szene (`JumpBrickScale_<Name>.unity`) auf denselben Stand zu
bringen. Ziel: alles Gemeinsame steckt in Prefabs, nur das Level selbst ist pro Person eigen.

## Was in die Szene gehört

| Objekt | Herkunft | Aktiv im Menü? | Zweck |
|---|---|---|---|
| `GameplayRoot` | Prefab | **nein** (wird beim Start deaktiviert) | Spieler, Kamera, Punkte-Stapel |
| `AudioBootstrap` | Prefab | ja | Ton beim direkten Szenenstart |
| `MenuCanvas`, `MenuStageRoot`, `MenuCamera`, `MenuFlowController` | Prefabs (Menu-Flow-Tool) | ja | Menüs |
| `Level`, `BackGround_Baked`, … | eigenes Leveldesign | ja | pro Person unterschiedlich |
| `WorldBrick`-Instanzen | Prefab | ja | anheftbare Bricks |
| `PointCollectable`-Instanzen | Prefab | ja | Punkte-Sammelobjekte |
| `Directional Light`, `EventSystem` | Unity-Standard | ja | – |

## Zwei Stolperfallen

**1. AudioBootstrap darf NICHT in GameplayRoot.**
`MenuFlowController.Start()` ruft `gameplayRoot.SetActive(false)` – alles darin ist im Menü aus.
Der AudioBootstrap gehört auf Root-Ebene, sonst fehlt die Menümusik.

**2. Nur EIN AudioListener.**
`Camera_SideScroll` (in `GameplayRoot`) bringt bereits einen mit. Keinen zweiten hinzufügen –
Unity warnt sonst und der Ton kann sich merkwürdig verhalten.
Der auf `Camera_SideScroll` ist der richtige: Beim Menüwechsel wird nur die *Kamera-Komponente*
abgeschaltet, das GameObject bleibt aktiv – der Listener läuft also durchgehend.

**3. Menü NICHT zum Prefab machen.**
`MenuCanvas`, `MenuStageRoot`, `MenuCamera` und `MenuFlowController` sind *generierter* Inhalt:
**Tools → Game Creation → Menu Flow Editor → „Generate"** baut sie in die aktuell offene Szene.
Wer das Menü braucht, drückt dort einmal Generate – nicht rüberkopieren.

Der Versuch, `MenuStageRoot` zum Prefab zu machen, wirft
*„Prefab instance data layout did not match…"* (ausgelöst von `Row_Fullscreen`, das zwei
verschachtelte Icon-Prefab-Instanzen plus nachträglich angehängte Komponenten enthält).
Selbst wenn es durchginge: Das nächste „Generate" baut den Inhalt in der Szene neu auf und würde
den Prefab-Link zerreißen. Dazu speichert `MenuFlowController` seine Bindings auf Objekte in
`MenuCanvas` *und* `MenuStageRoot` – diese Verdrahtung entsteht beim Generieren.

## Ablauf für eine neue Szene

1. Bestehende `JumpBrickScale.unity` als Vorlage duplizieren und umbenennen.
   (Einfachster Weg – Menü und Verdrahtung sind dann schon fertig. Alternativ: leere Szene und
   das Menü per Menu Flow Editor generieren.)
2. Eigenes Level bauen (`Level`-Objekt ersetzen/erweitern).
3. `WorldBrick`- und `PointCollectable`-Prefabs im Level verteilen.
4. Szene in die **Build Settings** eintragen – sonst kann sie sich nicht selbst neu laden, was
   „Hauptmenü → Play" und der Restart-Button brauchen.

## Änderungen teilen

Etwas an Spieler, Kamera oder Punkte-Stapel geändert und alle sollen es bekommen?
→ `GameplayRoot` in der Hierarchie anwählen → **Overrides → Apply All**.
Damit landet die Änderung im Prefab und alle Szenen ziehen automatisch nach.

Gilt genauso für `MainBrick`, `WorldBrick` und `PointCollectable` – Änderungen immer am Prefab
machen (oder per Apply zurückspielen), nie nur in der eigenen Szene.

## Einstellungen, die pro Szene stimmen müssen

- **Kamera-Grenzen:** Am `SideScrollCameraRig` (auf `Camera_SideScroll`) das Feld `Bounds Source`
  auf den Rahmen-Collider des eigenen Levels setzen, damit die Kamera nicht über den Rand schaut.
- **AudioBootstrap:** Feld `Library` auf `Shared/Data/AudioLibrary_Shared.asset`.
- **Musik-IDs:** Am `MenuFlowController` `Menu Music Id` / `Game Music Id` (aktuell `menu` / `game`).

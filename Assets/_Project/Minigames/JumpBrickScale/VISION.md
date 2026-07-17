# JumpBrickScale – Vision Dokument

> Lebendes Dokument. Wird während der Entwicklung erweitert/korrigiert, sobald sich das Spielkonzept ändert oder konkretisiert.
>
> Für die technische Umsetzung des Movement/Attachment-Systems siehe das detaillierte Anforderungsdokument: [Docs/BrickMovementController_Anforderungen_v0.2.md](Docs/BrickMovementController_Anforderungen_v0.2.md).

## Pitch

Ein 2.5D Physics-Platformer. Der Spieler steuert einen einzelnen "Brick"-Charakter durch ein Level, sammelt dabei Collectable-Bricks, die sich an seinen Körper anbauen und ihn dadurch verändern (Buffs/Debuffs), sowie runde Steine als Punkte. Ziel: In einer begrenzten Zeit möglichst viele Punkte sammeln.

## Charakter

- Besteht aus **3 flachen Bricks**, übereinander gestapelt, sodass er in der Höhe einem normalen (vollen) Brick entspricht – sieht aber "spezieller"/detaillierter aus als ein Standard-Brick.
- Hat **Noppen (Studs)**, die standardmäßig nach oben zeigen, aber je nach Ausrichtung auch nach rechts, links oder unten zeigen können.
- Der Charakter kann sich über **Physics rotieren** (kein gescriptetes Snapping) – konkret ist im aktuellen technischen Modell nur die **Rotation um die Z-Achse frei** (Position Z sowie Rotation X/Y sind gesperrt), wodurch sich seine Noppen-Ausrichtung on-plane ändert. Bewegung findet ausschließlich auf der X/Y-Ebene statt (Unity-Standardkonvention: **+Y als World-Up** für Sprung/Schwerkraft, X horizontal, Z gesperrte Tiefe/Kamera-Blickachse).
- Der Charakter-Brick (Main-Brick) ist immer der **Pivot/Root der Assembly**. Alles, was sich anbaut, hängt relativ an ihm (siehe Attachment-System im technischen Dokument).

## Level-Design

- Level-Geometrie ist so gebaut, dass **Noppen von Wänden/Plattformen immer zum Spieler zeigen** (analog zu Lego-Verbindungslogik).

## Collectable-Bricks (Attach-Mechanik)

- In der Welt verteilte Bricks in verschiedenen Farben, die wie Collectables funktionieren.
- Berührt der Spieler einen Brick mit einer bestimmten Seite, **attacht sich der Brick an genau diese Seite** des Charakters.
  - Beispiel: Brick berührt den Charakter rechts → Brick klebt rechts am Charakter → dadurch entsteht faktisch eine neue "Rechtslage" für künftige Kollisionen/Ausrichtung.
- Jeder angebaute Brick gibt dem Spieler ein **Up- oder Downgrade** (z. B. Movement Speed, Gravity, ggf. weitere Stats später).
- Dadurch baut sich der Spieler seinen Charakter **während des Levels dynamisch selbst zusammen** (Build-as-you-play).

## Punkte-Collectables

- Runde Steine im Level = reine **Punkte-Collectables** (kein Gameplay-Effekt, nur Score).

## Game Loop / Win-Condition

- Zeitbasiert: Ziel ist es, **innerhalb einer bestimmten Zeit** so viele Punkte (runde Steine) wie möglich zu sammeln.
- Bestehendes Grundgerüst (`JumpBrickScaleMinigameController`) meldet Score/Zeit über `Context.ReportScore` / `Context.ReportTime` und beendet das Spiel über `Context.CompleteGame(true/false)` – ausgelöst durch Level-Trigger (Goal-Trigger, Fall-Out-Trigger), nicht über UI-Buttons.

## Genre-Ziel

Soll sich in erster Linie als **gutes physikbasiertes Jump'n'Run** anfühlen – Physics-Feel und Steuerung haben Priorität vor Komplexität der Systeme.

## Geklärt durch das technische Anforderungsdokument (v0.2)

- Attachment ist **rein kardinal** (oben/unten/links/rechts), keine diagonalen Verbindungen; Zielzelle muss frei sein.
- Attach-Bricks bilden zusammen **einen** Rigidbody (Collider bleiben pro Brick aktiv, einzelne Rigidbodies werden deaktiviert) – stabil für den Jam-Rahmen.
- Verliert die Assembly einen verbindenden Brick, fallen alle davon getrennten Bricks per Flood-Fill vom Main-Brick aus automatisch ab.
- Stats (MoveSpeed, JumpHeight, Gravity, Weight, …) werden additiv + multiplikativ aus Basiswerten und allen verbundenen Brick-Modifikatoren aggregiert.
- Kamera folgt dem Main-Brick (nicht dem Schwerpunkt), mit Look-Ahead, Dead-Zone und einem festen Lock-Zustand am Levelende.

## Offene Punkte (TBD)

- Konkrete Liste der Upgrade/Downgrade-Effekte pro Farbe (welche Farbe = welcher Stat-Effekt, welche Werte).
- Wie viele Bricks kann der Charakter maximal anbauen? Gibt es ein Limit jenseits von Performance (P-02: mind. 12 Bricks stabil)?
- Wie genau greifen die runden Punkte-Steine ins Layer-/Trigger-System des technischen Dokuments ein (eigene Layer, Score-Event)?
- Zeitlimit-Wert und Scoring-Balance (Punkte pro Stein, Bonus für Ziel erreichen etc.).

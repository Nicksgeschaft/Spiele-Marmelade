# Minigames

Jedes Minigame bekommt seinen eigenen Ordner hier unter `Minigames/`.

## Neues Minigame anlegen

1. Den Ordner `_Template` kopieren und in den Namen des neuen Minigames umbenennen
   (z. B. `01_BrickRunner`).
2. Darin liegen bereits `Scenes/`, `Scripts/` und `Prefabs/` für dieses Minigame.
3. Die Hauptszene des Minigames in `Scenes/` ablegen.
4. Gemeinsame Dinge (Brick-Materialien, Brick-Prefabs, Player-Controller,
   UI-Bausteine, Audio) gehören nach `Assets/_Project/Shared/`, nicht in den
   Minigame-Ordner — so können alle Minigames sie wiederverwenden.

## Struktur eines Minigame-Ordners

```
Minigames/<Name>/
├── Scenes/    <- Hauptszene(n) des Minigames
├── Scripts/   <- Minigame-spezifische Logik
└── Prefabs/   <- Minigame-spezifische Prefabs
```

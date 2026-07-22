using System;
using System.Collections.Generic;
using SpieleMarmelade.Minigames.Brickrot.Survivor;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public interface ITetrisGameConfig
    {
        Vector2Int GridSizeInStuds { get; }
        float MoveSpeed { get; }
        float FallSpeedInStudsPerSecond { get; }

        GameObject BorderStudPrefab { get; }
        IEnumerable<GameObject> DamagePrefabs { get; }

        GameObject RedPrefab { get; }
        GameObject GreenPrefab { get; }
        GameObject BluePrefab { get; }
        GameObject WhitePrefab { get; }
        GameObject ForcedPrefab { get; }

        IEnumerable<GameObject> PiecePrefabs { get; }

        Camera TetrisCamera { get; }
        TetrisEvents TetrisEvents { get; }
        SurvivorEventsBase SurvivorEvents { get; }

        // No ability references here on purpose: the Tetris half announces a spawned brick via
        // TetrisEvents.OnBrickSpawned and nothing more. TetrisToSurvivorBridge listens and decides
        // what that means for the Survivor half. The original had both — a direct call from
        // TetrisGame AND the bridge — so every brick fired its ability twice.
    }
}

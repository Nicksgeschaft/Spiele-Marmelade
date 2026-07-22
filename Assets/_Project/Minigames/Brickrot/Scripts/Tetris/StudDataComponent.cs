using System;
using System.Linq;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
  public enum StudColor
  {
    Red,
    Green,
    Blue,
    White,
    Damage
  }

  public class StudDataComponent : MonoBehaviour
  {
    [NonSerialized]
    public GameObject Prefab;

    [NonSerialized]
    public Vector2Int Offset;

    public bool IsRed(ITetrisGameConfig config) => Prefab == config.RedPrefab;
    public bool IsGreen(ITetrisGameConfig config) => Prefab == config.GreenPrefab;
    public bool IsBlue(ITetrisGameConfig config) => Prefab == config.BluePrefab;
    public bool IsWhite(ITetrisGameConfig config) => Prefab == config.WhitePrefab;
    public bool IsDamage(ITetrisGameConfig config) => config.DamagePrefabs.Contains(Prefab);

    public StudColor? GetColor(ITetrisGameConfig config)
    {
      if (IsRed(config))
      {
        return StudColor.Red;
      }
      if (IsGreen(config))
      {
        return StudColor.Green;
      }
      if (IsBlue(config))
      {
        return StudColor.Blue;
      }
      if (IsWhite(config))
      {
        return StudColor.White;
      }
      if (IsDamage(config))
      {
        return StudColor.Damage;
      }

      return null;
    }
  }
}

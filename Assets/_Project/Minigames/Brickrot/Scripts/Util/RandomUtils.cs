using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    public static class RandomUtils
    {
        public static int ZeroOrOne() => Random.Range(0, 2);
        public static bool Choice() => ZeroOrOne() == 0;
        public static int OneOrNegativeOne() => OneOrNegativeOne(ZeroOrOne());
        public static int OneOrNegativeOne(int zeroOrOne) => zeroOrOne * 2 - 1;
    }
}

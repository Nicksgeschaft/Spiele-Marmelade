using System.Collections.Generic;

namespace SpieleMarmelade.Minigames.Brickrot
{
    public static class ListExtensions
    {
        /// <summary>
        /// Removes the element at <paramref name="index"/> in O(1) by moving the last element into
        /// its slot. Does NOT preserve order — only use where order is irrelevant.
        /// </summary>
        /// <remarks>
        /// The original project got this from com.unity.collections. That package isn't a
        /// dependency here and this was the only thing used from it, so it's reimplemented rather
        /// than dragging the package in.
        /// </remarks>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }
    }
}

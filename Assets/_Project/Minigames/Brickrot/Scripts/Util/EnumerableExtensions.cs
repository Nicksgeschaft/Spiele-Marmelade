using System;
using System.Collections.Generic;

namespace SpieleMarmelade.Minigames.Brickrot
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Picks one element uniformly at random in a single pass (reservoir sampling), so it works
        /// on sequences whose length isn't known up front.
        /// </summary>
        public static T RandomElement<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            T result = default!;
            int count = 0;

            foreach (var item in source)
            {
                ++count;
                if (rng.Next(count) == 0)
                {
                    result = item;
                }
            }

            if (count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            return result;
        }

        public static IEnumerable<T> ToEnumerable<T>(this Array array)
        {
            foreach (object item in array)
            {
                yield return (T)item;
            }
        }
    }
}

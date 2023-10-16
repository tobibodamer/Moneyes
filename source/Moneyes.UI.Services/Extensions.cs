using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    static class Extensions
    {
        public static bool IsNullOrEmpty(this SecureString secureString)
        {
            return secureString == null || secureString.Length == 0;
        }

        public static int IndexOfFirst<T>(this IList<T> list, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static (T min, T max) MinAndMax<T>(this IEnumerable<T> source)
            where T : IComparable<T>
        {
            using var enumerator = source.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var min = enumerator.Current;
                var max = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    if (Comparer<T>.Default.Compare(enumerator.Current, min) < 0)
                    {
                        min = enumerator.Current;
                    }

                    if (Comparer<T>.Default.Compare(enumerator.Current, max) > 0)
                    {
                        max = enumerator.Current;
                    }
                }

                return (min, max);
            }

            throw new InvalidOperationException("Sequence contains no elements.");
        }

    }
}
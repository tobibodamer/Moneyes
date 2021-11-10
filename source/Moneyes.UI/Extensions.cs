using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
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

        public static bool AddOrUpdate<T>(this IList<T> list, T newValue, Func<T, bool> predicate, 
            IComparer<T> comparer = null)
        {
            int index = IndexOfFirst(list, predicate);

            if (index == -1)
            {
                if (comparer == null)
                {
                    list.Add(newValue);
                }
                else
                {
                    InsertUsingComparer(list, comparer, newValue);
                }

                return true;
            }

            list[index] = newValue;

            return false;
        }

        private static void InsertUsingComparer<T>(IList<T> list, IComparer<T> comparer, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Compare(value, list[i]) <= 0)
                {
                    list.Insert(i, value);

                    return;
                }
            }

            list.Add(value);
        }

        public static bool AddOrUpdate<T, TSelect>(this IList<T> list, T newValue, Func<T, TSelect> selector, 
            IComparer<T> comparer = null)
        {
            if (selector == null) { return false; }

            return AddOrUpdate(list, newValue, item =>
                selector.Invoke(item).Equals(selector.Invoke(newValue)), comparer);
        }
    }
}
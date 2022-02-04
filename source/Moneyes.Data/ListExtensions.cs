using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    public static class ListExtensions
    {
        public static bool SetwiseEquivalentTo<T>(this IEnumerable<T> list, IEnumerable<T> other)
            where T : IEquatable<T>
        {
            if (list.Except(other).Any())
                return false;
            if (other.Except(list).Any())
                return false;
            return true;
        }
    }
}

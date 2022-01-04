using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
                    DispatcherHelper.InvokeIfNecessary(() => list.Add(newValue));
                }
                else
                {
                    DispatcherHelper.InvokeIfNecessary(() => InsertUsingComparer(list, comparer, newValue));
                }

                return true;
            }

            DispatcherHelper.InvokeIfNecessary(() => list[index] = newValue);

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

        /// <summary>
        /// Dynamically update a list with new values by inserting them at the right place using a <paramref name="insertComparer"/>,
        /// or replacing a existing value matched with the given <paramref name="equalityPredicate"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="newValues">The values to merge with the list.</param>
        /// <param name="equalityPredicate">A predicate that evaluates whether two values are equal.</param>
        /// <param name="insertComparer">A comparer for inserting the new values at the correct position.</param>
        /// <param name="removeOldValues">Remove old values that are not replaced by a new value.</param>
        public static void DynamicUpdate<T>(this IList<T> list,
            IEnumerable<T> newValues,
            Func<T, T, bool> equalityPredicate = null,
            IComparer<T> insertComparer = null,
            bool removeOldValues = true)
        {
            DispatcherHelper.InvokeIfNecessary(() =>
           {
               if (removeOldValues)
               {
                   var valuesToRemove = list
                               .Where(oldValue => !newValues.Any(newValue =>
                                   equalityPredicate?.Invoke(oldValue, newValue) ?? oldValue.Equals(newValue)))
                               .ToList();

                   foreach (var value in valuesToRemove)
                   {
                       list.Remove(value);
                   }
               }
               foreach (var value in newValues)
               {
                   list.AddOrUpdate(
                       value,
                       oldValue => equalityPredicate?.Invoke(value, oldValue) ?? oldValue.Equals(value),
                       insertComparer);
               }
           });
        }


        static Dictionary<string, object> ToPropertyDictionary(this object o)
        {
            return o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !Attribute.IsDefined(p, typeof(IgnoreDataMemberAttribute)))
                .ToDictionary
                (
                   p => p.Name,
                   p => p.GetValue(o)
                );
        }

        public static string ToQueryString(this object o)
        {
            return string.Join
            (
                "&",
                o.ToPropertyDictionary()
                 .Where(e => e.Value != null)
                 .Select
                 (
                     e => string.Format
                     (
                         "{0}={1}",
                         e.Key,
                         Uri.EscapeDataString(e.Value.ToString())
                     )
                 )
            );
        }

        public static string Hash(this SecureString secureString)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(secureString.ToUnsecuredString(), salt, 100000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);            
        }

        public static bool MatchWithHash(this SecureString secureString, string otherHash)
        {
            /* Extract the bytes */
            byte[] hashBytes = Convert.FromBase64String(otherHash);
            /* Get the salt */
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            /* Compute the hash on the password the user entered */
            var pbkdf2 = new Rfc2898DeriveBytes(secureString.ToUnsecuredString(), salt, 100000);
            byte[] hash = pbkdf2.GetBytes(20);

            bool match = true;

            /* Compare the results */
            for (int i = 0; i < 20; i++)
                if (hashBytes[i + 16] != hash[i])
                    match = false;

            return match;
        }
    }

    public class DispatcherHelper
    {
        public static async Task YieldIfNecessary()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(Thread.CurrentThread);
            if (dispatcher != null)
            {
                await Dispatcher.Yield();
            }
        }

        public static void InvokeIfNecessary(Action action)
        {
            if (Thread.CurrentThread == Application.Current.Dispatcher.Thread)
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }
    }
}
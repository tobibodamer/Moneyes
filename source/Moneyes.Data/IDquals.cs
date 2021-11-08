using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Data
{
    public static class IDSelectors
    {
        private static readonly Dictionary<Type, Func<object, object>> _selectors;

        public static void Register<T>(Func<T, object> selector)
        {
            Type type = typeof(T);
            object weakTypeSelector(object value) => selector((T)value);

            if (selector is null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (_selectors.ContainsKey(type))
            {
                _selectors[type] = weakTypeSelector;
            }

            _selectors.Add(type, weakTypeSelector);
        }

        public static object Resolve<T>(T instance)
        {
            Type type = typeof(T);

            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!_selectors.ContainsKey(type))
            {
                throw new KeyNotFoundException("Selector not registered.");
            }

            return _selectors[type](instance);
        }
    }

    public static class IDSelectorExtensions
    {
        public static bool Idquals<T>(this T obj, T other)
        {
            object lhsId = IDSelectors.Resolve<T>(obj);
            object rhsId = IDSelectors.Resolve<T>(other);

            return lhsId.Equals(rhsId);
        }
    }
}

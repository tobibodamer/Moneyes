using Moneyes.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Moneyes.UI.Blazor.Components.CategoryTreeView;
using static Moneyes.UI.Blazor.Stores.St;

namespace Moneyes.UI.Blazor.Stores
{
    public record TransactionSession
    (
        IReadOnlyDictionary<Guid, (Category category, Expenses expenses)>? ExpensesPerCategory,
        Guid? SelectedCategory,
        HashSet<CategoryTreeItem> CategoryTreeItems
    );
    public class TransactionStore
    {
        private static readonly TransactionSession DefaultState = new(null, Category.AllCategoryId, new HashSet<CategoryTreeItem>());

        private static readonly dynamic Dispatchers = DefineDispatchers<TransactionSession, MyDispatchers>();

        private static readonly DispatchingStore<TransactionSession> _store = new(DefaultState, Dispatchers);

        private class MyDispatchers
        {
            public static TransactionSession SetExpenses(
                TransactionSession curr,
                IEnumerable<(Category Category, Expenses Expenses)>? expenses) => curr with
                {
                    ExpensesPerCategory = expenses.ToDictionary(exp => exp.Category.Id, exp => exp)
                };

            public static TransactionSession SetSelectedCategory(
                TransactionSession curr,
                Guid? id) => curr with { SelectedCategory = id };

            public static TransactionSession SetCategories(
                TransactionSession curr,
                HashSet<CategoryTreeItem> categories) => curr with
                {
                    CategoryTreeItems = categories
                };
        }

        private static IEnumerable<Transaction> TransactionsFromState(TransactionSession state)
        {
            if (state.SelectedCategory == null ||
                state.ExpensesPerCategory == null ||
                !state.ExpensesPerCategory.TryGetValue(state.SelectedCategory.Value, out var expenses))
            {
                return Enumerable.Empty<Transaction>();
            }

            return expenses.expenses.Transactions;
        }

        public IObservable<IEnumerable<Transaction>> TransactionsO { get; } = _store.Subject
            .Select(TransactionsFromState)
            .DistinctUntilChanged(x => x.Select(t => t.UID), EnumerableComparer<string>.Default);

        public IEnumerable<Transaction> GetTransactions()
            => TransactionsFromState(_store.Value);

        public bool HasExpenses() => _store.Value.ExpensesPerCategory?.Any() ?? false;

        //public void SetExpenses(IReadOnlyDictionary<Category, Expenses>? expenses)
        //{
        //    _store.Dispatch(nameof(MyDispatchers.SetExpenses), expenses);
        //}

        public void SetSelectedCategory(Guid? id)
        {
            _store.Dispatch(nameof(MyDispatchers.SetSelectedCategory), id);
        }

        public Guid? GetSelectedCategory() => _store.Value.SelectedCategory;

        public IObservable<Guid?> SelectedCategoryIdO { get; } = _store.Subject.Select(state =>
        {
            return state.SelectedCategory;
        }).DistinctUntilChanged();

        public IObservable<Category?> SelectedCategoryO { get; } = _store.Subject.Select(state =>
        {
            return state.SelectedCategory != null
                ? state.ExpensesPerCategory.GetValueOrDefault(state.SelectedCategory!.Value).category
                : null;
        }).DistinctUntilChanged();

        public IEnumerable<(Category Category, Expenses Expenses)> GetExpensesPerCategory() => _store.Value.ExpensesPerCategory?.Values
            ?? Enumerable.Empty<(Category Category, Expenses Expenses)>();

        public IObservable<IEnumerable<(Category Category, Expenses Expenses)>> ExpensesPerCategoryO { get; }
            = _store.Subject.Select(state => state.ExpensesPerCategory?.Values
                ?? Enumerable.Empty<(Category Category, Expenses Expenses)>());

        public void SetExpenses(IEnumerable<(Category Category, Expenses Expenses)>? expenses)
        {
            _store.Dispatch(nameof(MyDispatchers.SetExpenses), expenses);
        }
        public HashSet<CategoryTreeItem> GetCategories()
            => _store.Value.CategoryTreeItems;

        public void SetCategories(HashSet<CategoryTreeItem> categories)
        {
            _store.Dispatch(nameof(MyDispatchers.SetCategories), categories);
        }

        public IObservable<HashSet<CategoryTreeItem>> CategoryTreeItemsO { get; }
            = _store.Subject.Select(state => state.CategoryTreeItems)
                .DistinctUntilChanged(value => value.Select(i => i.Category), EnumerableComparer<Category>.Default);
    }

    public class EnumerableComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public static EnumerableComparer<T> Default { get; } = new EnumerableComparer<T>();

        public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
        {
            // Compare the Reference
            return // If they both are list, Compare using Sequenece Equal
                   x.SequenceEqual(y);
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            unchecked
            {
                return obj
                        .Select(e => e.GetHashCode())
                        .Aggregate(17, (a, b) => 23 * a + b);
            }
        }
    }

    public class DeepComparer<T> : IEqualityComparer<T>
    {
        public static DeepComparer<T> Default { get; } = new DeepComparer<T>();

        public bool Equals(T x, T y)
        {
            // Compare the Reference
            return ReferenceEquals(x, y) ||
                   // Using Default Comparer to comparer the value
                   EqualityComparer<T>.Default.Equals(x, y) ||
                   // If they both are list, Compare using Sequenece Equal
                   x is IEnumerable enumerableX &&
                   y is IEnumerable enumerableY &&
                   enumerableX.Cast<object>().SequenceEqual(enumerableY.Cast<object>());
        }

        public int GetHashCode(T obj)
        {
            unchecked
            {
                return obj is IEnumerable enumerable
                    ? enumerable.Cast<object>()
                        .Select(e => e.GetHashCode())
                        .Aggregate(17, (a, b) => 23 * a + b)
                    : obj.GetHashCode();
            }
        }
    }

    class DispatchingStore<TStore>
    {
        private record DispatchType(
            string Dispatcher,
            object? Payload);

        private readonly BehaviorSubject<TStore> _state;
        private readonly Dictionary<string, GenericDispatcherFunc<TStore>> _dispatchers;
        private readonly Subject<DispatchType> _dispatches = new();

        public BehaviorSubject<TStore> Subject => _state;
        public TStore Value => _state.Value;

        public DispatchingStore(TStore initialValue, IEnumerable<Dispatcher<TStore>> dispatchers)
        {
            _state = new BehaviorSubject<TStore>(initialValue);
            _dispatchers = dispatchers.ToDictionary(d => d.Name, d => d.DispatcherFunc);

            _dispatches.Subscribe((dispatch) =>
            {
                var nextState = _dispatchers[dispatch.Dispatcher](Value, dispatch.Payload);

                _state.OnNext(nextState);
            });
        }

        public void Dispatch(string dispatcher, object? payload = null)
        {
            if (!_dispatchers.ContainsKey(dispatcher))
            {
                throw new Exception($"Undefined dispatch type '${dispatcher}'");
            }

            _dispatches.OnNext(new(dispatcher, payload));
        }
    }

    internal record Dispatcher<TStore>(string Name, GenericDispatcherFunc<TStore> DispatcherFunc);

    static class St
    {
        public delegate TStore GenericDispatcherFunc<TStore>(TStore currentVal, object? payload);
        public delegate TStore DispatcherFunc<TStore, TPayload>(TStore currentVal, TPayload payload);


        public static Dispatcher<TStore>[] DefineDispatchers<TStore, TDispatchers>()
            where TDispatchers : class, new()
        {
            var methods = typeof(TDispatchers).GetMethods().Where(m =>
                m.ReturnType == typeof(TStore) &&
                m.GetParameters().First().ParameterType == typeof(TStore)
                );

            var dispatchers = new TDispatchers();

            return methods.Select(m =>

                new Dispatcher<TStore>
                (
                    Name: m.Name,
                    DispatcherFunc: (state, payload) =>
                        (TStore)m.Invoke(dispatchers, new object[] { state!, payload })!
                )
            ).ToArray();
        }
    }

    //public delegate (Func<T?> Get, Action<T?> Set) CreateCustomRef<T>(Action track, Action trigger, Action<Action> onDispose);
    //public static class Ref
    //{
    //    public static Ref<T> Create<T>(T initialValue)
    //    {
    //        return new()
    //        {
    //            Value = initialValue
    //        };
    //    }

    //    public static Ref<T> Custom<T>(CreateCustomRef<T> create)
    //    {
    //        return new CustomRef<T>(create);
    //    }

    //    public static Ref<T> ToRef<T>(this IObservable<T> stream, T initialValue, Action<T?> setter)
    //        => UseStream(stream, initialValue, setter);

    //    public static Ref<T> ToRef<T>(this BehaviorSubject<T> subject, Action<T?> setter)
    //        => subject.ToRef(subject.Value, setter);

    //    public static Ref<T> UseStream<T>(IObservable<T> stream, T initialValue, Action<T?> setter)
    //    {
    //        return Ref.Custom<T>((track, trigger, onDispose) =>
    //        {
    //            T value = initialValue;

    //            var sub = stream.Subscribe((val) =>
    //            {
    //                if (EqualityComparer<T>.Default.Equals(val, value))
    //                {
    //                    return;
    //                }

    //                value = val;
    //                trigger();
    //            });

    //            onDispose(() =>
    //            {
    //                if (sub != null)
    //                {
    //                    sub.Dispose();
    //                }
    //            });

    //            return
    //            (
    //                Get: () =>
    //                {
    //                    track();
    //                    return value;
    //                },
    //                Set: (T? val) =>
    //                {
    //                    if (EqualityComparer<T>.Default.Equals(val, value))
    //                    {
    //                        return;
    //                    }

    //                    value = val;
    //                    trigger();
    //                    setter(val);
    //                }
    //            );
    //        });
    //    }




    //    //public static Ref<K> Select<T, K>(this Ref<T> @ref, Func<T, K> selector)
    //    //{
    //    //    return Custom((track, trigger, onDispose) =>
    //    //    {

    //    //        void onPropertyChanged(object? sender, EventArgs e)
    //    //        {
    //    //            trigger();
    //    //        }

    //    //        @ref.PropertyChanged += onPropertyChanged;

    //    //        onDispose(() =>
    //    //        {
    //    //            @ref.PropertyChanged -= onPropertyChanged;
    //    //        });


    //    //        return (
    //    //            () =>
    //    //            {
    //    //                track();
    //    //                return selector(@ref.Value);
    //    //            },
    //    //      (K value) =>
    //    //      {
    //    //      trigger();

    //    //        typeof(T).a @ref.Value =
    //    //   },
    //    //)
    //    //})
    //    // }
    //}

    //public class CustomRef<T> : Ref<T>
    //{
    //    private readonly Func<T?> _getter;
    //    private readonly Action<T?> _setter;
    //    private Action? _onDispose;
    //    internal CustomRef(Func<T?> getter, Action<T?> setter)
    //    {
    //        _getter = getter;
    //        _setter = setter;
    //    }

    //    internal CustomRef(CreateCustomRef<T> create)
    //    {
    //        (_getter, _setter) = create(OnValueRead, OnValueChanged, (dispose) => _onDispose = dispose);
    //    }

    //    protected override T? GetValue()
    //    {
    //        return _getter();
    //    }

    //    protected override void SetValue(T? value)
    //    {
    //        _setter(value);
    //    }

    //    public override void Dispose()
    //    {
    //        base.Dispose();

    //        _onDispose?.Invoke();
    //    }
    //}
    //public class Ref<T> : INotifyPropertyChanged, IDisposable
    //{
    //    public event PropertyChangedEventHandler? PropertyChanged;

    //    private T? _value;

    //    public T? Value
    //    {
    //        get => GetValue();
    //        set => SetValue(value);
    //    }

    //    protected virtual T? GetValue()
    //    {
    //        OnValueRead();
    //        return _value;
    //    }

    //    protected virtual void SetValue(T? value)
    //    {
    //        if (EqualityComparer<T>.Default.Equals(_value, value))
    //        {
    //            return;
    //        }

    //        _value = value;
    //        OnValueChanged();
    //    }

    //    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    //    {
    //        PropertyChanged?.Invoke(this, new(propertyName));
    //    }

    //    protected void OnValueChanged()
    //    {
    //        OnPropertyChanged(nameof(Value));
    //    }

    //    protected void OnValueRead() { }

    //    public virtual void Dispose() { }
    //}
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
namespace Moneyes.UI.JSON
{
    public sealed class DestructureTypeConverter<T, TDestructured> : JsonConverter
        where T : class
        where TDestructured : class
    {
        private readonly Func<T, TDestructured> _destructure;
        private readonly Func<TDestructured, T> _resolve;

        public DestructureTypeConverter(
            Func<T, TDestructured> destructure, Func<TDestructured, T> resolve)
        {
            _destructure = destructure;
            _resolve = resolve;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            T val = value as T;

            serializer.Serialize(writer, _destructure(val));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(TDestructured))
            {
                return _resolve(existingValue as TDestructured);
            }

            return existingValue;
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.JSON
{
    class SingleOrArrayConverter<TItem> : SingleOrArrayConverter<List<TItem>, TItem> { }
    class SingleOrArrayConverter<TCollection, TItem> : JsonConverter<TCollection>
        where TCollection : class, ICollection<TItem>, new()
    {
        public override TCollection ReadJson(JsonReader reader, Type objectType, TCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            switch (token.Type)
            {
                case JTokenType.Null:
                    return null;
                case JTokenType.Array:
                    return token.ToObject<TCollection>();
                default:
                    return new TCollection { token.ToObject<TItem>() };
            }
        }

        public override void WriteJson(JsonWriter writer, TCollection value, JsonSerializer serializer)
        {
            if (!CanWrite) { return; }

            if (value.Count == 1)
            {
                serializer.Serialize(writer, value.First());
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}

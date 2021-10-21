using Moneyes.Core.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Moneyes.Core.JSON
{
    class SaleConditionFilterJsonConverter : ConditionFilterJsonConverter<Transaction> { }
    class ConditionFilterJsonConverter<T> : JsonConverter<IConditionFilter<T>>
    {
        public override IConditionFilter<T> ReadJson(JsonReader reader, Type objectType,
            IConditionFilter<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            // Getting selector from json
            var selector = token.Value<string>("Selector");

            // Getting type of selector
            var selectorType = typeof(T).GetProperty(selector).PropertyType;
            
            // Make generic condition filter type
            var conditionType = typeof(ConditionFilter<,>).MakeGenericType(typeof(T), selectorType);

            // Make generic 'token.ToObject<ConditionFilter<T, TSelector>()' method
            var toObjectMethod = typeof(JToken).GetMethods()
                .Single(m => m.Name == "ToObject" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                .MakeGenericMethod(conditionType);

            // Deserializing as generic condition filter object
            var conditionFilter = toObjectMethod
                .Invoke(token, null);

            return conditionFilter as IConditionFilter<T>;
        }

        public override void WriteJson(JsonWriter writer, IConditionFilter<T> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

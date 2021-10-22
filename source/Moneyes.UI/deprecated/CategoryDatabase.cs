using Moneyes.Core;
using Moneyes.UI.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    internal class CategoryDatabase : JsonDatabase<Category>
    {
        public CategoryDatabase(string fileName)
            : base(fileName, c => c.Name, isReadOnly: false)
        {
            JsonReadSettings.Converters = new List<JsonConverter>()
            {
                new ConditionFilterJsonConverter<Transaction>()
            };
        }
    }
}

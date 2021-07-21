using MoneyesParser.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyesParser
{
    public class Categories
    {
        public static Category[] LoadFromJson(string filePath = "categories.json")
        {
            using StreamReader r = new(filePath);
            string json = r.ReadToEnd();

            Category[] deserializedCategories = JsonConvert.DeserializeObject<Category[]>(
                json, new ConditionFilterJsonConverter<ISale>());

            return deserializedCategories;
        }

        public static void WriteToJson(Category[] categories, string filePath = "categories.json")
        {
            using StreamWriter w = new(filePath);
            var json = JsonConvert.SerializeObject(categories);

            w.Write(json);
        }
    }
}

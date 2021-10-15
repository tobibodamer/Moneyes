using Moneyes.Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.Core
{
    public class Category
    {
        public string Name { get; set; }
        public SalesFilter Filter { get; set; }
    }
}

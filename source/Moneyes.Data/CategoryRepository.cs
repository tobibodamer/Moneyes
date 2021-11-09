using LiteDB;
using Moneyes.Core;
using System.Linq;

namespace Moneyes.Data
{
    public class CategoryRepository : CachedRepository<Category>
    {
        public CategoryRepository(ILiteDatabase db) 
            : base(db)
        {
            Collection.EnsureIndex(c => c.Name, true);
            Collection = Collection.Include(c => c.Parent);
        }

        public Category FindByName(string name)
        {
            //return Collection.FindOne(c => c.Name.Equals(name));
            return Cache.Values.FirstOrDefault(c => c.Name.Equals(name));
        }

        public override Category Create(Category entity)
        {
            if (entity == Category.NoCategory)
            {
                return null;
            }

            return base.Create(entity);
        }

        public override bool Set(Category entity)
        {
            if (entity == Category.NoCategory)
            {
                return false;
            }

            return base.Set(entity);
        }
    }
}

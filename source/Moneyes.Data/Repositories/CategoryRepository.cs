using LiteDB;
using Moneyes.Core;
using System.Linq;

namespace Moneyes.Data
{
    public class CategoryRepository : CachedRepository<Category>
    {
        protected override ILiteCollection<Category> Collection => base.Collection
            .Include(c => c.Parent);
        public CategoryRepository(IDatabaseProvider dbProvider)
            : base(dbProvider)
        {
        }

        protected override ILiteCollection<Category> CreateCollection()
        {
            var collection = base.CreateCollection();

            collection.EnsureIndex(c => c.Name, true);

            return collection;
        }

        public Category FindByName(string name)
        {
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

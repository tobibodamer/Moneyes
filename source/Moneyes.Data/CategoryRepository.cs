using LiteDB;
using Moneyes.Core;

namespace Moneyes.Data
{
    public class CategoryRepository : BaseRepository<Category>
    {
        public CategoryRepository(ILiteDatabase db) : base(db)
        {
            Collection.EnsureIndex(c => c.Name, true);
        }

        public Category FindByName(string name)
        {
            return Collection.FindOne(c => c.Name.Equals(name));
        }

        public override Category Create(Category entity)
        {
            if (entity == Category.NoCategory)
            {
                return null;
            }

            return base.Create(entity);
        }

        public override void Set(Category entity)
        {
            if (entity == Category.NoCategory)
            {
                return;
            }

            base.Set(entity);
        }
    }
}

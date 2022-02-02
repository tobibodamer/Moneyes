namespace Moneyes.Core
{
    public static class CategoryExtensions
    {
        public static bool IsAllCategory(this Category category)
        {
            return category.Id.Equals(Category.AllCategoryId);
        }

        public static bool IsNoCategory(this Category category)
        {
            return category.Id.Equals(Category.NoCategoryId);
        }
    }
}

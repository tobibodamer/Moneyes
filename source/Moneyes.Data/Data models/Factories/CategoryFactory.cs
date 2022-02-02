using Moneyes.Core;

namespace Moneyes.Data
{
    public class CategoryFactory : ICategoryFactory
    {
        private readonly IFilterFactory _filterFactory;

        public CategoryFactory(IFilterFactory filterFactory)
        {
            _filterFactory = filterFactory;
        }

        public Category CreateFromDbo(CategoryDbo categoryDbo)
        {
            var filterDto = categoryDbo.Filter != null
                ? _filterFactory.CreateTransactionFilterFromDto(categoryDbo.Filter)
                : null;
            var parentDbo = categoryDbo.Parent != null
                ? CreateFromDbo(categoryDbo.Parent)
                : null;

            return new Category(categoryDbo.Id)
            {
                Filter = filterDto,
                IsExlusive = categoryDbo.IsExlusive,
                Name = categoryDbo.Name,
                Parent = parentDbo,
                Target = categoryDbo.Target
            };
        }
    }
}

using Moneyes.Core;

namespace Moneyes.Data
{
    public interface ICategoryFactory
    {
        Category CreateFromDbo(CategoryDbo categoryDbo);
    }
}

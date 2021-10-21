using System.Globalization;
using System.Windows.Controls;

namespace Moneyes.UI.View
{
    internal class NullStringValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string valueString = value?.ToString();

            if (string.IsNullOrEmpty(valueString))
            {
                return new(false, "Field cannot be empty");
            }

            return new(true, null);
        }
    }
}

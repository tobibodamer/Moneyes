using System.Globalization;
using System.Windows.Controls;

namespace Moneyes.UI.View
{
    class BankCodeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string valueString = value?.ToString();

            if (string.IsNullOrEmpty(valueString))
            {
                //return new(false, "Bank code is empty");
                return new(true, null);
            }

            if (!int.TryParse(valueString, out var bankCode))
            {
                return new(false, "Invalid characters");
            }

            //if (valueString.Length != 8)
            //{
            //    return new(false, "Bank code must be 8 characters");
            //}


            return new(true, null);
        }
    }
}

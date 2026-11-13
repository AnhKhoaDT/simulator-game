using System.Globalization;
using Startup.Shared;

namespace Startup.Core.Localization
{
    // Turns game money into text for the active language. One game unit is 1,000 VND; English shows US dollars
    // at 25,000 VND per dollar, so prices and the wallet switch with the language.
    public static class Money
    {
        public const int VndPerUnit = 1000;
        public const int VndPerUsd = 25000;

        private static bool IsVietnamese => LocalizationService.CurrentCode == LanguageCodes.Vietnamese;

        // Full amount, e.g. "30.000 VND" or "$1.20".
        public static string Format(long units)
        {
            if (IsVietnamese)
            {
                var vnd = units * VndPerUnit;
                return vnd.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.') + " VND";
            }

            var dollars = units * VndPerUnit / (double)VndPerUsd;
            return "$" + dollars.ToString("N2", CultureInfo.InvariantCulture);
        }

        // Compact form for small tiles, e.g. "50k" or "$2".
        public static string Short(int units)
        {
            if (IsVietnamese)
            {
                return units + "k";
            }

            var dollars = units * VndPerUnit / (double)VndPerUsd;
            return "$" + dollars.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}

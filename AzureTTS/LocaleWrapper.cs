using System.Globalization;

namespace AzureTTS
{
    public record LocaleWrapper(CultureInfo? Culture)
    {
        public override string ToString()
        {
            if (Culture is null)
                return "All";

            return Culture.Name;
        }
    }
}
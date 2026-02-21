using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ValidadorDeCartoes.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public string CardNumber { get; set; }

        public string Result { get; set; }

        public void OnGet()
        {

        }

        public void OnPost()
        {
            var cleaned = (CardNumber ?? string.Empty);
            // remove non-digit characters
            cleaned = Regex.Replace(cleaned, "\\D", "");

            if (string.IsNullOrEmpty(cleaned))
            {
                Result = "Por favor informe o número do cartão.";
                return;
            }

            if (cleaned.Length < 12 || cleaned.Length > 19)
            {
                Result = "Número de cartão com tamanho inválido.";
                return;
            }

            var brand = DetermineCardBrand(cleaned);

            // Validate with Luhn algorithm
            var luhn = IsLuhnValid(cleaned);

            // Determine if the sequence likely belongs to a real card:
            // consider it real when Luhn is valid and the brand is recognized
            var isReal = IsProbablyReal(brand, luhn);

            Result = $"Bandeira: {brand}. A sequência apresentada pertence a um {(isReal ? "cartão real" : "cartão falso")}.";
        }

        private static bool IsProbablyReal(string brand, bool luhnValid)
        {
            if (!luhnValid)
                return false;

            if (string.IsNullOrEmpty(brand) || brand == "Desconhecida")
                return false;

            // Additional simple checks could be added here (length per brand, known BINs, etc.)
            return true;
        }

        // Determine brand according to rules provided
        private string DetermineCardBrand(string digits)
        {
            // American Express - starts with 34 or 37
            if (Regex.IsMatch(digits, "^(34|37)"))
                return "American Express";

            // MasterCard - 51-55 or 2221-2720
            if (Regex.IsMatch(digits, "^(5[1-5])") || IsInRangePrefix(digits, 2221, 2720, 4))
                return "MasterCard";

            // Discover - 6011, 65, or 644-649
            if (Regex.IsMatch(digits, "^(6011)") || Regex.IsMatch(digits, "^(65)") || IsInRangePrefix(digits, 644, 649, 3))
                return "Discover";

            // HiperCard - generally starts with 6062
            if (Regex.IsMatch(digits, "^(6062)"))
                return "HiperCard";

            // Elo - check some common prefixes
            var eloPrefixes = new[] { "4011", "4312", "4389", "4514", "4573", "4576", "5041", "5066", "5099", "6277",
            "6362", "6363", "6500", "6504", "6505", "6516", "6550"};
            foreach (var p in eloPrefixes)
            {
                if (digits.StartsWith(p))
                    return "Elo";
            }

            // Visa - starts with 4 (but check after Elo to avoid conflict with Elo prefixes like 4011)
            if (digits.StartsWith("4"))
                return "Visa";

            return "Desconhecida";
        }

        private static bool IsInRangePrefix(string digits, int start, int end, int prefixLength)
        {
            if (digits.Length < prefixLength)
                return false;

            if (!int.TryParse(digits.Substring(0, prefixLength), out var value))
                return false;

            return value >= start && value <= end;
        }

        // Luhn algorithm
        private static bool IsLuhnValid(string number)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(number[i]))
                    return false;

                int n = number[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10) == 0;
        }
    }
}

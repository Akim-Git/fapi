using System.Globalization;

public interface IRateService
{
    Task<Dictionary<string, decimal>> GetRatesCzkPerUnitAsync(CancellationToken ct);
}

public sealed class CnbRateService : IRateService
{
    // oficiální endpoint ČNB pro denní kurzy
    private const string Url =
        "https://www.cnb.cz/cs/financni_trhy/devizovy_trh/kurzy_devizoveho_trhu/denni_kurz.txt";

    private readonly HttpClient _http;
    // cache: kdy jsem data stáhl + samotné kurzy
    private (DateTimeOffset at, Dictionary<string, decimal> rates)? _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly object _lock = new();

    public CnbRateService(HttpClient http) => _http = http;

    public async Task<Dictionary<string, decimal>> GetRatesCzkPerUnitAsync(CancellationToken ct)
    {
        // nejdřív kontroluju, jestli nemám čerstvá data v cache
        lock (_lock)
        {
            if (_cache is { } c && DateTimeOffset.UtcNow - c.at < Ttl) return c.rates;
        }

        // pokud cache nemám nebo je stará, stáhnu data z ČNB
        var text = await _http.GetStringAsync(Url, ct);
        // tady si textový soubor převedu na mapu měn
        var rates = ParseDenniKurz(text);

        // uložím do cache
        lock (_lock) _cache = (DateTimeOffset.UtcNow, rates);
        return rates;
    }

    // formát: datum na 1. řádku, hlavička na 2. řádku, pak Země|Měna|Množství|Kód|Kurz
    private static Dictionary<string, decimal> ParseDenniKurz(string text)
    {

        // tady si rozdělím celý text z ČNB na jednotlivé řádky
        // odstraním prázdné řádky a ořežu mezery
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 3) throw new InvalidOperationException("CNB format unexpected.");

        // tady si připravím slovník kurzů
        // rovnou tam dávám CZK = 1, protože je to základní měna
        var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["CZK"] = 1m
        };

        for (int i = 2; i < lines.Length; i++)
        {
            // každý řádek má formát: Země | Měna | Množství | Kód | Kurz
            var parts = lines[i].Split('|');
            if (parts.Length < 5) continue;

            var amount = ParseDec(parts[2]);              // Množství
            var code = parts[3].Trim().ToUpperInvariant();// Kód
            var rate = ParseDec(parts[4]);                // Kurz

            if (amount <= 0 || rate <= 0) continue;

            // ČNB uvádí: amount jednotek = rate CZK
            // já to chci mít jednotně:
            // 1 jednotka měny = (rate / amount) CZK
            dict[code] = rate / amount;
        }

        return dict;

        static decimal ParseDec(string s)
            => decimal.Parse(s.Trim().Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}

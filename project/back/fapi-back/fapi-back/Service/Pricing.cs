public static class Pricing
{
    public const decimal VatRate = 0.21m;
    public static decimal Round2(decimal n) => Math.Round(n, 2, MidpointRounding.AwayFromZero);
}

using System.Numerics;

namespace SimpsonParadox;

public sealed record SearchResult(int MaxTotal, long Limit, long Examined, long Reversals,
    BigInteger TotalConfigurations, Dataset? Best, Rational BestMagnitude)
{
    public bool Exhausted => Examined == TotalConfigurations;
}

public static class DatasetSearch
{
    public static SearchResult Run(int maxTotal = 8, long maxConfigurations = 10_000_000)
    {
        if (maxTotal is < 1 or > 1000) throw new ArgumentException("max-total must be between 1 and 1000.");
        if (maxConfigurations < 1) throw new ArgumentException("max-configurations must be positive.");
        var cellCount = (BigInteger)maxTotal * (maxTotal + 3) / 2;
        var totalConfigurations = BigInteger.Pow(cellCount, 4);
        long examined = 0, reversals = 0;
        Dataset? best = null;
        var bestMagnitude = Rational.Zero;
        // Each cell is ordered by total, then successes. Ties retain the first dataset.
        foreach (var a1 in Cells(maxTotal))
        foreach (var a2 in Cells(maxTotal))
        foreach (var b1 in Cells(maxTotal))
        foreach (var b2 in Cells(maxTotal))
        {
            if (examined == maxConfigurations)
                return new(maxTotal, maxConfigurations, examined, reversals, totalConfigurations, best, bestMagnitude);
            examined++;
            var direction1 = Compare(a1.Successes, a1.Total, b1.Successes, b1.Total);
            var direction2 = Compare(a2.Successes, a2.Total, b2.Successes, b2.Total);
            if (direction1 == 0 || direction1 != direction2) continue;
            var overallDirection = Compare(a1.Successes + a2.Successes, a1.Total + a2.Total,
                b1.Successes + b2.Successes, b1.Total + b2.Total);
            if (overallDirection != -direction1) continue;
            reversals++;
            var magnitude = new Rational(BigInteger.Abs(
                (long)(a1.Successes + a2.Successes) * (b1.Total + b2.Total)
                - (long)(b1.Successes + b2.Successes) * (a1.Total + a2.Total)),
                (long)(a1.Total + a2.Total) * (b1.Total + b2.Total));
            if (best is not null && magnitude.CompareTo(bestMagnitude) <= 0) continue;
            best = new(new(a1.Successes, a1.Total), new(a2.Successes, a2.Total),
                new(b1.Successes, b1.Total), new(b2.Successes, b2.Total));
            bestMagnitude = magnitude;
        }
        return new(maxTotal, maxConfigurations, examined, reversals, totalConfigurations, best, bestMagnitude);
    }

    // With maxTotal <= 1000, these cross products fit comfortably in Int64.
    private static int Compare(int s1, int n1, int s2, int n2) => ((long)s1 * n2).CompareTo((long)s2 * n1);
    private static IEnumerable<(int Successes, int Total)> Cells(int bound)
    {
        for (var n = 1; n <= bound; n++)
            for (var s = 0; s <= n; s++) yield return (s, n);
    }
}

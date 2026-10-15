using System.Numerics;

namespace SimpsonParadox;

public sealed record Analysis(Dataset Data, Rational Weight1, Rational StandardizedA,
    Rational StandardizedB, string? SubgroupWinner, bool IsReversal, Rational ReversalMagnitude)
{
    public Rational Weight2 => Rational.One - Weight1;
    public Rational RawGap => Data.OverallA - Data.OverallB;
    public Rational StandardizedGap => StandardizedA - StandardizedB;
    // These signed contributions sum to RawGap - StandardizedGap.
    public Rational Group1CompositionEffect =>
        (Data.Group1ShareA - Weight1) * Data.A1.Rate - (Data.Group1ShareB - Weight1) * Data.B1.Rate;
    public Rational Group2CompositionEffect =>
        (Weight1 - Data.Group1ShareA) * Data.A2.Rate - (Weight1 - Data.Group1ShareB) * Data.B2.Rate;

    public static Rational PooledWeight(Dataset d) => new((BigInteger)d.A1.Total + d.B1.Total,
        (BigInteger)d.A1.Total + d.A2.Total + d.B1.Total + d.B2.Total);

    public static Analysis Calculate(Dataset d, Rational weight1)
    {
        if (weight1 < Rational.Zero || weight1 > Rational.One)
            throw new ArgumentException("Group 1 weight must be between zero and one.");
        string? winner = d.A1.Rate > d.B1.Rate && d.A2.Rate > d.B2.Rate ? "A"
            : d.A1.Rate < d.B1.Rate && d.A2.Rate < d.B2.Rate ? "B" : null;
        var gap = d.OverallA - d.OverallB;
        var reversal = winner == "A" && gap < Rational.Zero || winner == "B" && gap > Rational.Zero;
        var magnitude = !reversal ? Rational.Zero : winner == "A" ? Rational.Zero - gap : gap;
        return new(d, weight1, weight1 * d.A1.Rate + (Rational.One - weight1) * d.A2.Rate,
            weight1 * d.B1.Rate + (Rational.One - weight1) * d.B2.Rate, winner, reversal, magnitude);
    }
}

using SimpsonParadox;
using System.Numerics;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
void Reject(Action action)
{
    try { action(); } catch (ArgumentException) { checks++; return; }
    throw new Exception("Expected invalid input to be rejected.");
}

Check(new Rational(2, -4) == new Rational(-1, 2), "Normalization");
Check(default(Rational) == Rational.Zero, "Default zero");
Check(new Rational(BigInteger.Pow(10, 80), 3) > new Rational(BigInteger.Pow(10, 79), 2), "Large exact comparison");
Reject(() => new Rational(1, 0));
Reject(() => new Cell(0, 0));
Reject(() => new Cell(-1, 2));
Reject(() => new Cell(3, 2));
var d = Dataset.Demo;
var a = Analysis.Calculate(d, new(1, 2));
Check(a.IsReversal && a.SubgroupWinner == "A", "Demo reversal");
Check(d.OverallA == new Rational(29, 110) && d.OverallB == new Rational(81, 110), "Demo totals");
Check(a.StandardizedA == new Rational(11, 20) && a.StandardizedB == new Rational(9, 20), "Standardization");
Check(a.ReversalMagnitude == new Rational(26, 55), "Magnitude");
Check(a.Group1CompositionEffect + a.Group2CompositionEffect == a.RawGap - a.StandardizedGap, "Composition identity");
Check(Analysis.Calculate(new(d.B1, d.B2, d.A1, d.A2), new(1, 2)).SubgroupWinner == "B", "Treatment swap");
Check(Analysis.Calculate(new(d.A2, d.A1, d.B2, d.B1), new(1, 2)).ReversalMagnitude == a.ReversalMagnitude, "Group swap");
Check(!Analysis.Calculate(new(new(0, 1), new(1, 1), new(0, 1), new(1, 1)), new(1, 2)).IsReversal, "Ties and endpoints");
Reject(() => Analysis.Calculate(d, new(2, 1)));
for (var w = 0; w <= 20; w++)
    Check(Analysis.Calculate(d, new(w, 20)).StandardizedGap > Rational.Zero, "Shared weights preserve dominance");
var large = new Dataset(new(int.MaxValue, int.MaxValue), new(int.MaxValue, int.MaxValue), new(0, 1), new(0, 1));
Check(large.OverallA == Rational.One, "Totals do not overflow");
Reject(() => DatasetSearch.Run(0));
Reject(() => DatasetSearch.Run(1001));
Reject(() => DatasetSearch.Run(4, 0));
// Independent reference: enumerate totals first, then all successes; construct
// every dataset and classify using rational analysis instead of the search fast path.
long referenceCount = 0;
var referenceMagnitude = Rational.Zero;
for (var na = 1; na <= 4; na++)
for (var nb = 1; nb <= 4; nb++)
for (var nc = 1; nc <= 4; nc++)
for (var nd = 1; nd <= 4; nd++)
for (var sa = 0; sa <= na; sa++)
for (var sb = 0; sb <= nb; sb++)
for (var sc = 0; sc <= nc; sc++)
for (var sd = 0; sd <= nd; sd++)
{
    var candidate = Analysis.Calculate(new(new(sa, na), new(sb, nb), new(sc, nc), new(sd, nd)), new(1, 2));
    if (!candidate.IsReversal) continue;
    referenceCount++;
    if (candidate.ReversalMagnitude > referenceMagnitude) referenceMagnitude = candidate.ReversalMagnitude;
    Check(candidate.SubgroupWinner == "A" ? candidate.StandardizedGap > Rational.Zero : candidate.StandardizedGap < Rational.Zero,
        "Standardization invariant across exhaustive reference space");
}
var search = DatasetSearch.Run(4);
Check(search.Exhausted && search.Examined == 38_416, "Exact exhaustive count");
Check(search.Reversals == referenceCount && search.BestMagnitude == referenceMagnitude, "Independent reference agrees");
Check(search.Best is not null && Analysis.Calculate(search.Best, new(1, 2)).ReversalMagnitude == referenceMagnitude, "Best dataset valid");
Check(DatasetSearch.Run(4) == search, "Deterministic search");
Check(!DatasetSearch.Run(4, 100).Exhausted && DatasetSearch.Run(4, 100).Examined == 100, "Prefix budget");
Check(DatasetSearch.Run(4, 38_416).Exhausted, "Exact boundary budget");
Check(DatasetSearch.Run(1).Best is null, "No reversal at smallest bound");
Console.WriteLine($"PASS: {checks} checks; reference reversals: {referenceCount}");

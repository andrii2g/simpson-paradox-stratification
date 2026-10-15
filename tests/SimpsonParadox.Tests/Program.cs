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
Console.WriteLine($"PASS: {checks} checks");

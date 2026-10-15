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
var asymmetric = new Dataset(new(1, 2), new(3, 4), new(1, 3), new(2, 5));
Check(Analysis.PooledWeight(asymmetric) == new Rational(5, 14), "Pooled weights use all four totals");
var pooled = Analysis.Calculate(asymmetric, Analysis.PooledWeight(asymmetric));
Check(pooled.StandardizedA == new Rational(37, 56) && pooled.StandardizedB == new Rational(79, 210), "Unequal pooled standardization");
Check(pooled.Group1CompositionEffect + pooled.Group2CompositionEffect == pooled.RawGap - pooled.StandardizedGap, "Unequal composition decomposition");
var json = Report.Json(a, null, "equal");
using (var parsed = System.Text.Json.JsonDocument.Parse(json))
    Check(parsed.RootElement.GetProperty("analysis").GetProperty("overall").GetProperty("a").GetProperty("fraction").GetString() == "29/110", "JSON preserves exact fractions");
var svg = Report.Svg(a, null, "equal");
var xml = System.Xml.Linq.XDocument.Parse(svg);
System.Xml.Linq.XNamespace ns = "http://www.w3.org/2000/svg";
Check(xml.Descendants(ns + "rect").Count(e => (string?)e.Attribute("fill") is "#126caa" or "#b84d17") == 6, "Six treatment bars");
Check(xml.Descendants(ns + "text").Any(e => e.Value == "29/110"), "SVG exact count labels");
using (var parsed = System.Text.Json.JsonDocument.Parse(Report.Json(null, DatasetSearch.Run(1), "equal")))
    Check(parsed.RootElement.GetProperty("analysis").ValueKind == System.Text.Json.JsonValueKind.Null, "Empty search JSON");
Check(System.Xml.Linq.XDocument.Parse(Report.Svg(null, DatasetSearch.Run(1), "equal")).Descendants(ns + "text").Any(e => e.Value.Contains("No strict reversal")), "Empty search SVG");
var culture = System.Globalization.CultureInfo.CurrentCulture;
try
{
    System.Globalization.CultureInfo.CurrentCulture = new("fr-FR");
    Check(Report.Svg(a, search, "equal") == WithInvariantSvg(), "SVG is culture independent");
}
finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
string WithInvariantSvg()
{
    var previous = System.Globalization.CultureInfo.CurrentCulture;
    try { System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; return Report.Svg(a, search, "equal"); }
    finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
}
Console.WriteLine($"PASS: {checks} checks; reference reversals: {referenceCount}");

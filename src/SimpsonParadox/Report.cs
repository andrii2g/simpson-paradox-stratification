using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace SimpsonParadox;

public static class Report
{
    public static string Points(Rational rate) => (100 * rate.ToDouble()).ToString("0.00", CultureInfo.InvariantCulture);
    private static object Metric(Rational rate) => new { fraction = rate.ToString(), percent = rate.Percent() };

    public static string Json(Analysis? a, SearchResult? search, string weights)
    {
        object? analysis = null;
        if (a is not null)
        {
            var d = a.Data;
            object Group(string name, Cell x, Cell y, Rational shareA, Rational shareB) => new
            {
                name,
                a = new { successes = x.Successes, total = x.Total, rate = Metric(x.Rate), share = Metric(shareA) },
                b = new { successes = y.Successes, total = y.Total, rate = Metric(y.Rate), share = Metric(shareB) },
                gap = Metric(x.Rate - y.Rate)
            };
            analysis = new
            {
                groups = new[] { Group("Group 1", d.A1, d.B1, d.Group1ShareA, d.Group1ShareB),
                    Group("Group 2", d.A2, d.B2, Rational.One - d.Group1ShareA, Rational.One - d.Group1ShareB) },
                overall = new { a = Metric(d.OverallA), b = Metric(d.OverallB), gap = Metric(a.RawGap) },
                standardized = new { weights, group1Weight = a.Weight1.ToString(), group2Weight = a.Weight2.ToString(),
                    a = Metric(a.StandardizedA), b = Metric(a.StandardizedB), gap = Metric(a.StandardizedGap) },
                compositionEffects = new { group1 = Metric(a.Group1CompositionEffect), group2 = Metric(a.Group2CompositionEffect),
                    total = Metric(a.RawGap - a.StandardizedGap) },
                subgroupWinner = a.SubgroupWinner, strictReversal = a.IsReversal, reversalMagnitude = Metric(a.ReversalMagnitude)
            };
        }
        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            units = "Fractions are exact rates. Percent fields are rounded; gaps/effects represent percentage points.",
            search = search is null ? null : new { maxTotal = search.MaxTotal, limit = search.Limit,
                examined = search.Examined, reversals = search.Reversals,
                totalConfigurations = search.TotalConfigurations.ToString(CultureInfo.InvariantCulture),
                exhausted = search.Exhausted, selectionScope = search.Exhausted ? "entire bounded space" : "examined prefix" },
            analysis
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    public static void Write(string output, Analysis? a, SearchResult? search, string weights)
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "report.json"), Json(a, search, weights), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output, "report.svg"), Svg(a, search, weights), new UTF8Encoding(false));
    }

    public static string Svg(Analysis? a, SearchResult? search, string weights)
    {
        var svg = new StringBuilder("""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="890" viewBox="0 0 1200 890" role="img" aria-labelledby="title desc">
            <title id="title">Simpson's paradox: group rates and combined rates</title>
            <desc id="desc">Three grouped bar charts with a common zero to one hundred percent scale, followed by group proportions and direct standardization. Exact counts accompany the bars.</desc>
            <rect width="1200" height="890" fill="#f3f6fa"/>
            <style>text{font-family:Segoe UI,Arial,sans-serif;fill:#18283c} .muted{fill:#526277} .small{font-size:14px} .label{font-size:17px} .heading{font-size:22px;font-weight:700}</style>
            """);
        void Text(double x, double y, string text, string css = "label", string anchor = "start") =>
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x}\" y=\"{y}\" class=\"{css}\" text-anchor=\"{anchor}\">{System.Security.SecurityElement.Escape(text)}</text>\n");
        void Rect(double x, double y, double width, double height, string fill) =>
            svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x:0.###}\" y=\"{y:0.###}\" width=\"{width:0.###}\" height=\"{height:0.###}\" rx=\"3\" fill=\"{fill}\"/>\n");
        const string colorA = "#126caa", colorB = "#b84d17";
        Text(40, 47, "SIMPSON'S PARADOX", "heading");
        Text(40, 76, "Exact counts. Different population mixes. A reversed aggregate.", "label muted");
        var scope = search is null ? "Deterministic synthetic example" :
            FormattableString.Invariant($"Search: {search.Examined:N0} examined; {search.Reversals:N0} reversals; ") + (search.Exhausted ? "complete bounded space" : "examined prefix only");
        Text(40, 103, scope, "small muted");
        if (a is null)
        {
            Text(40, 175, "No strict reversal found in the examined configurations.", "heading");
            return svg.Append("</svg>\n").ToString();
        }
        var d = a.Data;
        void Chart(int panel, string title, Rational rateA, Rational rateB, string countA, string countB)
        {
            var left = 40 + panel * 390;
            Rect(left, 125, 360, 365, "#ffffff");
            Text(left + 180, 158, title, "heading", "middle");
            for (var tick = 0; tick <= 100; tick += 25)
            {
                var y = 420 - tick * 2;
                svg.Append($"<path d=\"M {left + 58} {y} H {left + 335}\" stroke=\"#dce3eb\"/>\n");
                Text(left + 48, y + 5, $"{tick}%", "small muted", "end");
            }
            void Bar(int x, Rational rate, string count, string treatment, string color)
            {
                var h = 200 * rate.ToDouble();
                Rect(x, 420 - h, 76, h, color);
                Text(x + 38, 410 - h, rate.Percent(), "label", "middle");
                Text(x + 38, 446, treatment, "label", "middle");
                Text(x + 38, 471, count, "small muted", "middle");
            }
            Bar(left + 90, rateA, countA, "A", colorA);
            Bar(left + 225, rateB, countB, "B", colorB);
            var winner = rateA > rateB ? "A" : rateA < rateB ? "B" : "Neither";
            Text(left + 180, 192, $"{winner} leads", "label", "middle");
        }
        Chart(0, "Group 1", d.A1.Rate, d.B1.Rate, $"{d.A1.Successes}/{d.A1.Total}", $"{d.B1.Successes}/{d.B1.Total}");
        Chart(1, "Group 2", d.A2.Rate, d.B2.Rate, $"{d.A2.Successes}/{d.A2.Total}", $"{d.B2.Successes}/{d.B2.Total}");
        Chart(2, "Combined population", d.OverallA, d.OverallB,
            $"{(BigInteger)d.A1.Successes + d.A2.Successes}/{(BigInteger)d.A1.Total + d.A2.Total}",
            $"{(BigInteger)d.B1.Successes + d.B2.Successes}/{(BigInteger)d.B1.Total + d.B2.Total}");
        Rect(40, 510, 1140, 145, "#ffffff");
        Text(60, 541, "WHY THE AGGREGATE CHANGES", "heading");
        Text(60, 573, $"A allocation: Group 1 {d.Group1ShareA.Percent()} / Group 2 {(Rational.One - d.Group1ShareA).Percent()}");
        Text(60, 602, $"B allocation: Group 1 {d.Group1ShareB.Percent()} / Group 2 {(Rational.One - d.Group1ShareB).Percent()}");
        Text(60, 632, $"The subgroup loser leads overall by {Points(a.ReversalMagnitude)} percentage points.");
        Rect(40, 675, 1140, 135, "#ffffff");
        Text(60, 706, "COMPARE THE SAME POPULATION MIX", "heading");
        Text(60, 736, $"Shared weights ({weights}): Group 1 {a.Weight1.Percent()}, Group 2 {a.Weight2.Percent()}");
        Text(60, 771, $"Standardized A: {a.StandardizedA.Percent()}     B: {a.StandardizedB.Percent()}     A - B: {Points(a.StandardizedGap)} pp", "heading");
        Text(40, 844, "Standardization changes the reference population; it does not establish a causal treatment effect.", "small muted");
        Text(40, 870, "Rates are exact fractions internally. Percentages are rounded only for display. Blue = A; orange = B.", "small muted");
        return svg.Append("</svg>\n").ToString();
    }
}

using System.Globalization;
using SimpsonParadox;

try
{
    if (args.Length == 0 || args[0] is "--help" or "-h")
    {
        Console.WriteLine("""
            demo   [--weights equal|pooled] [--output artifacts/demo]
            search [--max-total 8] [--max-configurations 10000000]
                   [--weights equal|pooled] [--output artifacts/search]
            Search totals must be 1..1000. Counts include all examined candidates.
            """);
        return 0;
    }
    var command = args[0];
    if (command is not ("demo" or "search")) throw new ArgumentException("Expected demo or search. Use --help.");
    var options = new Dictionary<string, string>();
    for (var i = 1; i < args.Length; i += 2)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Missing value for {args[i]}.");
        var key = args[i];
        if (key is not ("--weights" or "--output" or "--max-total" or "--max-configurations"))
            throw new ArgumentException($"Unknown option: {key}");
        if (command == "demo" && (key is "--max-total" or "--max-configurations"))
            throw new ArgumentException($"{key} is only valid for search.");
        if (!options.TryAdd(key, args[i + 1])) throw new ArgumentException($"Duplicate option: {key}");
    }
    var weights = options.GetValueOrDefault("--weights", "equal");
    if (weights is not ("equal" or "pooled")) throw new ArgumentException("weights must be equal or pooled.");
    var output = options.GetValueOrDefault("--output", Path.Combine("artifacts", command));
    SearchResult? search = null;
    Dataset? dataset = Dataset.Demo;
    if (command == "search")
    {
        if (!int.TryParse(options.GetValueOrDefault("--max-total", "8"), NumberStyles.None, CultureInfo.InvariantCulture, out var bound)
            || !long.TryParse(options.GetValueOrDefault("--max-configurations", "10000000"), NumberStyles.None, CultureInfo.InvariantCulture, out var limit))
            throw new ArgumentException("Search bounds must be positive integers within their supported range.");
        search = DatasetSearch.Run(bound, limit);
        dataset = search.Best;
        Console.WriteLine($"Examined {search.Examined} / {search.TotalConfigurations}; reversals {search.Reversals}.");
        Console.WriteLine(search.Exhausted ? "Exhausted the bounded space." : "Stopped at the configuration limit; results describe only the examined prefix.");
    }
    var analysis = dataset is null ? null : Analysis.Calculate(dataset,
        weights == "equal" ? new Rational(1, 2) : Analysis.PooledWeight(dataset));
    Report.Write(output, analysis, search, weights);
    if (analysis is null) Console.WriteLine("No strict reversal found in the examined configurations.");
    else
    {
        var d = analysis.Data;
        Console.WriteLine($"Group 1: A {d.A1.Successes}/{d.A1.Total} ({d.A1.Rate.Percent()}), B {d.B1.Successes}/{d.B1.Total} ({d.B1.Rate.Percent()})");
        Console.WriteLine($"Group 2: A {d.A2.Successes}/{d.A2.Total} ({d.A2.Rate.Percent()}), B {d.B2.Successes}/{d.B2.Total} ({d.B2.Rate.Percent()})");
        Console.WriteLine($"Raw: A {d.OverallA.Percent()} [{d.OverallA}], B {d.OverallB.Percent()} [{d.OverallB}]");
        Console.WriteLine($"Group 1 shares: A {d.Group1ShareA.Percent()}, B {d.Group1ShareB.Percent()}");
        Console.WriteLine($"Shared weights ({weights}): {analysis.Weight1}, {analysis.Weight2}");
        Console.WriteLine($"Standardized: A {analysis.StandardizedA.Percent()} [{analysis.StandardizedA}], B {analysis.StandardizedB.Percent()} [{analysis.StandardizedB}]");
        Console.WriteLine($"Subgroup winner: {analysis.SubgroupWinner ?? "none"}; strict reversal: {analysis.IsReversal}; magnitude: {Report.Points(analysis.ReversalMagnitude)} pp");
    }
    Console.WriteLine($"Reports: {Path.GetFullPath(output)}");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 2;
}

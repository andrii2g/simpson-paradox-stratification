using SimpsonParadox;

var analysis = Analysis.Calculate(Dataset.Demo, new Rational(1, 2));
Console.WriteLine($"Raw: A {analysis.Data.OverallA.Percent()}, B {analysis.Data.OverallB.Percent()}");
Console.WriteLine($"Standardized: A {analysis.StandardizedA.Percent()}, B {analysis.StandardizedB.Percent()}");
Console.WriteLine($"Strict reversal: {analysis.IsReversal}");

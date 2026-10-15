using System.Numerics;

namespace SimpsonParadox;

public sealed record Cell
{
    public int Successes { get; }
    public int Total { get; }
    public Rational Rate => new(Successes, Total);
    public Cell(int successes, int total)
    {
        if (total <= 0 || successes < 0 || successes > total)
            throw new ArgumentException("A cell needs a positive total and successes between zero and total.");
        Successes = successes;
        Total = total;
    }
}

public sealed record Dataset(Cell A1, Cell A2, Cell B1, Cell B2)
{
    public static Dataset Demo => new(new(9, 10), new(20, 100), new(80, 100), new(1, 10));
    public Rational OverallA => Combined(A1, A2);
    public Rational OverallB => Combined(B1, B2);
    public Rational Group1ShareA => new(A1.Total, (BigInteger)A1.Total + A2.Total);
    public Rational Group1ShareB => new(B1.Total, (BigInteger)B1.Total + B2.Total);
    private static Rational Combined(Cell x, Cell y) => new((BigInteger)x.Successes + y.Successes, (BigInteger)x.Total + y.Total);
}

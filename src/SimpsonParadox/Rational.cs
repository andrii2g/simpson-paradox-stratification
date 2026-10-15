using System.Globalization;
using System.Numerics;

namespace SimpsonParadox;

public readonly struct Rational : IComparable<Rational>, IEquatable<Rational>
{
    public BigInteger Numerator { get; }
    private readonly BigInteger denominator;
    // The default struct value represents zero.
    public BigInteger Denominator => denominator.IsZero ? BigInteger.One : denominator;

    public Rational(BigInteger numerator, BigInteger denominator)
    {
        if (denominator.IsZero) throw new ArgumentException("Denominator must be nonzero.");
        if (denominator.Sign < 0) { numerator = -numerator; denominator = -denominator; }
        var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / gcd;
        this.denominator = denominator / gcd;
    }

    public static Rational Zero => new(0, 1);
    public static Rational One => new(1, 1);
    public static Rational operator +(Rational a, Rational b) => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static Rational operator -(Rational a, Rational b) => new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
    public static Rational operator *(Rational a, Rational b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
    public int CompareTo(Rational other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
    public bool Equals(Rational other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is Rational other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
    public static bool operator ==(Rational a, Rational b) => a.Equals(b);
    public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
    public double ToDouble() => (double)Numerator / (double)Denominator;
    public string Percent() => (100 * ToDouble()).ToString("0.00", CultureInfo.InvariantCulture) + "%";
    public override string ToString() => $"{Numerator.ToString(CultureInfo.InvariantCulture)}/{Denominator.ToString(CultureInfo.InvariantCulture)}";
}

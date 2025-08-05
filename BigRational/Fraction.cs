using System.Numerics;
using System.Globalization;
using System.Runtime.InteropServices;
using ExtendedNumerics.Internal;

namespace ExtendedNumerics;

/// <summary>
/// Represents an arbitrarily large rational number as a fraction.
/// Implements the <see cref="IComparable" />
/// Implements the <see cref="IComparable{Fraction}" />
/// Implements the <see cref="IEquatable{Fraction}" />
/// Implements the <see cref="IEqualityComparer{Fraction}" />
/// </summary>
/// <seealso cref="IComparable" />
/// <seealso cref="IComparable{Fraction}" />
/// <seealso cref="IEquatable{Fraction}" />
/// <seealso cref="IEqualityComparer{Fraction}" />
public struct Fraction : IComparable, IComparable<Fraction>, IEquatable<Fraction>, IEqualityComparer<Fraction>
{

	#region Properties

	/// <summary>The numerator of the fraction.</summary>
	public BigInteger Numerator { get; private set; }

	/// <summary>The denominator of the fraction.</summary>
	public BigInteger Denominator { get; private set; }

	/// <summary>
	/// Gets the sign of the number.
	/// Returns a positive one (1) if the value is positive,
	/// a negative one (-1) if the value is negative,
	/// and zero (0) if the value is zero.
	/// </summary>
	public readonly int Sign => NormalizeSign(this).Numerator.Sign;

	/// <summary>Indicates whether the value of the current instance is zero (0).</summary>
	/// <value><c>true</c> if this instance is zero; otherwise, <c>false</c>.</value>
	public readonly bool IsZero => this == Zero;

	/// <summary>Indicates whether the value of the current instance is one (1).</summary>
	/// <value><c>true</c> if this instance is one; otherwise, <c>false</c>.</value>
	public readonly bool IsOne => this == One;

	#region Static Properties

	/// <summary>Gets a value that represents the number zero (0).</summary>
	public static readonly Fraction Zero = new (BigInteger.Zero, BigInteger.One);

	/// <summary>Gets a value that represents the number one (1).</summary>
	public static readonly Fraction One = new (BigInteger.One, BigInteger.One);

	/// <summary>Gets a value that represents the number minus one (-1).</summary>
	public static readonly Fraction MinusOne = new (BigInteger.MinusOne, BigInteger.One);

	/// <summary>Gets a value that represents the number one half (1/2).</summary>
	public static readonly Fraction OneHalf = new (BigInteger.One, new BigInteger(2));

	#endregion

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="fraction">The fraction.</param>
	public Fraction(Fraction fraction)
		: this(fraction.Numerator, fraction.Denominator)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="value">The value.</param>
	public Fraction(int value)
		: this((BigInteger)value, BigInteger.One)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="value">The value.</param>
	public Fraction(BigInteger value)
		: this(value, BigInteger.One)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="numerator">The numerator.</param>
	/// <param name="denominator">The denominator.</param>
	public Fraction(BigInteger numerator, BigInteger denominator)
	{
		Numerator = numerator;
		Denominator = denominator;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="value">The value.</param>
	public Fraction(float value)
		: this(value, 7)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="value">The value.</param>
	public Fraction(double value)
		: this(value, 13)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Fraction"/> class.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <exception cref="System.ArgumentException">invalid decimal - value</exception>
	public Fraction(decimal value)
	{
		var bits = decimal.GetBits(value);
		if (bits == null || bits.Length != 4 || (bits[3] & ~(DecimalSignMask | DecimalScaleMask)) != 0 || (bits[3] & DecimalScaleMask) > (28 << 16))
		{
			throw new ArgumentException("invalid decimal", "value");
		}

		var result = CheckForWholeValues((double)value);
		if (result != null)
		{
			Numerator = result.Item1;
			Denominator = result.Item2;
		}
		{
			// build up the numerator
			var ul = (((ulong)(uint)bits[2]) << 32) | ((ulong)(uint)bits[1]);  // (hi    << 32) | (mid)
			var numerator = (new BigInteger(ul) << 32) | (uint)bits[0];   // (hiMid << 32) | (low)

			var isNegative = (bits[3] & DecimalSignMask) != 0;
			if (isNegative)
			{
				numerator = BigInteger.Negate(numerator);
			}

			// build up the denominator
			var scale = (bits[3] & DecimalScaleMask) >> 16;     // 0-28, power of 10 to divide numerator by
			var denominator = BigInteger.Pow(10, scale);

			var notReduced = new Fraction(numerator, denominator);
			var reduced = Simplify(notReduced);
			Numerator = reduced.Numerator;
			Denominator = reduced.Denominator;
		}
	}

	private Fraction(double value, int precision)
	{

		var result = CheckForWholeValues(value);
		if (result != null)
		{
			Numerator = result.Item1;
			Denominator = result.Item2;
		}
		else
		{
			var sign = Math.Sign(value);
			var exponent = value.ToString(CultureInfo.CurrentCulture)
									.TrimEnd('0')
									.SkipWhile(c => c != '.').Skip(1)
									.Count();

			var oneOver = Math.Round(1 / Math.Abs(value), precision);

			var isWholeNumber = false;
			BigInteger denom;

			if (precision == 7)
			{
				float floatVal = (float)oneOver;
				isWholeNumber = (floatVal % 1 == 0);
				denom = (BigInteger)floatVal;
			}
			else
			{
				isWholeNumber = (oneOver % 1 == 0);
				denom = (BigInteger)oneOver;
			}

			if (isWholeNumber)
			{
				Numerator = sign;
				Denominator = denom;
				return;
			}

			if (exponent > 0)
			{
				var pow = value * Math.Pow(10, exponent);
				var notReduced = new Fraction((BigInteger)pow, BigInteger.Pow(10, (int)exponent));
				var reduced = Simplify(notReduced);
				Numerator = reduced.Numerator;
				Denominator = reduced.Denominator;
			}
			else
			{
				Numerator = new BigInteger(value);
				Denominator = BigInteger.One;
			}
		}
	}

	/// <summary>
	/// Converts the string representation of a number to its <see cref="ExtendedNumerics.Fraction"/> equivalent.
	/// </summary>
	/// <param name="value">A string that contains the number to convert.</param>
	/// <returns> A value that is equivalent to the number specified in the value parameter.</returns>
	/// <exception cref="System.ArgumentException">Argument cannot be null, empty or whitespace.</exception>
	/// <exception cref="System.ArgumentException">String should either be an integer (e.g. '34') or a fraction (e.g. '7/12').</exception>
	public static Fraction Parse(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Argument cannot be null, empty or whitespace.");
		}

		var parts = value.Trim().Split('/');

		if (parts.Length == 1)
		{
			return new Fraction(BigInteger.Parse(parts[0]));
		}

		if (parts.Length > 2)
		{
			throw new ArgumentException("String should either be an integer (e.g. '34') or a fraction (e.g. '7/12').");
		}

		var num = BigInteger.Parse(parts[0]);
		var denom = BigInteger.Parse(parts[1]);

		return new Fraction(num, denom);
	}

	private static Tuple<BigInteger, BigInteger> CheckForWholeValues(double value)
	{
		if (double.IsNaN(value))
		{
			throw new ArgumentException("Value is not a number", nameof(value));
		}
		if (double.IsInfinity(value))
		{
			throw new ArgumentException("Cannot represent infinity", nameof(value));
		}

		if (value == 0)
		{
			return new Tuple<BigInteger, BigInteger>(BigInteger.Zero, BigInteger.One);
		}
		else if (value == 1)
		{
			return new Tuple<BigInteger, BigInteger>(BigInteger.One, BigInteger.One);
		}
		else if (value == -1)
		{
			return new Tuple<BigInteger, BigInteger>(BigInteger.MinusOne, BigInteger.One);
		}
		else if (value % 1 == 0)
		{
			return new Tuple<BigInteger, BigInteger>((BigInteger)value, BigInteger.One);
		}
		return null;
	}

	#endregion

	#region Arithmetic Methods

	/// <summary>
	/// Adds two <see cref="Fraction"/> values and returns the sum.
	/// </summary>
	/// <param name="augend">The augend.</param>
	/// <param name="addend">The addend.</param>
	/// <returns>The sum.</returns>
	public static Fraction Add(Fraction augend, Fraction addend) =>
		// a/b + c/d  == (ad + bc)/bd
		new (
				BigInteger.Add(
					BigInteger.Multiply(augend.Numerator, addend.Denominator),
					BigInteger.Multiply(augend.Denominator, addend.Numerator)
				),
				BigInteger.Multiply(augend.Denominator, addend.Denominator)
			);

	/// <summary>
	/// Subtracts two <see cref="Fraction"/> values and returns the difference.
	/// </summary>
	/// <param name="minuend">The minuend.</param>
	/// <param name="subtrahend">The subtrahend.</param>
	/// <returns>The difference.</returns>
	public static Fraction Subtract(Fraction minuend, Fraction subtrahend) =>
		// a/b - c/d  == (ad - bc)/bd
		new (
				BigInteger.Subtract(
					BigInteger.Multiply(minuend.Numerator, subtrahend.Denominator),
					BigInteger.Multiply(minuend.Denominator, subtrahend.Numerator)
				),
				BigInteger.Multiply(minuend.Denominator, subtrahend.Denominator)
			);

	/// <summary>
	/// Multiplies two <see cref="Fraction"/> values and returns the product.
	/// </summary>
	/// <param name="multiplicand">The multiplicand.</param>
	/// <param name="multiplier">The multiplier.</param>
	/// <returns>The product.</returns>
	/// <exception cref="System.ArithmeticException">Multiply methods needs to simplify result. Please add this behavior to this method.</exception>
	public static Fraction Multiply(Fraction multiplicand, Fraction multiplier)
	{
		var frac1 =
		   new Fraction(
			   BigInteger.Multiply(multiplicand.Numerator, multiplier.Numerator),
			   BigInteger.Multiply(multiplicand.Denominator, multiplier.Denominator)
		   );

		var frac2 = Simplify(frac1);

		return frac1 != frac2
			?           throw new ArithmeticException("Multiply methods needs to simplify result. Please add this behavior to this method.")
			: frac1;
	}

	/// <summary>
	/// Divides two <see cref="Fraction"/> values and returns the quotient.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static Fraction Divide(Fraction dividend, Fraction divisor) => Simplify(Multiply(dividend, Reciprocal(divisor)));

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> numbers and returns the remainder.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The remainder.</returns>
	public static Fraction Remainder(BigInteger dividend, BigInteger divisor)
	{
		var remainder = dividend % divisor;
		return new Fraction(remainder, divisor);
	}

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> numbers and returns the remainder.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The remainder.</returns>
	public static Fraction Remainder(Fraction dividend, Fraction divisor) => new(
			BigInteger.Multiply(dividend.Numerator, divisor.Denominator) % BigInteger.Multiply(dividend.Denominator, divisor.Numerator),
			BigInteger.Multiply(dividend.Denominator, divisor.Denominator)
		);

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> numbers and returns the quotient and the remainder.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <param name="remainder">The remainder.</param>
	/// <returns>The quotient.</returns>
	public static Fraction DivRem(Fraction dividend, Fraction divisor, out Fraction remainder)
	{
		// a/b / c/d  == (ad)/(bc) ; a/b % c/d  == (ad % bc)/bd
		var ad = dividend.Numerator * divisor.Denominator;
		var bc = dividend.Denominator * divisor.Numerator;
		var bd = dividend.Denominator * divisor.Denominator;
		remainder = new Fraction((ad % bc), bd);
		return new Fraction(ad, bc);
	}

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> numbers and returns the quotient and the remainder.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <param name="remainder">The remainder.</param>
	/// <returns>The quotient.</returns>
	public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out Fraction remainder)
	{
		var remaind = new BigInteger(-1);
		var quotient = BigInteger.DivRem(dividend, divisor, out remaind);

		remainder = new Fraction(remaind, divisor);
		return quotient;
	}

	/// <summary>
	///  Raises the specified <see cref="T:ExtendedNumerics.Fraction" /> base value to an integer exponent.
	/// </summary>
	/// <param name="base">The base.</param>
	/// <param name="exponent">The exponent.</param>
	/// <returns>The power of the base raised to the exponent.</returns>
	/// <exception cref="System.ArgumentException">Cannot raise zero to a negative power - value</exception>
	public static Fraction Pow(Fraction @base, BigInteger exponent)
	{
		if (exponent.Sign == 0) { return Fraction.One; }
		if (@base.Sign == 0) { return Fraction.Zero; }
		if (exponent == 1) { return @base; }
		if (@base == 1) { return Fraction.One; }

		var inputValue = new Fraction(@base);
		var inputExponent = exponent;

		// Handle negative values of the exponent: n^(-e) -> (1/n)^e
		if (exponent.Sign < 0)
		{
			if (@base == Fraction.Zero)
			{
				throw new ArgumentException("Cannot raise zero to a negative power", nameof(@base));
			}

			inputValue = Fraction.Reciprocal(@base);
			inputExponent = BigInteger.Negate(exponent);
		}

		// (a/b)^m = (a^m / b^m)
		return new Fraction(Internal.ExtensionMethods.Pow(@base.Numerator, exponent), ExtensionMethods.Pow(@base.Denominator, exponent));
	}

	/// <summary>
	/// Raises the specified <see cref="T:ExtendedNumerics.Fraction" /> base value to <see cref="T:ExtendedNumerics.Fraction" /> exponent.
	/// </summary>
	/// <param name="base">The base.</param>
	/// <param name="exponent">The exponent.</param>
	/// <returns>The power of the base raised to the exponent.</returns>
	public static Fraction Pow(Fraction @base, Fraction exponent) =>
		// (a/b)^(m/n) -> n#(a^m) / n#(b^m), where n#m is the nth root of m function
		Divide(
			NthRoot(Pow(@base.Numerator, exponent.Numerator), exponent.Denominator),
			NthRoot(Pow(@base.Denominator, exponent.Numerator), exponent.Denominator)
		);

	/// <summary>
	/// Returns the square root of the specified value.
	/// </summary>
	/// <param name="value">The base value to square root.</param>
	/// <returns>The square root of the specified value.</returns>
	public static Fraction Sqrt(Fraction value, int precision = 30) => NthRoot(value, 2, precision);

	/// <summary>
	/// Returns the Nth root of a Fraction up to a desired precision.
	/// The precision parameter is given in terms of the minimum number of correct decimal places.
	/// </summary>
	/// <param name="value">The value to take the Nth root of.</param>
	/// <param name="root">The Nth root to find of value. Also called the index.</param>
	/// <param name="precision">The minimum number of correct decimal places to return if the answer is not a .</param>
	/// <returns>Fraction.</returns>
	/// <exception cref="System.Exception">Value must be a positive integer</exception>
	public static Fraction NthRoot(Fraction value, BigInteger root, int precision = 30)
	{
		var deviationBound = new Fraction(1, BigInteger.Pow(10, precision));

		//if (root < 1) throw new Exception("Root must be greater than or equal to 1");
		if (value.Sign == -1) throw new Exception("Value must be a positive integer");
		if (value == Fraction.One || value == Fraction.Zero || root == 1) { return value; }

		Fraction lowerbound = Fraction.Zero;
		Fraction upperbound = new Fraction(value);
		if (upperbound < Fraction.One)
		{
			upperbound = Fraction.One;
		}
		Fraction mediant;

		while (true)
		{
			mediant = Simplify(Mediant(lowerbound, upperbound));

			var testPow = Simplify(Pow(mediant, root));

			if (testPow > value) upperbound = mediant;
			if (testPow < value) lowerbound = mediant;
			if (testPow == value)
			{
				lowerbound = mediant;
				break;
			}
			if ((upperbound - lowerbound) <= deviationBound)
			{
				break;
			}
		}

		return lowerbound;
	}

	/// <summary>
	/// Returns a fraction half way between the left and the right parameter.
	/// The mediant of two fractions is defined as the sum of the numerators
	/// divided by the sum of the denominators, and is often used when generating
	/// the Farey sequence or a Stern–Brocot tree.
	/// </summary>
	/// <returns>The Fraction mid-way between the left Fraction and right Fraction.</returns>
	public static Fraction Mediant(Fraction left, Fraction right) => new Fraction(
				BigInteger.Add(left.Numerator, right.Numerator),
				 BigInteger.Add(left.Denominator, right.Denominator)
			);

	/// <summary>
	/// Returns the natural (base e) logarithm of a specified number.
	/// </summary>
	/// <param name="fraction">The number whose logarithm is to be found.</param>
	/// <returns>The natural (base e) logarithm of the specified value.</returns>
	public static double Log(Fraction fraction)
	{
		var a = BigInteger.Log(fraction.Numerator);
		var b = BigInteger.Log(fraction.Denominator);
		return (a - b);
	}

	/// <summary>
	/// Returns the Reciprocal of the specified <see cref="ExtendedNumerics.Fraction"/>
	/// by swapping the numerator and the denominator.
	/// </summary>
	/// <param name="fraction">The fraction.</param>
	/// <returns>The reciprocal.</returns>
	public static Fraction Reciprocal(Fraction fraction)
	{
		var result = new Fraction(fraction.Denominator, fraction.Numerator);
		var simplified = Fraction.Simplify(result);
		return simplified;
	}

	/// <summary>
	/// Returns the absolute value of a <see cref="ExtendedNumerics.Fraction"/>.
	/// </summary>
	/// <param name="fraction">A value to get the absolute value of.</param>
	/// <returns>The absolute value of value of the specified number.</returns>
	public static Fraction Abs(Fraction fraction) => (fraction.Numerator.Sign < 0 ? new Fraction(BigInteger.Abs(fraction.Numerator), fraction.Denominator) : fraction);

	/// <summary>
	/// Negates the specified value.
	/// </summary>
	/// <param name="fraction">The number to negate the value of.</param>
	/// <returns>The result of the specified value multiplied by negative one (-1).</returns>
	public static Fraction Negate(Fraction fraction)
	{
		return new Fraction(BigInteger.Negate(fraction.Numerator), fraction.Denominator);
	}

	#region GCD & LCM

	/// <summary>
	/// Finds the least common denominator of two <see cref="ExtendedNumerics.Fraction"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The least common denominator of left and right.</returns>
	public static Fraction LeastCommonDenominator(Fraction left, Fraction right) => new Fraction((left.Denominator * right.Denominator), BigInteger.GreatestCommonDivisor(left.Denominator, right.Denominator));

	/// <summary>
	/// Finds the greatest common divisor of two <see cref="ExtendedNumerics.Fraction"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The greatest common divisor of left and right.</returns>
	public static Fraction GreatestCommonDivisor(Fraction left, Fraction right)
	{
		var leftFrac = Simplify(left);
		var rightFrac = Simplify(right);

		var gcd = BigInteger.GreatestCommonDivisor(left.Numerator, right.Numerator);
		var lcm = LCM(left.Denominator, right.Denominator);

		return new Fraction(gcd, lcm);
	}

	/// <summary>
	/// Finds the least common denominator of two <see cref="System.Numerics.BigInteger"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The least common denominator of left and right.</returns>
	private static BigInteger LCM(BigInteger left, BigInteger right)
	{
		var absValue1 = BigInteger.Abs(left);
		var absValue2 = BigInteger.Abs(right);
		return (absValue1 * absValue2) / BigInteger.GreatestCommonDivisor(absValue1, absValue2);
	}

	#endregion

	#endregion

	#region Arithmetic Operators

	/// <summary>
	/// Adds two <see cref="T:ExtendedNumerics.Fraction" /> values and returns the sum.
	/// </summary>
	/// <param name="augend">The augend.</param>
	/// <param name="addend">The addend.</param>
	/// <returns>The sum.</returns>
	public static Fraction operator +(Fraction augend, Fraction addend) => Add(augend, addend);

	/// <summary>
	/// Subtracts two <see cref="T:ExtendedNumerics.Fraction" /> values and returns the difference.
	/// </summary>
	/// <param name="minuend">The minuend.</param>
	/// <param name="subtrahend">The subtrahend.</param>
	/// <returns>The difference.</returns>
	public static Fraction operator -(Fraction minuend, Fraction subtrahend) => Subtract(minuend, subtrahend);

	/// <summary>
	/// Multiplies two <see cref="T:ExtendedNumerics.Fraction" /> values and returns the product.
	/// </summary>
	/// <param name="multiplicand">The multiplicand.</param>
	/// <param name="multiplier">The multiplier.</param>
	/// <returns>The product.</returns>
	public static Fraction operator *(Fraction multiplicand, Fraction multiplier) => Multiply(multiplicand, multiplier);

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> values and returns the quotient.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static Fraction operator /(Fraction dividend, Fraction divisor) => Divide(dividend, divisor);

	/// <summary>
	/// Divides two <see cref="T:ExtendedNumerics.Fraction" /> values and returns the remainder/modulus.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The remainder that results from the division.</returns>
	public static Fraction operator %(Fraction dividend, Fraction divisor) => Remainder(dividend, divisor);

	/// <summary>
	/// Returns the value of the <see cref="ExtendedNumerics.Fraction"/> operand. (The sign of the operand is unchanged.)
	/// </summary>
	/// <param name="value">The value to return.</param>
	/// <returns>The value of the value operand.</returns>
	public static Fraction operator +(Fraction value) => value;

	/// <summary>
	/// Negates a specified <see cref="T:ExtendedNumerics.Fraction" /> value.
	/// </summary>
	/// <param name="value">The value to negate.</param>
	/// <returns>The result of the value parameter multiplied by negative one (-1).</returns>
	public static Fraction operator -(Fraction value) => Negate(value);

	/// <summary>
	/// Increments a <see cref="T:ExtendedNumerics.Fraction" /> value by 1.
	/// </summary>
	/// <param name="value">The value to increment.</param>
	/// <returns>The value of the value parameter incremented by 1.</returns>
	public static Fraction operator ++(Fraction value) => Add(value, Fraction.One);

	/// <summary>
	/// Decrements a <see cref="T:ExtendedNumerics.Fraction" /> value by 1.
	/// </summary>
	/// <param name="value">The value to decrement.</param>
	/// <returns>The value of the value parameter decremented by 1.</returns>
	public static Fraction operator --(Fraction value) => Subtract(value, Fraction.One);

	#endregion

	#region Comparison Operators

	/// <summary>
	/// Returns a value that indicates whether the values of two
	/// <see cref="T:ExtendedNumerics.Fraction" /> objects are equal.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>
	/// <c>true</c>  if the left and right parameters have the same value; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator ==(Fraction left, Fraction right) => Compare(left, right) == 0;

	/// <summary>
	/// Returns a value that indicates whether two <see cref="T:ExtendedNumerics.Fraction" />
	/// objects have different values.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>
	/// <c>true</c>  if left and right are not equal; otherwise, <c>false</c>.
	/// </returns>
	public static bool operator !=(Fraction left, Fraction right) => Compare(left, right) != 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="T:ExtendedNumerics.Fraction" /> value is
	/// less than another <see cref="T:ExtendedNumerics.Fraction" /> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value>
	/// <c>true</c> if left is less than right; otherwise, <c>false</c>.
	/// </value>
	public static bool operator <(Fraction left, Fraction right) => Compare(left, right) < 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="T:ExtendedNumerics.Fraction" /> value is
	/// less than or equal to another <see cref="T:ExtendedNumerics.Fraction" /> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value>
	/// <c>true</c> if left is less than or equal to right; otherwise, <c>false</c>.
	/// </value>
	public static bool operator <=(Fraction left, Fraction right) => Compare(left, right) <= 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="T:ExtendedNumerics.Fraction" /> value is
	/// greater than another <see cref="T:ExtendedNumerics.Fraction" /> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value>
	/// <c>true</c> if left is greater than right; otherwise, <c>false</c>.
	/// </value>
	public static bool operator >(Fraction left, Fraction right) => Compare(left, right) > 0;

	/// <summary>
	/// Implements the &gt;= operator.
	/// </summary>
	/// <param name="left">The left.</param>
	/// <param name="right">The right.</param>
	/// <returns>The result of the operator.</returns>
	public static bool operator >=(Fraction left, Fraction right) => Compare(left, right) >= 0;

	#endregion

	#region Compare

	/// <summary>
	/// Compares two <see cref="ExtendedNumerics.Fraction"/> values and
	/// returns an integer that indicates whether the first value is
	/// less than, equal to, or greater than the second value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>
	/// A signed integer that indicates the relative values of left and right.
	/// The return value has these meanings:
	/// Less than zero: left is less than right.
	/// Zero: left equals right.
	/// Greater than zero: left is greater than right.
	/// </returns>
	public static int Compare(Fraction left, Fraction right)
	{
		var l = BigInteger.Multiply(left.Numerator, right.Denominator);
		var r = BigInteger.Multiply(right.Numerator, left.Denominator);
		return BigInteger.Compare(l, r);
	}

	/// <summary>
	/// Compares the current instance with another object of the same type and 
	/// returns an integer that indicates whether the current instance
	/// precedes, follows, or occurs in the same position in the sort order as the other object.
	/// Satisfies the <see cref="IComparable" /> interface implementation.
	/// </summary>
	/// <param name="obj">An object to compare with this instance.</param>
	/// <returns>
	/// A value that indicates the relative order of the objects being compared.
	/// The return value has these meanings:
	/// Less than zero: This instance precedes <paramref name="obj" /> in the sort order.
	/// Zero: This instance occurs in the same position in the sort order as <paramref name="obj" />.
	/// Greater than zero: This instance follows <paramref name="obj" /> in the sort order.
	/// </returns>
	/// <exception cref="System.ArgumentException">Argument must be of type Fraction</exception>
	readonly int IComparable.CompareTo(object obj) => obj == null
			? 1
			: obj is not Fraction
			? throw new ArgumentException($"Argument must be of type {nameof(Fraction)}", nameof(obj))
			: Compare(this, (Fraction)obj);

	/// <summary>
	/// Compares the current instance with another object of the same type and
	/// returns an integer that indicates whether the current instance
	/// precedes, follows, or occurs in the same position in the sort order as the other object.
	/// Satisfies the <see cref="IComparable{Fraction}" /> interface implementation.
	/// </summary>
	/// <param name="other">An object to compare with this instance.</param>
	/// <returns>
	/// A value that indicates the relative order of the objects being compared. The return value has these meanings:
	/// Less than zero: This instance precedes <paramref name="other" /> in the sort order.
	/// Zero: This instance occurs in the same position in the sort order as <paramref name="other" />.
	/// Greater than zero: This instance follows <paramref name="other" /> in the sort order.
	/// </returns>
	public readonly int CompareTo(Fraction other) => Compare(this, other);

	#endregion

	#region Conversion Operators

	/// <summary>
	/// Performs an implicit conversion from <see cref="BigInteger"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(BigInteger value) => new Fraction(value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="System.Byte"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(byte value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="SByte"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(sbyte value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int16"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(short value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt16"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(ushort value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int32"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(int value) => new Fraction(value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt32"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(uint value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int64"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(long value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt64"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(ulong value) => new Fraction((BigInteger)value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Single"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator Fraction(float value) => new Fraction(value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Double"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator Fraction(double value) => new Fraction(value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Decimal"/> to <see cref="Fraction"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator Fraction(decimal value) => new Fraction(value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Fraction"/> to <see cref="BigRational"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(Fraction value) => new BigRational(BigInteger.Zero, value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="Fraction"/> to <see cref="BigInteger"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator BigInteger(Fraction value) => BigInteger.Divide(value.Numerator, value.Denominator);

	/// <summary>
	/// Performs an explicit conversion from <see cref="Fraction"/> to <see cref="System.Double"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator double(Fraction value)
	{
		if (IsInRangeDouble(value.Numerator) && IsInRangeDouble(value.Denominator))
		{
			return (double)value.Numerator / (double)value.Denominator;
		}

		var scaledup = BigInteger.Multiply(value.Numerator, _doublePrecision) / value.Denominator;
		if (scaledup.IsZero)
		{
			return 0d; // underflow. throw exception here instead?
		}

		var isDone = false;
		var result = 0.0;
		var scale = _doubleMaxScale;
		while (scale > 0)
		{
			if (!isDone)
			{
				if (IsInRangeDouble(scaledup))
				{
					result = (double)scaledup;
					isDone = true;
				}
				else
				{
					scaledup /= 10;
				}
			}

			result /= 10;
			scale--;
		}

		return isDone ? result : (value.Sign < 0) ? double.NegativeInfinity : double.PositiveInfinity;
	}

	/// <summary>
	/// Performs an explicit conversion from <see cref="Fraction"/> to <see cref="System.Decimal"/>.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns>The result of the conversion.</returns>
	/// <exception cref="System.OverflowException">Value was either too large or too small for a decimal.</exception>
	public static explicit operator decimal(Fraction value)
	{
		// The decimal value type represents decimal numbers ranging
		// from +79,228,162,514,264,337,593,543,950,335 to -79,228,162,514,264,337,593,543,950,335
		// the binary representation of a decimal value is of the form, ((-2^96 to 2^96) / 10^(0 to 28))
		if (IsInRangeDecimal(value.Numerator) && IsInRangeDecimal(value.Denominator))
		{
			return (decimal)value.Numerator / (decimal)value.Denominator;
		}

		// scale the numerator to preserve the fraction part through the integer division
		BigInteger denormalized = (value.Numerator * _decimalPrecision) / value.Denominator;
		if (denormalized.IsZero)
		{
			return decimal.Zero; // underflow - fraction is too small to fit in a decimal
		}
		for (int scale = DecimalMaxScale; scale >= 0; scale--)
		{
			if (!IsInRangeDecimal(denormalized))
			{
				denormalized /= 10;
			}
			else
			{
				var dec = new DecimalUInt32();
				dec.dec = (decimal)denormalized;
				dec.flags = (dec.flags & ~DecimalScaleMask) | (scale << 16);
				return dec.dec;
			}
		}
		throw new OverflowException("Value was either too large or too small for a decimal.");
	}

	#region Private Members

	private static bool IsInRangeDouble(BigInteger number) => (BigInteger)double.MinValue < number && number < (BigInteger)double.MaxValue;
	private static readonly int _doubleMaxScale = 308;
	private static readonly BigInteger _doublePrecision = BigInteger.Pow(10, _doubleMaxScale);
	private static readonly BigInteger _decimalPrecision = BigInteger.Pow(10, DecimalMaxScale);
	private static readonly BigInteger _decimalMaxValue = (BigInteger)decimal.MaxValue;
	private static readonly BigInteger _decimalMinValue = (BigInteger)decimal.MinValue;
	private const int DecimalScaleMask = 0x00FF0000;
	private const int DecimalSignMask = unchecked((int)0x80000000);
	private const int DecimalMaxScale = 28;

	private static bool IsInRangeDecimal(BigInteger number) => _decimalMinValue <= number && number <= _decimalMaxValue;

	[StructLayout(LayoutKind.Explicit)]
	internal struct DecimalUInt32
	{
		[FieldOffset(0)]
		public decimal dec;
		[FieldOffset(0)]
		public int flags;
	}

	#endregion

	#endregion

	#region Equality Methods

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns><see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override readonly bool Equals(object obj) => obj != null && obj is Fraction f && Equals(this, f);

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns><see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public readonly bool Equals(Fraction other) => Equals(this, other);

	/// <summary>
	/// Indicates equality between two instances of <see cref="Fraction" />.
	/// </summary>
	/// <param name="left">The left instance.</param>
	/// <param name="right">The right instance.</param>
	/// <returns><c>true</c> if left is equal to right, <c>false</c> otherwise.</returns>
	public readonly bool Equals(Fraction left, Fraction right) => left.Denominator == right.Denominator
			? left.Numerator == right.Numerator
			: (left.Numerator * right.Denominator) == (left.Denominator * right.Numerator);

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
	public override readonly int GetHashCode() => GetHashCode(this);

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <param name="fraction">The fraction.</param>
	/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
	public readonly int GetHashCode(Fraction fraction) => BigRational.CombineHashCodes(fraction.Numerator.GetHashCode(), fraction.Denominator.GetHashCode());

	#endregion

	#region Transform Methods

	/// <summary>
	/// Converts a <see cref="ExtendedNumerics.Fraction"/> into reduced form
	/// by dividing the numerator by the denominator and returning a <see cref="ExtendedNumerics.Fraction"/>
	/// with the WholePart containing the quotient.
	/// </summary>
	/// <param name="value">The fraction to reduce.</param>
	/// <returns>A BigRational in a reduced fraction form.</returns>
	public static BigRational ReduceToProperFraction(Fraction value)
	{
		var input = Simplify(value);

		if (input.Numerator.IsZero)
		{
			return new BigRational(BigInteger.Zero, input);
		}
		else if (input.Denominator.IsOne)
		{
			return new BigRational(input.Numerator, Fraction.Zero);
		}
		else
		{
			BigRational result;
			if (BigInteger.Abs(input.Numerator) > BigInteger.Abs(input.Denominator))
			{
				int sign = input.Numerator.Sign;

				var remainder = new BigInteger(-1);
				var wholeUnits = BigInteger.DivRem(BigInteger.Abs(input.Numerator), input.Denominator, out remainder);
				if (sign == -1)
				{
					wholeUnits = BigInteger.Negate(wholeUnits);
				}
				result = new BigRational(wholeUnits, new Fraction(remainder, input.Denominator));
				return result;
			}
			else
			{
				result = new BigRational(BigInteger.Zero, input.Numerator, input.Denominator);
				return result;
			}
		}
	}

	/// <summary>
	/// Divides out any common divisors between the numerator and the denominator
	/// and then normalizes the sign.
	/// </summary>
	public static Fraction Simplify(Fraction value)
	{
		var input = NormalizeSign(value);

		if (input.Numerator.IsZero || input.Numerator.IsOne || input.Numerator == BigInteger.MinusOne)
		{
			return new Fraction(input);
		}

		var num = input.Numerator;
		var denom = input.Denominator;
		var gcd = BigInteger.GreatestCommonDivisor(num, denom);
		return gcd > BigInteger.One ? new Fraction(num / gcd, denom / gcd) : new Fraction(input);
	}

	/// <summary>
	/// Normalizes the sign of the specified value.
	/// That is, moves the negative value to the numerator.
	/// </summary>
	internal static Fraction NormalizeSign(Fraction value)
	{
		var numer = value.Numerator;
		var denom = value.Denominator;

		if (numer.Sign == 1 && denom.Sign == 1)
		{
			return value;
		}
		else if (numer.Sign == -1 && denom.Sign == 1)
		{
			return value;
		}
		else if (numer.Sign == 1 && denom.Sign == -1)
		{
			numer = BigInteger.Negate(numer);
			denom = BigInteger.Negate(denom);
		}
		else if (numer.Sign == -1 && denom.Sign == -1)
		{
			numer = BigInteger.Negate(numer);
			denom = BigInteger.Negate(denom);
		}

		return new Fraction(numer, denom);
	}

	#endregion

	#region Overrides

	/// <summary>
	/// Converts the numeric value of the current <see cref="T:ExtendedNumerics.Fraction" />
	/// instance into its equivalent string representation.
	/// </summary>
	/// <returns>
	/// The string representation of the current <see cref="T:ExtendedNumerics.Fraction" /> value.
	/// </returns>
	public override readonly string ToString() => ToString(CultureInfo.CurrentCulture);

	/// <summary>
	/// Converts the numeric value of the current <see cref="T:ExtendedNumerics.Fraction" />
	/// instance into its equivalent string representation by using
	/// the specified format.
	/// </summary>
	/// <param name="format">A standard or custom numeric format string.</param>
	/// <returns>
	/// The string representation of the current <see cref="T:ExtendedNumerics.Fraction" /> value
	/// in the format specified by the format parameter.
	/// </returns>
	public readonly string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);

	/// <summary>
	/// Converts the numeric value of the current <see cref="T:ExtendedNumerics.Fraction" />
	/// instance into its equivalent string representation by using the specified
	/// culture-specific formatting information.
	/// </summary>
	/// <param name="provider">An object that supplies culture-specific formatting information.</param>
	/// <returns>
	/// The string representation of the current <see cref="T:ExtendedNumerics.Fraction" /> value in
	/// the format specified by the provider parameter.
	/// </returns>
	public readonly string ToString(IFormatProvider provider) => ToString("R", provider);

	/// <summary>
	/// Converts the numeric value of the current <see cref="T:ExtendedNumerics.Fraction" />
	/// instance into its equivalent string representation by using the specified
	/// format and culture-specific format information.
	/// </summary>
	/// <param name="format">A standard or custom numeric format string.</param>
	/// <param name="provider">An object that supplies culture-specific formatting information.</param>
	/// <returns>The string representation of the current <see cref="T:ExtendedNumerics.Fraction" /> value as
	/// specified by the format and provider parameters.
	/// </returns>
	public readonly string ToString(string format, IFormatProvider provider)
	{
		var numberFormatProvider = (NumberFormatInfo)provider.GetFormat(typeof(NumberFormatInfo));
		numberFormatProvider ??= CultureInfo.CurrentCulture.NumberFormat;

		var zeroString = numberFormatProvider.NativeDigits[0];
		var zeroChar = zeroString.First();

		return Numerator.IsZero
			? zeroString
			: Denominator.IsOne
				? $"{Numerator.ToString(format, provider)}"
				: $"{Numerator.ToString(format, provider)}/{Denominator.ToString(format, provider)}";
	}

	#endregion

}


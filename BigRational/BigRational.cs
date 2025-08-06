using System.Numerics;
using System.Globalization;

namespace ExtendedNumerics;

/// <summary>
/// Represents an arbitrarily large mixed fraction.
/// If you want an arbitrarily large rational number, <see cref="Fraction" />.
/// Implements the <see cref="IComparable" />
/// Implements the <see cref="IComparable{BigRational}" />
/// Implements the <see cref="IEquatable{BigRational}" />
/// </summary>
/// <seealso cref="IComparable" />
/// <seealso cref="IComparable{BigRational}" />
/// <seealso cref="IEquatable{BigRational}" />
public struct BigRational : IComparable, IComparable<BigRational>, IEquatable<BigRational>
{

	#region Properties

	/// <summary>The whole-number (non-fractional) integer value.</summary>
	public BigInteger WholePart { get; private set; }

	/// <summary>The fractional part of the value.</summary>		
	public Fraction FractionalPart { get; private set; }

	/// <summary>
	/// Gets the sign of the number.
	/// Returns a positive one (1) if the value is positive,
	/// a negative one (-1) if the value is negative,
	/// and zero (0) if the value is zero.
	/// </summary>		
	public int Sign => (NormalizeSign().WholePart != 0) ? WholePart.Sign : FractionalPart.Sign;

	/// <summary>Indicates whether the value of the current instance is zero (0).</summary>
	/// <value><c>true</c> if this instance is zero; otherwise, <c>false</c>.</value>
	public readonly bool IsZero => WholePart.IsZero && FractionalPart.IsZero;

	#region Static Properties

	/// <summary>Gets a value that represents the number one (1).</summary>
	public static readonly BigRational One = new(BigInteger.One);

	/// <summary>Gets a value that represents the number zero (0).</summary>
	public static readonly BigRational Zero = new(BigInteger.Zero);

	/// <summary>Gets a value that represents the number negative one (-1).</summary>
	public static readonly BigRational MinusOne = new(BigInteger.MinusOne);

	#endregion

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using a 32-bit signed integer value.
	/// </summary>
	/// <param name="value">A 32-bit signed integer.</param>
	public BigRational(int value)
		: this((BigInteger)value, Fraction.Zero)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	///  an arbitrarily large signed integer.
	/// </summary>
	/// <param name="value">An arbitrarily large signed integer.</param>
	public BigRational(BigInteger value)
		: this(value, Fraction.Zero)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	/// an arbitrarily large rational number.
	/// </summary>
	/// <param name="fraction">An arbitrarily large rational number (as a Fraction).</param>
	public BigRational(Fraction fraction)
		: this(BigInteger.Zero, fraction)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	/// an arbitrarily large signed integer and an arbitrarily large rational number.
	/// </summary>
	/// <param name="whole">An arbitrarily large signed integer whole number.</param>
	/// <param name="fraction">An arbitrarily large rational number (as a Fraction).</param>
	public BigRational(BigInteger whole, Fraction fraction)
		: this(whole, fraction.Numerator, fraction.Denominator)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	///  an arbitrarily large signed integer numerator and denominator.
	/// </summary>
	/// <param name="numerator">An arbitrarily large signed integer numerator.</param>
	/// <param name="denominator">An arbitrarily large signed integer denominator.</param>
	public BigRational(BigInteger numerator, BigInteger denominator)
		: this(new Fraction(numerator, denominator))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	///  an arbitrarily large signed integer whole number value,
	///  a numerator and a denominator.
	/// </summary>
	/// <param name="whole">An arbitrarily large signed integer whole number.</param>
	/// <param name="numerator">An arbitrarily large signed integer numerator.</param>
	/// <param name="denominator">An arbitrarily large signed integer denominator.</param>
	public BigRational(BigInteger whole, BigInteger numerator, BigInteger denominator)
	{
		WholePart = whole;
		FractionalPart = new (numerator, denominator);
		NormalizeSign();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	/// a single-precision floating-point value.
	/// </summary>
	/// <param name="value">A single-precision floating-point value.</param>
	public BigRational(float value)
	{
		var result = CheckForWholeValues((double)value);
		if (result != null)
		{
			WholePart = result.Item1;
			FractionalPart = result.Item2;
		}
		else
		{
			WholePart = (BigInteger)Math.Truncate(value);
			var fract = Math.Abs(value) % 1;
			FractionalPart = (fract == 0) ? Fraction.Zero : new Fraction(fract);
			NormalizeSign();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	/// A double-precision floating-point value.
	/// </summary>
	/// <param name="value">A double-precision floating-point value.</param>
	public BigRational(double value)
	{
		var result = CheckForWholeValues(value);
		if (result != null)
		{
			WholePart = result.Item1;
			FractionalPart = result.Item2;
		}
		else
		{
			WholePart = (BigInteger)Math.Truncate(value);
			var fract = Math.Abs(value) % 1;
			FractionalPart = (fract == 0) ? Fraction.Zero : new Fraction(fract);
			NormalizeSign();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtendedNumerics.BigRational"/> class using
	/// a 128-bit base-10 floating point decimal number.
	/// </summary>
	/// <param name="value">A 128-bit base-10 floating point decimal number.</param>
	public BigRational(decimal value)
	{
		var result = CheckForWholeValues((double)value);
		if (result != null)
		{
			WholePart = result.Item1;
			FractionalPart = result.Item2;
		}
		else
		{
			WholePart = (BigInteger)Math.Truncate(value);
			var fract = Math.Abs(value) % 1;
			FractionalPart = (fract == 0) ? Fraction.Zero : new Fraction(fract);
			NormalizeSign();
		}
	}

	/// <summary>
	/// Checks the value of a <see cref="Double"/> for 0, 1 or -1,
	/// setting the internal state and returning true if it is,
	/// throws an exception if it is NaN or +- Infinity,
	/// and returns false otherwise.
	/// </summary>
	/// <exception cref="System.ArgumentException">Value is not a number - value</exception>
	/// <exception cref="System.ArgumentException">Cannot represent infinity - value</exception>
	private static Tuple<BigInteger, Fraction> CheckForWholeValues(double value) => double.IsNaN(value)
			? throw new ArgumentException("Value is not a number", nameof(value))
			: double.IsInfinity(value)
			? throw new ArgumentException("Cannot represent infinity", nameof(value))
			: value switch
			{
				0 => new Tuple<BigInteger, Fraction>(BigInteger.Zero, Fraction.Zero),
				1 => new Tuple<BigInteger, Fraction>(BigInteger.One, Fraction.Zero),
				-1 => new Tuple<BigInteger, Fraction>(BigInteger.MinusOne, Fraction.Zero),
				_ => null,
			};

	#endregion

	#region Arithmetic Methods

	/// <summary>
	/// Adds two <see cref="ExtendedNumerics.BigRational"/> values and returns the sum.
	/// </summary>
	/// <param name="augend">The augend.</param>
	/// <param name="addend">The addend.</param>
	/// <returns>The sum.</returns>
	public static BigRational Add(BigRational augend, BigRational addend)
	{
		var fracAugend = augend.GetImproperFraction();
		var fracAddend = addend.GetImproperFraction();

		var result = Add(fracAugend, fracAddend);
		var reduced = Reduce(result);
		return reduced;
	}

	/// <summary>
	/// Subtracts two <see cref="ExtendedNumerics.BigRational"/> values and returns the difference.
	/// </summary>
	/// <param name="minuend">The minuend.</param>
	/// <param name="subtrahend">The subtrahend.</param>
	/// <returns>The difference.</returns>
	public static BigRational Subtract(BigRational minuend, BigRational subtrahend)
	{
		var fracMinuend = minuend.GetImproperFraction();
		var fracSubtrahend = subtrahend.GetImproperFraction();

		var result = Subtract(fracMinuend, fracSubtrahend);
		var reduced = Reduce(result);
		return reduced;
	}

	/// <summary>
	/// Multiplies two <see cref="ExtendedNumerics.BigRational"/> values and returns the product.
	/// </summary>
	/// <param name="multiplicand">The multiplicand.</param>
	/// <param name="multiplier">The multiplier.</param>
	/// <returns>The product.</returns>
	public static BigRational Multiply(BigRational multiplicand, BigRational multiplier)
	{
		var fracMultiplicand = multiplicand.GetImproperFraction();
		var fracMultiplier = multiplier.GetImproperFraction();

		var result = Fraction.ReduceToProperFraction(Fraction.Multiply(fracMultiplicand, fracMultiplier));
		var reduced = Reduce(result);
		return reduced;
	}

	/// <summary>
	/// Divides two <see cref="BigInteger"/> values and returns the quotient.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static BigRational Divide(BigInteger dividend, BigInteger divisor)
	{
		var remainder = new BigInteger(-1);
		var quotient = BigInteger.DivRem(dividend, divisor, out remainder);

		var result = new BigRational(
				quotient,
				new Fraction(remainder, divisor)
			);

		return result;
	}

	/// <summary>
	/// Divides two <see cref="ExtendedNumerics.BigRational"/> values and returns the quotient.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static BigRational Divide(BigRational dividend, BigRational divisor)
	{
		// a/b / c/d  == (ad)/(bc)			
		var l = dividend.GetImproperFraction();
		var r = divisor.GetImproperFraction();

		var ad = BigInteger.Multiply(l.Numerator, r.Denominator);
		var bc = BigInteger.Multiply(l.Denominator, r.Numerator);

		var newFraction = new Fraction(ad, bc);
		var result = Fraction.ReduceToProperFraction(newFraction);
		return result;
	}

	/// <summary>
	/// Divides two <see cref="ExtendedNumerics.BigRational"/> values and returns the remainder.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The remainder.</returns>
	public static BigRational Remainder(BigInteger dividend, BigInteger divisor)
	{
		var remainder = (dividend % divisor);
		return new BigRational(BigInteger.Zero, new Fraction(remainder, divisor));
	}

	/// <summary>
	/// Divides two <see cref="ExtendedNumerics.BigRational"/> values and returns the remainder (modulus).
	/// </summary>
	/// <param name="number">The dividend.</param>
	/// <param name="mod">The divisor.</param>
	/// <returns>The remainder (modulus).</returns>
	public static BigRational Mod(BigRational number, BigRational mod)
	{
		var num = number.GetImproperFraction();
		var modulus = mod.GetImproperFraction();

		return new BigRational(Fraction.Remainder(num, modulus));
	}

	/// <summary>
	/// Raises the specified <see cref="ExtendedNumerics.BigRational"/> base value to the specified exponent.
	/// </summary>
	/// <param name="baseValue">The base value.</param>
	/// <param name="exponent">The exponent.</param>
	/// <returns>The result of raising the base value to the exponent power.</returns>
	public static BigRational Pow(BigRational baseValue, BigInteger exponent)
	{
		var fractPow = Fraction.Pow(baseValue.GetImproperFraction(), exponent);
		return new BigRational(fractPow);
	}

	/// <summary>
	/// Returns the square root of the specified value.
	/// </summary>
	/// <param name="value">The base value to square root.</param>
	/// <returns>The square root of the specified value.</returns>
	public static BigRational Sqrt(BigRational value)
	{
		var input = value.GetImproperFraction();
		var result = Fraction.Sqrt(input);
		return Fraction.ReduceToProperFraction(result);
	}

	/// <summary>
	/// Returns the Nth root of a number up to a desired precision.
	/// The precision parameter is given in terms of the minimum number of correct decimal places.
	/// </summary>
	/// <param name="value">The value to take the Nth root of.</param>
	/// <param name="root">The Nth root to find of value. Also called the index.</param>
	/// <param name="precision">The minimum number of correct decimal places to return if the answer is not a rational number.</param>
	/// <returns>The Nth root of the specified value.</returns>
	/// <exception cref="System.Exception">Root must be greater than or equal to 1</exception>
	/// <exception cref="System.Exception">Value must be a positive integer</exception>
	public static BigRational NthRoot(BigRational value, int root, int precision = 30)
	{
		var input = value.GetImproperFraction();
		var result = Fraction.NthRoot(input, root, precision);
		return Fraction.ReduceToProperFraction(result);
	}

	/// <summary>
	/// Returns the natural (base e) logarithm of a specified number.
	/// </summary>
	/// <param name="rational">The number whose logarithm is to be found.</param>
	/// <returns>The natural (base e) logarithm of the specifed value.</returns>
	public static double Log(BigRational rational)
		=> Fraction.Log(rational.GetImproperFraction());

	/// <summary>
	/// Returns the absolute value of a <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="rational">A value to get the absolute value of.</param>
	/// <returns>The absolute value of value of the specified number.</returns>
	public static BigRational Abs(BigRational rational)
	{
		var input = Reduce(rational);
		return new (BigInteger.Abs(input.WholePart), input.FractionalPart);
	}

	/// <summary>
	/// Negates the specified value.
	/// </summary>
	/// <param name="rational">The number to negate the value of.</param>
	/// <returns>The result of the specified value multiplied by negative one (-1).</returns>
	public static BigRational Negate(BigRational rational)
	{
		var input = Reduce(rational);
		return input.WholePart == 0
			? new (input.WholePart, Fraction.Negate(input.FractionalPart))
			: new (BigInteger.Negate(input.WholePart), input.FractionalPart);
	}

	/// <summary>
	/// Adds two <see cref="ExtendedNumerics.Fraction"/> numbers and returns the sum as a <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="augend">The augend.</param>
	/// <param name="addend">The addend.</param>
	/// <returns>The sum.</returns>
	public static BigRational Add(Fraction augend, Fraction addend)
		=> new(BigInteger.Zero, Fraction.Add(augend, addend));

	/// <summary>
	/// Subtracts two <see cref="ExtendedNumerics.Fraction"/> numbers and returns the difference as a <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="minuend">The minuend.</param>
	/// <param name="subtrahend">The subtrahend.</param>
	/// <returns>The difference.</returns>
	public static BigRational Subtract(Fraction minuend, Fraction subtrahend)
		=> new(BigInteger.Zero, Fraction.Subtract(minuend, subtrahend));

	/// <summary>
	/// Multiplies two <see cref="ExtendedNumerics.Fraction"/> numbers and returns the product as a <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="multiplicand">The multiplicand.</param>
	/// <param name="multiplier">The multiplier.</param>
	/// <returns>The product.</returns>
	public static BigRational Multiply(Fraction multiplicand, Fraction multiplier)
		=> new(BigInteger.Zero, Fraction.Multiply(multiplicand, multiplier));

	/// <summary>
	/// Divides two <see cref="ExtendedNumerics.Fraction"/> numbers and returns the quotient as a <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static BigRational Divide(Fraction dividend, Fraction divisor)
		=> new (BigInteger.Zero, Fraction.Divide(dividend, divisor));

	#region GCD & LCM

	/// <summary>
	/// Finds the least common denominator of two <see cref="ExtendedNumerics.BigRational"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The least common denominator of left and right.</returns>
	public static BigRational LeastCommonDenominator(BigRational left, BigRational right)
	{
		var leftFrac = left.GetImproperFraction();
		var rightFrac = right.GetImproperFraction();

		return Reduce(new BigRational(Fraction.LeastCommonDenominator(leftFrac, rightFrac)));
	}

	/// <summary>
	/// Finds the greatest common divisor of two <see cref="ExtendedNumerics.BigRational"/> values.
	/// </summary>
	/// <param name="left">The first value.</param>
	/// <param name="right">The second value.</param>
	/// <returns>The greatest common divisor of left and right.</returns>
	public static BigRational GreatestCommonDivisor(BigRational left, BigRational right)
	{
		var leftFrac = left.GetImproperFraction();
		var rightFrac = right.GetImproperFraction();

		return Reduce(new BigRational(Fraction.GreatestCommonDivisor(leftFrac, rightFrac)));
	}

	#endregion

	#endregion

	#region Arithmetic Operators

	#region Binary Operator Overloads

	/// <summary>
	/// Adds two <see cref="BigRational"/> values and returns the sum.
	/// </summary>
	/// <param name="augend">The augend.</param>
	/// <param name="addend">The addend.</param>
	/// <returns>The sum.</returns>
	public static BigRational operator +(BigRational augend, BigRational addend) => Add(augend, addend);

	/// <summary>
	/// Subtracts two <see cref="BigRational"/> values and returns the difference.
	/// </summary>
	/// <param name="minuend">The minuend.</param>
	/// <param name="subtrahend">The subtrahend.</param>
	/// <returns>The difference.</returns>
	public static BigRational operator -(BigRational minuend, BigRational subtrahend) => Subtract(minuend, subtrahend);

	/// <summary>
	/// Multiplies two <see cref="BigRational"/> values and returns the product.
	/// </summary>
	/// <param name="multiplicand">The multiplicand.</param>
	/// <param name="multiplier">The multiplier.</param>
	/// <returns>The product.</returns>
	public static BigRational operator *(BigRational multiplicand, BigRational multiplier) => Multiply(multiplicand, multiplier);

	/// <summary>
	/// Divides two <see cref="BigRational"/> values and returns the quotient.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The quotient.</returns>
	public static BigRational operator /(BigRational dividend, BigRational divisor) => Divide(dividend, divisor);

	/// <summary>
	/// Divides two <see cref="BigRational"/> values and returns the remainder/modulus.
	/// </summary>
	/// <param name="dividend">The dividend.</param>
	/// <param name="divisor">The divisor.</param>
	/// <returns>The remainder that results from the division.</returns>
	public static BigRational operator %(BigRational dividend, BigRational divisor) => Mod(dividend, divisor);

	#endregion

	#region Unitary Operator Overloads

	/// <summary>
	/// Returns the value of the <see cref="ExtendedNumerics.BigRational"/> operand. (The sign of the operand is unchanged.)
	/// </summary>
	/// <param name="value">The value to return.</param>
	/// <returns>The value of the value operand.</returns>
	public static BigRational operator +(BigRational value) => value;

	/// <summary>
	/// Negates a specified <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="value">The value to negate.</param>
	/// <returns>The result of the value parameter multiplied by negative one (-1).</returns>
	public static BigRational operator -(BigRational value) => Negate(value);

	/// <summary>
	/// Increments a <see cref="ExtendedNumerics.BigRational"/> value by 1.
	/// </summary>
	/// <param name="value">The value to increment.</param>
	/// <returns>The value of the value parameter incremented by 1.</returns>
	public static BigRational operator ++(BigRational value) => Add(value, One);

	/// <summary>
	/// Decrements a <see cref="ExtendedNumerics.BigRational"/> value by 1.
	/// </summary>
	/// <param name="value">The value to decrement.</param>
	/// <returns>The value of the value parameter decremented by 1.</returns>
	public static BigRational operator --(BigRational value) => Subtract(value, One);

	#endregion

	#endregion

	#region Comparison Operators

	/// <summary>
	/// Returns a value that indicates whether the values of two
	/// <see cref="ExtendedNumerics.BigRational"/> objects are equal.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><c>true</c>  if the left and right parameters have the same value; otherwise, <c>false</c>.</returns>
	public static bool operator ==(BigRational left, BigRational right) => Compare(left, right) == 0;

	/// <summary>
	/// Returns a value that indicates whether two <see cref="ExtendedNumerics.BigRational"/> 
	/// objects have different values.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns><c>true</c>  if left and right are not equal; otherwise, <c>false</c>.</returns>
	public static bool operator !=(BigRational left, BigRational right) => Compare(left, right) != 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="ExtendedNumerics.BigRational"/> value is
	/// less than another <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value><c>true</c> if left is less than right; otherwise, <c>false</c>.</value>
	public static bool operator <(BigRational left, BigRational right) => Compare(left, right) < 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="ExtendedNumerics.BigRational"/> value is
	/// less than or equal to another <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value><c>true</c> if left is less than or equal to right; otherwise, <c>false</c>.</value>
	public static bool operator <=(BigRational left, BigRational right) => Compare(left, right) <= 0;

	/// <summary>
	/// Returns a value that indicates whether a <see cref="ExtendedNumerics.BigRational"/> value is
	/// greater than another <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value><c>true</c> if left is greater than right; otherwise, <c>false</c>.</value>
	public static bool operator >(BigRational left, BigRational right) => Compare(left, right) > 0;

	/// <summary>
	/// Returns a value that indicates whether a<see cref="ExtendedNumerics.BigRational"/> value is
	/// greater than or equal to another <see cref="ExtendedNumerics.BigRational"/> value.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <value><c>true</c> if left is greater than or equal to right; otherwise, <c>false</c>.</value>
	public static bool operator >=(BigRational left, BigRational right) => Compare(left, right) >= 0;

	#endregion

	#region Compare

	/// <summary>
	/// Compares two <see cref="ExtendedNumerics.BigRational"/> values and
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
	public static int Compare(BigRational left, BigRational right)
	{
		var leftRed = Reduce(left);
		var rightRed = Reduce(right);

		if (leftRed.WholePart == rightRed.WholePart)
		{
			var leftFrac = leftRed.GetImproperFraction();
			var rightFrac = right.GetImproperFraction();
			return Fraction.Compare(leftFrac, rightFrac);
		}
		else
		{
			return BigInteger.Compare(leftRed.WholePart, rightRed.WholePart);
		}
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
	/// <exception cref="System.ArgumentException">Argument must be of type BigRational</exception>
	public readonly int CompareTo(object obj) => obj == null
			? 1
			: obj is not BigRational r
			? throw new ArgumentException($"Argument must be of type {nameof(BigRational)}", nameof(obj))
			: Compare(this, r)
		;

	/// <summary>
	/// Compares the current instance with another object of the same type and
	/// returns an integer that indicates whether the current instance
	/// precedes, follows, or occurs in the same position in the sort order as the other object.
	/// Satisfies the <see cref="IComparable{BigRational}" /> interface implementation.
	/// </summary>
	/// <param name="other">An object to compare with this instance.</param>
	/// <returns>
	/// A value that indicates the relative order of the objects being compared. The return value has these meanings:
	/// Less than zero: This instance precedes <paramref name="other" /> in the sort order.
	/// Zero: This instance occurs in the same position in the sort order as <paramref name="other" />.
	/// Greater than zero: This instance follows <paramref name="other" /> in the sort order.
	/// </returns>
	public readonly int CompareTo(BigRational other) => Compare(this, other);

	#endregion

	#region Conversion

	/// <summary>
	/// Performs an implicit conversion from <see cref="System.Byte"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(byte value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="SByte"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(sbyte value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int16"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(short value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt16"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(ushort value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int32"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(int value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt32"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(uint value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="Int64"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(long value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="UInt64"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(ulong value) => new ((BigInteger)value);

	/// <summary>
	/// Performs an implicit conversion from <see cref="BigInteger"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator BigRational(BigInteger value) => new (value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Single"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator BigRational(float value) => new (value);


	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Double"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator BigRational(double value) => new (value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="System.Decimal"/> to <see cref="ExtendedNumerics.BigRational"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.BigRational"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator BigRational(decimal value) => new (value);

	/// <summary>
	/// Performs an explicit conversion from <see cref="ExtendedNumerics.BigRational"/> to <see cref="System.Double"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="System.Double"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator double(BigRational value)
	{
		var fract = (double)value.FractionalPart;
		var whole = (double)value.WholePart;
		var result = whole + (fract * (value.Sign == 0 ? 1 : value.Sign));
		if (value.WholePart == 0)
		{
			result = fract;
		}
		return result;
	}

	/// <summary>
	/// Performs an explicit conversion from <see cref="ExtendedNumerics.BigRational"/> to <see cref="System.Decimal"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="System.Decimal"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static explicit operator decimal(BigRational value)
	{
		var fract = (decimal)value.FractionalPart;
		var whole = (decimal)value.WholePart;
		var result = whole + (fract * (value.Sign == 0 ? 1 : value.Sign));
		if (value.WholePart == 0)
		{
			result = fract;
		}
		return result;
	}

	/// <summary>
	/// Performs an implicit conversion from <see cref="ExtendedNumerics.BigRational"/> to <see cref="ExtendedNumerics.Fraction"/>.
	/// </summary>
	/// <param name="value">The value to convert to a <see cref="ExtendedNumerics.Fraction"/>.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator Fraction(BigRational value) => Fraction.Simplify(new Fraction(
				BigInteger.Add(value.FractionalPart.Numerator, BigInteger.Multiply(value.WholePart, value.FractionalPart.Denominator)),
				value.FractionalPart.Denominator
			));

	/// <summary>
	/// Converts the string representation of a number to its <see cref="ExtendedNumerics.BigRational"/> equivalent.
	/// </summary>
	/// <param name="value">A string that contains the number to convert.</param>
	/// <returns> A value that is equivalent to the number specified in the value parameter.</returns>
	/// <exception cref="System.ArgumentException">Argument cannot be null, empty or whitespace.</exception>
	/// <exception cref="System.ArgumentException">Invalid string given for number.</exception>
	/// <exception cref="System.ArgumentException">Invalid string given for numerator.</exception>
	/// <exception cref="System.ArgumentException">Invalid string given for whole number.</exception>
	/// <exception cref="System.ArgumentException">Invalid fraction given as string to parse.</exception>
	/// <exception cref="System.ArgumentException">Invalid string given for denominator.</exception>
	public static BigRational Parse(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Argument cannot be null, empty or whitespace.");
		}

		var parts = value.Trim().Split('/');
		if (parts.Length == 1)
		{
			return !BigInteger.TryParse(parts[0], out var whole)
				?              throw new ArgumentException("Invalid string given for number.")
				: new BigRational(whole);
		}
		else if (parts.Length == 2)
		{
			BigInteger whole = BigInteger.Zero, numerator, denominator;

			var firstParts = parts[0].Trim().Split(['+', ' '], StringSplitOptions.RemoveEmptyEntries);
			if (firstParts.Length == 1)
			{
				if (!BigInteger.TryParse(parts[0].Trim(), out numerator))
				{
					throw new ArgumentException("Invalid string given for numerator.");
				}
			}
			else if (firstParts.Length == 2)
			{
				if (!BigInteger.TryParse(firstParts[0].Trim(), out whole))
				{
					throw new ArgumentException("Invalid string given for whole number.");
				}
				if (!BigInteger.TryParse(firstParts[1].Trim(), out numerator))
				{
					throw new ArgumentException("Invalid string given for numerator.");
				}
			}
			else
			{
				throw new ArgumentException("Invalid fraction given as string to parse.");
			}

			if (!BigInteger.TryParse(parts[1].Trim(), out denominator))
			{
				throw new ArgumentException("Invalid string given for denominator.");
			}
			return new BigRational(whole, numerator, denominator);
		}
		else
		{
			throw new ArgumentException("Invalid fraction given as string to parse.");
		}
	}

	#endregion

	#region Equality Methods

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// Satisfies the <see cref="IEquatable{BigRational}" /> interface implementation.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns><see langword="true" /> if the current object is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false" />.</returns>
	public readonly bool Equals(BigRational other)
	{
		var reducedThis = Reduce(this);
		var reducedOther = Reduce(other);

		var result = true;
		result &= reducedThis.WholePart.Equals(reducedOther.WholePart);
		result &= reducedThis.FractionalPart.Numerator.Equals(reducedOther.FractionalPart.Numerator);
		result &= reducedThis.FractionalPart.Denominator.Equals(reducedOther.FractionalPart.Denominator);
		return result;
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns><see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override readonly bool Equals(object obj)
		=> obj != null && obj is BigRational b && Equals(b);

	/// <summary>
	/// Returns a hash code for this instance.
	/// </summary>
	/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
	public override readonly int GetHashCode()
		=> CombineHashCodes(WholePart.GetHashCode(), FractionalPart.GetHashCode());

	/// <summary>
	/// Combines two hash codes into one.
	/// </summary>
	/// <param name="h1">The first hash.</param>
	/// <param name="h2">The second hash.</param>
	/// <returns>A new hashcode that represents the combination of the two specified hash codes.</returns>
	internal static int CombineHashCodes(int h1, int h2) => (((h1 << 5) + h1) ^ h2);

	#endregion

	#region Transform Methods

	/// <summary>
	/// Returns the value of this instance as a <see cref="ExtendedNumerics.Fraction"/>
	/// who's numerator may be larger than its denominator, called an improper fraction.
	/// </summary>
	/// <returns>A Fraction <see cref="ExtendedNumerics.Fraction"/> representation of this instance value.</returns>
	public readonly Fraction GetImproperFraction()
	{
		var input = NormalizeSign(this);

		if (input.WholePart == 0 && input.FractionalPart.Sign == 0)
		{
			return Fraction.Zero;
		}

		if (input.FractionalPart.Sign != 0 || input.FractionalPart.Denominator > 1)
		{
			if (input.WholePart.Sign != 0)
			{
				var whole = BigInteger.Multiply(input.WholePart, input.FractionalPart.Denominator);

				var remainder = input.FractionalPart.Numerator;

				if (input.WholePart.Sign == -1)
				{
					remainder = BigInteger.Negate(remainder);
				}

				var total = BigInteger.Add(whole, remainder);
				var newFractional = new Fraction(total, input.FractionalPart.Denominator);
				return newFractional;
			}
			else
			{
				return input.FractionalPart;
			}
		}
		else
		{
			return new Fraction(input.WholePart, BigInteger.One);
		}
	}

	/// <summary>
	/// Divides out any common divisors between the numerator and the denominator
	/// and then normalizes the sign.
	/// </summary>
	public static BigRational Reduce(BigRational value)
	{
		var input = NormalizeSign(value);
		var reduced = Fraction.ReduceToProperFraction(input.FractionalPart);
		var result = new BigRational(value.WholePart + reduced.WholePart, reduced.FractionalPart);
		return result;
	}

	/// <summary>
	/// Normalizes the sign of the specified value.
	/// That is, it examines all parts of the number
	/// (WholePart, FractionalPart Numerator and Denominator)
	/// and accounts for any negative values found and removes them.
	/// The resulting parity of the number is reflected in the
	/// sign of the WholePart property.
	/// </summary>
	public static BigRational NormalizeSign(BigRational value)
		=> value.NormalizeSign();

	/// <summary>
	/// Internal method that normalizes the sign of the current instance.
	/// </summary>
	internal BigRational NormalizeSign()
	{
		FractionalPart = Fraction.NormalizeSign(FractionalPart);
		if (WholePart > 0 && WholePart.Sign == 1 && FractionalPart.Sign == -1)
		{
			WholePart = BigInteger.Negate(WholePart);
			FractionalPart = Fraction.Negate(FractionalPart);
		}
		return this;
	}

	#endregion

	#region Overrides

	/// <summary>
	/// Converts the numeric value of the current <see cref="ExtendedNumerics.BigRational"/>
	/// instance into its equivalent string representation.
	/// </summary>
	/// <returns>The string representation of the current <see cref="ExtendedNumerics.BigRational"/> value.</returns>
	public override readonly string ToString()
		=> ToString(CultureInfo.CurrentCulture);

	/// <summary>
	/// Converts the numeric value of the current <see cref="ExtendedNumerics.BigRational"/>
	/// instance into its equivalent string representation by using the specified format.
	/// </summary>
	/// <param name="format">A standard or custom numeric format string.</param>
	/// <returns>
	/// The string representation of the current <see cref="ExtendedNumerics.BigRational"/> value
	/// in the format specified by the format parameter.
	/// </returns>
	public readonly string ToString(string _)
		=> ToString(CultureInfo.CurrentCulture);

	/// <summary>
	/// Converts the numeric value of the current <see cref="ExtendedNumerics.BigRational"/>
	/// instance into its equivalent string representation by using the specified
	/// culture-specific formatting information.
	/// </summary>
	/// <param name="provider">An object that supplies culture-specific formatting information.</param>
	/// <returns>
	/// The string representation of the current <see cref="ExtendedNumerics.BigRational"/> value in
	///	the format specified by the provider parameter.
	/// </returns>
	public readonly string ToString(IFormatProvider provider)
		=> ToString("R", provider);

	/// <summary>
	/// Converts the numeric value of the current <see cref="ExtendedNumerics.BigRational"/>
	/// instance into its equivalent string representation by using the specified 
	/// format and culture-specific format information.
	/// </summary>
	/// <param name="format">A standard or custom numeric format string.</param>
	/// <param name="provider">An object that supplies culture-specific formatting information.</param>
	/// <returns>
	/// The string representation of the current <see cref="ExtendedNumerics.BigRational"/> value as
	/// specified by the format and provider parameters.
	/// </returns>
	public readonly string ToString(string format, IFormatProvider provider)
	{
		var numberFormatProvider = (NumberFormatInfo)provider.GetFormat(typeof(NumberFormatInfo));
		numberFormatProvider ??= CultureInfo.CurrentCulture.NumberFormat;

		var zeroString = numberFormatProvider.NativeDigits[0];

		var input = Reduce(this);

		var whole = input.WholePart != 0 ? String.Format(provider, "{0}", input.WholePart.ToString(format, provider)) : string.Empty;
		var fractional = input.FractionalPart.Numerator != 0 ? String.Format(provider, "{0}", input.FractionalPart.ToString(format, provider)) : string.Empty;
		var join = string.Empty;

		if (!string.IsNullOrWhiteSpace(whole) && !string.IsNullOrWhiteSpace(fractional))
		{
			join = input.WholePart.Sign < 0 ? $" {numberFormatProvider.NegativeSign} " : $" {numberFormatProvider.PositiveSign} ";
		}

		return string.IsNullOrWhiteSpace(whole) && string.IsNullOrWhiteSpace(join) && string.IsNullOrWhiteSpace(fractional)
			? zeroString
			: string.Concat(whole, join, fractional);
	}

	#endregion
}

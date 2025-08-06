using System.Numerics;
using ExtendedNumerics;
using NUnit.Framework;

namespace TestBigRational;

[TestFixture(Category = "Core")]
public class TestBigRational
{
	public TestContext TestContext { get { return m_testContext; } set { m_testContext = value; } }
	private TestContext m_testContext;

	[Test]
	public void TestConstruction()
	{
		var result1 = new BigRational(BigInteger.Zero, new Fraction(182, 26));
		var result1_2 = new BigRational(new Fraction(182, 26));
		var result2 = new BigRational(BigInteger.Zero, new Fraction(-7, 5));

		var expected1 = new BigRational(7);
		var expected2 = new BigRational(-1, 2, 5);

		Assert.That(result1, Is.EqualTo(expected1));
		Assert.Multiple(() =>
		{
			Assert.That(result1_2, Is.EqualTo(expected1));
			Assert.That(result2, Is.EqualTo(expected2));
		});
	}

	[Test]
	public void TestExpandImproperFraction()
	{
		var threeAndOneThird = new BigRational(3, 1, 3);
		var oneEightyTwoTwentySixths = new BigRational(new Fraction(182, 26));
		var negativeThreeAndOneSeventh = new BigRational(-3, 1, 7);
		var seven = new BigRational(7);

		var expected313 = new Fraction(10, 3);
		var expected18226 = new Fraction(91, 13);
		var expectedNeg317 = new Fraction(-22, 7);
		var expected7over1 = new Fraction(7, 1);

		var result1 = threeAndOneThird.GetImproperFraction();
		var result2 = oneEightyTwoTwentySixths.GetImproperFraction();
		var result3 = negativeThreeAndOneSeventh.GetImproperFraction();
		var result7 = seven.GetImproperFraction();


		Assert.AreEqual(expected313, result1);
		Assert.AreEqual(expected18226, result2);
		Assert.AreEqual(expectedNeg317, result3);
		Assert.AreEqual(expected7over1, result7);
	}

	[Test]
	public void TestMullersRecurrenceConvergesOnFive()
	{
		// Set an upper limit to the number of iterations to be tried
		int n = 100;

		// Precreate some constants to use in the calculations
		var c108 = new BigRational(108);
		var c815 = new BigRational(815);
		var c1500 = new BigRational(1500);
		var convergencePoint = new BigRational(5);

		// Seed the initial values
		var X0 = new BigRational(4);
		var X1 = new BigRational(new Fraction(17, 4));
		var Xprevious = X0;
		var Xn = X1;

		// Get the current distance to the convergence point, this should be constantly
		// decreasing with each iteration
		var distanceToConvergence = BigRational.Subtract(convergencePoint, X1);

		int count = 1;
		for (int i = 1; i < n; ++i)
		{
			var Xnext = c108 - (c815 - c1500 / Xprevious) / Xn;
			var nextDistanceToConvergence = BigRational.Subtract(convergencePoint, Xnext);
			Assert.That(nextDistanceToConvergence, Is.LessThan(distanceToConvergence));

			Xprevious = Xn;
			Xn = Xnext;
			distanceToConvergence = nextDistanceToConvergence;
			if ((double)Xn == 5d)
				break;
			++count;
		}
		Assert.AreEqual((double)Xn, 5d);
		Assert.IsTrue(count == 70);
	}

	[Test]
	public void TestGetHashCode()
	{
		var testA1 = new BigRational(0, 1, 31);
		var testA2 = new BigRational(0, 2, 31);

		Assert.AreNotEqual(testA1.GetHashCode(), testA2.GetHashCode());
	}

	[Test]
	public void TestCompare()
	{
		var toCompareAgainst = new BigRational(0, 3, 5);

		var same = new BigRational(0, 6, 10);
		var larger = new BigRational(0, 61, 100);
		var smaller = new BigRational(0, 59, 100);
		var negative = new BigRational(0, -3, 5);

		int expected_Same = 0;
		int expected_Larger = -1;
		int expected_Smaller = 1;
		int expected_Negative = 1;

		int actual_Same = BigRational.Compare(toCompareAgainst, same);
		int actual_Larger = BigRational.Compare(toCompareAgainst, larger);
		int actual_Smaller = BigRational.Compare(toCompareAgainst, smaller);
		int actual_Negative = BigRational.Compare(toCompareAgainst, negative);

		Assert.AreEqual(expected_Same, actual_Same, $"Same: BigRational.Compare({toCompareAgainst}, {same}) == {expected_Same} (expected) ; Actual: {actual_Same}");
		Assert.AreEqual(expected_Larger, actual_Larger, $"Larger: BigRational.Compare({toCompareAgainst}, {larger}) == {expected_Larger} (expected) ; Actual: {actual_Larger}");
		Assert.AreEqual(expected_Smaller, actual_Smaller, $"Smaller: BigRational.Compare({toCompareAgainst}, {smaller}) == {expected_Smaller} (expected) ; Actual: {actual_Smaller}");
		Assert.AreEqual(expected_Negative, actual_Negative, $"Negative: BigRational.Compare({toCompareAgainst}, {negative}) == {expected_Negative} (expected) ; Actual: {actual_Negative}");
	}
}

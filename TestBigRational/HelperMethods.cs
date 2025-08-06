namespace TestBigRational;

public static class HelperMethods
{
	public static string FormatTimeSpan(TimeSpan timeSpan)
	{
		List<string> entries =
		[
			FormatTimeUnit(nameof(TimeSpan.Days), timeSpan.Days),
			FormatTimeUnit(nameof(TimeSpan.Hours), timeSpan.Hours),
			FormatTimeUnit(nameof(TimeSpan.Minutes), timeSpan.Minutes),
			FormatTimeUnit(nameof(TimeSpan.Seconds), timeSpan.Seconds),
			FormatTimeUnit(nameof(timeSpan.Milliseconds), timeSpan.Milliseconds),
		];
		return string.Join(", ", entries.Where(s => !string.IsNullOrEmpty(s)));
	}

	private static string FormatTimeUnit(string unitName, int quantity)
		=> (quantity > 0) ? $"{quantity} {unitName}" : string.Empty;
}

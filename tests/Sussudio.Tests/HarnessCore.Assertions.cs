static partial class Program
{
    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void AssertNearlyEqual(double expected, double actual, double tolerance, string fieldName)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}', tolerance '{tolerance}'.");
        }
    }

    private static void AssertContains(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}'.");
        }
    }

    private static void AssertDoesNotContain(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        if (normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected value not to contain '{token}'.");
        }
    }

    private static void AssertNotNull(object? value, string fieldName)
    {
        if (value == null)
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: value was null.");
        }
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n").Replace('\r', '\n');
}

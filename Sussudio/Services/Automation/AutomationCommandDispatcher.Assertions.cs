using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> SnapshotPropertyCache = new(StringComparer.OrdinalIgnoreCase);

    private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);
        var assertions = ParseAssertions(payload);
        var failures = new List<string>();
        foreach (var assertion in assertions)
        {
            if (!TryEvaluateAssertion(snapshot, assertion, out var failure))
            {
                failures.Add(failure ?? $"assertion-failed({assertion.Field})");
            }
        }

        var passed = failures.Count == 0;
        return CreateResponse(
            correlationId,
            passed
                ? $"All {assertions.Count} snapshot assertions passed."
                : $"{failures.Count} of {assertions.Count} snapshot assertions failed.",
            data: new Dictionary<string, object?>
            {
                ["assertions"] = assertions.Count,
                ["passed"] = passed,
                ["failures"] = failures
            },
            errorCode: passed ? null : "assertion-failed",
            success: passed,
            status: passed ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            snapshot: snapshot);
    }

    private static List<SnapshotAssertion> ParseAssertions(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("assertions", out var assertionsElement) ||
            assertionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("AssertSnapshot requires an 'assertions' array.");
        }

        var assertions = new List<SnapshotAssertion>();
        foreach (var item in assertionsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var field = GetString(item, "field");
            var op = GetString(item, "op") ?? "eq";
            var value = GetString(item, "value");
            if (string.IsNullOrWhiteSpace(field))
            {
                continue;
            }

            assertions.Add(new SnapshotAssertion
            {
                Field = field,
                Op = op,
                Value = value
            });
        }

        if (assertions.Count == 0)
        {
            throw new InvalidOperationException("AssertSnapshot requires at least one valid assertion object.");
        }

        return assertions;
    }

    private static bool TryEvaluateAssertion(
        AutomationSnapshot snapshot,
        SnapshotAssertion assertion,
        out string? failure)
    {
        var property = SnapshotPropertyCache.GetOrAdd(
            assertion.Field,
            field => typeof(AutomationSnapshot).GetProperty(
                field,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
        if (property == null)
        {
            failure = $"field-not-found({assertion.Field})";
            return false;
        }

        var actual = property.GetValue(snapshot);
        var op = assertion.Op?.Trim().ToLowerInvariant() ?? "eq";
        var expected = assertion.Value ?? string.Empty;

        if (TryCompareNumeric(actual, expected, op, out var numericResult))
        {
            failure = numericResult
                ? null
                : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actual})";
            return numericResult;
        }

        if (TryCompareBoolean(actual, expected, op, out var boolResult))
        {
            failure = boolResult
                ? null
                : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actual})";
            return boolResult;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty;
        var result = op switch
        {
            "eq" => string.Equals(actualText, expected, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(actualText, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => actualText.Contains(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        failure = result
            ? null
            : $"assertion-failed(field={property.Name},op={op},expected={expected},actual={actualText})";
        return result;
    }

    private static bool TryCompareNumeric(object? actual, string expected, string op, out bool result)
    {
        result = false;
        if (!double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber))
        {
            return false;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture);
        if (!double.TryParse(actualText, NumberStyles.Float, CultureInfo.InvariantCulture, out var actualNumber))
        {
            return false;
        }

        result = op switch
        {
            "eq" => Math.Abs(actualNumber - expectedNumber) < 0.0001,
            "neq" => Math.Abs(actualNumber - expectedNumber) >= 0.0001,
            "gt" => actualNumber > expectedNumber,
            "gte" => actualNumber >= expectedNumber,
            "lt" => actualNumber < expectedNumber,
            "lte" => actualNumber <= expectedNumber,
            _ => false
        };
        return true;
    }

    private static bool TryCompareBoolean(object? actual, string expected, string op, out bool result)
    {
        result = false;
        if (!bool.TryParse(expected, out var expectedBool))
        {
            return false;
        }

        var actualText = Convert.ToString(actual, CultureInfo.InvariantCulture);
        if (!bool.TryParse(actualText, out var actualBool))
        {
            return false;
        }

        result = op switch
        {
            "eq" => actualBool == expectedBool,
            "neq" => actualBool != expectedBool,
            _ => false
        };
        return true;
    }
}

using System;
using System.Globalization;
using System.Reflection;

namespace Sussudio.Services.Automation;

/// <summary>
/// Reads the text an <c>ItemsControl.DisplayMemberPath</c> shows for a view-model item, so a combo box of option objects
/// lists "1920x1080" instead of the item's type name. The path is a simple or dotted chain of public instance properties
/// (what the binding engine reads). Anything that does not resolve (blank path, no item, a missing or null segment, a
/// throwing getter, a value that is not text-like) returns null and never throws, so the caller falls back to the
/// container's visible text and then to <c>ToString()</c>; a failed read must cost that item's text, not the whole tree.
/// Contract: docs/architecture/performance-telemetry.md section 2.10.
/// </summary>
public static class UiDisplayMemberPath
{
    private const int MaxSegments = 8;

    public static string? Resolve(object? item, string? path)
    {
        if (item is null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var segments = path.Trim().Split('.');
        if (segments.Length > MaxSegments)
        {
            return null;
        }

        var current = item;
        foreach (var segment in segments)
        {
            if (segment.Length == 0 || current is null)
            {
                return null;
            }

            current = ReadProperty(current, segment);
        }

        return Format(current);
    }

    // Public instance properties with a public getter, by exact name. A member that hides an inherited one wins (the
    // most-derived declaration is what the binding engine reads), and an indexer is skipped because it needs arguments.
    private static object? ReadProperty(object target, string name)
    {
        try
        {
            var type = target.GetType();
            PropertyInfo? found = null;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!string.Equals(property.Name, name, StringComparison.Ordinal)
                    || property.GetIndexParameters().Length != 0
                    || property.GetMethod is not { IsPublic: true })
                {
                    continue;
                }

                if (found is null || property.DeclaringType == type)
                {
                    found = property;
                }
            }

            return found?.GetValue(target);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A throwing getter surfaces as TargetInvocationException; whatever it was, the item just has no resolved text.
            return null;
        }
    }

    // Only values with a natural text form: a nested view-model object would print its type name as if it were the text.
    private static string? Format(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        return value.GetType().IsPrimitive || value is IFormattable
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
    }
}

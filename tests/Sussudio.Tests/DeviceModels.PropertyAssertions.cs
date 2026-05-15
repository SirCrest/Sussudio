using System;
using System.Reflection;

static partial class Program
{
    private enum SetterExpectation
    {
        Required,
        Forbidden
    }

    private static PropertyInfo RequirePublicProperty(
        Type type,
        string propertyName,
        Type propertyType,
        SetterExpectation setterExpectation)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(property, $"{type.Name}.{propertyName}");
        AssertEqual(propertyType, property!.PropertyType, $"{type.Name}.{propertyName} property type");
        if (property.GetMethod == null || !property.GetMethod.IsPublic)
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must expose a public getter.");
        }

        if (setterExpectation == SetterExpectation.Required &&
            (property.SetMethod == null || !property.SetMethod.IsPublic))
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must expose a public setter.");
        }

        if (setterExpectation == SetterExpectation.Forbidden && property.SetMethod != null)
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must not expose a setter.");
        }

        return property;
    }
}

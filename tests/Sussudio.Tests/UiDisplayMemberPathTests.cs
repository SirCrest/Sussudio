using System;
using System.Globalization;
using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of reading a combo item's text from ItemsControl.DisplayMemberPath (docs/architecture/performance-telemetry.md
// section 2.10). The live app lists ResolutionComboBox and FrameRateComboBox items as "Sussudio.Models.ResolutionOption"
// because the items are view-model objects shown through DisplayMemberPath="DisplayText"; select <text> cannot work on them.
// Written before the code:
//   1. No path, a blank path or no item: nothing resolves (the caller falls back to the container's visible text, then
//      ToString), and nothing throws.
//   2. A simple property is read by its exact name, including a computed one such as DisplayText; a string is returned as is.
//   3. A dotted path walks nested public properties; an empty segment (".A", "A..B", "A.") resolves nothing.
//   4. A missing property at any segment, a null at any segment (intermediate or final) and a throwing getter resolve
//      nothing and never throw: a failed read must cost the item's text, not the whole tree.
//   5. Only public instance properties with a public getter are read: private and internal members, fields, static
//      properties and a property whose getter is not public are not what the binding engine shows, so they are not read.
//   6. An indexer is never read (it needs arguments); a member that hides an inherited one of another type resolves to the
//      hiding member instead of throwing an ambiguous-match error; an inherited public property resolves.
//   7. Non-string values are formatted invariantly: a frame rate of 59.94 must read "59.94" whatever the machine culture, or
//      the same item would have different text on different machines.
//   8. A value that is not text-like (a nested view-model object) resolves nothing instead of printing its type name as if
//      that were the item text.
//   9. An empty string is a resolved (empty) text, not a failure: the caller decides what to do with it.
public sealed class UiDisplayMemberPathTests
{
    private enum Mode
    {
        Fast,
        Slow,
    }

    private sealed class Inner
    {
        public string Label { get; init; } = "inner label";

        public Inner? Next { get; init; }
    }

    private class BaseOption
    {
        public string DisplayText => "base text";

        public string Shadowed => "base shadowed";
    }

    private sealed class Option : BaseOption
    {
        public string Value { get; init; } = "1920x1080";

        public string? Override { get; init; }

        public new int Shadowed => 7;

        public string Computed => string.IsNullOrWhiteSpace(Override) ? Value : Override!;

        public double Rate { get; init; } = 59.94;

        public bool Enabled { get; init; } = true;

        public Mode Mode { get; init; } = Mode.Slow;

        public Inner? Inner { get; init; } = new() { Next = new Inner { Label = "deep" } };

        public Inner? NoInner { get; init; }

        public object Complex { get; init; } = new object();

        public string Boom => throw new InvalidOperationException("getter failed");

        public string Empty { get; init; } = string.Empty;

        public string PrivateGetter { private get => "private"; set { _ = value; } }

        internal string InternalText => "internal";

        public static string StaticText => "static";

        public string FieldText = "field";

        public string this[int index] => "indexed " + index;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoPath_ResolvesNothing(string? path)
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), path));

    [Fact]
    public void NoItem_ResolvesNothing()
        => Assert.Null(UiDisplayMemberPath.Resolve(null, "Value"));

    [Fact]
    public void SimpleProperty_ReadsTheStringAsIs()
        => Assert.Equal("1920x1080", UiDisplayMemberPath.Resolve(new Option(), "Value"));

    [Fact]
    public void ComputedProperty_ReadsItsCurrentValue()
        => Assert.Equal("4K", UiDisplayMemberPath.Resolve(new Option { Override = "4K" }, "Computed"));

    [Fact]
    public void SurroundingWhitespaceInThePath_IsIgnored()
        => Assert.Equal("1920x1080", UiDisplayMemberPath.Resolve(new Option(), "  Value "));

    [Fact]
    public void DottedPath_WalksNestedProperties()
    {
        Assert.Equal("inner label", UiDisplayMemberPath.Resolve(new Option(), "Inner.Label"));
        Assert.Equal("deep", UiDisplayMemberPath.Resolve(new Option(), "Inner.Next.Label"));
    }

    [Theory]
    [InlineData(".Value")]
    [InlineData("Inner..Label")]
    [InlineData("Inner.")]
    public void EmptyPathSegment_ResolvesNothing(string path)
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), path));

    [Theory]
    [InlineData("Missing")]
    [InlineData("Inner.Missing")]
    [InlineData("Missing.Label")]
    public void MissingProperty_ResolvesNothing(string path)
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), path));

    [Theory]
    [InlineData("NoInner")]
    [InlineData("NoInner.Label")]
    [InlineData("Inner.Next.Next.Label")]
    public void NullAtAnySegment_ResolvesNothing(string path)
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), path));

    [Fact]
    public void ThrowingGetter_ResolvesNothingWithoutThrowing()
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), "Boom"));

    [Theory]
    [InlineData("PrivateGetter")]
    [InlineData("InternalText")]
    [InlineData("StaticText")]
    [InlineData("FieldText")]
    public void MembersThatAreNotPublicInstanceProperties_AreNotRead(string path)
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), path));

    [Fact]
    public void Indexer_IsNeverRead()
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), "Item"));

    [Fact]
    public void HidingMember_ResolvesToTheHidingMemberInsteadOfThrowing()
        => Assert.Equal("7", UiDisplayMemberPath.Resolve(new Option(), "Shadowed"));

    [Fact]
    public void InheritedPublicProperty_Resolves()
        => Assert.Equal("base text", UiDisplayMemberPath.Resolve(new Option(), "DisplayText"));

    [Fact]
    public void NonStringValues_AreFormattedInvariantly()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal("59.94", UiDisplayMemberPath.Resolve(new Option(), "Rate"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        Assert.Equal("True", UiDisplayMemberPath.Resolve(new Option(), "Enabled"));
        Assert.Equal("Slow", UiDisplayMemberPath.Resolve(new Option(), "Mode"));
    }

    [Fact]
    public void ValueThatIsNotTextLike_ResolvesNothingInsteadOfItsTypeName()
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), "Inner"));

    [Fact]
    public void ObjectValue_ResolvesNothing()
        => Assert.Null(UiDisplayMemberPath.Resolve(new Option(), "Complex"));

    [Fact]
    public void EmptyString_IsAResolvedEmptyText()
        => Assert.Equal(string.Empty, UiDisplayMemberPath.Resolve(new Option(), "Empty"));
}

using System.Collections;
using System.Reflection;

namespace XmlFormatter.Tests;

/// <summary>
/// Equality is handwritten because the synthesized version compared the patterns list by
/// reference. <see cref="Every_option_participates_in_equality"/> is what stops a new option
/// silently falling out of it.
/// </summary>
public class OptionsEqualityTests
{
    [Fact]
    public void Two_default_instances_are_equal()
    {
        // Named rather than inline: `new Options() == new Options()` reads to an analyzer as
        // comparing an expression with itself, and the operator is part of what is under test.
        var left = new Options();
        var right = new Options();

        Assert.Equal(left, right);
        Assert.True(left == right);
    }

    [Fact]
    public void Identical_settings_are_equal_and_hash_alike()
    {
        var left = new Options { IndentLength = 2, PreserveNewLines = true };
        var right = new Options { IndentLength = 2, PreserveNewLines = true };

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equal_instances_deduplicate_in_a_hash_set()
    {
        // The practical consequence of the old behavior: Options was unusable as a key.
        var set = new HashSet<Options>
        {
            new() { IndentLength = 2 },
            new() { IndentLength = 2 },
        };

        Assert.Single(set);
    }

    [Fact]
    public void Patterns_are_compared_by_content_not_by_reference()
    {
        var left = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["Content*"] };
        var right = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["Content*"] };

        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Patterns_differing_in_content_are_not_equal()
    {
        var left = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["Content*"] };
        var right = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["Label*"] };

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void Patterns_differing_only_in_order_are_not_equal()
    {
        // Order decides which pattern matches first, so it is part of the value.
        var left = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["a", "b"] };
        var right = new Options { WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["b", "a"] };

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void A_default_instance_equals_one_with_an_empty_pattern_list()
    {
        // default(Options) leaves the property null rather than empty; the two must not differ.
        Assert.Equal(default, new Options
        {
            IndentLength = 0,
            UseSelfClosingTags = false,
            AllowSingleQuoteInAttributeValue = false,
            AddSpaceBeforeSelfClosingTag = false,
            WrapCommentTextWithSpaces = false,
            AllowWhiteSpaceUnicodesInAttributeValues = false,
            PositionFirstAttributeOnSameLine = false,
            AddXmlDeclarationIfMissing = false,
            AttributesInNewlineThreshold = 0,
            WildCardedExceptionsForPositionAllAttributesOnFirstLine = [],
        });
    }

    [Fact]
    public void Every_option_participates_in_equality()
    {
        var properties = typeof(Options).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(properties);

        foreach (var property in properties)
        {
            object baseline = new Options();
            object changed = new Options();

            property.SetValue(changed, DifferentValueFor(property, baseline));

            Assert.False(((Options)baseline).Equals((Options)changed),
                         $"{property.Name} is missing from Options.Equals - two Options differing only "
                       + "in it compare equal.");
        }
    }

    /// <summary>A value guaranteed to differ from the one <paramref name="instance"/> currently holds.</summary>
    private static object DifferentValueFor(PropertyInfo property, object instance)
    {
        var current = property.GetValue(instance);

        return current switch
        {
            bool flag => !flag,
            int number => number + 1,
            IEnumerable => new List<string> { "a-pattern-no-default-has" },
            _ => throw new NotSupportedException($"{property.PropertyType.Name} has no rule here. Add one "
                                               + "so this guard keeps covering every option."),
        };
    }
}

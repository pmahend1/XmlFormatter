namespace XmlFormatter.Tests;

/// <summary>
/// <c>xml:space="preserve"</c> (XML 1.0 section 2.10, #55): no whitespace is added, removed or
/// replaced anywhere in preserved content - text and the whitespace between child elements alike,
/// as the XSLT serializer's indent rules require. The element carrying it is still placed like
/// any other, since that whitespace belongs to its parent.
///
/// A property of the document, not an option, so every option set must honour it. It is
/// inherited and <c>default</c> turns it back off, so the nearest ancestor carrying it decides.
///
/// Whitespace is written as \n and \t escapes so re-indenting this file cannot change the inputs.
/// </summary>
public class XmlSpacePreserveTests
{
    [Fact]
    public void The_content_of_the_element_carrying_preserve_is_left_as_written()
    {
        // The reported case: the reflow deleted the leading spaces on "b" for good.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">a\n  b</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">a\n  b</pre>\n</r>", formatted);
    }

    [Fact]
    public void A_tab_in_preserved_content_survives()
    {
        // The reflow's Trim() drops a tab as readily as a space.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">a\n\tb</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">a\n\tb</pre>\n</r>", formatted);
    }

    [Fact]
    public void Single_line_preserved_content_is_left_as_written()
    {
        // Never broken - text without a line break is never reflowed.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">  a  </pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">  a  </pre>\n</r>", formatted);
    }

    [Fact]
    public void No_indentation_is_added_around_a_preserved_child_element()
    {
        // Leading with an element used to lay the content out as a block, which indented <b />,
        // trimmed the trailing spaces and broke the line before </pre>.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\"><b/>a\n  b  </pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\"><b />a\n  b  </pre>\n</r>", formatted);
    }

    [Fact]
    public void Whitespace_between_preserved_child_elements_is_left_as_written()
    {
        // The resx shape. This whitespace is the one thing XSLT's xml:space handling protects.
        const string input = "<r><data xml:space=\"preserve\">\n\t\t<value>x\ny</value>\n\t\t<comment>c</comment>\n\t</data></r>";

        var formatted = TestFormatter.Format(input, TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <data xml:space=\"preserve\">\n\t\t<value>x\ny</value>\n\t\t<comment>c</comment>\n\t</data>\n</r>",
                     formatted);
    }

    [Fact]
    public void Whitespace_only_preserved_content_is_left_as_written()
    {
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">   </pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">   </pre>\n</r>", formatted);
    }

    [Fact]
    public void Comments_cdata_and_processing_instructions_in_preserved_content_are_left_as_written()
    {
        // Each has its own line-breaking rule outside preserved content.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">a <!--  c  --> <![CDATA[ d ]]> <?pi x ?> e</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">a <!--  c  --> <![CDATA[ d ]]> <?pi x ?> e</pre>\n</r>",
                     formatted);
    }

    [Fact]
    public void Layout_after_a_preserved_element_is_unaffected()
    {
        // The issue's second example, which used to lose indentation for everything after it.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">  a\n   b  <i>x</i>  </pre><b/></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">  a\n   b  <i>x</i>  </pre>\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void Preserved_text_still_escapes_invisible_characters_when_asked()
    {
        // An escape changes how a character is spelled, not whether it is there.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\">a&#xA0;\n  b</pre></r>",
                                             TestOptions.NoDeclaration with { EscapeInvisibleNonAsciiCharacters = true });

        Assert.Equal("<r>\n    <pre xml:space=\"preserve\">a&#xA0;\n  b</pre>\n</r>", formatted);
    }

    [Fact]
    public void Preserve_on_the_parent_governs_a_child_element()
    {
        var formatted = TestFormatter.Format("<r xml:space=\"preserve\"><pre>a\n  b</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r xml:space=\"preserve\"><pre>a\n  b</pre></r>", formatted);
    }

    [Fact]
    public void Preserve_governs_a_descendant_further_down_than_one_level()
    {
        // Two levels down: the old check looked at exactly one fixed ancestor.
        var formatted = TestFormatter.Format("<r xml:space=\"preserve\"><a><pre>x\n  y</pre></a></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r xml:space=\"preserve\"><a><pre>x\n  y</pre></a></r>", formatted);
    }

    [Fact]
    public void The_nearest_ancestor_wins_when_a_descendant_sets_default()
    {
        // default reflows <pre>'s own content, but the whitespace around it is still <r>'s.
        var formatted = TestFormatter.Format("<r xml:space=\"preserve\"><pre xml:space=\"default\">a\n  b</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r xml:space=\"preserve\"><pre xml:space=\"default\">\n        a\n        b\n    </pre></r>",
                     formatted);
    }

    [Fact]
    public void A_padded_attribute_value_is_read_the_way_the_reader_reads_it()
    {
        // The reader accepts a padded value, so the formatter must read it the same way.
        var formatted = TestFormatter.Format("<r><pre xml:space=\"  preserve  \">a\n  b</pre></r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <pre xml:space=\"  preserve  \">a\n  b</pre>\n</r>", formatted);
    }

    [Theory]
    [InlineData("<r><pre xml:space=\"preserve\">a\n  b\n\tc  </pre></r>")]
    [InlineData("<r><pre xml:space=\"preserve\"><b/>a\n  b  </pre></r>")]
    [InlineData("<r><data xml:space=\"preserve\">\n\t\t<value>x\ny</value>\n\t</data></r>")]
    [InlineData("<r xml:space=\"preserve\"><pre xml:space=\"default\">a\n  b</pre></r>")]
    public void Preserved_content_does_not_move_on_a_second_format(string input)
    {
        var once = TestFormatter.Format(input, TestOptions.NoDeclaration);
        var twice = TestFormatter.Format(once, TestOptions.NoDeclaration);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Preserve_is_honoured_under_every_baseline_option_set()
    {
        foreach (var (_, options) in OptionSets.All)
        {
            var formatted = TestFormatter.Format("<r><pre xml:space=\"preserve\"><b/>a\n  b  </pre></r>", options);

            Assert.Contains("<pre xml:space=\"preserve\"><b />a\n  b  </pre>", formatted);
        }
    }
}

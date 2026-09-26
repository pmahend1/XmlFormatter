namespace XmlFormatter.Tests;

/// <summary>
/// Indentation across mixed content - an element holding character data and child elements
/// together. The indent count used to be raised and lowered on conditions that mentioned text,
/// so text on one side of an element's children and markup on the other left every later end
/// tag a level out, up to and including the root's own (#56, #60).
/// <para>
/// Where the end tag goes is a separate question, answered the way PrettyXML's range formatter
/// already answers it: an element written whole on one line keeps its end tag on it, and one
/// whose content has started a line of its own is a block, so the end tag takes one too.
/// </para>
/// </summary>
public class MixedContentIndentationTests
{
    [Fact]
    public void An_element_written_on_one_line_keeps_its_end_tag_on_it()
    {
        var formatted = TestFormatter.Format("<r><p>a<i/></p></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <p>a<i /></p>
                     </r>
                     """, formatted);
    }

    [Fact]
    public void A_sibling_after_mixed_content_keeps_its_parents_indent()
    {
        var formatted = TestFormatter.Format("<r><p>a<i/></p><z/></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <p>a<i /></p>
                         <z />
                     </r>
                     """, formatted);
    }

    [Fact]
    public void Content_that_ends_in_text_does_not_indent_the_end_tags_that_follow_it()
    {
        // The root's own end tag was written at column 4 here, which no document should ever do.
        var formatted = TestFormatter.Format("<r><p><i/>a</p></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <p>
                             <i />a
                         </p>
                     </r>
                     """, formatted);
    }

    [Fact]
    public void Every_ancestor_of_mixed_content_closes_at_its_own_column()
    {
        var formatted = TestFormatter.Format("<r><s><t><p><b>x</b> c</p></t></s></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <s>
                             <t>
                                 <p>
                                     <b>x</b> c
                                 </p>
                             </t>
                         </s>
                     </r>
                     """, formatted);
    }

    [Fact]
    public void An_element_after_text_is_indented_one_level_below_its_parent()
    {
        // <b /> belongs under <r>, not beside it: the text before <a /> is content, not a level.
        var formatted = TestFormatter.Format("<w><r>x<a/><b/></r></w>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <w>
                         <r>x<a />
                             <b />
                         </r>
                     </w>
                     """, formatted);
    }

    [Fact]
    public void Hand_indented_mixed_content_is_left_where_it_was()
    {
        // The case a user actually sees: markup inside a paragraph of text, already laid out.
        const string source = "<p>\n    some\n    <b>bold</b>\n    text\n</p>";

        var formatted = TestFormatter.Format(source, TestOptions.NoDeclaration);

        Assert.Equal(source, formatted);
    }

    [Fact]
    public void Text_between_two_elements_stays_on_their_line()
    {
        var formatted = TestFormatter.Format("<r><a/>x<b/>y</r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <a />x<b />y
                     </r>
                     """, formatted);
    }

    [Fact]
    public void Trailing_whitespace_is_dropped_from_the_last_line_of_a_block()
    {
        // The end tag is about to start a line, so the space before it is the formatter's own.
        // Left in place it rejoins the text node on the next format and reflows as multi-line.
        var once = TestFormatter.Format("<r><a/>x </r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <a />x
                     </r>
                     """, once);
        Assert.Equal(once, TestFormatter.Format(once, TestOptions.NoDeclaration));
    }

    [Theory]
    [InlineData("<r><p>y z\n\n </p></r>", "<r>\n    <p>\n        y z\n\n    </p>\n</r>")]
    [InlineData("<r><p>a\n\nb</p></r>", "<r>\n    <p>\n        a\n\n        b\n    </p>\n</r>")]
    public void A_blank_line_inside_text_is_written_without_indent(string xml, string expected)
    {
        var formatted = TestFormatter.Format(xml, TestOptions.NoDeclaration);

        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData("<r><p>a<i/></p></r>")]
    [InlineData("<r><p>a<i/></p><z/></r>")]
    [InlineData("<r><p><i/>a</p></r>")]
    [InlineData("<r><q><p>a\nb<i>x</i></p></q></r>")]
    [InlineData("<w><r><a/>x</r></w>")]
    [InlineData("<r><s><t><p><b>x</b> c</p></t></s></r>")]
    [InlineData("<r><p>a <b>x</b></p><q/></r>")]
    [InlineData("<w><r>x<a/><b/></r></w>")]
    [InlineData("<p>\n    some\n    <b>bold</b>\n    text\n</p>")]
    [InlineData("<r><a/>x</r>")]
    [InlineData("<p><b>bold</b> text</p>")]
    [InlineData("<r><a/>x<b/>y</r>")]
    public void Mixed_content_settles_after_one_format(string xml)
    {
        var once = TestFormatter.Format(xml, TestOptions.NoDeclaration);
        var twice = TestFormatter.Format(once, TestOptions.NoDeclaration);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void The_indent_count_comes_back_to_where_it_started()
    {
        // The invariant behind all of the above: the count is a field, so a document that fails
        // to unwind it indents the next one formatted by the same instance.
        var formatter = new Formatter();

        formatter.Format("<r><p><i/>a</p></r>", TestOptions.NoDeclaration);
        var second = formatter.Format("<r><a/></r>", TestOptions.NoDeclaration).Replace("\r\n", "\n");

        Assert.Equal(TestFormatter.Format("<r><a/></r>", TestOptions.NoDeclaration), second);
    }
}

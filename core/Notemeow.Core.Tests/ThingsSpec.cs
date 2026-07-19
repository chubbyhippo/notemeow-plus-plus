// Copyright (C) 2026 Chubby Hippo
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <https://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: GPL-3.0-or-later

using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class ThingsSpec : SpecDsl
    {
        [Fact(DisplayName =
            "given caret inside parens when comma r then inner round is selected forward")]
        public void InnerRoundForward()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys(",r");
            ThenSelection("bar baz");
            ThenSelType(SelType.Transient);
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName =
            "given caret inside parens when dot r then bounds include the parens and select backward")]
        public void BoundsRoundBackward()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys(".r");
            ThenSelection("(bar baz)");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given nested pairs when comma r then the innermost pair wins")]
        public void InnermostPairWins()
        {
            Given("nested", "(a (b<caret>) c)");
            WhenKeys(",r");
            ThenSelection("b");
        }

        [Fact(DisplayName = "given square and curly things then s and c select them")]
        public void SquareAndCurly()
        {
            Given("square", "a [b<caret> c] d");
            WhenKeys(",s");
            ThenSelection("b c");

            Given("curly", "a {b<caret> c} d");
            WhenKeys(".c");
            ThenSelection("{b c}");
        }

        [Fact(DisplayName =
            "given a double quoted string when comma g then the quoted run is selected")]
        public void DoubleQuotedString()
        {
            Given("string", "say \"hi th<caret>ere\" now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("\"hi there\"");
        }

        [Fact(DisplayName =
            "given a single quoted string when comma g then inner selects the run and dot g keeps the quotes")]
        public void SingleQuotedString()
        {
            Given("single quotes", "say 'hi th<caret>ere' now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("'hi there'");
        }

        [Fact(DisplayName =
            "given a backtick string when comma g then inner selects the run and dot g keeps the backticks")]
        public void BacktickString()
        {
            Given("backticks", "say `hi th<caret>ere` now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("`hi there`");
        }

        [Fact(DisplayName =
            "given a triple double quoted string when comma g then inner drops all three quotes and dot g keeps them")]
        public void TripleDoubleQuotedString()
        {
            Given("triple double", "say \"\"\"hi th<caret>ere\"\"\" now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("\"\"\"hi there\"\"\"");
        }

        [Fact(DisplayName =
            "given a triple single quoted string when comma g then inner drops all three quotes and dot g keeps them")]
        public void TripleSingleQuotedString()
        {
            Given("triple single", "say '''hi th<caret>ere''' now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("'''hi there'''");
        }

        [Fact(DisplayName =
            "given a triple backtick fence when comma g then inner drops all three backticks and dot g keeps them")]
        public void TripleBacktickFence()
        {
            Given("triple backtick", "say ```hi th<caret>ere``` now");
            WhenKeys(",g");
            ThenSelection("hi there");
            WhenKeys(".g");
            ThenSelection("```hi there```");
        }

        [Fact(DisplayName =
            "given a triple quoted docstring spanning lines when comma g then the whole multiline run is selected")]
        public void TripleQuotedDocstringSpanningLines()
        {
            Given("multiline docstring", "x = \"\"\"\nhe<caret>llo\nworld\n\"\"\"");
            WhenKeys(",g");
            ThenSelection("\nhello\nworld\n");
            WhenKeys(".g");
            ThenSelection("\"\"\"\nhello\nworld\n\"\"\"");
        }

        [Fact(DisplayName =
            "given an apostrophe earlier on another line when comma g then the real string below still selects")]
        public void ApostropheEarlierAnotherLine()
        {
            Given("stray apostrophe", "don't\nx = 'h<caret>i'");
            WhenKeys(",g");
            ThenSelection("hi");
        }

        [Fact(DisplayName = "given an unterminated quote when comma g then nothing is selected")]
        public void UnterminatedQuote()
        {
            Given("unterminated", "it'<caret>s fine");
            WhenKeys(",g");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given a symbol thing when comma e then the symbol is selected")]
        public void SymbolThing()
        {
            Given("symbol", "f<caret>oo_bar baz");
            WhenKeys(",e");
            ThenSelection("foo_bar");
        }

        [Fact(DisplayName = "given a paragraph when comma p then the block of lines is selected")]
        public void ParagraphInner()
        {
            Given("paragraphs", "aaa\nb<caret>bb\n\nccc");
            WhenKeys(",p");
            ThenSelection("aaa\nbbb");
        }

        [Fact(DisplayName = "given a paragraph when dot p then trailing blank lines are included")]
        public void ParagraphBounds()
        {
            Given("paragraphs", "aaa\nb<caret>bb\n\nccc");
            WhenKeys(".p");
            ThenSelection("aaa\nbbb\n\n");
        }

        [Fact(DisplayName = "given a line thing then comma l excludes and dot l includes the newline")]
        public void LineThing()
        {
            Given("lines", "a<caret>b\ncd");
            WhenKeys(",l");
            ThenSelection("ab");
            WhenKeys(".l");
            ThenSelection("ab\n");
        }

        [Fact(DisplayName = "given the buffer thing when comma b then everything is selected")]
        public void BufferThing()
        {
            Given("buffer", "on<caret>e\ntwo");
            WhenKeys(",b");
            ThenSelection("one\ntwo");
        }

        [Fact(DisplayName = "given sentences when comma dot then the sentence around point is selected")]
        public void SentenceThing()
        {
            Given("sentences", "One. Tw<caret>o. Three.");
            WhenKeys(",.");
            ThenSelection("Two.");
        }

        [Fact(DisplayName =
            "given a curly block in plain text when comma d then the defun fallback selects the braces")]
        public void DefunFallback()
        {
            Given("pseudo function", "fun x() {\n  bo<caret>dy\n}");
            WhenKeys(",d");
            ThenSelection("{\n  body\n}");
        }

        [Fact(DisplayName =
            "given open bracket r then selects from point back to the thing beginning with cursor at the beginning")]
        public void BeginningOfThing()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("[r");
            ThenSelection("b");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName =
            "given close bracket r then selects from point to the thing end with cursor at the end")]
        public void EndOfThing()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("]r");
            ThenSelection("ar baz");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName = "given angle bracket aliases then they behave like square brackets")]
        public void AngleBracketAlias()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("<r");
            ThenCaretAtSelectionStart();
            ThenSelection("b");
        }

        [Fact(DisplayName = "given no thing at point when comma r then the selection is unchanged")]
        public void NoThingAtPoint()
        {
            Given("no parens", "he<caret>llo");
            WhenKeys(",r");
            ThenNoSelection();
        }

        [Fact(DisplayName = "given o then the enclosing block including delimiters is selected")]
        public void BlockIncludesDelimiters()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("o");
            ThenSelection("(bar baz)");
            ThenSelType(SelType.Block);
        }

        [Fact(DisplayName = "given a block selection when o again then it expands to the parent block")]
        public void BlockExpandsToParent()
        {
            Given("nested", "((x<caret>))");
            WhenKeys("o");
            ThenSelection("(x)");
            WhenKeys("o");
            ThenSelection("((x))");
        }

        [Fact(DisplayName = "given a negative argument when o then the block selection is backward")]
        public void BlockNegativeBackward()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("-o");
            ThenSelection("(bar baz)");
            ThenCaretAtSelectionStart();
        }

        [Fact(DisplayName = "given O then selects from point to the end of the current block")]
        public void ToBlockEnd()
        {
            Given("round pair", "foo (b<caret>ar baz) qux");
            WhenKeys("O");
            ThenSelection("ar baz)");
            ThenCaretAtSelectionEnd();
        }

        [Fact(DisplayName =
            "given m then the join region between this line and the previous non-empty one is selected")]
        public void JoinRegionBackward()
        {
            Given("indented continuation", "one\n  t<caret>wo");
            WhenKeys("m");
            ThenSelType(SelType.Join);
            ThenSelection("\n  ");
        }

        [Fact(DisplayName = "given the first line when m then nothing is selected")]
        public void JoinFirstLineNothing()
        {
            Given("first line", "o<caret>ne\ntwo");
            WhenKeys("m");
            ThenNoSelection();
        }

        [Fact(DisplayName =
            "given negative argument when - m then the join region reaches forward instead")]
        public void JoinForwardNegative()
        {
            Given("forward join", "o<caret>ne\n  two");
            WhenKeys("-m");
            ThenSelType(SelType.Join);
            ThenSelection("\n  ");
        }

        [Fact(DisplayName = "given a CRLF document then the line thing bounds include the whole delimiter")]
        public void CrlfLineThingBoundsIncludeWholeDelimiter()
        {
            Given("two crlf lines", "a<caret>b\r\ncd");
            WhenKeys(".l");
            ThenSelection("ab\r\n");
        }
    }
}

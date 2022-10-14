using Extension.SnippetFormats;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	internal class EditorUtilsTest
	{
		[Test]
		[TestCase("// this is a comment", ExpectedResult = true)]
		[TestCase("//this is also a comment", ExpectedResult = true)]
		[TestCase("			//this is a comment as well", ExpectedResult = true)]
		[TestCase("var test = \"test\";", ExpectedResult = false)]
		[TestCase("var test = \"test\"; // inline comment", ExpectedResult = false)]
		[TestCase("/ this is not a comment", ExpectedResult = false)]
		public bool IsComment_should_consider_csharp_comment(string line)
		{
			// act & assert
			return EditorUtils.IsComment(line);
		}

		[Test]
		[TestCase("", ExpectedResult = true)]
		[TestCase("\t\t", ExpectedResult = true)]
		[TestCase("			", ExpectedResult = true)]
		[TestCase("var test = \"test\";", ExpectedResult = false)]
		[TestCase("\tsome text", ExpectedResult = false)]
		[TestCase(" // test", ExpectedResult = false)]
		public bool IsStartOfLine_should_ignore_whitespaces_and_tabs(string line)
		{
			// act & assert
			return EditorUtils.IsStartOfLine(line);
		}

		[Test]
		[TestCase("//read file", ExpectedResult = true)]
		[TestCase("// read file async", ExpectedResult = true)]
		[TestCase("//read", ExpectedResult = false)]
		[TestCase("// read", ExpectedResult = false)]
		[TestCase("var test = \"test\";", ExpectedResult = false)]
		[TestCase("var test = \"test\"; // inline comment", ExpectedResult = false)]
		public bool IsSemanticSearchComment_should_consider_keyword_count(string line)
		{
			// act & assert
			return EditorUtils.IsSemanticSearchComment(line);
		}

		[Test]
		// use tabs
		[TestCase("\t\tpublic void main()", 4, 4, false, ExpectedResult = 2)]
		[TestCase("\tpublic void main()", 4, 4, false, ExpectedResult = 1)]
		[TestCase("\tpublic void main()", 2, 4, false, ExpectedResult = 2)]
		[TestCase("\t\tpublic void main()", 2, 4, false, ExpectedResult = 4)]
		[TestCase("\tpublic void main()", 2, 8, false, ExpectedResult = 4)]
		[TestCase("\tpublic void main()", 8, 4, false, ExpectedResult = 0)]
		[TestCase("\tpublic void main()", 4, 8, false, ExpectedResult = 2)]
		// use space
		[TestCase("        public void main()", 4, 4, true, ExpectedResult = 2)]
		[TestCase("    public void main()", 4, 4, true, ExpectedResult = 1)]
		[TestCase("    public void main()", 2, 4, true, ExpectedResult = 2)]
		[TestCase("        public void main()", 2, 4, true, ExpectedResult = 4)]
		[TestCase("        public void main()", 2, 8, true, ExpectedResult = 4)]
		// mixed
		[TestCase("\tpublic void main()", 4, 8, false, ExpectedResult = 2)]
		[TestCase("\t    public void main()", 4, 8, false, ExpectedResult = 3)]
		[TestCase("\t     public void main()", 4, 8, false, ExpectedResult = 3)]
		public int GetIndentLevel_should_return_indent_level_based_on_config(string line, int indentSize, int tabSize, bool useSpace)
		{
			// act
			var level = EditorUtils.GetIndentLevel(line, indentSize, tabSize, useSpace);

			// assert
			return level;
		}

		[Test]
		public void IndentCodeBlock_should_retain_existing_indentation_using_tabs()
		{
			// arrange
			var codeBlock =
				"\tpublic void main()\n" +
				"{\n" +
				"\t\n" +
				"}";
																																									
			// act
			var indented = EditorUtils.IndentCodeBlock(
				code: codeBlock, 
				indentLevel: 1, 
				indentSize: 4, 
				tabSize: 4, 
				useSpace: false);

			// assert
			Assert.That(indented, Is.EqualTo("\tpublic void main()\n" +
											 "\t{\n" +
											 "\t\t\n" +
											 "\t}"));
		}

		[Test]
		public void IndentCodeBlock_should_retain_existing_indentation_using_whitespaces()
		{		
			// arrange
			var codeBlock =
				"    public void main()\n" +
				"{\n" +
				"    \n" +
				"}";

			// act
			var indented = EditorUtils.IndentCodeBlock(
				code: codeBlock,
				indentLevel: 1,
				indentSize: 4,
				tabSize: 4,
				useSpace: true);

			// assert
			Assert.That(indented, Is.EqualTo("    public void main()\n" +
											 "    {\n" +
											 "        \n" +
											 "    }"));
					}

		[Test]
		public void IndentCodeBlock_should_retain_existing_indentation_using_small_indent_size()
		{
			// arrange
			var codeBlock =
				"  public void main()\n" +
				"{\n" +
				"  \n" +
				"}";

			// act
			var indented = EditorUtils.IndentCodeBlock(
				code: codeBlock,
				indentLevel: 1,
				indentSize: 2,
				tabSize: 4,
				useSpace: false);

			// assert
			Assert.That(indented, Is.EqualTo("  public void main()\n" +
											 "  {\n" +
											 "    \n" +
											 "  }"));
		}
	}
}

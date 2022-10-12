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
	}
}

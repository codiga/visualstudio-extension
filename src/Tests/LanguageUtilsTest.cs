using Extension.SnippetFormats;
using NUnit.Framework;

namespace Tests
{
	/// <summary>
	/// Unit test for <see cref="LanguageUtils"/>.
	/// </summary>
	[TestFixture]
	internal class LanguageUtilsTest
	{
		[Test]
		[TestCase(".cs", ExpectedResult = LanguageUtils.LanguageEnumeration.Csharp)]
		[TestCase(".py", ExpectedResult = LanguageUtils.LanguageEnumeration.Python)]
		[TestCase(".css", ExpectedResult = LanguageUtils.LanguageEnumeration.Css)]
		[TestCase(".java", ExpectedResult = LanguageUtils.LanguageEnumeration.Java)]
		[TestCase(".cpp", ExpectedResult = LanguageUtils.LanguageEnumeration.Cpp)]
		[TestCase(".unknwon", ExpectedResult = LanguageUtils.LanguageEnumeration.Unknown)]
		[TestCase(null, ExpectedResult = LanguageUtils.LanguageEnumeration.Unknown)]
		public LanguageUtils.LanguageEnumeration Parse_should_return_language_based_on_file_extension(string ext)
		{
			//act & assert
			return LanguageUtils.Parse(ext);
		}

		[Test]
		[TestCase("dockerfile.myfile", ExpectedResult = LanguageUtils.LanguageEnumeration.Docker)]
		[TestCase("myfile.dockerfile", ExpectedResult = LanguageUtils.LanguageEnumeration.Docker)]
		[TestCase("Dockerfile", ExpectedResult = LanguageUtils.LanguageEnumeration.Docker)]
		[TestCase("dockerfile", ExpectedResult = LanguageUtils.LanguageEnumeration.Docker)]
		[TestCase("dock", ExpectedResult = LanguageUtils.LanguageEnumeration.Unknown)]
		[TestCase("myfile.cpp", ExpectedResult = LanguageUtils.LanguageEnumeration.Cpp)]
		public LanguageUtils.LanguageEnumeration ParseFromFileName_should_return_language_based_on_filename(
			string fileName)
		{
			return LanguageUtils.ParseFromFileName(fileName);
		}
	}
}

using Extension.SnippetFormats;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
	[TestFixture]
	internal class LanguageUtilsTest
	{
		//TODO complete the test
		[Test]
		[TestCase(".cs", ExpectedResult = LanguageUtils.LanguageEnumeration.Csharp)]
		[TestCase(".py", ExpectedResult = LanguageUtils.LanguageEnumeration.Python)]
		[TestCase(".css", ExpectedResult = LanguageUtils.LanguageEnumeration.Css)]
		[TestCase(".java", ExpectedResult = LanguageUtils.LanguageEnumeration.Java)]
		[TestCase(".cpp", ExpectedResult = LanguageUtils.LanguageEnumeration.Cpp)]
		public LanguageUtils.LanguageEnumeration Parse_should_return_language_based_on_file_extension(string ext)
		{
			//act & assert
			return LanguageUtils.Parse(ext);
		}
	}
}

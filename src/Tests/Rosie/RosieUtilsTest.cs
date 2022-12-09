using Extension.Rosie;
using Extension.SnippetFormats;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="RosieUtils"/>.
    /// </summary>
    [TestFixture]
    public class RosieUtilsTest
    {
        [Test]
        public void GetRosieLanguage_should_return_language_string_for_supported_language()
        {
            var language = RosieUtils.GetRosieLanguage(LanguageUtils.LanguageEnumeration.Python);

            Assert.That(language, Is.EqualTo("python"));
        }
        
        [Test]
        public void GetRosieLanguage_should_return_unknown_for_not_supported_language()
        {
            var language = RosieUtils.GetRosieLanguage(LanguageUtils.LanguageEnumeration.Apex);

            Assert.That(language, Is.EqualTo("unknown"));
        }
    }
}
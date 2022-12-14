using Extension.Rosie;
using Extension.SnippetFormats;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="RosieLanguageSupport"/>.
    /// </summary>
    [TestFixture]
    public class RosieLanguageSupportTest
    {
        [Test]
        public void IsLanguageSupported_should_return_true_for_supported_language()
        {
            var isLanguageSupported = RosieLanguageSupport.IsLanguageSupported(LanguageUtils.LanguageEnumeration.Python);

            Assert.That(isLanguageSupported, Is.True);
        }
        
        [Test]
        public void IsLanguageSupported_should_return_false_for_unsupported_language()
        {
            var isLanguageSupported = RosieLanguageSupport.IsLanguageSupported(LanguageUtils.LanguageEnumeration.Apex);

            Assert.That(isLanguageSupported, Is.False);
        }
        
        [Test]
        public void GetRosieLanguage_should_return_language_string_for_supported_language()
        {
            var language = RosieLanguageSupport.GetRosieLanguage(LanguageUtils.LanguageEnumeration.Python);

            Assert.That(language, Is.EqualTo("python"));
        }

        [Test]
        public void GetRosieLanguage_should_return_unknown_for_not_supported_language()
        {
            var language = RosieLanguageSupport.GetRosieLanguage(LanguageUtils.LanguageEnumeration.Apex);

            Assert.That(language, Is.EqualTo("unknown"));
        }
    }
}
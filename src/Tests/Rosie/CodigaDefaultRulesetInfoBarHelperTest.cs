using System.IO;
using Extension.Rosie;
using Extension.SnippetFormats;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="CodigaDefaultRulesetInfoBarHelper"/>.
    /// </summary>
    [TestFixture]
    public class CodigaDefaultRulesetInfoBarHelperTest
    {
        private string _solutionDir;

        [SetUp]
        public void Setup()
        {
            _solutionDir = $"{Path.GetTempPath()}solDir";
            Directory.CreateDirectory(_solutionDir);
        }

        [Test]
        public void FindSupportedFile_should_find_not_find_supported_file_in_root_only()
        {
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Unknown));
        }

        [Test]
        public void FindSupportedFile_should_find_not_find_supported_file_in_subdirectory()
        {
            Directory.CreateDirectory($"{_solutionDir}\\subDir");
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{_solutionDir}\\subDir\\text_file.txt", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Unknown));
        }

        [Test]
        public void FindSupportedFile_should_find_python_file_in_root()
        {
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{_solutionDir}\\python_file.py", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Python));
        }

        [Test]
        public void FindSupportedFile_should_find_typescript_file_in_root()
        {
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{_solutionDir}\\ts_file.ts", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Typescript));
        }

        [Test]
        public void FindSupportedFile_should_find_python_file_in_subdirectory()
        {
            var subDir = $"{Path.GetTempPath()}solDir\\subDir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\python_file.py", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Python));
        }

        [Test]
        public void FindSupportedFile_should_find_js_file_in_subdirectory()
        {
            var subDir = $"{Path.GetTempPath()}solDir\\subDir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\js_file.js", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Javascript));
        }

        [Test]
        public void FindSupportedFile_should_find_jsx_file_in_subdirectory()
        {
            var subDir = $"{Path.GetTempPath()}solDir\\subDir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\jsx_file.jsx", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Javascript));
        }

        [Test]
        public void FindSupportedFile_should_find_ts_file_in_subdirectory()
        {
            var subDir = $"{Path.GetTempPath()}solDir\\subDir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\ts_file.ts", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Typescript));
        }

        [Test]
        public void FindSupportedFile_should_find_tsx_file_in_subdirectory()
        {
            var subDir = $"{Path.GetTempPath()}solDir\\subDir";
            Directory.CreateDirectory(subDir);
            File.WriteAllText($"{_solutionDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\text_file.txt", "");
            File.WriteAllText($"{subDir}\\tsx_file.tsx", "");

            var language = CodigaDefaultRulesetsInfoBarHelper.FindSupportedFile(_solutionDir);

            Assert.That(language, Is.EqualTo(LanguageUtils.LanguageEnumeration.Typescript));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_solutionDir))
                Directory.Delete(_solutionDir, true);
        }
    }
}
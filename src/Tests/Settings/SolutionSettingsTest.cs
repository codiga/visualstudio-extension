using System.IO;
using Extension.Settings;
using Microsoft.VisualStudio.Shell;
using NUnit.Framework;
using static Tests.ServiceProviderMockSupport;

namespace Tests.Settings
{
    /// <summary>
    /// Unit test for <see cref="SolutionSettings"/>.
    /// </summary>
    [TestFixture]
    public class SolutionSettingsTest
    {
        private string _solutionSettingsFile;
        private string _dotVsDirectory;

        #region IsShouldNotifyUserToCreateCodigaConfig

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_no_solution_root()
        {
            var serviceProvider = MockServiceProvider("/");

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_no_dot_vs_in_solution_root()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_no_solution_settings_file()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            _dotVsDirectory = $"{Path.GetTempPath()}.vs";

            Directory.CreateDirectory(_dotVsDirectory);

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_true_setting()
        {
            var serviceProvider = SetupSolutionSettingsFile("{\n    \"ShouldNotifyUserToCreateCodigaConfig\": true\n}");

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_non_existent_property()
        {
            var serviceProvider = SetupSolutionSettingsFile("{\n    \"CreateCodigaConfig\": true\n}");

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_true_for_invalid_settings()
        {
            var serviceProvider = SetupSolutionSettingsFile("{\n    \"CreateCodigaConfig\"");

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.True);
        }

        [Test]
        public void IsShouldNotifyUserToCreateCodigaConfig_should_return_false_for_false_setting()
        {
            var serviceProvider =
                SetupSolutionSettingsFile("{\n    \"ShouldNotifyUserToCreateCodigaConfig\": false\n}");

            var isShouldNotify = SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider);

            Assert.That(isShouldNotify, Is.False);
        }

        #endregion

        #region SaveNeverNotifyUserToCreateCodigaConfigFile

        [Test]
        public void SaveNeverNotifyUserToCreateCodigaConfigFile_should_create_the_solution_settings_file_and_dot_vs()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            SetupPaths();

            Assert.That(Directory.Exists(_dotVsDirectory), Is.False);

            SolutionSettings.SaveNeverNotifyUserToCreateCodigaConfigFile(serviceProvider);

            Assert.That(File.Exists(_solutionSettingsFile), Is.True);
            Assert.That(File.ReadAllText(_solutionSettingsFile),
                Is.EqualTo("{\"ShouldNotifyUserToCreateCodigaConfig\":false}"));
        }

        [Test]
        public void SaveNeverNotifyUserToCreateCodigaConfigFile_should_create_the_solution_settings_file()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            SetupPaths();
            Directory.CreateDirectory(_dotVsDirectory);

            Assert.That(File.Exists(_solutionSettingsFile), Is.False);

            SolutionSettings.SaveNeverNotifyUserToCreateCodigaConfigFile(serviceProvider);

            Assert.That(File.Exists(_solutionSettingsFile), Is.True);
            Assert.That(File.ReadAllText(_solutionSettingsFile),
                Is.EqualTo("{\"ShouldNotifyUserToCreateCodigaConfig\":false}"));
        }

        [Test]
        public void SaveNeverNotifyUserToCreateCodigaConfigFile_should_update_the_solution_settings_file()
        {
            var serviceProvider = SetupSolutionSettingsFile("{\n    \"ShouldNotifyUserToCreateCodigaConfig\": true\n}");

            SolutionSettings.SaveNeverNotifyUserToCreateCodigaConfigFile(serviceProvider);

            Assert.That(File.ReadAllText(_solutionSettingsFile),
                Is.EqualTo("{\"ShouldNotifyUserToCreateCodigaConfig\":false}"));
        }

        #endregion

        private SVsServiceProvider SetupSolutionSettingsFile(string settingsFileContent)
        {
            SetupPaths();
            Directory.CreateDirectory(_dotVsDirectory);
            File.WriteAllText(_solutionSettingsFile, settingsFileContent);
            return MockServiceProvider(Path.GetTempPath());
        }

        private void SetupPaths()
        {
            _dotVsDirectory = $"{Path.GetTempPath()}.vs";
            _solutionSettingsFile = $"{Path.GetTempPath()}.vs\\CodigaSolutionSettings.json";
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_solutionSettingsFile))
                File.Delete(_solutionSettingsFile);
            if (Directory.Exists(_dotVsDirectory))
                Directory.Delete(_dotVsDirectory);
        }
    }
}
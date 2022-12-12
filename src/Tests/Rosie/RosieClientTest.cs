using System.IO;
using System.Text;
using System.Threading.Tasks;
using Extension.Rosie;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="RosieClient"/>.
    /// </summary>
    [TestFixture]
    public class RosieClientTest
    {
        private string? _testFile;

        [Test]
        public async Task GetAnnotations_should_return_no_annotation_for_no_filename()
        {
            var rosieClient = new RosieClient(_ => null, _ => "");

            var annotations = await rosieClient.GetAnnotations(new Mock<ITextBuffer>().Object);

            Assert.That(annotations, Is.Empty);
        }

        [Test]
        public async Task GetAnnotations_should_return_no_annotation_for_non_existent_file()
        {
            var rosieClient = new RosieClient(_ => "", _ => "");

            var annotations = await rosieClient.GetAnnotations(new Mock<ITextBuffer>().Object);

            Assert.That(annotations, Is.Empty);
        }

        [Test]
        public async Task GetAnnotations_should_return_no_annotation_for_not_supported_file_language()
        {
            CreateTestFile("not_supported_file_type.xaml");
            var rosieClient = new RosieClient(_ => _testFile, _ => "");

            var annotations = await rosieClient.GetAnnotations(new Mock<ITextBuffer>().Object);

            Assert.That(annotations, Is.Empty);
        }

        [Test]
        public async Task GetAnnotations_should_return_no_annotation_for_no_rule_returned_for_the_current_language()
        {
            CreateTestFile("python_file.py");
            var rosieClient = new RosieClient(_ => _testFile, _ => "");
            var textBuffer = new Mock<ITextBuffer>();

            var annotations = await rosieClient.GetAnnotations(textBuffer.Object);

            Assert.That(annotations, Is.Empty);
        }

        /// <summary>
        /// Creates a file with the given file name in the current system's temp directory.
        /// <br/>
        /// This file acts as the currently edited document for which code analysis is performed. 
        /// </summary>
        private void CreateTestFile(string fileName)
        {
            _testFile = $"{Path.GetTempPath()}{fileName}";
            var info = Encoding.UTF8.GetBytes("");
            using var fs = File.Create(_testFile);
            fs.Write(info, 0, info.Length);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testFile != null)
                File.Delete(_testFile);
        }
    }
}
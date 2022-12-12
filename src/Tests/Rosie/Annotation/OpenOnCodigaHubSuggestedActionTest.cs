using System.Collections.Generic;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using NUnit.Framework;

namespace Tests.Rosie.Annotation
{
    /// <summary>
    /// Unit test for <see cref="OpenOnCodigaHubSuggestedAction"/>.
    /// </summary>
    [TestFixture]
    public class OpenOnCodigaHubSuggestedActionTest
    {
        [Test]
        public void GetUrlString_should_return_formatter_rule_page_url()
        {
            var rosieViolation = new RosieViolation
            {
                Message = "open_browser_fix",
                Start = new RosiePosition { Line = 1, Col = 5 },
                End = new RosiePosition { Line = 1, Col = 10 },
                Severity = "INFORMATIONAL",
                Category = "CODE_STYLE",
                Fixes = new List<RosieViolationFix>()
            };

            var rosieAnnotation = new RosieAnnotation("rule-for-open-browser", "custom-ruleset-name", rosieViolation);

            var urlString = new OpenOnCodigaHubSuggestedAction(rosieAnnotation).GetUrlString();

            Assert.That(urlString,
                Is.EqualTo("https://app.codiga.io/hub/ruleset/custom-ruleset-name/rule-for-open-browser"));
        }
    }
}
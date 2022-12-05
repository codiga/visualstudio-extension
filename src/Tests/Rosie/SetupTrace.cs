using System.Diagnostics;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// See https://docs.nunit.org/articles/vs-test-adapter/Trace-and-Debug.html.
    /// </summary>
    // [SetUpFixture]
    public class SetupTrace
    {
        // [OneTimeSetUp]
        public void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        // [OneTimeTearDown]
        public void EndTest()
        {
            Trace.Flush();
        }
    }
}
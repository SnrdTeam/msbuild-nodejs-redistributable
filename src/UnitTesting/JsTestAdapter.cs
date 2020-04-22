using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri("executor://JsTestExecutor/v1")]
    [ExtensionUri("executor://JsTestExecutor/v1")]
    public class JsTestAdapter : ITestDiscoverer, ITestExecutor
    {
        public void Cancel()
        {
        }

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, "Starting JsTestDiscoverer...");

            foreach (var source in sources)
            {
                discoverySink.SendTestCase(
                    new TestCase(
                        $"{source}.Suite.Case", 
                        new Uri("executor://JsTestExecutor/v1"), 
                        source));
            }

            logger.SendMessage(TestMessageLevel.Informational, "JsTestDiscoverer complete.");
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            foreach (var test in tests)
            {
                frameworkHandle.RecordStart(test);
                frameworkHandle.RecordEnd(test, TestOutcome.Passed);
            }
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {

        }
    }
}

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Adeptik.NodeJs.UnitTesting.TestAdapter.Utils;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter
{
    [FileExtension(DllExtension)]
    [FileExtension(ExeExtension)]
    [DefaultExecutorUri(ExecutorUri)]
    [ExtensionUri(ExecutorUri)]
    public class JasmineTestAdapter : ITestDiscoverer, ITestExecutor
    {
        /// <summary>
        /// Extensions that handles by TestAdapter
        /// </summary>
        public const string DllExtension = ".dll";
        public const string ExeExtension = ".exe";

        /// <summary>
        /// Test file extensions 
        /// </summary>
        public const string JSTestExtension = ".test.js";
        public const string TSTestExtension = ".test.ts";

        /// <summary>
        /// Base uri used by test executor
        /// </summary>
        public const string ExecutorUri = "executor://JasmineTestExecutor/v1";

        private IEnumerable<string> FindTestFilesInProject(string projectDir)
        {
            return Directory.GetFiles(projectDir, $"*{JSTestExtension}").Union(
                Directory.GetFiles(projectDir, $"*{TSTestExtension}"));
        }

        /// <inheritdoc/>
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            var log = new LoggerHelper(logger, Stopwatch.StartNew());
            log.Log("Starting NodeJs test discovery...");

            foreach (var source in sources)
            {
                log.LogWithSource(source, "Discovering...");
                var tests = DiscoverTests(source);
                tests.ForEach(test =>
                {
                    discoverySink.SendTestCase(test);
                    log.LogWithSource(source, $"{test.DisplayName} found");
                });
                log.LogWithSource(source, "discovering complete.");
            }
            log.Log("NodeJs test discovery complete.");
        }

        private List<TestCase> DiscoverTests(string source)
        {
            var projectPath = Path.GetDirectoryName(source);

            return FindTestFilesInProject(projectPath)
                .Select(testFile =>
                    new TestCase(GetTestName(testFile), new Uri(ExecutorUri), source)
                    {
                        CodeFilePath = Path.Combine(projectPath, testFile)
                    }).ToList();

            static string GetTestName(string file) => Path.GetFileNameWithoutExtension(file);
        }

        /// <inheritdoc/>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var log = new LoggerHelper(frameworkHandle, Stopwatch.StartNew());
            log.Log("Start test run for sources...");

            foreach (var source in sources)
            {
                var tests = DiscoverTests(source);
                RunTestsWithJasmine(tests, runContext, frameworkHandle, log);
            }

            log.Log("Test run complete.");
        }

        /// <inheritdoc/>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var log = new LoggerHelper(frameworkHandle, Stopwatch.StartNew());
            log.Log("Start test run for tests...");
            RunTestsWithJasmine(tests, runContext, frameworkHandle, log);

            log.Log("Test run complete.");
        }

        private void RunTestsWithJasmine(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle, LoggerHelper log)
        {
            var jasmineConfig = new
            {
                spec_files = tests.Select(x => x.CodeFilePath).ToArray(),
                stopSpecOnExpectationFailure = false,
                random = false
            };

            File.WriteAllText ($"{runContext.TestRunDirectory}/jasmine.json", JsonSerializer.Serialize(jasmineConfig));
        }

        /// <inheritdoc/>
        public void Cancel()
        {
        }
    }
}

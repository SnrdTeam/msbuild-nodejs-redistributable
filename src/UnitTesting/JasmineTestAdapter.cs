using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Adeptik.NodeJs.UnitTesting.TestAdapter.Utils;
using System.Text;

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

        /// <summary>
        /// Test run is canceled?
        /// </summary>
        private bool _canceled = false;

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
            var jasmineResults = GetTestResultsFromJasmine(source);
            List<TestCase> testCasesInProject = new List<TestCase>();
            foreach (var jasmineResult in jasmineResults)
            {
                testCasesInProject.Add(new TestCase
                {
                    DisplayName = jasmineResult.Item1
                });
            }
            return testCasesInProject;
        }

        private static string GetPathToCmdJasmine(string pathToBuildResult)
            => Path.Combine(pathToBuildResult, "jasmine.cmd");

        /// <summary>
        /// Get test result
        /// </summary>
        /// <param name="source">Output project directory</param>
        /// <returns>List of unit test result. T1 is name of UnitTest, T2 is UnitTest's status</returns>
        private List<Tuple<string, string>> GetTestResultsFromJasmine(string source)
        {
            var jasmineUnitTesting = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetPathToCmdJasmine(Directory.GetParent(source).FullName),
                    Arguments = "/c",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            jasmineUnitTesting.Start();
            jasmineUnitTesting.WaitForExit();
            var rawResultFromJasmine = jasmineUnitTesting.StandardOutput.ReadToEnd();

            var clearOutputLines = ClearAndSplitOutput(rawResultFromJasmine).ToArray(); 

            var result = new List<Tuple<string, string>>();
            //The file format includes pairs of lines representing specs
            for (int i = 0; i < clearOutputLines.Length; i += 2)
            {
                //(i): spec name, (i + 1): status
                result.Add(new Tuple<string, string>(clearOutputLines[i], clearOutputLines[i + 1]));
            }
            return result;
        }

        /// <summary>
        /// Remove all jasmine debug info and split our output into separate lines
        /// </summary>
        /// <param name="output">Program output</param>
        /// <returns>Separate lines of program operation without debug output</returns>
        private IEnumerable<string> ClearAndSplitOutput(string output)
        {
            const string startReporting = "Started\n";
            const string endReporting = "\n\n\n";
            var leftCut = output.Substring(output.IndexOf(startReporting) + startReporting.Length);
            var rightCut = leftCut.Substring(0, leftCut.IndexOf(endReporting));
            return rightCut.Split('\n');
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
            
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            _canceled = true;
        }
    }
}

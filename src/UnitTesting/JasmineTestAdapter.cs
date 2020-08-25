using Adeptik.NodeJs.UnitTesting.TestAdapter.Utils;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
        /// Default path to posix shell
        /// </summary>
        private const string DefaultPathToShell = "/bin/sh";

        /// <summary>
        /// File containing commands for the operating system shell
        /// </summary>
        private const string ShellFile = "jasmine.cmd";

        /// <summary>
        /// Test run is canceled?
        /// </summary>
        private bool _canceled;

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
            var completedTestCases = GetTestCasesFromSource(source).ToList();
            return completedTestCases;
        }

        /// <summary>
        /// Return completed test cases from source
        /// </summary>
        /// <param name="source">Output project directory</param>
        /// <returns>List of test cases</returns>
        private IEnumerable<TestCase> GetTestCasesFromSource(string source)
        {
            var jasmineResults = GetTestResultsFromJasmine(source);
            var testCases = new List<TestCase>();
            foreach (var jasmineResult in jasmineResults)
            {
                var testCase = new TestCase(jasmineResult.Item1, new Uri(ExecutorUri), source);
                testCase.SetPropertyValue(TestResultProperties.Outcome,
                    jasmineResult.Item2 == "passed" ? TestOutcome.Passed : TestOutcome.Failed);
                testCases.Add(testCase);
            }

            return testCases;
        }

        /// <summary>
        /// Get path to jasmine executing script
        /// </summary>
        /// <param name="pathToBuildResult"></param>
        /// <returns>Path to jasmine executing script</returns>
        private static string GetPathToCmdJasmine(string pathToBuildResult)
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.Combine(pathToBuildResult, ShellFile)
                : Path.Combine(pathToBuildResult, ShellFile);


        /// <summary>
        /// Function get exec file name and arguments for process
        /// </summary>
        /// <param name="shellFile">File contains shell script</param>
        /// <returns>T1 - executing file, T2 - arguments for command line</returns>
        private static Tuple<string, string> GetExecutingFileNameAndArguments(string shellFile)
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Tuple<string, string>(shellFile, String.Empty)
                : new Tuple<string, string>(DefaultPathToShell, shellFile);

        /// <summary>
        /// Get test result
        /// </summary>
        /// <param name="source">Output project directory</param>
        /// <returns>Collection of unit test result. T1 is name of UnitTest, T2 is UnitTest's status</returns>
        private IEnumerable<Tuple<string, string>> GetTestResultsFromJasmine(string source)
        {
            var shellFile = GetPathToCmdJasmine(Directory.GetParent(source).FullName);
            var executeFileAndArgs = GetExecutingFileNameAndArguments(shellFile);
            var jasmineUnitTesting = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executeFileAndArgs.Item1,
                    Arguments = executeFileAndArgs.Item2,
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
            for (var i = 0; i < clearOutputLines.Length; i += 2)
            {
                //(i): spec name, (i + 1): status
                result.Add(new Tuple<string, string>(RemoveFirstSimbol(clearOutputLines[i]), clearOutputLines[i + 1]));
            }
            return result;
        }

        /// <summary>
        /// Remove autogenerate by jasmine spec simbol
        /// </summary>
        /// <param name="specInputName">Spec name</param>
        /// <returns>Spec name without first simbol</returns>
        private static string RemoveFirstSimbol(string specInputName)
            => specInputName.Remove(0, 1);

        /// <summary>
        /// Remove all jasmine debug info and split our output into separate lines
        /// </summary>
        /// <param name="output">Program output</param>
        /// <returns>Separate lines of program operation without debug output</returns>
        private static IEnumerable<string> ClearAndSplitOutput(string output)
        {
            const string startReporting = "Started\n";
            const string endReporting = "\n\n";
            var leftIndex = output.IndexOf(startReporting, StringComparison.Ordinal);
            if (leftIndex == -1)
            {
                throw new FormatException("Invalid jasmine output format (Beginning Output)");
            }
            var leftCut = output.Substring(leftIndex + startReporting.Length);
            var rigthIndex = leftCut.IndexOf(endReporting, StringComparison.Ordinal);
            if (rigthIndex == -1)
            {
                throw new FormatException("Invalid jasmine output format (Ending Output)");
            }
            var rightCut = leftCut.Substring(0, rigthIndex);
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
            var uniqueSources = tests.Select(test => test.Source).Distinct();
            List<TestCase> updatedTestCases = new List<TestCase>();
            foreach (var source in uniqueSources)
            {
                updatedTestCases.AddRange(DiscoverTests(source));
            }
            RunTestsWithJasmine(updatedTestCases.Where(test => tests.Select(oldTest => oldTest.FullyQualifiedName).Contains(test.FullyQualifiedName)), runContext, frameworkHandle, log);
            log.Log("Test run complete.");
        }

        private void RunTestsWithJasmine(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle, LoggerHelper log)
        {
            _canceled = false;
            foreach (var test in tests)
            {
                if (_canceled)
                {
                    break;
                }

                var testResult = new TestResult(test)
                {
                    Outcome = (TestOutcome)test.GetPropertyValue(TestResultProperties.Outcome)
                };
                frameworkHandle.RecordResult(testResult);
            }
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            _canceled = true;
        }
    }
}

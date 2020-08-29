﻿using Adeptik.NodeJs.UnitTesting.TestAdapter.Utils;
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
        private const string DllExtension = ".dll";

        private const string ExeExtension = ".exe";

        /// <summary>
        /// Base uri used by test executor
        /// </summary>
        private const string ExecutorUri = "executor://JasmineTestExecutor/v1";

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
            var completedTestCases = GetTestCasesFromSource().ToList();
            return completedTestCases;
            
            IEnumerable<TestCase> GetTestCasesFromSource()
            {
                var jasmineResults = GetTestResultsFromJasmine();
                var testCases = new List<TestCase>();
                foreach (var (name, status) in jasmineResults)
                {
                    var testCase = new TestCase(name, new Uri(ExecutorUri), source);
                    testCase.SetPropertyValue(TestResultProperties.Outcome,
                        status == "passed" ? TestOutcome.Passed : TestOutcome.Failed);
                    testCases.Add(testCase);
                }

                return testCases;
                
                //Get test result
                //Return collection of unit test result. T1 is name of UnitTest, T2 is UnitTest's status
                IEnumerable<(string, string)> GetTestResultsFromJasmine()
                {
                    var shellFile = Path.Combine(Directory.GetParent(source).FullName, ShellFile);
                    var (execFile, args) = GetExecutingFileNameAndArguments();
                    var jasmineUnitTesting = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = execFile,
                            Arguments = args,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    jasmineUnitTesting.Start();
                    jasmineUnitTesting.WaitForExit();
                    var rawResultFromJasmine = jasmineUnitTesting.StandardOutput.ReadToEnd();
                    var clearOutputLines = ClearAndSplitOutput().ToArray();
                    var result = new List<(string, string)>();
                    //The file format includes pairs of lines representing specs
                    for (var i = 0; i < clearOutputLines.Length; i += 2)
                    {
                        //(i): spec name, (i + 1): status
                        result.Add((clearOutputLines[i], clearOutputLines[i + 1]));
                    }

                    return result;

                    (string, string) GetExecutingFileNameAndArguments()
                        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? (shellFile, String.Empty)
                            : (DefaultPathToShell, shellFile);
                    
                    //Remove jasmine error output and split our output into separate lines
                    IEnumerable<string> ClearAndSplitOutput()
                    {
                        const string startReporting = "Started\n\n";
                        const string endReporting = "\n\nEnded";
                        var leftIndex = rawResultFromJasmine.IndexOf(startReporting, StringComparison.Ordinal);
                        var rightIndex = rawResultFromJasmine.LastIndexOf(endReporting, StringComparison.Ordinal);
                        if (leftIndex == -1 || rightIndex == -1)
                        {
                            throw new FormatException("Invalid jasmine output format");
                        }

                        var clearOutput = rawResultFromJasmine
                            .Substring(leftIndex + startReporting.Length, rightIndex - (leftIndex + startReporting.Length))
                            .Split('\n');
                        return clearOutput;
                    }
                }
            }
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
            var testMaterializeArray = tests.ToArray();
            var uniqueSources = testMaterializeArray.Select(test => test.Source).Distinct();
            List<TestCase> updatedTestCases = new List<TestCase>();
            foreach (var source in uniqueSources)
            {
                updatedTestCases.AddRange(DiscoverTests(source));
            }
            RunTestsWithJasmine(updatedTestCases.Where(test => testMaterializeArray.Select(oldTest => oldTest.FullyQualifiedName).Contains(test.FullyQualifiedName)), runContext, frameworkHandle, log);
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

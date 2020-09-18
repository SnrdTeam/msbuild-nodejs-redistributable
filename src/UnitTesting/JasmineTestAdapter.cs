using Adeptik.NodeJs.UnitTesting.TestAdapter.Utils;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Adeptik.NodeJs.UnitTesting.TestAdapter.Data;

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
        /// Pipe for IPC communication with jasmine process
        /// </summary>
        private const string JasminePipeName = "ReporterJasminePipe";

        /// <summary>
        /// Relative path to locall install jasmine framework from source
        /// </summary>
        private static readonly string RelativeDefaultPathToJasmine = "node_modules/jasmine/bin/jasmine.js";
        
        /// <summary>
        /// End of text character
        /// </summary>
        private const char EndOfText = (char)3;
        
        /// <summary>
        /// Base uri used by test executor
        /// </summary>
        private const string ExecutorUri = "executor://JasmineTestExecutor/v1";

        /// <summary>
        /// Output status message when spec is passed
        /// </summary>
        private const string TestCompleteMessage = "passed";

        /// <summary>
        /// File containing path to node, path to script for executing jasmine with custom reporter and path to config file
        /// </summary>
        private const string ExecutionConfig = ".jasmineLaunchSettings.json";

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
            var jsonTextFromConfigFile = File.ReadAllText(Path.Combine(Directory.GetParent(source).FullName, ExecutionConfig));
            ExecuteConfig config;
            try
            {
                config = JsonSerializer.Deserialize<ExecuteConfig>(jsonTextFromConfigFile);
            }
            catch (JsonException e)
            {
                throw new FormatException($"{ExecutionConfig} file is not in the correct format", e);
            }
            
            if (config.WorkingDirectory == null)
            {
                throw new NullReferenceException("Path to the working directory equal null");
            }
            var jasmineExecutePath = Path.Combine(config.WorkingDirectory, RelativeDefaultPathToJasmine);
            if (!File.Exists(jasmineExecutePath))
            {
                throw new Exception($"Jasmine executable not found [{jasmineExecutePath}]");
            }
            var completedTestCases = GetTestCasesFromSource().ToList();
            return completedTestCases;

            IEnumerable<TestCase> GetTestCasesFromSource()
            {
                var specs = GetTestResultsFromJasmine();
                var testCases = new List<TestCase>();
                foreach (var spec in specs)
                {
                    var testCase = new TestCase(spec.FullName, new Uri(ExecutorUri), source);
                    testCase.SetPropertyValue(TestResultProperties.Outcome,
                        spec.Status == TestCompleteMessage ? TestOutcome.Passed : TestOutcome.Failed);
                    testCase.SetPropertyValue(TestResultProperties.Duration, TimeSpan.FromMilliseconds(spec.Duration ?? 0));
                    testCases.Add(testCase);
                }

                return testCases;

                //Get test result
                //Return collection of unit test result. T1 is name of UnitTest, T2 is UnitTest's status
                IEnumerable<SpecResult> GetTestResultsFromJasmine()
                {
                    var (execFile, args) = GetExecutingFileNameAndArguments();
                    var uniqueIdentificatorForPipe = Guid.NewGuid().ToString();
                    var jasmineUnitTesting = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = execFile,
                            Arguments = $"{args} {uniqueIdentificatorForPipe}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = config.WorkingDirectory
                        }
                    };
                    var pipeTask = Task.Run(() =>
                    {
                        using var readerPipe = new NamedPipeServerStream($"{JasminePipeName}{uniqueIdentificatorForPipe}");
                        var specsFormJasmineOutput = new List<SpecResult>();
                        var streamReader = new StreamReader(readerPipe, Encoding.UTF8);
                        readerPipe.WaitForConnection();
                        while (true)
                        {
                            var lineFromPipe = streamReader.ReadLine();
                            if (lineFromPipe == EndOfText.ToString())
                            {
                                break;
                            }
                            try
                            {
                                specsFormJasmineOutput.Add(JsonSerializer.Deserialize<SpecResult>(lineFromPipe));
                            }
                            catch (JsonException e)
                            {
                                throw new FormatException($"{lineFromPipe} is not a valid string", e);
                            }
                        }
                        return specsFormJasmineOutput;
                    });
                    jasmineUnitTesting.Start();
                    var specs = pipeTask.Result;
                    jasmineUnitTesting.WaitForExit();

                    return specs;

                    (string? NodeExecuteFile, string) GetExecutingFileNameAndArguments()
                    {
                        return (config.NodeExecuteFile, $"{config.JasmineLauncher} {config.JasmineConfig}");
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
                    Outcome = (TestOutcome)test.GetPropertyValue(TestResultProperties.Outcome),
                    Duration = (TimeSpan)test.GetPropertyValue(TestResultProperties.Duration)
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

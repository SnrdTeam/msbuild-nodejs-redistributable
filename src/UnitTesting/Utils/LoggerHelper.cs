using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter.Utils
{
    class LoggerHelper
    {
        public IMessageLogger InnerLogger { get; }

        public Stopwatch Stopwatch { get; }

        public LoggerHelper(IMessageLogger logger, Stopwatch stopwatch)
        {
            InnerLogger = logger;
            Stopwatch = stopwatch;
        }
    
        private void SendMessage(TestMessageLevel level, string? filename, string message)
        {
            var fileNameText = string.IsNullOrEmpty(filename) ? string.Empty : $"{filename} ";
            InnerLogger.SendMessage(level, $"[NodeJS TestAdapter {Stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}] {fileNameText}{message}");
        }

        public void Log(string message) => SendMessage(TestMessageLevel.Informational, null, message);

        public void LogWithSource(string source, string message) => SendMessage(TestMessageLevel.Informational, source, message);

        public void LogError(string message) => SendMessage(TestMessageLevel.Error, null, message);

        public void LogErrorWithSource(string source, string message) => SendMessage(TestMessageLevel.Error, source, message);

        public void LogWarning(string message) => SendMessage(TestMessageLevel.Warning, null, message);

        public void LogWarningWithSource(string source, string message) => SendMessage(TestMessageLevel.Warning, source, message);
    }
}

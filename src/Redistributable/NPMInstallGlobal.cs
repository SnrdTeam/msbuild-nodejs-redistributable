using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{

    public class NPMInstallGlobal : Task
    {
        /// <summary>
        /// Required command to execute node
        /// </summary>
        [Required]
        public string? NodeExecutable { get; set; }
        
        /// <summary>
        /// Path to npm-cli.js
        /// </summary>
        [Required]
        public string? NPMScriptPath { get; set; }
        
        /// <summary>
        /// Required jasmine version
        /// </summary>
        [Required]
        public string? PackageVersion { get; set; }

        /// <summary>
        /// Require package
        /// </summary>
        [Required]
        public string? PackageName { get; set; }

        /// <summary>
        /// Install globally argument for npm
        /// </summary>
        private const string NPMCommand = "install -g";

        /// <summary>
        /// Waiting time for executable process in milliseconds
        /// </summary>
        private const int WaitingTime = 60000;

        public override bool Execute()
        {
            if (NodeExecutable == null || string.IsNullOrWhiteSpace(NodeExecutable))
                throw new ArgumentException("Path to node executable is not specified.");
            
            if(NPMScriptPath == null || string.IsNullOrWhiteSpace(NPMScriptPath))
                throw new ArgumentException("Path to npm-cli.js script is not specified.");
            
            if (PackageName == null || string.IsNullOrWhiteSpace(PackageName))
                throw new ArgumentException("Package name not specified.");

            var versionForInstallation = PackageVersion ?? "latest";
            var fullPackageName = $"{PackageName}@{versionForInstallation}";

            using var installPackageMutex = new Mutex(false, $@"Global\{PackageName}{versionForInstallation}");
            try
            {
                installPackageMutex.WaitOne();
                InstallPackage();
            }
            finally
            {
                installPackageMutex.ReleaseMutex();
            }
            return !Log.HasLoggedErrors;

            void InstallPackage()
            {
                Log.LogMessage($"Start installing {fullPackageName}...");

                var (executable, arguments) = GetExecutingFileNameAndArguments();

                using var NPMProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                NPMProcess.Start();

                if (!NPMProcess.WaitForExit(WaitingTime))
                {
                    Log.LogError("Installation timeout. Terminating...");
                    NPMProcess.Kill();
                    return;
                }

                Log.LogMessage($"Finish installing {fullPackageName}");


                (string executable, string arguments) GetExecutingFileNameAndArguments()
                    => (NodeExecutable, $"{NPMScriptPath} {NPMCommand} {fullPackageName}");
                

            }

        }
    }
}

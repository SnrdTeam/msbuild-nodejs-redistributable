using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{

    public class NPMInstallGlobal : Task
    {
        /// <summary>
        /// Required command to execute npm
        /// </summary>
        [Required]
        public string? NPMExecutable { get; set; }

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
        /// Waiting time for executable process
        /// </summary>
        private const int WaitingTime = 60000;

        public override bool Execute()
        {
            if (NPMExecutable == null || string.IsNullOrWhiteSpace(NPMExecutable))
                throw new ArgumentException("Path to npm executable is not specified.");

            if (PackageName == null || string.IsNullOrWhiteSpace(PackageName))
                throw new ArgumentException("Package name not specified.");

            var fullPackageName = $"{PackageName}@{PackageVersion ?? "latest"}";

            using var installPackageMutex = new Mutex(false, $@"Global\{PackageName}{PackageVersion}");
            installPackageMutex.WaitOne();
            InstallPackage();
            installPackageMutex.ReleaseMutex();
            
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
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        return (NPMExecutable, $"{NPMCommand} {fullPackageName}");
            
                    // In Linux & MacOS NPMExecutable contains path to node & path to npm.js as parameter <see cref="InstallNodeJS"/> 
                    var lastSpaceIdx = NPMExecutable.LastIndexOf(' ');
                    if (lastSpaceIdx == -1)
                        throw new ArgumentException("NPMExecutable value is invalid for current OS.");

                    var nodeExecutable = NPMExecutable.Substring(0, lastSpaceIdx);
                    var npmJSRelPath = NPMExecutable.Substring(lastSpaceIdx+1);

                    return (nodeExecutable, $"{npmJSRelPath} {NPMCommand} {fullPackageName}");
                }

            }

        }
    }
}

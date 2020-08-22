using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{
    public class NPMInstallGlobal : Task
    {
        /// <summary>
        /// Required command to execute npm
        /// </summary>
        [Required]
        public string? NPMExecutable { get; private set; }
        
        /// <summary>
        /// Required jasmine version
        /// </summary>
        [Required]
        public string? PackageVersion { get; private set; }

        /// <summary>
        /// Require package
        /// </summary>
        [Required]
        public string? PackageName { get; private set; }

        /// <summary>
        /// Install globally argument for npm
        /// </summary>
        private const string ArgumentForNpm = "install -g";
       
        /// <summary>
        /// Waiting time for executable process
        /// </summary>
        private const int WaitingTime = 60000;

        public override bool Execute()
        {
            if (PackageName == null)
            {
                throw new NullReferenceException("Package name not specified");
            }
            using var installPackageMutex = new Mutex(false, $@"Global\{PackageName}{PackageVersion}");
            installPackageMutex.WaitOne();
            InstallPackage(PackageName, PackageVersion ?? "latest");
            installPackageMutex.ReleaseMutex();
            return !Log.HasLoggedErrors;
        }

        private void InstallPackage(string packageName, string version)
        {
            Log.LogMessage($"Start installing: {packageName} - Version: {version}");
            string executeFileName, arguments;
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executeFileName = NPMExecutable!.Split(' ')[0];
                arguments = $"{NPMExecutable.Split(' ')[1]} {CreateNPMArguments($"{packageName}@{version}")}";
            }
            else
	        {
                executeFileName = NPMExecutable!;
                arguments = CreateNPMArguments($"{packageName}@{version}");
	        }
            var NPMProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executeFileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            NPMProcess.Start();
            if(!NPMProcess.WaitForExit(WaitingTime))
            {
                Log.LogError("Installation TimeOut");
                NPMProcess.Kill();
            }
            Log.LogMessage($"Finish installing: {packageName} - Version: {version}");
        }

        private string CreateNPMArguments(string packageName)
            => $"{ArgumentForNpm} {packageName}";
    }
}

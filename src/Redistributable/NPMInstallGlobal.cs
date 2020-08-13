using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{
    public class NPMInstallGlobal : Task
    {
        /// <summary>
        /// Required command to execute nodejs
        /// </summary>
        [Required]
        public string? NodeExecutable { get; private set; }

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
        /// Mutex for creating critical section in installation packages time
        /// </summary>
        private static readonly Mutex InstallPackagesMutex = new Mutex(false, "MtxPackage");

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
            InstallPackagesMutex.WaitOne();
            InstallPackage(PackageName, PackageVersion ?? "latest");
            InstallPackagesMutex.ReleaseMutex();
            return !Log.HasLoggedErrors;
        }

        private void InstallPackage(string packageName, string version)
        {
            Log.LogMessage($"Start installing: {packageName} - Version: {version}");
            var NPMProcess = Process.Start(NPMExecutable, CreateNPMArguments($"{packageName}@{version}"));
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

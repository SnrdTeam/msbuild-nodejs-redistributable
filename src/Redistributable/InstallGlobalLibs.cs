using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{
    public class InstallGlobalLibs : Task
    {
        /// <summary>
        /// Required command to execute nodejs
        /// </summary>
        [Required]
        public string? NodeExecutable { get; private set; }
        
        /// <summary>
        /// Path to directory where installing libs
        /// </summary>
        [Required]
        public string? GlobalNodeModulesPath { get; private set; }

        /// <summary>
        /// Required command to execute npm
        /// </summary>
        [Required]
        public string? NPMExecutable { get; private set; }
        
        /// <summary>
        /// Required jasmine version
        /// </summary>
        [Required]
        public string? JasmineVersion { get; private set; }

        /// <summary>
        /// Required yarn version
        /// </summary>
        [Required]
        public string? YarnVersion { get; private set; }

        /// <summary>
        /// Return path to yarn executable file
        /// </summary>
        [Output]
        public string? YarnExecutable { get; private set; }

        /// <summary>
        /// Return path to jasmine executable file
        /// </summary>
        [Output]
        public string? JasmineExecutable { get; private set; }

        /// <summary>
        /// Mutex for creating critical section in installation libs time
        /// </summary>
        private static readonly Mutex Mutex = new Mutex(false, "MtxLib");

        /// <summary>
        /// Install globally argument for npm
        /// </summary>
        private const string ArgumentForNpm = "install -g";
        
        /// <summary>
        /// Path to installed yarn
        /// </summary>
        private const string LocallyPathToYarn = "/yarn/bin/yarn.js";
        
        /// <summary>
        /// Path to installed jasmine
        /// </summary>
        private const string LocallyPathToJasmine = "/jasmine/bin/jasmine.js";
       
        /// <summary>
        /// Waiting time for executable process
        /// </summary>
        private const int WaitingTime = 60000;

        public override bool Execute()
        {
            Mutex.WaitOne();
            InstallLib("yarn", YarnVersion ?? "latest");
            InstallLib("jasmine", JasmineVersion ?? "latest");
            Mutex.ReleaseMutex();
            YarnExecutable = GetExecutableCommand(LocallyPathToYarn);
            JasmineExecutable = GetExecutableCommand(LocallyPathToJasmine);
            return !Log.HasLoggedErrors;
        }

        private void InstallLib(string packageName, string version)
        {
            Log.LogMessage($"Start installing: {packageName} - Version: {version}");
            var NPMProcess = Process.Start(NPMExecutable, CreateNPMArguments($"{packageName}@{version}"));
            if(!NPMProcess.WaitForExit(WaitingTime))
            {
                Log.LogError("Installation TimeOut");
                NPMProcess.Kill();
            }
            Log.LogMessage($"Finish installing: {packageName} - Version {version}");
        }

        private string GetExecutableCommand(string locallyPath)
            => $"{NodeExecutable} {GlobalNodeModulesPath}{locallyPath}";

        private string CreateNPMArguments(string packageName)
            => $"{ArgumentForNpm} {packageName}";
    }
}

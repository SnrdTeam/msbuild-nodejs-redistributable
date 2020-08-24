using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
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
            if (NPMExecutable == null)
            {
                throw new NullReferenceException("Path to npm executable is not specified");
            }

            var executeFileAndArgs = GetExecutingFileNameAndArguments(NPMExecutable, packageName, version);
            var NPMProcess = new Process()
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
            NPMProcess.Start();
            if (!NPMProcess.WaitForExit(WaitingTime))
            {
                Log.LogError("Installation TimeOut");
                NPMProcess.Kill();
            }
            Log.LogMessage($"Finish installing: {packageName} - Version: {version}");
        }

        /// <summary>
        /// Function get execute command for install NPM's package (exec file and args)
        /// </summary>
        /// <param name="NPMExecutableCommand">Full exec command</param>
        /// <param name="packageName">Name of package in NPM</param>
        /// <param name="packageVersion">Version of package in NPM</param>
        /// <returns>T1 - executing file name, T2 - </returns>
        private static Tuple<string, string> GetExecutingFileNameAndArguments(string NPMExecutableCommand, string packageName, string packageVersion)
        {

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var executeAndArgsCollection = NPMExecutableCommand.Split(' ');
                if (executeAndArgsCollection.Length != 2)
                {
                    throw new FormatException("Path to the \".nuget\" directory contains spaces");
                }
                return new Tuple<string, string>(executeAndArgsCollection[0], $"{executeAndArgsCollection[1]} {CreateNPMArguments($"{packageName}@{packageVersion}")}");
            }
            return new Tuple<string, string>(NPMExecutableCommand, CreateNPMArguments($"{packageName}@{packageVersion}"));
        }

        private static string CreateNPMArguments(string packageName)
            => $"{ArgumentForNpm} {packageName}";
    }
}

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Adeptik.NodeJs.Redistributable
{
    /// <summary>
    /// Node installer task
    /// </summary>
    public class InstallNodeJs : Task
    {
        private static readonly Regex VersionRegex = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);
        private static readonly string OSVersion;

        private static readonly string OSArchitecture;

        static InstallNodeJs()
        {
            OSVersion = GetOSVersion();

            OSArchitecture = GetOSPlatform();

            static string GetOSVersion()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "win";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "darwin";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "linux";

                throw new NotSupportedException("OS is not supported.");
            }

            static string GetOSPlatform() => RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm => "armv7l",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                _ => throw new NotSupportedException("Architecture is not supported."),
            };
        }

        /// <summary>
        /// Required version of NodeJS
        /// </summary>
        [Required]
        public string? NodeJsVersion { get; set; }

        /// <summary>
        /// Path where to download & unpack nodejs
        /// </summary>
        [Required]
        public string? WorkingDirectoryPath { get; set; }

        /// <summary>
        /// Returns path to downloaded & unpacked node
        /// </summary>
        [Output]
        public string? NodeJsPath { get; private set; }

        /// <summary>
        /// Returns path to global node_modules directory
        /// </summary>
        [Output]
        public string? GlobalNodeModulesPath { get; private set; }

        /// <summary>
        /// Returns commandline to run node
        /// </summary>
        [Output]
        public string? NodeExecutable { get; private set; }

        /// <summary>
        /// Returns commandline to run npm
        /// </summary>
        [Output]
        public string? NPMExecutable { get; private set; }

        private const string NodeUrl = "https://nodejs.org/download/release";

        /// <summary>
        /// Ensure that NodeJS downloaded & install yarn
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            Log.LogMessage($"Ensuring NodeJS {NodeJsVersion} exists...");

            if (NodeJsVersion == null) throw new ArgumentNullException(nameof(NodeJsVersion));

            DirectoryInfo workingDirectory;
            string distribName;
            string distribFileName;
            string distribFilePath;
            string nodeDirectoryPath;
            string lockfileFilePath;
            Uri distribHashSumUrl;
            Uri distribUrl;
            using var nodeInstallMutex = new Mutex(false, $@"Global\{NodeJsVersion}");
            try
            {
                InitPathInfo();
                nodeInstallMutex.WaitOne();
                if (!NodeJSExist())
                {
                    if (!NodeJSDownloaded())
                        DownloadNodeJS();
                    UnpackNodeJS();
                }
                SetOutputProperties();
            }
            catch (Exception e)
            {
                var messages = new List<string> { "NodeJs installing failed." };
                for (var ex = e; ex != null; ex = ex.InnerException)
                    messages.Add(ex.Message);

                Log.LogError(string.Join(" -> ", messages));
            }
            finally
            {
                nodeInstallMutex.ReleaseMutex();
            }

            return !Log.HasLoggedErrors;


            void InitPathInfo()
            {
                workingDirectory = GetWorkingDirectory();
                distribName = GetDistribName();
                distribFileName = $"{distribName}.{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz")}";
                distribFilePath = Path.Combine(workingDirectory.FullName, distribFileName);
                nodeDirectoryPath = Path.Combine(workingDirectory.FullName, distribName);
                lockfileFilePath = Path.Combine(nodeDirectoryPath, $"{GetDistribName()}.lock");

                distribHashSumUrl = new Uri($"{NodeUrl}/v{NodeJsVersion}/SHASUMS256.txt");
                distribUrl = new Uri($"{NodeUrl}/v{NodeJsVersion}/{distribFileName}");

                DirectoryInfo GetWorkingDirectory()
                {
                    if (string.IsNullOrEmpty(WorkingDirectoryPath))
                        throw new Exception("NodePath property is invalid.");

                    return !Directory.Exists(WorkingDirectoryPath) ?
                        Directory.CreateDirectory(WorkingDirectoryPath) :
                        new DirectoryInfo(WorkingDirectoryPath);
                }

                string GetDistribName()
                {
                    if (string.IsNullOrEmpty(NodeJsVersion) && VersionRegex.IsMatch(NodeJsVersion))
                        throw new Exception("NodeJsVersion property value is invalid.");

                    return $"node-v{NodeJsVersion}-{OSVersion}-{OSArchitecture}";
                }
            }

            bool NodeJSExist()
            {
                if (File.Exists(lockfileFilePath))
                    return File.ReadAllText(lockfileFilePath) == NodeJsVersion;

                return false;
            }

            bool NodeJSDownloaded()
            {
                if (!File.Exists(distribFilePath))
                    return false;

                var fileHashSum = CalculateFileHashSum(distribFilePath).ToLower();
                Log.LogMessage($"Calcualted hash sum {fileHashSum}.");

                Log.LogMessage($"Downloading NodeJS hash sum file.");
                var wellknownHashSum = GetWellknownHashSum(distribFileName.ToLower(), distribHashSumUrl);
                Log.LogMessage($"Found hash sum {wellknownHashSum}.");

                return fileHashSum == wellknownHashSum;

                static string CalculateFileHashSum(string filePath)
                {
                    using var stream = File.OpenRead(filePath);
                    using var algorithm = SHA256.Create();
                    var hash = algorithm.ComputeHash(stream);

                    return ConvertByteArrayToHex(hash);

                    static string ConvertByteArrayToHex(byte[] bytes)
                    {
                        var sb = new StringBuilder();
                        foreach (var b in bytes)
                            sb.AppendFormat("{0:X2}", b);

                        return sb.ToString();
                    }
                }

                static string GetWellknownHashSum(string fileName, Uri hashsumFileUri)
                {
                    using var memoryStream = new MemoryStream();
                    DownloadFile(hashsumFileUri, memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using var reader = new StreamReader(memoryStream);
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine().ToLower();
                        if (line.EndsWith(fileName))
                            return line
                                .Substring(0, line.Length - fileName.Length)
                                .Trim();
                    }
                    throw new Exception($"No information found about {fileName} in hashsum file.");
                }
            }

            void DownloadNodeJS()
            {
                Log.LogMessage($"Downloading NodeJS from {distribUrl}");

                using (var distribFile = new FileStream(distribFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    DownloadFile(distribUrl, distribFile);
                }

                if (!NodeJSDownloaded())
                    throw new Exception("Downloaded NodeJS distrib is corrupted.");
            }

            void UnpackNodeJS()
            {
                ClearTargetDirectory();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Log.LogMessage($"Unzipping {distribFilePath}...");
                    ZipFile.ExtractToDirectory(distribFilePath, workingDirectory.FullName);
                }
                else
                {
                    Log.LogMessage($"Unpacking {distribFilePath} using tar...");
                    var process = new Process()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "tar",
                            Arguments = $" -xzf {distribFilePath}",
                            WorkingDirectory = workingDirectory.FullName,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }

                WriteLockFile(lockfileFilePath, NodeJsVersion);

                void ClearTargetDirectory()
                {
                    if (Directory.Exists(nodeDirectoryPath))
                    {
                        Log.LogMessage($"Clear target directory {nodeDirectoryPath}...");
                        Directory.Delete(nodeDirectoryPath, true);
                    }
                }

                static void WriteLockFile(string path, string version)
                {
                    using var writer = File.CreateText(path);
                    writer.Write(version);
                }
            }

            void SetOutputProperties()
            {
                NodeJsPath = nodeDirectoryPath;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    GlobalNodeModulesPath = Path.Combine(NodeJsPath, "node_modules");
                    NodeExecutable = Path.Combine(NodeJsPath, "node.exe");
                    NPMExecutable = Path.Combine(NodeJsPath, "npm.cmd");
                }
                else
                {
                    GlobalNodeModulesPath = Path.Combine(NodeJsPath, "lib", "node_modules");
                    NodeExecutable = Path.Combine(NodeJsPath, "bin", "node");
                    NPMExecutable = $"{NodeExecutable} {Path.Combine(NodeJsPath, "lib/node_modules/npm/bin/npm-cli.js")}";
                }
            }
        }

        private static void DownloadFile(Uri uri, Stream stream)
        {
            try
            {
                using var client = new HttpClient(new HttpClientHandler(), disposeHandler: true);
                using HttpResponseMessage response = client
                    .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead)
                    .GetAwaiter()
                    .GetResult();

                response.EnsureSuccessStatusCode();

                using Stream responseStream = response.Content
                    .ReadAsStreamAsync()
                    .GetAwaiter()
                    .GetResult();

                responseStream.CopyToAsync(stream, 1024).Wait();
            }
            catch (HttpRequestException e)
            {
                throw new Exception("Download failed. Url not resolved.", e);
            }
            catch (IOException e)
            {
                throw new Exception("Download failed. I/O exception.", e);
            }
        }
    }
}

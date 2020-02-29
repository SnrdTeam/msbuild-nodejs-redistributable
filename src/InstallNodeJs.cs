using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Adeptik.NodeJs.Redistributable
{
    /// <summary>
    /// Node installer task
    /// </summary>
    public class InstallNodeJs : Task
    {
        private readonly Regex VersionRegex = new Regex(@"^\d+\.\d+\.\d+$");

        /// <summary>
        /// Required version of NodeJS
        /// </summary>
        [Required]
        public string NodeJsVersion { get; set; }

        /// <summary>
        /// Path where to download & unpack nodejs
        /// </summary>
        [Required]
        public string WorkingDirectoryPath { get; set; }

        private DirectoryInfo GetWorkingDirectory()
        {
            if (string.IsNullOrEmpty(WorkingDirectoryPath))
                throw new Exception("NodePath property is invalid.");

            return !Directory.Exists(WorkingDirectoryPath) ?
                Directory.CreateDirectory(WorkingDirectoryPath) :
                new DirectoryInfo(WorkingDirectoryPath);
        }

        private string GetDistribName()
        {
            static string GetNodeOSVersion()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "win";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "darwin";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "linux";

                throw new NotSupportedException("OS is not supported.");
            }

            static string GetNodeArchitectureVersion() => RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm => "armv7l",
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                _ => throw new NotSupportedException("Architecture is not supported."),
            };

            if (string.IsNullOrEmpty(NodeJsVersion) && VersionRegex.IsMatch(NodeJsVersion))
                throw new Exception("NodeJsVersion property value is invalid.");

            return $"node-v{NodeJsVersion}-{GetNodeOSVersion()}-{GetNodeArchitectureVersion()}";
        }

        private string GetDistribFileName() =>
            $"{GetDistribName()}.{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz")}";

        /// <summary>
        /// Returns path to downloaded & unpacked node
        /// </summary>
        [Output]
        public string NodeJsPath =>
            $"{GetWorkingDirectory().FullName}/{GetDistribName()}/";

        /// <summary>
        /// Returns path to global node_modules directory
        /// </summary>
        [Output]
        public string GlobalNodeModulesPath =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            $"{NodeJsPath}node_modules/" :
            $"{NodeJsPath}lib/node_modules/";

        /// <summary>
        /// Returns commandline to run node
        /// </summary>
        [Output]
        public string NodeExecutable =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            $"{NodeJsPath}node.exe" :
            $"{NodeJsPath}bin/node";

        /// <summary>
        /// Returns commandline to run npm
        /// </summary>
        [Output]
        public string NPMExecutable =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            $"{NodeJsPath}npm.cmd" :
            $"{NodeJsPath}lib/node_modules/npm/bin/npm";

        private string GetDistribArchiveFilePath() => $"{GetWorkingDirectory().FullName}/{GetDistribFileName()}";

        private string GetNodeJsLockFilePath() => $"{GetWorkingDirectory().FullName}/{GetDistribName()}/{GetDistribName()}.lock";

        private const string NodeUrl = "https://nodejs.org/download/release";

        private Uri GetDistribUrl() => new Uri($"{NodeUrl}/v{NodeJsVersion}/{GetDistribFileName()}");

        private Uri GetDistribHashSumUrl() => new Uri($"{NodeUrl}/v{NodeJsVersion}/SHASUMS256.txt");

        /// <summary>
        /// Ensure that NodeJS downloaded & install yarn
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            Log.LogMessage($"Ensuring NodeJS {NodeJsVersion} exists...");

            try
            {
                if (!NodeJSExist())
                {
                    if (!NodeJSDownloaded())
                        DownloadNodeJS();

                    UnpackNodeJS();
                }
            }
            catch (Exception e)
            {
                Log.LogError($"NodeJs installing failed. {e.Message}. {e.StackTrace}");
            }

            return !Log.HasLoggedErrors;
        }
        private bool NodeJSExist()
        {
            if (File.Exists(GetNodeJsLockFilePath()))
                return File.ReadAllText(GetNodeJsLockFilePath()) == NodeJsVersion;

            return false;
        }

        private bool NodeJSDownloaded()
        {
            static string CalculateFileHashSum(string filePath)
            {
                static string ConvertByteArrayToHex(byte[] bytes)
                {
                    var sb = new StringBuilder();
                    foreach (var b in bytes)
                        sb.AppendFormat("{0:X2}", b);

                    return sb.ToString();
                }

                using var stream = File.OpenRead(filePath);
                using var algorithm = SHA256.Create();
                var hash = algorithm.ComputeHash(stream);

                return ConvertByteArrayToHex(hash);
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
                            .Remove(line.IndexOf(fileName))
                            .Trim();
                }
                throw new Exception($"No information found about {fileName} in hashsum file.");
            }

            if (!File.Exists(GetDistribArchiveFilePath()))
                return false;

            var fileHashSum = CalculateFileHashSum(GetDistribArchiveFilePath()).ToLower();
            Log.LogMessage($"Calcualted hash sum {fileHashSum}.");

            Log.LogMessage($"Downloading NodeJS hash sum file.");
            var wellknownHashSum = GetWellknownHashSum(GetDistribFileName().ToLower(), GetDistribHashSumUrl());
            Log.LogMessage($"Found hash sum {wellknownHashSum}.");

            return fileHashSum == wellknownHashSum;
        }

        private void DownloadNodeJS()
        {
            Log.LogMessage($"Downloading NodeJS from {GetDistribUrl()}");

            using (var distribFile = new FileStream(GetDistribArchiveFilePath(), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                DownloadFile(GetDistribUrl(), distribFile);
            }

            if (!NodeJSDownloaded())
                throw new Exception("Downloaded NodeJS distrib is corrupted.");
        }

        private void UnpackNodeJS()
        {
            static void WriteLockFile(string path, string version)
            {
                using var writer = File.CreateText(path);
                writer.Write(version);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.LogMessage($"Unzipping {GetDistribArchiveFilePath()}...");
                ZipFile.ExtractToDirectory(GetDistribArchiveFilePath(), $"{WorkingDirectoryPath}");
            }
            else
            {
                Log.LogMessage($"Unpacking {GetDistribArchiveFilePath()} using tar...");
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $" -xzf {GetDistribArchiveFilePath()}",
                        WorkingDirectory = WorkingDirectoryPath,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit();
            }

            WriteLockFile(GetNodeJsLockFilePath(), NodeJsVersion);
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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Adeptik.NodeJs.Redistributable
{
    /// <summary>
    /// Node installer task
    /// </summary>
    public class InstallNodeJs : Task
    {
        /// <summary>
        /// Required version of NodeJS
        /// </summary>
        [Required]
        public string NodeJsVersion { get; set; }

        /// <summary>
        /// Returns path to downloaded node
        /// </summary>
        [Output]
        public string NodeJsPath => $"{NodePath}/{GetDistribName()}/";

        private string GetNodeOSVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "darwin";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";

            throw new NotSupportedException("OS is not supported.");
        }

        private string GetNodeArchitectureVersion() => RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm => "armv7l",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            _ => throw new NotSupportedException("Architecture is not supported."),
        };

        private string NodePath
        {
            get
            {
                var assemblyFileInfo = new FileInfo(typeof(InstallNodeJs).GetTypeInfo().Assembly.Location);
                return Path.Combine(assemblyFileInfo.Directory.Parent.Parent.FullName, "node");
            }
        }

        private string NodeUrl => "https://nodejs.org/download/release";

        private string GetArchiveExtension() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz";

        private string GetDistribName() => $"node-v{NodeJsVersion}-{GetNodeOSVersion()}-{GetNodeArchitectureVersion()}";

        private string GetDistribFileName() => $"{GetDistribName()}.{GetArchiveExtension()}";

        private string GetDistribFilePath() => $"{NodePath}/{GetDistribFileName()}";

        private Uri GetDistribUrl() => new Uri($"{NodeUrl}/v{NodeJsVersion}/{GetDistribName()}.{GetArchiveExtension()}");

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
                if (!Directory.Exists(NodePath))
                    Directory.CreateDirectory(NodePath);

                if (!NodeJSExist())
                {
                    if (!NodeJSDownloaded())
                        DownloadNodeJS();

                    UnpackNodeJS();
                }
            }
            catch (Exception e)
            {
                Log.LogError($"NodeJs installing failed. {e.Message}");
            }

            return !Log.HasLoggedErrors;
        }

        private void UnpackNodeJS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.LogMessage($"Unzipping {GetDistribFilePath()}...");
                ZipFile.ExtractToDirectory(GetDistribFilePath(), $"{NodePath}");
            }
            else
            {
                Log.LogMessage($"Unpacking {GetDistribFilePath()} using tar...");
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "tar",
                        Arguments = $" -xzf {GetDistribFilePath()}",
                        WorkingDirectory = NodePath,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit();
            }
        }

        private void DownloadNodeJS()
        {
            Log.LogMessage($"Downloading NodeJS from {GetDistribUrl()}");

            using var distribFile = new FileStream(GetDistribFilePath(), FileMode.Create, FileAccess.Write, FileShare.None);
            DownloadFile(GetDistribUrl(), distribFile);

            if (!NodeJSDownloaded())
                throw new Exception("Downloaded NodeJS distrib is corrupted.");
        }

        private bool NodeJSDownloaded()
        {
            string CalculateFileHashSum(string filePath)
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

            string GetWellknownHashSum(string fileName)
            {
                Log.LogMessage($"Downloading NodeJS hash sum file.");

                using var memoryStream = new MemoryStream();
                DownloadFile(GetDistribHashSumUrl(), memoryStream);
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

            if (!File.Exists(GetDistribFilePath()))
                return false;

            var fileHashSum = CalculateFileHashSum(GetDistribFilePath()).ToLower();
            Log.LogMessage($"Calcualted hash sum {fileHashSum}.");
            var wellknownHashSum = GetWellknownHashSum(GetDistribFileName().ToLower());
            Log.LogMessage($"Found hash sum {wellknownHashSum}.");

            return fileHashSum == wellknownHashSum;
        }

        private bool NodeJSExist() => Directory.Exists($"{NodePath}/{GetDistribName()}");

        private void DownloadFile(Uri uri, Stream stream)
        {
            Log.LogMessage($"Downloading file from {uri}.");
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

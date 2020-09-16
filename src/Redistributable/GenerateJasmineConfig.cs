using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Adeptik.NodeJs.Redistributable
{
    /// <summary>
    /// Task for jasmine config generation
    /// </summary>
    public class GenerateJasmineConfig : Task
    {
        /// <summary>
        /// Extension of test files
        /// </summary>
        public const string JSTestExtension = ".test.js";
        
        /// <summary>
        /// Jasmine config name with extension
        /// </summary>
        public const string JasmineConfigName = "jasmine.json";

        /// <summary>
        /// The name of the file that launches jasmine
        /// </summary>
        public const string JasmineLauncher = "JasmineExecutor.js";
        
        /// <summary>
        /// The name of the file storing information to run jasmine
        /// </summary>
        public const string JasmineLaunchSettings = ".jasmineLaunchSettings.json";

        /// <summary>
        /// Path to compiled project with a slash at the end
        /// </summary>
        [Required]
        public string? OutDir { get; set; }
        
        /// <summary>
        /// Path to execute file NodeJS
        /// </summary>
        [Required]
        public string? NodeExecutable { get; set; }
        
        /// <summary>
        /// Enumerate Jasmine test files & write jasmine config file and jasmine launch settings to output folder
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(OutDir) || !Directory.Exists(OutDir))
                return false;

            var testFiles = FindTestFilesInBuildFolder().ToArray();
            var jsonConfigJasmineString = $@"{{
                    ""spec_dir"": ""."",
                    ""spec_files"": [
                        {String.Join(",", testFiles.Select(testFile => $"\"{testFile}\""))}
                    ],
                    ""stopSpecOnExpectationFailure"": ""false"",
                    ""random"": ""false""
            }}".Replace("\\", "/");
            
            var configJasminePath = Path.Combine(OutDir, JasmineConfigName);
            File.WriteAllText(configJasminePath, jsonConfigJasmineString);
            
            var jsonLaunchSettings = $@"{{
                    ""NodeExecuteFile"": ""{NodeExecutable}"",
                    ""JasmineLauncher"": ""{$"{Path.Combine(OutDir, JasmineLauncher)}"}"",
                    ""JasmineConfig"": ""{configJasminePath}""
            }}".Replace("\\", "/");
            
            File.WriteAllText(Path.Combine(OutDir, JasmineLaunchSettings), jsonLaunchSettings);
            

            return true;

            IEnumerable<string> FindTestFilesInBuildFolder()
            {
                return Directory.EnumerateFiles(OutDir, $"*{JSTestExtension}", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath);
            }
        }


    }
}

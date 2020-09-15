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
        /// Path to compiled project
        /// </summary>
        [Required]
        public string? BuildPath { get; set; }

        /// <summary>
        /// Enumerate Jasmine test files & write jasmine config file to output folder
        /// </summary>
        /// <returns></returns>
        public override bool Execute()
        {
            if (string.IsNullOrEmpty(BuildPath) || !Directory.Exists(BuildPath))
                return false;

            var testFiles = FindTestFilesInBuildFolder().ToArray();
            var jsonString = $@"{{
                    ""spec_dir"": ""."",
                    ""spec_files"": [
                        {String.Join(",", testFiles.Select(filePath => $@"""{filePath.Replace(@"\", @"\\")}"""))}
                    ],
                    ""stopSpecOnExpectationFailure"": ""false"",
                    ""random"": ""false""
            }}";

            File.WriteAllText($"{BuildPath}/jasmine.json", jsonString);

            return true;

            IEnumerable<string> FindTestFilesInBuildFolder()
            {
                return Directory.EnumerateFiles(BuildPath, $"*{JSTestExtension}", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath);
            }
        }


    }
}

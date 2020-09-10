using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            var jsonStringBuilder = new StringBuilder();
            jsonStringBuilder.AppendLine("{");
            jsonStringBuilder.AppendLine("\t\"spec_dir\": \".\",");
            jsonStringBuilder.AppendLine("\t\"spec_files\": [");
            for (int testFileIndex = 0; testFileIndex < testFiles.Length; testFileIndex++)
                jsonStringBuilder.AppendLine($"\t\t\t\"{testFiles[testFileIndex].Replace('\\', '/')}\"" +
                                             $"{(testFileIndex != testFiles.Length - 1 ? "," : String.Empty)}");
            jsonStringBuilder.AppendLine("\t\t],");
            jsonStringBuilder.AppendLine("\t\"stopSpecOnExpectationFailure\": \"false\",");
            jsonStringBuilder.AppendLine("\t\"random\": \"false\"");
            jsonStringBuilder.AppendLine("}");

            File.WriteAllText($"{BuildPath}/jasmine.json", jsonStringBuilder.ToString());

            return true;

            IEnumerable<string> FindTestFilesInBuildFolder()
            {
                return Directory.EnumerateFiles(BuildPath, $"*{JSTestExtension}", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath);
            }
        }


    }
}

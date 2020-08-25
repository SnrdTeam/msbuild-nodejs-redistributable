﻿using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Text;

namespace Adeptik.NodeJs.Redistributable
{
    /// <summary>
    /// Task for jasmine config generation
    /// </summary>
    public class GenerateJasmineConfig : Task
    {
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

            //var testFiles = FindTestFilesInBuildFolder();

            var jsonStringBuilder = new StringBuilder();
            jsonStringBuilder.AppendLine("{");
            jsonStringBuilder.AppendLine("\t\"spec_dir\": \".\",");
            /*jsonStringBuilder.AppendLine("\t\"spec_files\": [");
            int fileNumber = 0;
            foreach (var testFile in testFiles)
                jsonStringBuilder.AppendLine($"\t\t\t\"{testFile.Replace('\\','/')}\"{(fileNumber++ != testFiles.Count() - 1 ? "," : "")}");
            jsonStringBuilder.AppendLine("\t\t],");*/
            jsonStringBuilder.AppendLine("\t\"stopSpecOnExpectationFailure\": \"false\",");
            jsonStringBuilder.AppendLine("\t\"random\": \"false\"");
            jsonStringBuilder.AppendLine("}");

            File.WriteAllText($"{BuildPath}/jasmine.json", jsonStringBuilder.ToString());

            return true;

            /*IEnumerable<string> FindTestFilesInBuildFolder()
            {
                return Directory.EnumerateFiles(BuildPath, $"*{JSTestExtension}", SearchOption.AllDirectories).Select(localPath => Path.GetFullPath(localPath));
            }*/
        }


    }
}

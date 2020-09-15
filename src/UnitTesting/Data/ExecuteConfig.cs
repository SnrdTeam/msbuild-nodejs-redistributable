using System;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter.Data
{
    /// <summary>
    /// This class provides access to the execution configuration
    /// </summary>
    [Serializable]
    public class ExecuteConfig
    {
        public string? NodeExecuteFile { get; set; }
        public string? JasmineLauncher { get; set; }
        public string? JasmineConfig { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter.Data
{
    /// <summary>
    /// This class provides access to the jasmine specDone structure
    /// </summary>
    [Serializable]
    public class SpecResult
    {
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        
        [JsonPropertyName("failedExpectations")]
        public List<Expectation>? FailedExpectations { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("duration")]
        public uint? Duration { get; set; }
    }
}
using System;
using System.Text.Json.Serialization;

namespace Adeptik.NodeJs.UnitTesting.TestAdapter.Data
{
    /// <summary>
    /// Expectation without js object's
    /// </summary>
    [Serializable]
    public class Expectation
    {
        [JsonPropertyName("matcherName")]
        public string? MatcherName { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("stack")]
        public string? Stack { get; set; }
        
        [JsonPropertyName("passed")]
        public bool? Passed { get; set; }
    }
}
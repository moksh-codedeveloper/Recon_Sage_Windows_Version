using System;
using System.Diagnostics.Tracing;
using System.Text.Json;

namespace ScanModels.CLIVersion
{
    class JsonFileAnalyser
    {
        public string JsonFilePath { get; set; } = string.Empty;
        public JsonFileAnalyser(string jsonFilePath)
        {
            JsonFilePath = jsonFilePath;
        }

        public class AnalyseDataModel
        {
            public string target { get; set; } = string.Empty;
            public string message { set; get; } = string.Empty;
            public int statuscode { set; get; }
            public double latencyms { set; get; }
            public Dictionary<string, string>? headers { set; get; }
        }

        public async Task<List<AnalyseDataModel>> ReadJSONFile()
        {
            using FileStream openStream = File.OpenRead(JsonFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return await JsonSerializer.DeserializeAsync<List<AnalyseDataModel>>(openStream, options);
        }

        public Dictionary<string, int> StructureResult(List<AnalyseDataModel> result)
        {
            var stats = new Dictionary<string, int>
            {
                ["count_200s"] = 0,
                ["count_error_codes"] = 0,
                ["count_404s"] = 0,
                ["count_Ok_msg"] = 0,
                ["count_Other_Error_message"] = 0,
                ["count_Timeout_msg"] = 0,
                ["spike_latency_ms"] = 0,
                ["count_network_error_msg"] = 0
            };

            foreach (var e in result)
            {
                if (e.statuscode >= 200 && e.statuscode < 300) stats["count_200s"]++;
                if (e.statuscode == 404 ) stats["count_404s"]++;
                if (e.statuscode >= 400) stats["count_error_codes"]++;
                if (e.latencyms >= 1000) stats["spike_latency_ms"]++;
                if (e.message == "OK") stats["count_Ok_msg"]++;
                else stats["count_Other_Error_message"]++;

                if (e.message == "Timeout") stats["count_Timeout_msg"]++;
                if (e.message == "Network error") stats["count_network_error_msg"]++;
            }
            return stats;
        }
    }
}
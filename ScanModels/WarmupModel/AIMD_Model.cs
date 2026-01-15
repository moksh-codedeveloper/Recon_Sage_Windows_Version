using System;
using System.Collections.Generic;
using System.Linq;

namespace ScanModels.WarmupModel
{
    public class SpeedTuningOutputModel
    {
        public int IncreasingLatency{set; get;}
        public int DecreasingLatency{set;get;}
        public List<double> SpikedLatency{get; set;} = new();
        public int SuggestedConcurrency{set; get;}
        public int SuggestedTimemout{set; get;}
        public int wafHit{set; get;}
        public int rateLimtedHits{set; get;}

        public List<int> WafRateLimitedCodes{set; get;} = new();
    }
    public class SpeedTuning
    {
        public int[] StatusCodeList{set;get;}
        public double[] LatencyList{get; set;}
        public int SuggestedConcurrency{set; get;}
        public int SuggestedTimeout{set; get;}
        public int IncreasingLatencyTrend = 0;
        public List<int> WafRateLimitCodesDetected{get;} = new();
        public int DecreasingLatencyTrend = 0;
        public  List<int> RLStatusCode = new List<int>{429, 420, 402, 403, 503};
        public List<int> WafStatusCode = new List<int>{409, 405, 444, 429, 990, 900, 406, 401, 404, 403};
        public SpeedTuning(double[] latencyList,int[] statusCodeList, int Concurrency, int Timeout)
        {
            SuggestedConcurrency = Concurrency;
            SuggestedTimeout = Timeout;
            LatencyList = latencyList;
            StatusCodeList = statusCodeList;
        }
        public SpeedTuningOutputModel CalculateSpeedUsingCodes()
        {
            SpeedTuningOutputModel data = new SpeedTuningOutputModel();
            var SpikedLatency = new List<double>();
            int wafHit = 0;
            int rateLimitHit = 0;
            // Status Code analysis :- 
            for(int i = 0; i < StatusCodeList.Length; i++)
            {
                int code = StatusCodeList[i];
                bool isWaf = WafStatusCode.Contains(code);
                bool isVendorSpecific = code >= 900;
                bool isRateLimited = RLStatusCode.Contains(code);
                if (isRateLimited)
                {
                    SuggestedConcurrency -= (int)(SuggestedConcurrency * 0.5);
                    SuggestedTimeout += 50;
                    WafRateLimitCodesDetected.Add(code);
                    rateLimitHit++;
                } else if(isWaf || isVendorSpecific)
                {
                    SuggestedConcurrency -= (int)(SuggestedConcurrency * 0.3);
                    SuggestedTimeout += 30;
                    WafRateLimitCodesDetected.Add(code);
                    wafHit++;
                }
                else if (code >= 200 && code < 400)
                {
                    SuggestedConcurrency += 10;
                    SuggestedTimeout -= 10;
                }
                SuggestedConcurrency = Math.Clamp(SuggestedConcurrency, 1, 1000);
                SuggestedTimeout = Math.Clamp(SuggestedTimeout, 1, 1000);
            }
            // Latency analysis
            double mean = LatencyList.Average();
            double variance = LatencyList.Select(x => Math.Pow(x - mean, 2)).Average();
            double stdev = Math.Sqrt(variance);
            double thresholds = mean + (2 * stdev);

            for(int i = 1; i < LatencyList.Length; i++)
            {
                double lat = LatencyList[i];
                double prevLat = LatencyList[i-1];
                bool isSusLat = lat >= 1000;
                bool isIncreasing = lat > prevLat;
                bool isDecreasing = lat < prevLat;
                bool isSpiked = lat > thresholds;
                if (isSpiked)
                {
                    SuggestedConcurrency -= (int)(SuggestedConcurrency * 0.5);
                    SuggestedTimeout += 50;
                    SpikedLatency.Add(lat);
                } else if (isSusLat)
                {
                    SuggestedConcurrency -= (int)(SuggestedConcurrency * 0.5);
                    SuggestedTimeout += 50;
                }
                if (isIncreasing)
                {
                    IncreasingLatencyTrend++;
                    SuggestedConcurrency -= (int)(SuggestedConcurrency * 0.3);
                    SuggestedTimeout += 30;
                }
                if (isDecreasing)
                {
                    DecreasingLatencyTrend++;
                    SuggestedConcurrency += 10;
                    SuggestedTimeout -= 10;
                }
                SuggestedConcurrency = Math.Clamp(SuggestedConcurrency, 1, 1000);
                SuggestedTimeout = Math.Clamp(SuggestedTimeout, 1, 1000);
            }
            data.SuggestedConcurrency = SuggestedConcurrency;
            data.SuggestedTimemout = SuggestedTimeout;
            data.WafRateLimitedCodes = WafRateLimitCodesDetected;
            data.wafHit = wafHit;
            data.rateLimtedHits = rateLimitHit;
            data.SpikedLatency = SpikedLatency;
            data.DecreasingLatency = DecreasingLatencyTrend;
            data.IncreasingLatency = IncreasingLatencyTrend;
            return data;
        }
    }
}
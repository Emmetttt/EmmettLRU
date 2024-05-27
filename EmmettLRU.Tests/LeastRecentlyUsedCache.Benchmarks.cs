using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace EmmettLRU.Tests;

[SimpleJob]
public class LeastRecentlyUsedCacheBenchmarks
{
    [Test]
    public void RunBenchmarks() => BenchmarkRunner
        .Run<Benchmarks>(new ManualConfig()
            // NUnit is non-optimized
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddLogger(new ConsoleLogger())
            .AddColumnProvider(new SimpleColumnProvider())
            .AddJob(Job.Default.WithGcServer(true))
        );

    public class Benchmarks
    {
        private const string RandomValue = "myValue";
        private const int PrePopulatedCacheCapacity = 100;
        private readonly LeastRecentlyUsedCache<int, string> _prePopulatedCache = new LeastRecentlyUsedCache<int, string>(PrePopulatedCacheCapacity);

        [SetUp]
        public void SetUp()
        {
            for (var i = 0; i < PrePopulatedCacheCapacity; i++)
            {
                _prePopulatedCache.Put(i, RandomValue);
            }
        }
        
        [Benchmark]
        /*
            Runtime = .NET 6.0.25 (6.0.2523.51912), Arm64 RyuJIT AdvSIMD; GC = Concurrent Server
            Mean = 40.294 ms, StdErr = 0.030 ms (0.07%), N = 15, StdDev = 0.115 ms
            Min = 40.143 ms, Q1 = 40.209 ms, Median = 40.290 ms, Q3 = 40.347 ms, Max = 40.500 ms
            IQR = 0.138 ms, LowerFence = 40.002 ms, UpperFence = 40.554 ms
            ConfidenceInterval = [40.171 ms; 40.417 ms] (CI 99.9%), Margin = 0.123 ms (0.31% of Mean)
            Skewness = 0.51, Kurtosis = 1.86, MValue = 2
        */
        public void InsertAMillionItemsWithEvictions()
        {
            var capacity = 1_000;
            var inserts = 1_000_000;
            var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);

            for (var i = 0; i < inserts; i++)
            {
                cache.Put(i, RandomValue);
            }
        }
        
        [Benchmark]
        /*
            Runtime = .NET 6.0.25 (6.0.2523.51912), Arm64 RyuJIT AdvSIMD; GC = Concurrent Server
            Mean = 37.529 ms, StdErr = 0.329 ms (0.88%), N = 96, StdDev = 3.227 ms
            Min = 33.630 ms, Q1 = 35.061 ms, Median = 36.216 ms, Q3 = 39.214 ms, Max = 45.807 ms
            IQR = 4.153 ms, LowerFence = 28.831 ms, UpperFence = 45.443 ms
            ConfidenceInterval = [36.411 ms; 38.647 ms] (CI 99.9%), Margin = 1.118 ms (2.98% of Mean)
            Skewness = 1, Kurtosis = 2.93, MValue = 2.25
        */
        public void InsertAMillionItemsWithoutEvictions()
        {
            var capacity = 1_000_000;
            var inserts = 1_000_000;
            var cache = new LeastRecentlyUsedCache<int, string>(capacity: capacity);

            for (var i = 0; i < inserts; i++)
            {
                cache.Put(i, RandomValue);
            }
        }
        
        [Benchmark]
        /*
            Runtime = .NET 6.0.25 (6.0.2523.51912), Arm64 RyuJIT AdvSIMD; GC = Concurrent Server
            Mean = 89.553 ms, StdErr = 0.120 ms (0.13%), N = 12, StdDev = 0.417 ms
            Min = 89.155 ms, Q1 = 89.220 ms, Median = 89.361 ms, Q3 = 89.782 ms, Max = 90.404 ms
            IQR = 0.562 ms, LowerFence = 88.378 ms, UpperFence = 90.625 ms
            ConfidenceInterval = [89.019 ms; 90.087 ms] (CI 99.9%), Margin = 0.534 ms (0.60% of Mean)
            Skewness = 0.76, Kurtosis = 2.06, MValue = 2
        */
        public void SuccessfulReads()
        {
            var reads = 10_000_000;
            for (var i = 0; i < reads; i++)
            {
                _prePopulatedCache.TryGet(i % PrePopulatedCacheCapacity, out _);
            }
        }
        
        [Benchmark]
        /*
            Runtime = .NET 6.0.25 (6.0.2523.51912), Arm64 RyuJIT AdvSIMD; GC = Concurrent Server
            Mean = 86.814 ms, StdErr = 0.147 ms (0.17%), N = 13, StdDev = 0.532 ms
            Min = 86.421 ms, Q1 = 86.511 ms, Median = 86.542 ms, Q3 = 86.849 ms, Max = 88.259 ms
            IQR = 0.338 ms, LowerFence = 86.004 ms, UpperFence = 87.357 ms
            ConfidenceInterval = [86.177 ms; 87.450 ms] (CI 99.9%), Margin = 0.637 ms (0.73% of Mean)
            Skewness = 1.56, Kurtosis = 4.42, MValue = 2
        */
        public void UnsuccessfulReads()
        {
            var reads = 10_000_000;
            for (var i = 0; i < reads; i++)
            {
                _prePopulatedCache.TryGet(PrePopulatedCacheCapacity + i, out _);
            }
        }
    }
}
using BenchmarkDotNet.Running;
using Fuse8_ByteMinds.SummerSchool.Benchmarks;

var config = new ManualConfig();

BenchmarkRunner.Run<StringInternBenchmark>();

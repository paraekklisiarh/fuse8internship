using BenchmarkDotNet.Running;
using Fuse8_ByteMinds.SummerSchool.Benchmarks;

BenchmarkRunner.Run<AccountProcessorBenchmark>();

BenchmarkRunner.Run<StringInternBenchmark>();

using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var config = ManualConfig.CreateEmpty();
config.Add(DefaultConfig.Instance);

var job = Job.Default
	.WithPlatform(Platform.X64)
	.WithMinWarmupCount(2)
	.WithMaxWarmupCount(7)
	.WithMinIterationCount(2)
	.WithMaxIterationCount(8);

BenchmarkRunner.Run(
	Assembly.GetExecutingAssembly(),
	config
		.AddJob(job.WithRuntime(CoreRuntime.Core90))
		.AddDiagnoser(MemoryDiagnoser.Default),
	args);
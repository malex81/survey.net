using DeviceSurvey;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace GpuSurvey;

/*
 * https://ilgpu.net/docs/ - general docs
 * https://ilgpu.net/docs/02-beginner/01-context-and-accelerators/ - samples about devices
 * https://ilgpu.net/docs/02-beginner/04-structs/ - examples with GetPreferredDevice
 */
internal class InvestigateDevice : ISurveyArea
{
	private readonly ILogger<InvestigateDevice> logger;

	public InvestigateDevice(ILogger<InvestigateDevice> logger)
	{
		this.logger = logger;
	}

	public string Name => "Investigate GPU devices";

	public void Survey()
	{
		SurveyAll();
		//SurveyDefault();
	}

	void SurveyAll()
	{
		using Context context = Context.Create(builder => builder.AllAccelerators().EnableAlgorithms());
		//using Context context = Context.Create(builder => builder.AllAccelerators());
		//using Context context = Context.CreateDefault();

		foreach (Device device in context)
			ExploreDevice(context, device);
	}

	void SurveyDefault()
	{
		using Context context = Context.Create(builder => builder.Default().EnableAlgorithms());
		var device = context.GetPreferredDevice(preferCPU: false);
		ExploreDevice(context, device);
	}

	void ExploreDevice(Context context, Device device)
	{
		using Accelerator accelerator = device.CreateAccelerator(context);
		StringWriter accInfo = new();
		accelerator.PrintInformation(accInfo);
		logger.LogInformation("Device:\t{device}"
			+ "\nAccelerator:\t{accelerator}",
			device, accInfo.ToString());
		ExecuteKernel(accelerator);
		Thread.Sleep(100);
	}

	const int INPUT_SIZE = 1000000,
		OUTPUT_SIZE = 2000000;

	void ExecuteKernel(Accelerator accelerator)
	{
		var timeLog = new StringWriter();
		var sw = Stopwatch.StartNew();
		float[] input = new float[INPUT_SIZE];
		for (int i = 0; i < INPUT_SIZE; i++) { input[i] = (float)(2f * i * Math.PI / INPUT_SIZE); }
		timeLog.WriteLine($"fill input: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Restart();
		using var inputBuffer = accelerator.Allocate1D(input);
		using var outputBuffer = accelerator.Allocate1D<double>(OUTPUT_SIZE);
		timeLog.WriteLine($"upload input: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Restart();
		var loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<double>>(KernelProc);
		timeLog.WriteLine($"load kernel: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Restart();
		loadedKernel(OUTPUT_SIZE, inputBuffer.View, outputBuffer.View);
		accelerator.Synchronize();
		timeLog.WriteLine($"exec kernel: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Restart();
		loadedKernel(OUTPUT_SIZE, inputBuffer.View, outputBuffer.View);
		accelerator.Synchronize();
		timeLog.WriteLine($"re-exec kernel: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Restart();
		var hostOutput = outputBuffer.GetAsArray1D();
		timeLog.WriteLine($"fetch output: {sw.Elapsed.TotalMilliseconds} ms");

		sw.Stop();
		Console.WriteLine($"Output: {string.Join("; ", hostOutput.Skip(1000).Take(100))}");

		logger.LogInformation("--- Timings ---\r\n{timeLog}", timeLog);
	}

	static void KernelProc(Index1D i, ArrayView<float> data, ArrayView<double> output)
	{
		//var sin = 10*MathF.Sin(data[i % data.Length]);
		var sin = XMath.Sin((double)data[i % data.Length]);
		if (sin < 0) sin *= -1;
		if (sin < 0.1) sin *= 100;
		output[i] = sin;
		//output[i] = XMath.Sin(data[i % data.Length]);
		//output[i] = data[i % data.Length] * data.Length;
	}
}

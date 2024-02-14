using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeviceSurvey;
using ILGPU.Runtime;
using ILGPU;
using ILGPU.Algorithms;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace GpuSurvey
{
	/*
	 * https://ilgpu.net/docs/ - general docs
	 * https://ilgpu.net/docs/02-beginner/01-context-and-accelerators/ - samples about devices
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
			using Context context = Context.Create(builder => builder.AllAccelerators().EnableAlgorithms());
			// using Context context = Context.CreateDefault()

			foreach (Device device in context)
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
			using var outputBuffer = accelerator.Allocate1D<float>(OUTPUT_SIZE);
			timeLog.WriteLine($"upload input: {sw.Elapsed.TotalMilliseconds} ms");
			sw.Restart();

			var loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<float>, ArrayView<float>>(Kernel);
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
			sw.Restart();

			Console.WriteLine($"Output: {string.Join("; ", hostOutput.Take(100))}");

			logger.LogInformation("Timings:\t{timeLog}", timeLog);
		}

		static void Kernel(Index1D i, ArrayView<float> data, ArrayView<float> output)
		{
			// output[i] = (float)Math.Sin(data[i % data.Length]);
			output[i] = XMath.Sin(data[i % data.Length]);
			// output[i] = data[i % data.Length] * data.Length;
		}
	}
}

using ILGPU;
using ILGPU.Backends.EntryPoints;
using ILGPU.Backends;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace GpuSurvey.CodeGenStudy;
internal class KernelCompilation : ISurveyArea
{
	private readonly ILogger<KernelCompilation> logger;

	public KernelCompilation(ILogger<KernelCompilation> logger)
	{
		this.logger = logger;
	}

	public string Name => "Study generated code";

	public void Survey()
	{
		using var context = Context.Create(builder => builder
					.Default()
					.EnableAlgorithms());
		var device = context.Devices.FirstOrDefault(d => d.AcceleratorType == AcceleratorType.OpenCL);
		if (device == null)
		{
			logger.LogCritical("No suitable device found for research");
			return;
		}
		using var accelerator = device.CreateAccelerator(context);

		//ResearchSubject1(accelerator);
		ResearchMixColors(accelerator);
	}

	#region Subject #1
	static void SubjectKernel1(Index1D index, ArrayView<uint> dataView, uint constant)
	{
		dataView[index] = (uint)index + constant;
	}
	static void ResearchSubject1(Accelerator accelerator)
	{
		var compiledKernel = CompileAutoGroupedKernel(accelerator, nameof(SubjectKernel1));

		using var kernel = accelerator.LoadAutoGroupedKernel(compiledKernel);
		var launcher = kernel.CreateLauncherDelegate<Action<AcceleratorStream, Index1D, ArrayView<uint>, uint>>();
		// -------------------------------------------------------------------------------

		using var buffer = accelerator.Allocate1D<uint>(1024);
		launcher(accelerator.DefaultStream, (int)buffer.Length, buffer.View, 42);

		// Reads data from the GPU buffer into a new CPU array.
		// Implicitly calls accelerator.DefaultStream.Synchronize() to ensure
		// that the kernel and memory copy are completed first.
		var data = buffer.GetAsArray1D();
		for (int i = 0, e = data.Length; i < e; ++i)
		{
			if (data[i] != 42 + i)
				Console.WriteLine($"Error at element location {i}: {data[i]} found");
		}
	}
	#endregion

	#region Survey MixColors
	static void MixColorsKernel(Index1D index, ArrayView<uint> dataView, uint c1, uint c2)
	{
		dataView[index] = CalcProc.MixColors(c1, c2, 0.5f);
	}
	static void ResearchMixColors(Accelerator accelerator)
	{
		var compiledKernel = CompileAutoGroupedKernel(accelerator, nameof(MixColorsKernel));

		using var kernel = accelerator.LoadAutoGroupedKernel(compiledKernel);
		var launcher = kernel.CreateLauncherDelegate<Action<AcceleratorStream, Index1D, ArrayView<uint>, uint, uint>>();
		// -------------------------------------------------------------------------------

		using var buffer = accelerator.Allocate1D<uint>(1024);
		launcher(accelerator.DefaultStream, (Index1D)buffer.Length, buffer.View, 0xaabbcc, 0xff1122);

		var data = buffer.GetAsArray1D();
		Console.WriteLine("Some first elements: {0}", string.Join(", ", data.Take(10).Select(v => v.ToString("X8"))));
	}
	#endregion
	#region Helpers
	static CompiledKernel CompileAutoGroupedKernel(Accelerator accelerator, string methodName)
	{
		// Access the current backend for this device
		var backend = accelerator.GetBackend();

		// Resolve and compile method into a kernel
		var method = typeof(KernelCompilation).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
		var entryPointDesc = EntryPointDescription.FromImplicitlyGroupedKernel(method);
		return backend.Compile(entryPointDesc, default);
		// Info: If the current accelerator is a CudaAccelerator, we can cast the compiled kernel to a
		// PTXCompiledKernel in order to extract the PTX assembly code.

		// -------------------------------------------------------------------------------
		// Load the implicitly grouped kernel with an automatically determined group size.
		// Note that the kernel has to be disposed manually.
	}
	#endregion
}

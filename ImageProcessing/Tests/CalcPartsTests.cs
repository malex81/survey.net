using ImageProcessing.RenderingMath;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageProcessing.Tests;

[TestFixture]
public class CalcPartsTests
{
	[TestCase(0.84089642f)]
	[TestCase(0.55f)]
	[TestCase(5f)]
	[TestCase(10f)]
	public void TestGausianMatrix(float sigma)
	{
		var matrix = CalcProc.ComputeGaussianMatrix(sigma);
		Assert.That(matrix, Has.All.GreaterThan(0));

		float[] elements = new float[matrix.GetLength(0) * matrix.GetLength(1)];
		Buffer.BlockCopy(matrix, 0, elements, 0, elements.Length * sizeof(float));
		var sum = elements.Sum();
		Assert.That(sum, Is.InRange(0.99f, 1f));
	}
}

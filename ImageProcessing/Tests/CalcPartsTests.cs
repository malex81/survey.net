using ImageProcessing.RenderingMath;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing.Tests;

[TestFixture]
public class CalcPartsTests
{
	[TestCase(0.84089642f)]
	public void TestGausianMatrix(float sigma)
	{
		var matrix = CalcProc.ComputeGausianMatrix(sigma);
		Assert.That(matrix, Has.All.GreaterThan(0));
	}
}

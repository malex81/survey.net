using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceSurvey
{
	internal interface ISurveyArea
	{
		string Name { get; }
		void Survey();
	}
}

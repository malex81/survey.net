using Avalonia.Controls;

namespace ImageProcessing.Base;

public interface ISurveyComponent
{
	string Code { get; }
	string Title { get; }
	Control View { get; }
}

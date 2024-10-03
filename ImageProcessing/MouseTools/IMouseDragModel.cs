using Avalonia;
using Avalonia.Input;

namespace ImageProcessing.MouseTools;

public record DragState(Point BeginPos, Point CurrentPos, MouseButton MouseButton);
public interface IMouseDragModel
{
	void DragStart(DragState dragState);
	void DragProcess(DragState dragState);
}

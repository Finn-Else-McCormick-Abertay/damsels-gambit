using Godot;

namespace DamselsGambit;

public interface IFocusableContainer
{
    public Node GetNextFocus(FocusDirection direction, Node child) => null;

    public Node TryGainFocus(FocusDirection direction) => null;
    
    public (Node Focus, Viewport Viewport) TryGainFocus(FocusDirection direction, Viewport fromViewport) => (TryGainFocus(direction), null);
    
    public bool TryLoseFocus(FocusDirection direction, out bool popViewport) {
        popViewport = false;
        return true;
    }
}
using Godot;

namespace DamselsGambit;

public interface IFocusableContainer
{
    public Control GetNextFocus(InputManager.FocusDirection direction, int childIndex) => null;

    public Control TryGainFocus(InputManager.FocusDirection direction) => null;
    
    public (Control Control, Viewport Viewport) TryGainFocus(InputManager.FocusDirection direction, Viewport fromViewport) => (TryGainFocus(direction), null);
    
    public bool TryLoseFocus(InputManager.FocusDirection direction, out bool popViewport) {
        popViewport = false;
        return true;
    }
}
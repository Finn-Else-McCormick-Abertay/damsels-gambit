using Godot;

namespace DamselsGambit;

public interface IFocusableContainer
{
    public Control GetNextFocus(InputManager.FocusDirection direction, int childIndex) => null;

    public Control TryGainFocus(InputManager.FocusDirection direction) => null;
    
    public bool TryLoseFocus(InputManager.FocusDirection direction) => true;
}
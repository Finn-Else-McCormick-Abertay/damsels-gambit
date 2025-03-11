using Godot;

namespace DamselsGambit;

public interface IFocusableContainer
{
    public abstract Control GetNextFocus(InputManager.FocusDirection direction, int childIndex);
}
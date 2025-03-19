using Godot;

namespace DamselsGambit;

public interface IFocusableContainer
{
    public Control GetNextFocus(InputManager.FocusDirection direction, int childIndex) => null;
}
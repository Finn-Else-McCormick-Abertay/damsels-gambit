using Godot;

namespace DamselsGambit;

public interface IFocusContext
{
    public virtual int FocusContextPriority => 0;
    public virtual Control GetDefaultFocus() => GetDefaultFocus(InputManager.FocusDirection.Right);
    public virtual Control GetDefaultFocus(InputManager.FocusDirection direction) => null;
}
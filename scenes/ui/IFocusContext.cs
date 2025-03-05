using Godot;

namespace DamselsGambit;

public interface IFocusContext
{
    public virtual int FocusContextPriority => 0;
    public abstract Control GetDefaultFocus();
    public virtual Control GetDefaultFocus(InputManager.FocusDirection direction) => GetDefaultFocus();
}
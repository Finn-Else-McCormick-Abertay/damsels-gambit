using Godot;

namespace DamselsGambit;

public interface IFocusContext
{
    public abstract Control GetDefaultFocus();
    public virtual Control GetDefaultFocus(InputManager.FocusDirection direction) => GetDefaultFocus();
}
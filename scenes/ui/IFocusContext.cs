using Godot;

namespace DamselsGambit;

public interface IFocusContext
{
    public abstract Control GetDefaultFocus();
}
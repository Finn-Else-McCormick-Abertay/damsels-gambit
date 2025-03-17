using Godot;

namespace DamselsGambit;

public interface IBackContext
{
    public virtual int BackContextPriority => 0;
    public abstract bool UseBackInput();
}
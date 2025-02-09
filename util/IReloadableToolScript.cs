using Godot;

namespace DamselsGambit.Util;

public interface IReloadableToolScript : ISerializationListener
{
    public abstract void _EnterTree();
    public abstract void _ExitTree();

    protected virtual void PreScriptReload() {}
    protected virtual void OnScriptReload() {}

	void ISerializationListener.OnBeforeSerialize() => PreScriptReload();
    void ISerializationListener.OnAfterDeserialize() {
        _ExitTree();
        OnScriptReload();
        _EnterTree();
    }
}
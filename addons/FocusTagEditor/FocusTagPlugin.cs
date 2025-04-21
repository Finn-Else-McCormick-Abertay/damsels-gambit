#if TOOLS
using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit.Editor.FocusTagPlugin;

[Tool]
public partial class FocusTagPlugin : EditorPlugin, ISerializationListener
{
	private FocusTagInspector _inspectorPlugin;

	private void Setup() {
		_inspectorPlugin = new(); AddInspectorPlugin(_inspectorPlugin);
	}

	private void Cleanup() {
		if (_inspectorPlugin.IsValid()) RemoveInspectorPlugin(_inspectorPlugin); _inspectorPlugin = null;
	}

	public override void _EnterTree() => Setup(); public void OnAfterDeserialize() => Setup();
	public override void _ExitTree() => Cleanup(); public void OnBeforeSerialize() => Cleanup();
}
#endif

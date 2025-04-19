#if TOOLS
using Godot;
using System;
using DamselsGambit.Util;

namespace DamselsGambit.Editor.FocusTagPlugin;

[Tool]
public partial class FocusTagInspector : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object) => @object is Control;

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide) {
        if (name.StartsWith("focus_") && type == Variant.Type.NodePath && hintType == PropertyHint.NodePathValidTypes && hintString == "Control") {
            //GD.Print($"Parse property : {@object.ToPrettyString()}, {type.ToPrettyString()}, {name}, {hintType.ToPrettyString()} : {hintString}, {usageFlags.ToPrettyString()}, Wide: {wide}");
            var propertyEditor = new TaggedNodePathProperty();
            AddPropertyEditor(name, propertyEditor);
            return true; 
        }
        return false;
    }
}

#endif

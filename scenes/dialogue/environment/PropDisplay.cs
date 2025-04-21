using Godot;
using System;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using System.Collections.Generic;
using System.Linq;

namespace DamselsGambit.Environment;

[Tool, GlobalClass]
public partial class PropDisplay : Sprite2D, IEnvironmentDisplay
{
	private StringName _propNameOverride = null;
	[Export] public StringName PropName { get => !_propNameOverride.IsNullOrEmpty() ? _propNameOverride : Name; set { _propNameOverride = value != Name ? value : null; UpdateTexture(); } }
	[Export] public int Variant { get; set { field = value; UpdateTexture(); } } = 0;

	private string TexturePath { get; set { field = value; Texture = ResourceLoader.Exists(TexturePath) ? ResourceLoader.Load<Texture2D>(TexturePath) : new PlaceholderTexture2D() { Size = new(200,200) }; } }

	private IEnumerable<int> _validVariants = [];
	private void UpdateTexture() {
		TexturePath = $"res://assets/items/{Case.ToSnake(PropName)}{(Variant > 0 ? $"_{Variant}" : "")}.png";
		if (Engine.IsEditorHint()) {
			_validVariants = GetValidVariants(PropName);
			if (!_validVariants.Contains(Variant)) Variant = _validVariants.FirstOrDefault();
		}
	}

	public static IEnumerable<int> GetValidVariants(string propName) {
		List<int> validVariants = [];
		if (ResourceLoader.Exists($"res://assets/items/{Case.ToSnake(propName)}.png")) validVariants.Add(0);
		int variant = 1; while (ResourceLoader.Exists($"res://assets/items/{Case.ToSnake(propName)}_{variant}.png")) { validVariants.Add(variant); variant++; }
		return validVariants;
	}
	
	private bool _initiallyVisible;

	public override void _EnterTree() {
		if(Engine.IsEditorHint()) { this.TryConnect(Node.SignalName.Renamed, OnRenamed); return; }
		EnvironmentManager.Register(this); _initiallyVisible = Visible; Hide();
	}
	public override void _ExitTree() {
		if(Engine.IsEditorHint()) { this.TryDisconnect(Node.SignalName.Renamed, OnRenamed); return; }
		EnvironmentManager.Deregister(this);
	}

	private void OnRenamed() { UpdateTexture(); NotifyPropertyListChanged(); }

    public override bool _PropertyCanRevert(StringName property) => property switch {
		_ when property == PropertyName.PropName => true,
		_ when property == PropertyName.Variant => true,
		_ => false
	};

	public override Variant _PropertyGetRevert(StringName property) => property switch {
		_ when property == PropertyName.PropName => Name,
		_ when property == PropertyName.Variant => _validVariants.FirstOrDefault(),
		_ => new()
	};

    public override void _ValidateProperty(Godot.Collections.Dictionary property) {
		var name = property["name"].AsStringName();
		if (name == PropertyName.Variant) {
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = Godot.Variant.From(_validVariants.Count() == 1 && _validVariants.Single() == 0 ? "Single" : $",{string.Join(",", _validVariants.Select(x => $"Variant {x}"))}");
		}
    }
	
	public void RestoreInitialVisibility() => Visible = _initiallyVisible;
}
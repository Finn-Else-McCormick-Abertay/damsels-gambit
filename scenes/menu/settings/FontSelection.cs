using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit.Settings;

public partial class FontSelection : HBoxContainer
{
	[Export] public OptionButton OptionButton { get; set; }

	public override void _Ready() {
		foreach (var fontState in Enum.GetValues<FontManager.FontState>()) OptionButton.AddItem(Enum.GetName(fontState).Capitalize(), (int)fontState);
		OptionButton.Select((int)FontManager.FontState.Default);
		OptionButton.Connect(OptionButton.SignalName.ItemSelected, this, MethodName.OnItemSelected);
	}

	private void OnItemSelected(int index) => FontManager.Font = (FontManager.FontState)index;
}

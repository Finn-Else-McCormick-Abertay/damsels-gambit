using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit.Settings;

public partial class FontSelection : HBoxContainer
{
    [Export] public OptionButton OptionButton { get; set; }

    private readonly List<Font> _fonts = [];

    public override void _Ready() {
        OptionButton.Clear();
        _fonts.Clear();
        foreach (var field in typeof(FontManager.Fonts).GetFields()) {
            OptionButton.AddItem(Case.ToSnake(field.Name).Capitalize());
            _fonts.Add(field.GetValue(null) as Font);
        }
        OptionButton.Select(_fonts.IndexOf(FontManager.DefaultFont));
        OptionButton.Connect(OptionButton.SignalName.ItemSelected, this, MethodName.OnItemSelected);
    }

    private void OnItemSelected(int index) {
        if (index < 0 || index >= _fonts.Count) return;
        FontManager.DefaultFont = _fonts[index];
    }
}

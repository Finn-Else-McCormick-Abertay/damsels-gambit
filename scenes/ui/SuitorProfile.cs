using Godot;
using System;

namespace DamselsGambit;

[Tool]
public partial class SuitorProfile : Control
{
    [ExportGroup("Nodes")]
    [Export] private Button TabButton { get; set; }
}

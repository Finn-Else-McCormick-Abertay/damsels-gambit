using Godot;

namespace DamselsGambit;

[GlobalClass, Tool]
public partial class CardInfo : Resource
{
    [Export]
    public string DisplayName { get; set { field = value; EmitChanged(); } }
    
    [Export]
    public string Score { get; set { field = value; EmitChanged(); } }
}
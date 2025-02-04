using System.Linq;
using Godot;

namespace DamselsGambit.Util;

static class NodeExtensions
{
    public static Godot.Collections.Array<Node> GetInternalChildren<TNode>(this TNode self)
        where TNode : Node
    {
        var publicChildren = self.GetChildren();
        var internalChildren = self.GetChildren(true).Where(x => !publicChildren.Contains(x));
        return new(internalChildren);
    }
}
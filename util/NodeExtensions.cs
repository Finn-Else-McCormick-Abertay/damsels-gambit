using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DamselsGambit.Util;

static class NodeExtensions
{
    public static Godot.Collections.Array<Node> GetInternalChildren<TNode>(this TNode self) where TNode : Node {
        var publicChildren = self.GetChildren();
        var internalChildren = self.GetChildren(true).Where(x => !publicChildren.Contains(x));
        return new(internalChildren);
    }

    public static TNode FindChildOfType<TNode>(this Node self) where TNode : Node {
        foreach (var child in self.GetChildren()) { if (child is TNode) { return child as TNode; } }
        foreach (var child in self.GetChildren()) {
            var result = child.FindChildOfType<TNode>();
            if (result is not null) { return result; }
        }
        return null;
    }
    public static Godot.Collections.Array<TNode> FindChildrenOfType<[MustBeVariant]TNode>(this Node self) where TNode : Node {
        var validChildren = new List<TNode>();
        foreach (var child in self.GetChildren()) {
            if (child is TNode) { validChildren.Add(child as TNode); }
            validChildren.AddRange(child.FindChildrenOfType<TNode>());
        }
        return [.. validChildren];
    }

    // Signal helper functions

    public static Error Connect(this GodotObject self, StringName signal, Action action) {
        return self.Connect(signal, Callable.From(action));
    }
    public static void Disconnect(this GodotObject self, StringName signal, Action action) {
        self.Disconnect(signal, Callable.From(action));
    }

    public static Error TryConnect(this GodotObject self, StringName signal, Callable callable) {
        if (self.IsConnected(signal, callable)) return Error.AlreadyExists;
        return self.Connect(signal, callable);
    }
    public static Error TryDisconnect(this GodotObject self, StringName signal, Callable callable) {
        if (!self.IsConnected(signal, callable)) return Error.DoesNotExist;
        self.Disconnect(signal, callable);
        return Error.Ok;
    }

    public static Error TryConnect(this GodotObject self, StringName signal, Action action) {
        return self.TryConnect(signal, Callable.From(action));
    }
    public static Error TryDisconnect(this GodotObject self, StringName signal, Action action) {
        return self.TryDisconnect(signal, Callable.From(action));
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DamselsGambit.Util;

static class NodeExtensions
{
    public static bool IsValid(this GodotObject self) => GodotObject.IsInstanceValid(self);

    // If node ready, call immediately. Else defer until node is ready
    public static void OnReady(this Node self, Action action) {
        if (!self.IsValid()) throw new NullReferenceException($"OnReady called on invalid node instance {self}");
        if (self.IsNodeReady()) action(); else self.Connect(Node.SignalName.Ready, Callable.From(action), (uint)GodotObject.ConnectFlags.OneShot);
    }

    public static void OnReady<TNode>(this TNode self, Action<TNode> action) where TNode : Node {
        if (!self.IsValid()) throw new NullReferenceException($"OnReady called on invalid node instance {self}");
        if (self.IsNodeReady()) action(self); else self.Connect(Node.SignalName.Ready, Callable.From(() => action(self)), (uint)GodotObject.ConnectFlags.OneShot);
    }

    public static void AddOwnedChild(this Node self, Node child, bool force = false) {
        if (force && child.GetParent() is Node parent) parent.RemoveChild(child);
        self.AddChild(child); child.Owner = self;
    }

    public static Godot.Collections.Array<Node> GetInternalChildren(this Node self) {
        var publicChildren = self.GetChildren();
        var internalChildren = self.GetChildren(true).Where(x => !publicChildren.Contains(x));
        return [..internalChildren];
    }

    public static TNode FindChildOfType<TNode>(this Node self, bool recursive = true) where TNode : Node {
        foreach (var child in self.GetChildren()) { if (child is TNode) { return child as TNode; } }
        if (recursive) {
            foreach (var child in self.GetChildren()) {
                var result = child.FindChildOfType<TNode>(recursive);
                if (result is not null) { return result; }
            }
        }
        return null;
    }
    public static Godot.Collections.Array<TNode> FindChildrenOfType<[MustBeVariant]TNode>(this Node self, bool recursive = true) where TNode : Node {
        var validChildren = new List<TNode>();
        foreach (var child in self.GetChildren()) {
            if (child is TNode) { validChildren.Add(child as TNode); }
            if (recursive) validChildren.AddRange(child.FindChildrenOfType<TNode>(recursive));
        }
        return [.. validChildren];
    }

    public static TNode FindChildWhere<TNode>(this Node self, Func<TNode, bool> predicate, bool recursive = true) where TNode : Node {
        foreach (var child in self.GetChildren()) if (child is TNode && predicate(child as TNode)) return child as TNode;
        if (recursive) {
            foreach (var child in self.GetChildren()) {
                var result = child.FindChildWhere(predicate, recursive);
                if (result is not null) { return result; }
            }
        }
        return null;
    }
    public static Node FindChildWhere(this Node self, Func<Node, bool> predicate, bool recursive = true) => self.FindChildWhere<Node>(predicate, recursive);

    public static Godot.Collections.Array<TNode> FindChildrenWhere<[MustBeVariant]TNode>(this Node self, Func<TNode, bool> predicate, bool recursive = true) where TNode : Node {
        var validChildren = new List<TNode>();
        foreach (var child in self.GetChildren()) {
            if (child is TNode && predicate(child as TNode)) validChildren.Add(child as TNode);
            if (recursive) validChildren.AddRange(child.FindChildrenWhere(predicate, recursive));
        }
        return [..validChildren];
    }
    public static Godot.Collections.Array<Node> FindChildrenWhere(this Node self, Func<Node, bool> predicate, bool recursive = true) => self.FindChildrenWhere<Node>(predicate, recursive);

    public static TNode FindParentOfType<TNode>(this Node self) where TNode : Node {
        var parent = self.GetParent();
        if (parent is TNode) { return parent as TNode; }
        return parent?.FindParentOfType<TNode>();
    }
    public static Godot.Collections.Array<TNode> FindParentsOfType<[MustBeVariant]TNode>(this Node self) where TNode : Node {
        var validParents = new List<TNode>();
        var parent = self.GetParent();
        if (parent is TNode) validParents.Add(parent as TNode);
        if (parent is not null) validParents.AddRange(parent.FindParentsOfType<TNode>());
        return [.. validParents];
    }
    
    public static TNode FindParentWhere<TNode>(this Node self, Func<TNode, bool> predicate) where TNode : Node {
        var parent = self.GetParent();
        if (parent is TNode && predicate(parent as TNode)) return parent as TNode;
        return parent?.FindParentWhere(predicate);
    }
    public static Node FindParentWhere(this Node self, Func<Node, bool> predicate) => self.FindParentWhere<Node>(predicate);

    public static Godot.Collections.Array<TNode> FindParentsWhere<[MustBeVariant]TNode>(this Node self, Func<TNode, bool> predicate) where TNode : Node {
        var validParents = new List<TNode>();
        var parent = self.GetParent();
        if (parent is TNode && predicate(parent as TNode)) validParents.Add(parent as TNode);
        if (parent is not null) validParents.AddRange(parent.FindParentsWhere(predicate));
        return [.. validParents];
    }
    public static Godot.Collections.Array<Node> FindParentsWhere(this Node self, Func<Node, bool> predicate) => self.FindParentsWhere<Node>(predicate);

    public static int FindDistanceToParent(this Node self, Node ancestor) {
        var root = self.GetTree().Root;
        int working = 0;
        Node workingNode = self;
        while (workingNode != ancestor) {
            if (workingNode == root || workingNode is null) return -1;
            working++;
            workingNode = workingNode.GetParent();
        }
        return working;
    }
    public static int FindDistanceToChild(this Node self, Node child) => child.FindDistanceToParent(self);

    public static Godot.Collections.Array<Node> FindAncestorChainTo(this Node self, Node ancestor, bool includeSelf = true, bool includeTarget = false) {
        var ancestorChain = new List<Node>();
        if (includeSelf) ancestorChain.Add(self);
        bool ancestorFound = false;
        void FindChainRecursive(Node current) {
            var parent = current.GetParent();
            ancestorFound |= parent == ancestor;
            if (parent is null) return;
            if (parent != ancestor) {
                ancestorChain.Add(parent);
                FindChainRecursive(parent);
            }
            else if (includeTarget) ancestorChain.Add(parent);
        }
        FindChainRecursive(self);
        if (ancestorFound) return [..ancestorChain];
        return [];
    }
    public static Godot.Collections.Array<Node> FindChildChainTo(this Node self, Node child, bool includeSelf = false, bool includeTarget = true) {
        var ancestorChain = child.FindAncestorChainTo(self, includeTarget, includeSelf);
        ancestorChain.Reverse();
        return ancestorChain;
    }

    public static Godot.Collections.Array<Node> GetSelfAndChildren(this Node self) {
        var array = new Godot.Collections.Array<Node> { self };
        array.AddRange(self.GetChildren());
        return array;
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
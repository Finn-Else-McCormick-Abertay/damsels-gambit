using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DamselsGambit.Util;

// Helper methods for common actions related to nodes, such as checking whether an instance is valid or deferring until the node is ready
static class NodeExtensions
{
    /// <summary>
    /// Returns true if instance is a valid <see cref="GodotObject"/> (e.g. has not been deleted from memory).
    /// <para> Shorthand for <see cref="GodotObject.IsInstanceValid(GodotObject?)"/>  </para>
    /// </summary>
    /// <returns>If the instance is a valid object.</returns>
    public static bool IsValid(this GodotObject self) => GodotObject.IsInstanceValid(self);

    /// <summary>
    /// Returns true if instance is not a valid <see cref="GodotObject"/> (e.g. has been deleted from memory).
    /// <para> Shorthand for !<see cref="GodotObject.IsInstanceValid(GodotObject?)"/>  </para>
    /// </summary>
    /// <returns>If the instance is not a valid object.</returns>
    public static bool IsInvalid(this GodotObject self) => !GodotObject.IsInstanceValid(self);

    /// <summary> If node ready, call immediately. Otherwise defer call until node is ready. </summary>
    /// <param name="callable">Callable to be run.</param>
    /// <exception cref="NullReferenceException">Thrown if node is invalid.</exception>
    public static void OnReady(this Node self, Callable callable) {
        if (!self.IsValid()) throw new NullReferenceException($"OnReady called on invalid node instance {self}");
        if (self.IsNodeReady()) callable.Call(); else if (!self.IsConnected(Node.SignalName.Ready, callable)) self.Connect(Node.SignalName.Ready, callable, (uint)GodotObject.ConnectFlags.OneShot);
    }

    /// <inheritdoc cref="OnReady(Node, Callable)"/>
    /// <param name="action">Action to be run.</param>
    public static void OnReady(this Node self, Action action) => self.OnReady(Callable.From(action));
    
    /// <inheritdoc cref="OnReady(Node, Callable)"/>
    /// <param name="action">Action to be run. Takes parameter of node type, to which the node in question will be passed.</param>
    public static void OnReady<TNode>(this TNode self, Action<TNode> action) where TNode : Node => self.OnReady(Callable.From(() => action?.Invoke(self)));

    /// <summary>
    /// <para>Add child which is owned by the current scene (or the parent if used at runtime).</para>
    /// <para>If node already has parent and <paramref name="force"/> is <see langword="true"/>, node will first be removed from parent.</para>
    /// </summary>
    public static void AddOwnedChild(this Node self, Node child, bool force = false) {
        if (force && child.GetParent() is Node parent) parent.RemoveChild(child);
        self.AddChild(child); child.Owner = Engine.IsEditorHint() ? EditorInterface.Singleton.GetEditedSceneRoot() : self;
    }

    /// <summary> Returns all direct internal children of this node. </summary>
    public static Godot.Collections.Array<Node> GetInternalChildren(this Node self) {
        var publicChildren = self.GetChildren();
        var internalChildren = self.GetChildren(true).Where(x => !publicChildren.Contains(x));
        return [..internalChildren];
    }

    /// <summary>
    /// <para>Find first <typeparamref name="TNode"/> below this node in the hierarchy.</para>
    /// <para>If <paramref name="recursive"/> is <see langword="true"/>, searches all descendants. If <see langword="false"/>, searches only direct children.</para>
    /// </summary>
    /// <returns>First descendant of type <typeparamref name="TNode"/>, or <see langword="null"/> if none exist.</returns>
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

    /// <summary>
    /// <para>Find all <typeparamref name="TNode"/> nodes below this node in the hierarchy.</para>
    /// <para>If <paramref name="recursive"/> is <see langword="true"/>, searches all descendants. If <see langword="false"/>, searches only direct children.</para>
    /// </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <typeparamref name="TNode"/></returns>
    public static Godot.Collections.Array<TNode> FindChildrenOfType<[MustBeVariant]TNode>(this Node self, bool recursive = true) where TNode : Node {
        var validChildren = new List<TNode>();
        foreach (var child in self.GetChildren()) {
            if (child is TNode) { validChildren.Add(child as TNode); }
            if (recursive) validChildren.AddRange(child.FindChildrenOfType<TNode>(recursive));
        }
        return [.. validChildren];
    }

    /// <summary>
    /// <para>Find first <typeparamref name="TNode"/> below this node in the hierarchy for which <paramref name="predicate"/> returns true.</para>
    /// <para>If <paramref name="recursive"/> is <see langword="true"/>, searches all descendants. If <see langword="false"/>, searches only direct children.</para>
    /// </summary>
    /// <returns>First valid descendant, or <see langword="null"/> if none exist.</returns>
    public static TNode FindChildWhere<TNode>(this Node self, Func<TNode, bool> predicate, bool recursive = true) where TNode : Node {
        foreach (var child in self.GetChildren()) if (child is TNode tChild && predicate(tChild)) return tChild;
        if (recursive) {
            foreach (var child in self.GetChildren()) {
                var result = child.FindChildWhere(predicate, recursive);
                if (result is not null) { return result; }
            }
        }
        return null;
    }
    /// <inheritdoc cref="FindChildWhere<TNode>(Node, Func<TNode, bool>, bool)"/>
    public static Node FindChildWhere(this Node self, Func<Node, bool> predicate, bool recursive = true) => self.FindChildWhere<Node>(predicate, recursive);

    /// <summary>
    /// <para>Find all <typeparamref name="TNode"/> nodes below this node in the hierarchy for which <paramref name="predicate"/> returns true.</para>
    /// <para>If <paramref name="recursive"/> is <see langword="true"/>, searches all descendants. If <see langword="false"/>, searches only direct children.</para>
    /// </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <typeparamref name="TNode"/></returns>
    public static Godot.Collections.Array<TNode> FindChildrenWhere<[MustBeVariant]TNode>(this Node self, Func<TNode, bool> predicate, bool recursive = true) where TNode : Node {
        var validChildren = new List<TNode>();
        foreach (var child in self.GetChildren()) {
            if (child is TNode tChild && predicate(tChild)) validChildren.Add(tChild);
            if (recursive) validChildren.AddRange(child.FindChildrenWhere(predicate, recursive));
        }
        return [..validChildren];
    }
    /// <inheritdoc cref="FindChildrenWhere<TNode>(Node, Func<TNode, bool>, bool)"/>
    public static Godot.Collections.Array<Node> FindChildrenWhere(this Node self, Func<Node, bool> predicate, bool recursive = true) => self.FindChildrenWhere<Node>(predicate, recursive);

    /// <summary> Find first <typeparamref name="TNode"/> above this node in the hierarchy. </summary>
    /// <returns>First ancestor of type <typeparamref name="TNode"/>, or <see langword="null"/> if none exist.</returns>
    public static TNode FindParentOfType<TNode>(this Node self) where TNode : Node {
        var parent = self.GetParent();
        if (parent is TNode tParent) { return tParent; }
        return parent?.FindParentOfType<TNode>();
    }

    /// <summary> Find all <typeparamref name="TNode"/> nodes above this node in the hierarchy. </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <typeparamref name="TNode"/></returns>
    public static Godot.Collections.Array<TNode> FindParentsOfType<[MustBeVariant]TNode>(this Node self) where TNode : Node {
        var validParents = new List<TNode>();
        var parent = self.GetParent();
        if (parent is TNode tParent) validParents.Add(tParent);
        if (parent is not null) validParents.AddRange(parent.FindParentsOfType<TNode>());
        return [.. validParents];
    }

    /// <summary> Find first <typeparamref name="TNode"/> above this node in the hierarchy for which <paramref name="predicate"/> returns true. </summary>
    /// <returns>First valid ancestor of type <typeparamref name="TNode"/>, or <see langword="null"/> if none exist.</returns>
    public static TNode FindParentWhere<TNode>(this Node self, Func<TNode, bool> predicate) where TNode : Node {
        var parent = self.GetParent();
        if (parent is TNode tParent && predicate(tParent)) return tParent;
        return parent?.FindParentWhere(predicate);
    }
    /// <<inheritdoc cref="FindParentWhere<TNode>(Node, Func<Node,bool>)"/> 
    public static Node FindParentWhere(this Node self, Func<Node, bool> predicate) => self.FindParentWhere<Node>(predicate);

    /// <summary> Find all <typeparamref name="TNode"/> nodes above this node in the hierarchy for which <paramref name="predicate"/> returns true. </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <typeparamref name="TNode"/></returns>
    public static Godot.Collections.Array<TNode> FindParentsWhere<[MustBeVariant]TNode>(this Node self, Func<TNode, bool> predicate) where TNode : Node {
        List<TNode> validParents = [];
        var parent = self.GetParent();
        if (self.GetParent() is TNode tParent && predicate(tParent)) validParents.Add(tParent);
        if (parent is not null) validParents.AddRange(parent.FindParentsWhere(predicate));
        return [.. validParents];
    }
    /// <<inheritdoc cref="FindParentsWhere<TNode>(Node, Func<Node,bool>)"/> 
    public static Godot.Collections.Array<Node> FindParentsWhere(this Node self, Func<Node, bool> predicate) => self.FindParentsWhere<Node>(predicate);

    /// <summary> Find number of nodes from self to <paramref name="ancestor"/> in tree, including self but not <paramref name="ancestor"/>. <para>Eg: if <paramref name="ancestor"/> is direct parent, returns 1.</para> </summary>
    /// <returns>Distance from this node to <paramref name="ancestor"/>, or -1 if it cannot be reached.</returns>
    public static int FindDistanceToParent(this Node self, Node ancestor) {
        var root = self.GetTree().Root;
        int runningCount = 0; Node workingNode = self;
        while (workingNode != ancestor) {
            if (workingNode == root || workingNode is null) return -1;
            runningCount++;
            workingNode = workingNode.GetParent();
        }
        return runningCount;
    }

    /// <summary> Find number of nodes from self to <paramref name="child"/> in tree, including <paramref name="child"/> but not self. <para>Eg: if <paramref name="child"/> is direct child, returns 1.</para> </summary>
    /// <returns>Distance from this node to <paramref name="child"/>, or -1 if it cannot be reached.</returns>
    public static int FindDistanceToChild(this Node self, Node child) => child.FindDistanceToParent(self);

    /// <summary> Get chain of nodes connecting self to <paramref name="ancestor"/> in tree, or empty array if cannot be reached. </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <see cref="Node"/></returns>
    public static Godot.Collections.Array<Node> FindAncestorChainTo(this Node self, Node ancestor, bool includeSelf = true, bool includeTarget = false) {
        List<Node> ancestorChain = includeSelf ? [self] : [];
        bool ancestorFound = false;
        void FindChainRecursive(Node current) {
            var parent = current.GetParent();
            ancestorFound |= parent == ancestor;
            if (parent is null) return;
            if (parent != ancestor) { ancestorChain.Add(parent); FindChainRecursive(parent); } else if (includeTarget) ancestorChain.Add(parent);
        }
        FindChainRecursive(self);
        return ancestorFound ? [..ancestorChain] : [];
    }
    
    /// <summary> Get chain of nodes connecting self to <paramref name="descendant"/> in tree, or empty array if cannot be reached. </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <see cref="Node"/></returns>
    public static Godot.Collections.Array<Node> FindChildChainTo(this Node self, Node descendant, bool includeSelf = false, bool includeTarget = true) => [..descendant.FindAncestorChainTo(self, includeTarget, includeSelf).Reversed()];

    /// <summary> Get array containing self and direct children, for convenience. </summary>
    /// <returns><see cref="Godot.Collections.Array"/> of <see cref="Node"/></returns>
    public static Godot.Collections.Array<Node> GetSelfAndChildren(this Node self) => [self, ..self.GetChildren()];
}
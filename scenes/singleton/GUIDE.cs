using Godot;
using System;
using DamselsGambit.Util;
using System.Linq;
using System.Collections.Generic;
using Bridge;

namespace DamselsGambit;

// Setup by GameManager - bridge to GUIDE GDScript autoload
public sealed class GUIDE
{
    private static GUIDE Instance { get; } = new();
    private GUIDE () {}

    private Node _guide;

    public static bool Initialise(Node GUIDE) {
        if (Instance._guide is not null) throw new Exception("Attempted to init GUIDE more than once");
        Instance._guide = GUIDE;

        GUIDE.Call("enable_remapping_context", MappingContextDefault.InnerObject);

        return Instance._guide is not null;
    }
    
    public static readonly GUIDEMappingContext MappingContextDefault = GUIDEMappingContext.From(ResourceLoader.Load("res://assets/input/context_default.tres", "GUIDEMappingContext"));
}
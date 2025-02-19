using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DamselsGambit.Dialogue;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public static CardGameController CardGameController { 
        get {
            if (field is not null && IsInstanceValid(field)) return field;
            CardGameController = Instance?.GetTree()?.Root?.FindChildOfType<CardGameController>();
            return field;
        } 
        private set;
    }

    public override void _EnterTree() { Instance = this; }
    public override void _Ready() {
        var mainMenu = Bridge.MainMenu.From(GetTree().Root.GetNode<Control>("Main_menu"));
        mainMenu.Ready += () => {
            mainMenu.StartButton.QueueFree();
        };
        //mainMenu.QueueFree();
        //if (GUIDE.Initialise(GetTree().Root.GetNode("GUIDE"))) { GUIDE.EnableMappingContext(GUIDE.MappingContextDefault); }
    }

    public void QuitToTitle() {
        DialogueManager.Instance.Reset();
        var mainNode = GetTree().Root.GetNode("Main");
        GetTree().Root.RemoveChild(mainNode); mainNode.QueueFree();

        var mainScene = ResourceLoader.Load<PackedScene>("res://scenes/main.tscn");
        var newMainNode = mainScene.Instantiate();
        GetTree().Root.AddChild(newMainNode);
        GetTree().Paused = false;

        CardGameController = GetTree().Root.FindChildOfType<CardGameController>();
    }
} 
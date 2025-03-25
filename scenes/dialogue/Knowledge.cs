using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;

namespace DamselsGambit.Dialogue;

public class Knowledge
{
    public event Action OnChanged;

    public bool Knows(string id) => _facts.Contains(NormaliseFactId(id));
    public ReadOnlyCollection<string> KnownFacts => _facts.ToList().AsReadOnly();

    // Gain fact, trigger OnChanged
    public void Learn(string id) { if (_facts.Add(NormaliseFactId(id))) OnChanged?.Invoke(); }

    // Lose fact, trigger OnChanged
    public void Unlearn(string id) { if (_facts.Remove(NormaliseFactId(id))) OnChanged?.Invoke(); }

    // Clear all facts, trigger OnChanged
    public void Reset() { _facts.Clear(); OnChanged?.Invoke(); }

    private readonly HashSet<string> _facts = [];
    private static string NormaliseFactId(string id) => string.Join("", id.Replace('\\', '/').Split('/').Select(x => x.StripEdges().ToLower().Replace(' ', '_')));
}
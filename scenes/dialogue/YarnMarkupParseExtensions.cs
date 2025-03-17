using System.Collections.Generic;
using System.Linq;
using System.Text;
using DamselsGambit.Util;
using Godot;

namespace DamselsGambit.Dialogue;

public static class YarnMarkupParseExtensions
{
    public static string AsBBCode(this Yarn.Markup.MarkupParseResult result) {

        Dictionary<int, List<string>> insertions = [];
        Dictionary<int, int> removals = [];
        
        foreach (var (Position, Length, Name, Properties, UseTag, UseText, ReplacementText) in result.Attributes.Select(InterpretAttribute)) {
            if (UseTag) {
                var sb = new StringBuilder();
                sb.Append('[').Append(Name);
                if (Properties.Any(x => x.Key == Name)) sb.Append('=').Append(Properties.Where(x => x.Key == Name).SingleOrDefault().Value);
                if (Properties.Any(x => x.Key != Name)) sb.Append(' ').AppendJoin(' ', Properties.Where(x => x.Key != Name).Select(x => $"{x.Key}={x.Value}"));
                sb.Append(']');

                insertions.TryAdd(Position, []); insertions[Position].Add(sb.ToString());
                insertions.TryAdd(Position + Length, []); insertions[Position + Length].Add($"[/{Name}]");
            }
            else if (ReplacementText is not null) { insertions.TryAdd(Position, []); insertions[Position].Add(ReplacementText); }

            if (!UseText) removals.Add(Position, Length);
        }

        var removalEndPoints = removals.Select(pair => pair.Key + pair.Value);

        var text = result.Text;
        int runningTotal = 0; bool isRemoving = false;
        foreach (var index in insertions.Keys.Concat(removals.Keys).Concat(removalEndPoints).Order()) {
            if (!isRemoving && removals.TryGetValue(index, out int removalLength)) {
                text = text.Remove(index + runningTotal, removalLength);
                runningTotal -= removalLength;
                isRemoving = true;
            }
            if (removalEndPoints.Contains(index)) isRemoving = false;

            if (!isRemoving) insertions.GetValueOrDefault(index)?.ForEach(insertion => {
                text = text.Insert(index + runningTotal, insertion);
                runningTotal += insertion.Length;
            });
        }

        return text;
    }

    private static (int Position, int Length, string Name, Dictionary<string, string> Properties, bool UseTag, bool UseText, string ReplacementText) InterpretAttribute(Yarn.Markup.MarkupAttribute attribute) {
        (int Position, int Length, string Name, Dictionary<string, string> Properties, bool UseTag, bool UseText, string ReplacementText) tuple
            = (attribute.Position, attribute.Length, attribute.Name, attribute.Properties.Select(x => (x.Key, x.Value.ToString())).ToDictionary(), true, true, null);
        if (tuple.Properties.TryGetValue("color", out string val)) {
            tuple.Properties["color"] = val switch {
                _ when val.MatchN("action") => "6d85ff",
                _ when val.MatchN("topic") => "d67931",
                _ when val.MatchN("love") => "d78799",
                _ when val.MatchN("hate") => "2b308b",
                _ => val
            };
        }
        if (tuple.Name.Match("knowledge")) {
            tuple.UseTag = false;
            Dictionary<string, bool> knowledgeRequirements = [];
            if (tuple.Properties.TryGetValue("knowledge", out string knowledgeString)) {
                foreach (var arg in knowledgeString.Split(' ', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)) {
                    if (arg.StartsWith('!')) knowledgeRequirements.TryAdd(arg.StripFront('!'), false); else knowledgeRequirements.TryAdd(arg, true);
                }
            }
            foreach (var (propertyName, propertyVal) in tuple.Properties.Where(x => !x.Key.IsAnyOf([ "knowledge", "required" ]))) knowledgeRequirements.TryAdd(propertyName, bool.Parse(propertyVal));

            bool all = tuple.Properties.GetValueOrDefault("required") switch { null => true, "all" => true, "any" => false, _ => throw new System.Exception($"Invalid markup argument [knowledge required={tuple.Properties.GetValueOrDefault("required")}]") };

            tuple.UseText = all ? true : false;
            foreach (var (id, state) in knowledgeRequirements) {
                var thisFactState = DialogueManager.Knowledge.Knows(id) == state;
                tuple.UseText = all ? tuple.UseText && thisFactState : tuple.UseText || thisFactState;
            }
        }
        if (tuple.Name.Match("br")) {
            tuple.UseTag = false;
            tuple.ReplacementText = "\n";
        }
        return tuple;
    }
}
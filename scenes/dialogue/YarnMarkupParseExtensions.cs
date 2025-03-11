using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace DamselsGambit.Dialogue;

public static class YarnMarkupParseExtensions
{
    public static string AsBBCode(this Yarn.Markup.MarkupParseResult result) {

        Dictionary<int, List<string>> insertions = [];
        
        foreach (var attribute in result.Attributes.Select(InterpretAttribute)) {
            var openerSb = new StringBuilder();
            openerSb.Append('[').Append(attribute.Name);
            if (attribute.Properties.Any(x => x.Key == attribute.Name)) openerSb.Append('=').Append(attribute.Properties.Where(x => x.Key == attribute.Name).SingleOrDefault().Value);
            if (attribute.Properties.Any(x => x.Key != attribute.Name)) openerSb.Append(' ').AppendJoin(' ', attribute.Properties.Where(x => x.Key != attribute.Name).Select(x => $"{x.Key}={x.Value}"));
            openerSb.Append(']');

            insertions.TryAdd(attribute.Position, []); insertions[attribute.Position].Add(openerSb.ToString());
            insertions.TryAdd(attribute.Position + attribute.Length, []); insertions[attribute.Position + attribute.Length].Add($"[/{attribute.Name}]");
        }

        var text = result.Text;
        int runningTotal = 0;
        foreach (var (index, tags) in insertions.OrderBy(x => x.Key)) foreach (var tag in tags) { text = text.Insert(index + runningTotal, tag); runningTotal += tag.Length; }

        return text;
    }

    private static (int Position, int Length, string Name, Dictionary<string, string> Properties) InterpretAttribute(Yarn.Markup.MarkupAttribute attribute) {
        (int Position, int Length, string Name, Dictionary<string, string> Properties) tuple = (attribute.Position, attribute.Length, attribute.Name, attribute.Properties.Select(x => (x.Key, x.Value.ToString())).ToDictionary());
        if (tuple.Properties.TryGetValue("color", out string val)) {
            tuple.Properties["color"] = val switch {
                _ when val.MatchN("action") => "6d85ff",
                _ when val.MatchN("topic") => "d67931",
                _ when val.MatchN("love") => "d78799",
                _ when val.MatchN("hate") => "2b308b",
                _ => val
            };
        }
        return tuple;
    }
}
using Godot;
using System;
using System.Collections.Generic;
using DamselsGambit.Util;
using System.Linq;
using System.Collections.ObjectModel;

namespace DamselsGambit;

#nullable enable

public class TaggedNodePath
{
    private static readonly char TagDelimiter = '$';
    private static readonly char EvaluationPlaceholder = '\uFFFC';
    
    public TaggedNodePath(string nodePath, params IEnumerable<(string StartingArg, Func<string, bool>)> predicates) {
        RawText = nodePath ?? "";
        Dictionary<string, string> tags = [];
        if (nodePath?.Contains(TagDelimiter) ?? false) {
            var tagSplit = RawText.Split(TagDelimiter, StringSplitOptions.TrimEntries);
            tags.Add("root", tagSplit[0]);
            foreach (var tag in tagSplit[1..]) {
                if (tag.StartsWith("if")) {
                    if (tag.Contains("then") || tag.Contains("else")) {
                        var result = EvaluateIfThenElse(tag, predicates);
                        if (result != $"{true}" && result is not null) tags["root"] = result;
                    }
                    else if (TryEvaluateCondition(tag.StripFront("if").Trim(), out bool result, predicates) && !result) tags["root"] = "";
                }
                else if (tag.Contains('=')) { var split = tag.Split('=', 2, StringSplitOptions.TrimEntries); tags[split.First()] = split.Last(); }
                else tags[tag] = "";
            }
        }
        else tags.Add("root", nodePath?.Trim() ?? "");
        Tags = tags.AsReadOnly();
    }

    public readonly string RawText;

    public NodePath RootPath => new(Tags["root"]);
    public readonly ReadOnlyDictionary<string, string> Tags;

    public bool HasTag(string tag, bool caseSensitive = false) => Tags.Any(x => x.Key.Match(tag, caseSensitive));
    public string? GetTag(string tag, bool caseSensitive = false) {
        var matchingTags = Tags.Where(x => x.Key.Match(tag, caseSensitive));
        if (matchingTags.Count() > 1) Console.Warning($"Multiple tags matched case {caseSensitive switch { true => "", false => "in" }}sensitive query for '{tag}'. All after first were ignored.");
        if (!matchingTags.Any()) return null;
        return matchingTags.First().Value;
    }

    public bool? GetFlag(string tag, bool caseSensitive = false) {
        if (GetTag(tag, caseSensitive) is string condition) {
            if (condition.IsNullOrWhitespace()) return true;
            try { return EvaluateCondition(condition); } catch {}
        }
        return null;
    }
    public bool GetFlagOrDefault(string tag, bool caseSensitive = false) => GetFlag(tag, caseSensitive) ?? default;

    public bool? GetComplexFlag(string flagType, string flag, bool caseSensitive = false) {
        if (GetFlag($"{flagType} {flag}", caseSensitive) is bool val) return val;
        if (GetTag(flagType, caseSensitive) is string tagString) return tagString.Match(flag, caseSensitive);
        return null;
    }
    public bool GetComplexFlagOrDefault(string flagType, string flag, bool caseSensitive = false) => GetComplexFlag(flagType, flag, caseSensitive) ?? default;

    public static string? EvaluateIfThenElse(string? text, params IEnumerable<(string StartingArg, Func<string, bool>)> predicates) {
        if (text is null || !text.StartsWith("if")) return null;
        text = text.StripFront("if").Trim();

        string? elseStr = null;
        if (text.Contains("else")) {
            var split = text.Split("else", 2);
            text = split[0].Trim();
            elseStr = split[1].Trim();
        }
        
        string? thenStr = null;
        if (text.Contains("then")) {
            var split = text.Split("then", 2);
            text = split[0].Trim();
            thenStr = split[1].Trim();
        }

        //Console.Info($"if ({text}){(thenStr is null ? "" : $" then {thenStr}")}{(elseStr is null ? "" : $" else {elseStr}")} Predicates: {predicates.Select(x => x.StartingArg).ToPrettyString()}");

        if (TryEvaluateCondition(text, out bool result, predicates)) {
            string? relevantString = result switch { true => thenStr, false => elseStr };
            //Console.Info($" - eval({text}) => {result} => {relevantString.ToPrettyString()}");
            if (relevantString is null) return $"{true}";
            return EvaluateIfThenElse(relevantString, predicates) ?? relevantString;
        }
        return null;
    }
    
    public static bool TryEvaluateCondition(string condition, out bool result, params IEnumerable<(string StartingArg, Func<string, bool>)> predicates) {
        try {
            result = EvaluateCondition(condition, predicates);
            return true;
        }
        catch (Exception e) { Console.Warning($"Condition parse error: {e.Message}\n{e.StackTrace}"); }
        result = false; return false;
    }

    public static bool EvaluateCondition(string condition, params IEnumerable<(string StartingArg, Func<string, bool>)> predicates) {
        condition = condition.Trim().StripFront('(').StripBack(')').Trim();

        //Console.Info($"Evaluating {condition} with predicates {predicates.Select(x => x.StartingArg).ToPrettyString()}");

        List<(int Start, int End)> nonOverlappingBracedConditions = [];
        Stack<int> openBrackets = [];
        foreach (int i in RangeOf<int>.UpTo(condition.Length)) {
            switch (condition[i]) {
                case '(': { openBrackets.Push(i); } break;
                case ')': {
                    if (openBrackets.Count == 0) throw new Exception("Closing bracket without matching opening bracket.");
                    int correspondingOpeningBracket = openBrackets.Pop();
                    if (openBrackets.Count == 0) nonOverlappingBracedConditions.Add((correspondingOpeningBracket, i));
                } break;
                default: break;
            }
        }

        string initialCondition = condition;
        Dictionary<int, string> replacedConditions = []; int lengthDifference = 0;
        foreach (var (index, pair) in nonOverlappingBracedConditions.Index()) {
            string replacementText = $"{EvaluationPlaceholder}{index}{EvaluationPlaceholder}";
            string conditionText = initialCondition[pair.Start..(pair.End + 1)];
            replacedConditions.Add(index, conditionText);

            condition = $"{condition[..(pair.Start - lengthDifference)]}{replacementText}{((pair.End + 1 - lengthDifference < condition.Length) ? condition[(pair.End + 1 - lengthDifference)..] : "")}";
            lengthDifference += replacementText.Length - conditionText.Length;
        }

        string ConvertBack(string cond) {
            bool isInsideReplacement = false;
            int prevPlaceholder = -1, thisPlaceholder = -1;
            do {
                thisPlaceholder = cond.Find(EvaluationPlaceholder, prevPlaceholder + 1);

                if (isInsideReplacement) {
                    var indexString = cond[(prevPlaceholder+1)..thisPlaceholder];
                    if (!int.TryParse(indexString, out int index)) throw new Exception($"Failed to undo brace replacement. Input cannot contain evaluation placeholder \\u{System.Text.RegularExpressions.Regex.Unescape($"{EvaluationPlaceholder}")}.");

                    var replacement = replacedConditions.GetValueOrDefault(index);
                    cond = $"{cond[..prevPlaceholder]}{replacement}{(thisPlaceholder + 1 < cond.Length ? cond[(thisPlaceholder + 1)..] : "")}";
                    prevPlaceholder = -1;
                }
                isInsideReplacement = !isInsideReplacement;
            } while(thisPlaceholder >= 0);
            return cond;
        }

        // Handle or
        var orSplit = condition.Split("||", StringSplitOptions.TrimEntries);
        if (orSplit.Length > 1) { foreach (string cond in orSplit) { if (EvaluateCondition(ConvertBack(cond), predicates)) return true; } }
        
        // Handle and
        var andSplit = condition.Split("&&", StringSplitOptions.TrimEntries);
        if (andSplit.Length > 1) { foreach (string cond in andSplit) { if (!EvaluateCondition(ConvertBack(cond), predicates)) return false; } }
        
        condition = ConvertBack(condition);

        // Handle not
        if (condition.StartsWith('!')) return !EvaluateCondition(condition.StripFront('!'), predicates);

        // Handle parsing true and false directly
        if (bool.TryParse(condition, out bool booleanVar)) return booleanVar;

        // Situation-specific cases
        foreach (var (startingArg, predicate) in predicates) {
            if (startingArg.IsNullOrWhitespace()) { if (predicate(condition)) return true; }
            else if (condition.StartsWith(startingArg)) {
                var result = predicate(condition.StripFront(startingArg).Trim());
                //Console.Info($"Pred '{startingArg}' => {result}");
                if (result) return true;
            } 
        }

        return false;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DamselsGambit;

// Setup by GameManager - bridge to LimboConsole GDScript autoload
public sealed class Console
{
    private static Console Instance { get; } = new();
    private Console () {}

    private Node _limboConsole;

    public static bool Initialise(Node limboConsole) {
        if (Instance._limboConsole is not null) throw new Exception("Attempted to init console more than once");
        Instance._limboConsole = limboConsole;
        return Instance._limboConsole is not null;
    }

    private static Variant Call(StringName method, params Variant[] args) {
        if (Instance._limboConsole is null) throw new NullReferenceException("LimboConsole was null");
        return Instance._limboConsole.Call(method, args);
    }

    public static bool Visible { get => Call(Methods.is_visible).AsBool(); }

    public static void Open() => Call(Methods.open_console);
    public static void Close() => Call(Methods.close_console);
    public static void Toggle() => Call(Methods.toggle_console);

    public static void Clear() => Call(Methods.clear_console);

    public static void Info(string msg) => Call(Methods.info, msg);
    public static void Error(string msg) => Call(Methods.error, msg);
    public static void Warn(string msg) => Call(Methods.warn, msg);
    public static void Debug(string msg) => Call(Methods.debug, msg);

    public static void RegisterCommand(string name, string description, Callable callable) => Call(Methods.register_command, callable, name, description);
    public static void RegisterCommand(string name, string description, Callable callable, Dictionary<int, Callable> autocompleteArgs) {
        RegisterCommand(name, description, callable); if (autocompleteArgs is not null) { foreach ((var index, var func) in autocompleteArgs) { AddArgumentAutocompleteSource(name, index, func); } }
    }
    public static void RegisterCommand(string name, string description, Callable callable, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) {
        RegisterCommand(name, description, callable); if (autocompleteArgs is not null) { foreach ((var index, var func) in autocompleteArgs) { AddArgumentAutocompleteSource(name, index, func); } }
    }
    public static void RegisterCommand(string name, string description, Callable callable, Dictionary<int, IEnumerable<string>> autocompleteArgs) {
        RegisterCommand(name, description, callable); if (autocompleteArgs is not null) { foreach ((var index, var strings) in autocompleteArgs) { AddArgumentAutocompleteSource(name, index, strings); } }
    }

    public static void UnregisterCommand(string name) => Call(Methods.unregister_command, name);

    public static void AddArgumentAutocompleteSource(string command, int argIndex, Callable source) => Call(Methods.add_argument_autocomplete_source, command, argIndex, source);
    public static void AddArgumentAutocompleteSource(string command, int argIndex, Func<Godot.Collections.Array> source) => Call(Methods.add_argument_autocomplete_source, command, argIndex, Callable.From(source));
    public static void AddArgumentAutocompleteSource(string command, int argIndex, IEnumerable<string> source) => Call(Methods.add_argument_autocomplete_source, command, argIndex, Callable.From(() => new Godot.Collections.Array(source.Select(x => Variant.From(x)))));

    public static bool HasCommand(string name) => Call(Methods.has_command, name).AsBool();

    public static string[] GetCommandNames() => Call(Methods.get_command_names).AsStringArray();
    public static string GetCommandDescription(string command) => Call(Methods.get_command_description, command).AsString();

    public static void AddAlias(string alias, string command) => Call(Methods.add_alias, alias, command);
    public static void RemoveAlias(string alias) => Call(Methods.remove_alias, alias);
    public static bool HasAlias(string alias) => Call(Methods.has_alias, alias).AsBool();

    public static Dictionary<string, string[]> GetAliases() {
        Dictionary<string, string[]> dict = [];
        Call(Methods.get_aliases).AsStringArray().ToList().ForEach(alias => dict.Add(alias, Call(Methods.get_alias_argv, alias).AsStringArray()));
        return dict;
    }

    public static void SetEvalVariable(string name, Variant value) => Call(Methods.add_eval_input, name, value);
    public static void SetEvalVariable<[MustBeVariant]T>(string name, T value) => Call(Methods.add_eval_input, name, Variant.From<T>(value));
    public static void RemoveEvalVariable(string name) => Call(Methods.remove_eval_input, name);

    public static string[] GetEvalVariableNames() => Call(Methods.get_eval_input_names).AsStringArray();
    public static Variant GetEvalVariableValue(string variable) {
        var names = GetEvalVariableNames();
        int index = Array.IndexOf(names, variable);
        if (index == -1) { return new Variant(); }
        return Call(Methods.get_eval_inputs).AsGodotArray().ElementAt(index);
    }

    public static void ExecuteCommand(string command, bool silent = false) => Call(Methods.execute_command, command, silent);
    public static void ExecuteScript(string path, bool silent = false) => Call(Methods.execute_script, path, silent);

    // God I wish there were a better way to do actions with variable arguments in C#

    public static void RegisterCommand(string name, string description, Action action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant] T0>(string name, string description, Action<T0> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1>(string name, string description, Action<T0, T1> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2>(string name, string description, Action<T0, T1, T2> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3>(string name, string description, Action<T0, T1, T2, T3> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4>(string name, string description, Action<T0, T1, T2, T3, T4> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5>(string name, string description, Action<T0, T1, T2, T3, T4, T5> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7> action) => RegisterCommand(name, description, Callable.From(action));
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action) => RegisterCommand(name, description, Callable.From(action));

    public static void RegisterCommand(string name, string description, Action action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant] T0>(string name, string description, Action<T0> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1>(string name, string description, Action<T0, T1> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2>(string name, string description, Action<T0, T1, T2> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3>(string name, string description, Action<T0, T1, T2, T3> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4>(string name, string description, Action<T0, T1, T2, T3, T4> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5>(string name, string description, Action<T0, T1, T2, T3, T4, T5> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action, Dictionary<int, Callable> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);

    public static void RegisterCommand(string name, string description, Action action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant] T0>(string name, string description, Action<T0> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1>(string name, string description, Action<T0, T1> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2>(string name, string description, Action<T0, T1, T2> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3>(string name, string description, Action<T0, T1, T2, T3> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4>(string name, string description, Action<T0, T1, T2, T3, T4> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5>(string name, string description, Action<T0, T1, T2, T3, T4, T5> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action, Dictionary<int, Func<Godot.Collections.Array>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    
    public static void RegisterCommand(string name, string description, Action action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant] T0>(string name, string description, Action<T0> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1>(string name, string description, Action<T0, T1> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2>(string name, string description, Action<T0, T1, T2> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3>(string name, string description, Action<T0, T1, T2, T3> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4>(string name, string description, Action<T0, T1, T2, T3, T4> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5>(string name, string description, Action<T0, T1, T2, T3, T4, T5> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    public static void RegisterCommand<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8>(string name, string description, Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action, Dictionary<int, IEnumerable<string>> autocompleteArgs) => RegisterCommand(name, description, Callable.From(action), autocompleteArgs);
    
    private static class Methods {
        public static StringName open_console = "open_console";
        public static StringName close_console = "close_console";
        public static StringName is_visible = "is_visible";
        public static StringName toggle_console = "toggle_console";
        public static StringName clear_console = "clear_console";
        
        public static StringName print_boxed = "print_boxed";
        public static StringName print_line = "print_line";

        public static StringName info = "info";
        public static StringName error = "error";
        public static StringName warn = "warn";
        public static StringName debug = "debug";
        
        public static StringName register_command = "register_command";
        public static StringName unregister_command = "unregister_command";
        public static StringName has_command = "has_command";
        public static StringName get_command_names = "get_command_names";
        public static StringName get_command_description = "get_command_description";

        public static StringName add_alias = "add_alias";
        public static StringName remove_alias = "remove_alias";
        public static StringName has_alias = "has_alias";
        public static StringName get_aliases = "get_aliases";
        public static StringName get_alias_argv = "get_alias_argv";

        public static StringName add_argument_autocomplete_source = "add_argument_autocomplete_source";

        public static StringName execute_command = "execute_command";
        public static StringName execute_script = "execute_script";

        public static StringName add_eval_input = "add_eval_input";
        public static StringName remove_eval_input = "remove_eval_input";
        public static StringName get_eval_input_names = "get_eval_input_names";
        public static StringName get_eval_inputs = "get_eval_inputs";
    }
}
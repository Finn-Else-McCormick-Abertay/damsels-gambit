using System;
using System.Collections.Generic;
using Godot;

namespace DamselsGambit.Util;

public static class SignalExtensions
{    
    public static Error TryConnect(this GodotObject self, StringName signal, Callable callable, uint flags = 0) {
        if (!self.IsValid()) return Error.Failed;
        if (self.IsConnected(signal, callable)) return Error.AlreadyExists;
        return self.Connect(signal, callable, flags);
    }
    public static Error TryDisconnect(this GodotObject self, StringName signal, Callable callable) {
        if (!self.IsValid()) return Error.Failed;
        if (!self.IsConnected(signal, callable)) return Error.DoesNotExist;
        self.Disconnect(signal, callable);
        return Error.Ok;
    }

    public static Error Connect(this GodotObject self, StringName signal, Action action, uint flags = 0) => self.Connect(signal, Callable.From(action), flags);
    public static Error TryConnect(this GodotObject self, StringName signal, Action action, uint flags = 0) => self.TryConnect(signal, Callable.From(action), flags);

    public static void Disconnect(this GodotObject self, StringName signal, Action action) => self.Disconnect(signal, Callable.From(action));
    public static void TryDisconnect(this GodotObject self, StringName signal, Action action) => self.TryDisconnect(signal, Callable.From(action));
    
    public static Error Connect(this GodotObject self, StringName signal, StringName method, uint flags = 0) => self.Connect(signal, new Callable(self, method), flags);
    public static void Disconnect(this GodotObject self, StringName signal, StringName method) => self.Disconnect(signal, new Callable(self, method));

    public static Error TryConnect(this GodotObject self, StringName signal, StringName method, uint flags = 0) => self.TryConnect(signal, new Callable(self, method), flags);
    public static void TryDisconnect(this GodotObject self, StringName signal, StringName method) => self.TryDisconnect(signal, new Callable(self, method));

    // Versions of all of the above which accept arrays of tuples each containing a signal name and a callable, to make the code more readable when having to change multiple signals in one go
    // The array of tuples ones don't accept flags as we then wouldn't be able to use params, so you have to connect them separately if you need flags.
    
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Callable));
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Action));
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Method));
    public static void ConnectAll(this GodotObject self, Dictionary<StringName, Callable> signals, uint flags = 0) => signals.ForEach(x => self.Connect(x.Key, x.Value, flags));
    
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Callable));
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Action));
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Method));
    public static void TryConnectAll(this GodotObject self, Dictionary<StringName, Callable> signals, uint flags = 0) => signals.ForEach(x => self.TryConnect(x.Key, x.Value, flags));
    
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Callable));
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Action));
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Method));
    public static void DisconnectAll(this GodotObject self, Dictionary<StringName, Callable> signals) => signals.ForEach(x => self.Disconnect(x.Key, x.Value));
    
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Callable));
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Action));
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Method));
    public static void TryDisconnectAll(this GodotObject self, Dictionary<StringName, Callable> signals) => signals.ForEach(x => self.TryDisconnect(x.Key, x.Value));
}
using System;
using System.Collections.Generic;
using Godot;

namespace DamselsGambit.Util;

// Helper methods for safely connecting & disconnecting signals, as well as shorthands for connecting actions as callables, multiple signals, etc
public static class SignalExtensions
{
    /// <summary> <para>Try to connect, first checking that the signal is not already connected.</para> </summary>
    /// <param name="signal">Name of signal to connect.</param>
    /// <param name="callable">Callable to connect.</param>
    /// <returns><see langword="true"/> if connected succesfully.</returns>
    public static bool TryConnect(this GodotObject self, StringName signal, Callable callable, uint flags = 0) {
        if (self.IsInvalid() || self.IsConnected(signal, callable)) return false;
        return self.Connect(signal, callable, flags) == Error.Ok;
    }

    /// <summary> <para>Try to discconnect, first checking that the signal is actually connected.</para> </summary>
    /// <param name="signal">Name of signal to disconnect.</param>
    /// <param name="callable">Callable to disconnect.</param>
    /// <returns><see langword="true"/> if disconnected succesfully.</returns>
    public static bool TryDisconnect(this GodotObject self, StringName signal, Callable callable) {
        if (self.IsInvalid() || !self.IsConnected(signal, callable)) return false;
        self.Disconnect(signal, callable); return true;
    }

    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/> <param name="action">Action to connect. Will become Callable.From(action).</param>
    public static Error Connect(this GodotObject self, StringName signal, Action action, uint flags = 0) => self.Connect(signal, Callable.From(action), flags);

    /// <inheritdoc cref="TryConnect(GodotObject, StringName, Callable, uint)"/> <param name="action">Action to connect. Will become Callable.From(action).</param>
    public static bool TryConnect(this GodotObject self, StringName signal, Action action, uint flags = 0) => self.TryConnect(signal, Callable.From(action), flags);

    /// <inheritdoc cref="GodotObject.Disconnect(StringName, Callable)"/> <param name="action">Action to disconnect. Will become Callable.From(action).</param>
    public static void Disconnect(this GodotObject self, StringName signal, Action action) => self.Disconnect(signal, Callable.From(action));

    /// <inheritdoc cref="TryDisconnect(GodotObject, StringName, Callable)"/> <param name="action">Action to disconnect. Will become Callable.From(action).</param>
    public static bool TryDisconnect(this GodotObject self, StringName signal, Action action) => self.TryDisconnect(signal, Callable.From(action));
    
    /// <summary>Shorthand for connecting to <see cref="Callable"/> to method denoted by given MethodName on self. <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/> </summary>
    /// <param name="method">Name of method to connect.</param>
    public static Error Connect(this GodotObject self, StringName signal, StringName method, uint flags = 0) => self.Connect(signal, new Callable(self, method), flags);

    /// <summary>Shorthand for attempting to connect to <see cref="Callable"/> to method denoted by given MethodName on self. <inheritdoc cref="TryConnect(GodotObject, StringName, Callable, uint)"/> </summary>
    /// <param name="method">Name of method to connect.</param>
    public static bool TryConnect(this GodotObject self, StringName signal, StringName method, uint flags = 0) => self.TryConnect(signal, new Callable(self, method), flags);

    /// <summary>Shorthand for disconnecting from <see cref="Callable"/> to method denoted by given MethodName on self. <inheritdoc cref="GodotObject.Disconnect(StringName, Callable)"/> </summary>
    /// <param name="method">Name of method to discconnect.</param>
    public static void Disconnect(this GodotObject self, StringName signal, StringName method) => self.Disconnect(signal, new Callable(self, method));

    /// <summary>Shorthand for attempting to discconnect from <see cref="Callable"/> to method denoted by given MethodName on self. <inheritdoc cref="TryDisconnect(GodotObject, StringName, Callable)"/> </summary>
    /// <param name="method">Name of method to discconnect.</param>
    public static bool TryDisconnect(this GodotObject self, StringName signal, StringName method) => self.TryDisconnect(signal, new Callable(self, method));

    // Versions of all of the above which accept arrays of tuples each containing a signal name and a callable, to make the code more readable when having to change multiple signals in one go
    // The array of tuples ones don't accept flags as we then wouldn't be able to use params, so you have to connect them separately if you need flags.
    
    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Callable));
    /// <inheritdoc cref="Connect(GodotObject, StringName, Action, uint)"/>
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Action));
    /// <inheritdoc cref="Connect(GodotObject, StringName, StringName, uint)"/>
    public static void ConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.Connect(x.Signal, x.Method));
    /// <inheritdoc cref="GodotObject.Connect(StringName, Callable, uint)"/>
    public static void ConnectAll(this GodotObject self, Dictionary<StringName, Callable> signals, uint flags = 0) => signals.ForEach(x => self.Connect(x.Key, x.Value, flags));
    
    /// <summary><inheritdoc cref="TryConnect(GodotObject, StringName, Callable, uint)"/></summary>
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Callable));
    /// <summary><inheritdoc cref="TryConnect(GodotObject, StringName, Action, uint)"/></summary>
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Action));
    /// <summary><inheritdoc cref="TryConnect(GodotObject, StringName, StringName, uint)"/></summary>
    public static void TryConnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.TryConnect(x.Signal, x.Method));
    /// <summary><inheritdoc cref="TryConnect(GodotObject, StringName, Callable, uint)"/></summary>
    public static void TryConnectAll(this GodotObject self, Dictionary<StringName, Callable> signals, uint flags = 0) => signals.ForEach(x => self.TryConnect(x.Key, x.Value, flags));
    
    /// <inheritdoc cref="GodotObject.Disconnect(StringName, Callable)"/>
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Callable));
    /// <inheritdoc cref="Disconnect(GodotObject, StringName, Action)"/>
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Action));
    /// <inheritdoc cref="Disconnect(GodotObject, StringName, StringName)"/>
    public static void DisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.Disconnect(x.Signal, x.Method));
    /// <inheritdoc cref="GodotObject.Disconnect(StringName, Callable)"/>
    public static void DisconnectAll(this GodotObject self, Dictionary<StringName, Callable> signals) => signals.ForEach(x => self.Disconnect(x.Key, x.Value));
    
    /// <summary><inheritdoc cref="TryDisconnect(GodotObject, StringName, Callable)"/></summary>
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Callable Callable)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Callable));
    /// <summary><inheritdoc cref="TryDisconnect(GodotObject, StringName, Action)"/></summary>
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, Action Action)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Action));
    /// <summary><inheritdoc cref="TryDisconnect(GodotObject, StringName, StringName)"/></summary>
    public static void TryDisconnectAll(this GodotObject self, params IEnumerable<(StringName Signal, StringName Method)> signals) => signals.ForEach(x => self.TryDisconnect(x.Signal, x.Method));
    /// <summary><inheritdoc cref="TryDisconnect(GodotObject, StringName, Callable)"/></summary>
    public static void TryDisconnectAll(this GodotObject self, Dictionary<StringName, Callable> signals) => signals.ForEach(x => self.TryDisconnect(x.Key, x.Value));
}
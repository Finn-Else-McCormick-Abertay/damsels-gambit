using System;
using Godot;

namespace DamselsGambit;

[Serializable]
public class AutoloadException : Exception
{
    public AutoloadException () : base(FormatMessage("")) {}

    public AutoloadException (string autoloadName) : base(FormatMessage(autoloadName)) {}
    public AutoloadException (Type autoloadType) : base(FormatMessage(autoloadType)) {}

    public AutoloadException (string autoloadName, Exception innerException) : base (FormatMessage(autoloadName), innerException) {}
    public AutoloadException (Type autoloadType, Exception innerException) : base (FormatMessage(autoloadType), innerException) {}

    public static AutoloadException For<T>() => new(typeof(T));
    public static AutoloadException For<T>(T _) => For<T>();

    private static string FormatMessage(string autoloadName) => $"Attempted to re-instantiate autoload{(!string.IsNullOrWhiteSpace(autoloadName) ? $" {autoloadName}" : "")}.";
    private static string FormatMessage(Type autoloadType) => FormatMessage(autoloadType.FullName);
}
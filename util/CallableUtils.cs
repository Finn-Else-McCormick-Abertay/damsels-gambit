using System;
using Godot;

namespace DamselsGambit.Util;

public static class CallableUtils
{
    public static void CallDeferred(Action action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0>(Action<T0> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1>(Action<T0, T1> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2>(Action<T0, T1, T2> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3>(Action<T0, T1, T2, T3> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4>(Action<T0, T1, T2, T3, T4> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5>(Action<T0, T1, T2, T3, T4, T5> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6>(Action<T0, T1, T2, T3, T4, T5, T6> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> action, params Variant[] args) => Callable.From(action).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action, params Variant[] args) => Callable.From(action).CallDeferred(args);

    public static void CallDeferred<[MustBeVariant]TResult>(Func<TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]TResult>(Func<T0, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]TResult>(Func<T0, T1, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]TResult>(Func<T0, T1, T2, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, T4, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, T4, T5, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, T4, T5, T6, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
    public static void CallDeferred<[MustBeVariant]T0, [MustBeVariant]T1, [MustBeVariant]T2, [MustBeVariant]T3, [MustBeVariant]T4, [MustBeVariant]T5, [MustBeVariant]T6, [MustBeVariant]T7, [MustBeVariant]T8, [MustBeVariant]TResult>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> func, params Variant[] args) => Callable.From(func).CallDeferred(args);
}
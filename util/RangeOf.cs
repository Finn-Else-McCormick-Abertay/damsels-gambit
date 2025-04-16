using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace DamselsGambit.Util;

public class RangeOf<TIndex> : IEnumerable<TIndex> where TIndex : IBinaryInteger<TIndex>, IMinMaxValue<TIndex>
{
    private readonly TIndex _start, _end;
    private readonly bool _empty;

    internal RangeOf(TIndex start, TIndex end) { _start = start; _end = end; _empty = false; }
    internal RangeOf() { _empty = true; }

    public IEnumerable<TIndex> AsEnumerable() {
        if (_empty) yield break;
        bool forwards = _end >= _start;
        for (TIndex i = _start; forwards ? i <= _end : i >= _end; i = forwards ? ++i : --i) yield return i;
        yield break;
    }

    public TIndex Clamp(TIndex value) => TIndex.Clamp(value, _start, _end);

    public bool Contains<TCompare>(TCompare value) where TCompare : IBinaryInteger<TCompare>, IMinMaxValue<TCompare> =>
        !_empty && !IsBeyondTypeMinMax(value) && ToIndexType(value) is TIndex valueAsIndex && _start <= valueAsIndex && _end >= valueAsIndex;
    
    public bool IsInRange<TCompare>(TCompare value) where TCompare : IBinaryInteger<TCompare>, IMinMaxValue<TCompare> => Contains(value);

    public RangeOf<TIndex> Reverse() => _empty ? new() : new(_end, _start);

    private bool IsBeyondTypeMinMax<TCompare>(TCompare value) where TCompare : IBinaryInteger<TCompare>, IMinMaxValue<TCompare> =>
        (TCompare.MinValue < ToType<TCompare>(TIndex.MinValue) && (value < ToType<TCompare>(TIndex.MinValue))) ||
        (TCompare.MaxValue > ToType<TCompare>(TIndex.MaxValue) && (value < ToType<TCompare>(TIndex.MinValue)));

    internal static TIndex ToIndexType<TChange>(TChange i) where TChange : IBinaryInteger<TChange> => (TIndex)Convert.ChangeType(i, typeof(TIndex));
    internal static TChange ToType<TChange>(TIndex i) where TChange : IBinaryInteger<TChange> => (TChange)Convert.ChangeType(i, typeof(TChange));

    /// <summary>Range from <paramref name="start"/> to <paramref name="end"/>, inclusive.</summary>
    /// <param name="start">First value in range, inclusive.</param> <param name="end">Last value in range, inclusive.</param>
    public static RangeOf<TIndex> Over(TIndex start, TIndex end) => new(start, end);
    
    /// <summary>Range from <paramref name="start"/> to <paramref name="end"/>, non-inclusive.</summary>
    /// <param name="start">Value before first in range.</param> <param name="end">Value after last in range.</param>
    public static RangeOf<TIndex> Between(TIndex start, TIndex end) => start == end || start + ToIndexType(1) == end || start - ToIndexType(1) == end ? new() :
        Over(end < start ? start - ToIndexType(1) : start + ToIndexType(1), end < start ? end + ToIndexType(1) : end - ToIndexType(1));
    
    /// <summary>Range from <paramref name="start"/> to max value, inclusive.</summary>
    /// <param name="start">First value in range, inclusive.</param>
    public static RangeOf<TIndex> From(TIndex start) => Over(start, TIndex.MaxValue);
    
    /// <summary>Range from zero to <paramref name="end"/> to max value, inclusive.</summary>
    /// <param name="end">Last value in range, inclusive.</param>
    public static RangeOf<TIndex> To(TIndex end) => Over(ToIndexType(0), end);
    
    /// <summary>Range from zero (inclusive) up to <paramref name="end"/> (non-inclusive).</summary>
    /// <param name="end">Value after last in range.</param>
    public static RangeOf<TIndex> UpTo(TIndex end) => FromUpTo(ToIndexType(0), end);

    /// <summary>Range from <paramref name="start"/> (inclusive) up to <paramref name="end"/> (non-inclusive).</summary>
    /// <param name="start">First value in range, inclusive.</param> <param name="end">Value after last in range.</param>
    public static RangeOf<TIndex> FromUpTo(TIndex start, TIndex end) => end == start ? new() : new(start, end < start ? end + ToIndexType(1) : end - ToIndexType(1));

    public IEnumerator<TIndex> GetEnumerator() => AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// This is valid C# but makes GDBridge fail for some reason
/*
public static class RangeOf
{
    /// <inheritdoc cref="RangeOf{T}.Over(T, T)"/>
    public static RangeOf<TIndex> Over<TIndex>(TIndex start, TIndex end) where TIndex : IBinaryInteger<TIndex>, IMinMaxValue<TIndex> => start < end ? new(start, end) : new(end, start);
    
    /// <inheritdoc cref="RangeOf{T}.Between(T, T)"/>
    public static RangeOf<TIndex> Between<TIndex>(TIndex start, TIndex end) where TIndex : IBinaryInteger<TIndex>, IMinMaxValue<TIndex> => start == end || start + RangeOf<TIndex>.ToIndexType(1) == end ? new() :
        Over(end < start ? start - RangeOf<TIndex>.ToIndexType(1) : start + RangeOf<TIndex>.ToIndexType(1), end < start ? end + RangeOf<TIndex>.ToIndexType(1) : end - RangeOf<TIndex>.ToIndexType(1));
    
    /// <inheritdoc cref="RangeOf{T}.From(T)"/>
    public static RangeOf<TIndex> From<TIndex>(TIndex start) where TIndex : IBinaryInteger<TIndex>, IMinMaxValue<TIndex> => new(start, TIndex.MaxValue);
    
    /// <inheritdoc cref="RangeOf{T}.To(T)"/>
    public static RangeOf<TIndex> To<TIndex>(TIndex end) where TIndex : IBinaryInteger<TIndex>, IMinMaxValue<TIndex> => new(TIndex.MinValue, end);
}
*/
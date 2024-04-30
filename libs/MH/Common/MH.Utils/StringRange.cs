using System;
using System.Collections.Generic;

namespace MH.Utils;

public class StringRange {
  public string StartString { get; init; }
  public string StartEndString { get; init; }
  public string EndString { get; init; }
  public int Start { get; private set; }
  public int End { get; private set; }
  public StringComparison ComparisonType { get; init; } = StringComparison.OrdinalIgnoreCase;

  public StringRange(string startString) {
    StartString = startString;
  }

  public StringRange(string startString, string endString) {
    StartString = startString;
    EndString = endString;
  }

  public StringRange(string startString, string startEndString, string endString) {
    StartString = startString;
    StartEndString = startEndString;
    EndString = endString;
  }

  public IEnumerable<StringRange> AsEnumerable(string text, StringRange innerRange) {
    var idx = Start;
    return AsEnumerable(() => innerRange.From(text, ref idx, End));
  }

  public IEnumerable<T> AsEnumerable<T>(Func<T> func) {
    while (true) {
      if (func() is not { } value) yield break;
      yield return value;
    }
  }

  public int AsInt32(string text, int ifNull = 0) =>
    int.TryParse(AsString(text), out var i) ? i : ifNull;

  public string AsString(string text) =>
    text[Start..End];

  public StringRange From(string text, ref int searchStart, int searchEnd = -1) {
    if (!Found(text, searchStart, searchEnd)) return null;
    searchStart = Start;
    return this;
  }

  public StringRange From(string text, int searchStart, int searchEnd = -1) =>
    Found(text, searchStart, searchEnd) ? this : null;

  public StringRange From(string text, StringRange range) =>
    Found(text, range.Start, range.End) ? this : null;

  public bool Found(string text, int searchStart, int searchEnd = -1) {
    var count = GetCountForIndexOf(text, searchStart, searchEnd);

    // search start
    Start = text.IndexOf(StartString, searchStart, count, ComparisonType);
    if (Start == -1) return false;
    Start += StartString.Length;

    // optionally search for start end
    if (!string.IsNullOrEmpty(StartEndString)) {
      count = GetCountForIndexOf(text, Start, searchEnd);
      Start = text.IndexOf(StartEndString, Start, count, ComparisonType);
      if (Start == -1) return false;
      Start += StartEndString.Length;
    }

    // search for end
    if (string.IsNullOrEmpty(EndString)) {
      End = searchEnd == -1 ? text.Length - 1 : searchEnd;
    }
    else {
      count = GetCountForIndexOf(text, Start, searchEnd);
      End = text.IndexOf(EndString, Start, count, ComparisonType);
      if (End == -1) return false;
    }

    return true;
  }

  private static int GetCountForIndexOf(string text, int searchStart, int searchEnd = -1) =>
    searchEnd == -1 ? text.Length - searchStart : searchEnd - searchStart;
}
using System;
using System.Collections.Generic;

namespace MH.Utils;

//TODO change to class
//TODO rename Start to StartString and StartIndex to Start
public struct StringRange {
  public string Start { get; init; }
  public string StartEnd { get; init; }
  public string End { get; init; }
  public int StartIndex { get; private set; }
  public int EndIndex { get; private set; }
  public StringComparison ComparisonType { get; init; } = StringComparison.OrdinalIgnoreCase;

  public StringRange() { }

  public StringRange(string start, string end) {
    Start = start;
    End = end;
  }

  public StringRange(string start, string startEnd, string end) {
    Start = start;
    StartEnd = startEnd;
    End = end;
  }

  public IEnumerable<StringRange> AsEnumerable(string text, StringRange innerRange) {
    var idx = StartIndex;

    while (true) {
      if (innerRange.From(text, ref idx, EndIndex) == null) yield break;
      yield return innerRange;
    }
  }

  public int AsInt32(string text, int ifNull = 0) =>
    int.TryParse(AsString(text), out var i) ? i : ifNull;

  public string AsString(string text) =>
    text[StartIndex..EndIndex];

  // TODO use AsEnumerable
  public List<string> AsStrings(string text, StringRange innerRange) {
    var strings = new List<string>();
    var idx = StartIndex;

    while (true) {
      if (innerRange.From(text, ref idx, EndIndex) == null) break;
      strings.Add(innerRange.AsString(text));
    }

    return strings;
  }

  public bool From(string text, StringRange range, out string value) {
    if (From(text, range)) {
      value = AsString(text);
      return true;
    }

    value = null;
    return false;
  }

  // TODO remove this, it is confusing
  public StringRange? From(string text) =>
    From(text, 0) ? this : null;

  public StringRange? From(string text, ref int searchStart, int searchEnd = -1) {
    if (!From(text, searchStart, searchEnd)) return null;
    searchStart = StartIndex;
    return this;
  }

  public bool From(string text, StringRange range) =>
    From(text, range.StartIndex, range.EndIndex);

  // TODO use end limit for IndexOf
  public bool From(string text, int searchStart, int searchEnd = -1) {
    // search start
    StartIndex = text.IndexOf(Start, searchStart, ComparisonType);
    if (StartIndex == -1) return false;
    StartIndex += Start.Length;

    // optionally search for start end
    if (!string.IsNullOrEmpty(StartEnd)) {
      StartIndex = text.IndexOf(StartEnd, StartIndex, ComparisonType);
      if (StartIndex == -1) return false;
      StartIndex += StartEnd.Length;
    }

    // search for end
    EndIndex = text.IndexOf(End, StartIndex, ComparisonType);
    if (EndIndex == -1) return false;

    // optionally limit the end
    if (searchEnd > -1 && searchEnd < EndIndex) return false;

    return true;
  }
}
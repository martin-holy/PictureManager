﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MH.Utils.Extensions {
  public static class DateTimeExtensions {
    /// <summary>
    /// Tries to parse date and time from first 15 characters of the string 
    /// </summary>
    /// <param name="text">DateTime string in format yyyyMMdd_HHmmss</param>
    /// <param name="dateFormats">Example: {{"d", "d. "}, {"M", "MMMM "}, {"y", "yyyy"}}</param>
    /// <param name="timeFormat">Example: H:mm:ss</param>
    /// <returns>Formated "date, time" or string.Empty</returns>
    public static string DateTimeFromString(string? text, Dictionary<string, string> dateFormats, string timeFormat) {
      if (string.IsNullOrEmpty(text) || text.Length < 15 || text[8] != '_') return string.Empty;

      var locDateFormats = dateFormats.ToDictionary(df => df.Key, df => df.Value);

      if (text.Substring(4, 2) == "00") {
        locDateFormats["M"] = string.Empty;
        text = $"{text.Substring(0, 5)}1{text.Substring(6, 9)}";
      }

      if (text.Substring(6, 2) == "00") {
        locDateFormats["d"] = string.Empty;
        text = $"{text.Substring(0, 7)}1{text.Substring(8, 7)}";
      }

      if (text.Length > 15) text = text.Substring(0, 15);

      if (!DateTime.TryParseExact(text, "yyyyMMdd_HHmmss",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        return string.Empty;

      var dateFormat = locDateFormats.Aggregate(string.Empty, (f, current) => f + current.Value);
      var dateF = dt.ToString(dateFormat, CultureInfo.CurrentCulture);
      var timeF = dt.ToString(timeFormat, CultureInfo.CurrentCulture);

      return dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0 || string.IsNullOrEmpty(timeFormat) ? dateF : $"{dateF}, {timeF}";
    }

    public static string DateFromString(string text, Dictionary<string, string> dateFormats) {
      if (text.Length < 8
          || !int.TryParse(text[..4], out var y)
          || !int.TryParse(text[4..6], out var m)
          || !int.TryParse(text[6..8], out var d)) return string.Empty;

      var locDateFormats = dateFormats.ToDictionary(df => df.Key, df => df.Value);
      if (m == 0) {
        locDateFormats["M"] = string.Empty;
        m = 1;
      }

      if (d == 0) {
        locDateFormats["d"] = string.Empty;
        d = 1;
      }

      try {
        if (m > 12 || d > 31) return string.Empty;
        var dt = new DateTime(y, m, d);
        var dateFormat = locDateFormats.Aggregate(string.Empty, (f, current) => f + current.Value);
        return dt.ToString(dateFormat, CultureInfo.CurrentCulture);
      }
      catch (Exception) {
        return string.Empty;
      }
    }
  }
}

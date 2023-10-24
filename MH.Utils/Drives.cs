using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MH.Utils;

public static class Drives {
  public static readonly Dictionary<string, string> SerialNumbers = new();

  public static void UpdateSerialNumbers() {
    SerialNumbers.Clear();
    foreach (var info in GetLogicalDrivesInfo())
      SerialNumbers.Add(info.Item1, info.Item3);
  }

  public static List<Tuple<string, string, string>> GetLogicalDrivesInfo() {
    static string Extract(string s) =>
      s[(s.LastIndexOf(" ", StringComparison.OrdinalIgnoreCase) + 1)..];

    var output = new List<Tuple<string, string, string>>();

    using var process = new Process {
      StartInfo = new("cmd") {
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        UseShellExecute = false
      }
    };

    foreach (var drv in Environment.GetLogicalDrives()) {
      var drive = drv[..2];
      process.StartInfo.Arguments = $"/c vol {drive}";
      process.Start();
      var lines = process.StandardOutput.ReadToEnd().Split("\r\n");
      var label = lines[0].EndsWith(".") ? string.Empty : Extract(lines[0]);
      var sn = Extract(lines[1]);
      output.Add(new(drive, label, sn));
      process.WaitForExit();
    }

    return output;
  }
}

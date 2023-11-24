using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Text;

namespace PictureManager.Domain.Models;

public class WhatIsNewM {
  private static WhatIsNewM _inst;

  public static string Content { get; set; }

  public static RelayCommand<object> OpenCommand { get; } = new(Open);

  private static void Open() {
    try {
      _inst ??= new();
      using var sr = new StreamReader("WhatIsNew.txt", Encoding.UTF8);
      Content = sr.ReadToEnd();
      Core.MainTabs.Activate(Res.IconInformation, "What's new", _inst);
    }
    catch (Exception) {
      // ignored
    }
  }
}
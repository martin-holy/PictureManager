﻿using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Text;

namespace PictureManager.Common.Features.WhatIsNew;

public class WhatIsNewVM {
  private static WhatIsNewVM? _inst;

  public static string Content { get; set; } = string.Empty;

  public static RelayCommand OpenCommand { get; } = new(_open, null, "What's new");

  private static void _open() {
    try {
      _inst ??= new();
      using var sr = new StreamReader("WhatIsNew.txt", Encoding.UTF8);
      Content = sr.ReadToEnd();
      Core.VM.MainTabs.Activate(Res.IconInformation, "What's new", _inst);
    }
    catch (Exception) {
      // ignored
    }
  }
}
﻿using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.Common;

public class DateM(string icon, string text, string raw) : ListItem(icon, text) {
  public string Raw { get; set; } = raw;
}
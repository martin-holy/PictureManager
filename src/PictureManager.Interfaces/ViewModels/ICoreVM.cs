﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using PictureManager.Interfaces.Models;
using System;

namespace PictureManager.Interfaces.ViewModels;

public interface ICoreVM {
  public TabControl MainTabs { get; }
  public TabControl ToolsTabs { get; }
  public ToggleDialog ToggleDialog { get; }
  public ISegmentVM Segment { get; }

  public event EventHandler AppClosingEvent;

  public bool AnyActive();
  public IMediaItemM[] GetActive();
  public void ScrollToFolder(IFolderM folder);
}
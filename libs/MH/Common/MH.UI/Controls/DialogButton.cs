﻿using MH.Utils.BaseClasses;

namespace MH.UI.Controls;

public class DialogButton(RelayCommand command, bool isDefault = false, bool isCancel = false) : ObservableObject {
  public bool IsDefault { get; } = isDefault;
  public bool IsCancel { get; } = isCancel;
  public RelayCommand Command { get; } = command;
}
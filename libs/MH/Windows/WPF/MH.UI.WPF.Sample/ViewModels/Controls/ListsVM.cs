using MH.Utils.BaseClasses;
using System.Collections.Generic;

namespace MH.UI.WPF.Sample.ViewModels.Controls;

public class ListsVM : ObservableObject {
  public List<string> ListData { get; } = new();

  public ListsVM() {
    for (var i = 0; i < 30; i++)
      ListData.Add($"Sample list item {i}");
  }
}
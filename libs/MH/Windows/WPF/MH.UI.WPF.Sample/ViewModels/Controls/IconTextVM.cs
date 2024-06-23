using System.Collections.Generic;
using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels.Controls;

public sealed class IconTextVM : ObservableObject {
  public List<string> Data { get; } = [
    "Python", "JavaScript", "Java", "C#", "C++", "Ruby", "Swift", "Go", "PHP", "TypeScript", "Kotlin", "Rust", "Dart",
    "Scala", "Perl", "R", "Objective-C", "MATLAB", "Elixir", "Haskell"
  ];
}
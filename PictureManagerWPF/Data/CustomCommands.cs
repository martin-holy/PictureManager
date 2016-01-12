using System.Windows.Input;

namespace PictureManager.Data {
  public static class CustomCommands {
    public static readonly RoutedUICommand KeywordShowAll = new RoutedUICommand("Show all (recursive)", "KeywordShowAll", typeof (CustomCommands));
    public static readonly RoutedUICommand KeywordNew = new RoutedUICommand("New", "KeywordNew", typeof(CustomCommands));
  }
}

using System.Windows.Input;

namespace PictureManager {
  public static class CustomCommands {
    public static readonly RoutedUICommand CmdKeywordsEdit = new RoutedUICommand(
      "Edit", "CmdKeywordsEdit", typeof (CustomCommands),
      new InputGestureCollection {new KeyGesture(Key.E, ModifierKeys.Control)}
      );

    public static readonly RoutedUICommand CmdKeywordsSave = new RoutedUICommand(
      "Save", "CmdKeywordsSave", typeof (CustomCommands),
      new InputGestureCollection {new KeyGesture(Key.S, ModifierKeys.Control)}
      );

    public static readonly RoutedUICommand CmdKeywordsCancel = new RoutedUICommand(
      "Cancel", "CmdKeywordsCancel", typeof (CustomCommands),
      new InputGestureCollection {new KeyGesture(Key.Q, ModifierKeys.Control)}
      );

    public static readonly RoutedUICommand CmdKeywordsComment = new RoutedUICommand(
      "Comment", "CmdKeywordsComment", typeof(CustomCommands),
      new InputGestureCollection { new KeyGesture(Key.K, ModifierKeys.Control) }
      );

    public static readonly RoutedUICommand CmdCompressPictures = new RoutedUICommand(
      "Compress Pictures", "CmdCompressPictures", typeof (CustomCommands));

    public static readonly RoutedUICommand CmdTestButton = new RoutedUICommand(
      "Test Button", "CmdTestButton", typeof (CustomCommands));

    public static readonly RoutedUICommand CmdReloadMetadata = new RoutedUICommand(
      "Reload Metadata", "CmdReloadMetadata", typeof (CustomCommands));

    public static readonly RoutedUICommand CmdOpenSettings = new RoutedUICommand(
      "Settings", "CmdOpenSettings", typeof (CustomCommands));

    public static readonly RoutedUICommand CmdAbout = new RoutedUICommand(
      "About", "CmdAbout", typeof (CustomCommands));

    public static readonly RoutedUICommand CmdCatalog = new RoutedUICommand(
      "Catalog", "CmdCatalog", typeof(CustomCommands));

    public static readonly RoutedUICommand CmdShowHideTabMain = new RoutedUICommand(
      "S/H", "CmdShowHideTabMain", typeof(CustomCommands));
  }
}

using System.Collections.Generic;
using System.Windows.Input;

namespace PictureManager {
  public static class Commands {
    //Window Commands
    public static RoutedUICommand SwitchToFullScreen { get; } = new RoutedUICommand();
    public static RoutedUICommand SwitchToBrowser { get; } = CreateCommand("Switch to Browser", "SwitchToBrowser", new KeyGesture(Key.Escape));
    //MediaItems Commands
    public static RoutedUICommand MediaItemNext { get; } = CreateCommand("Next", "MediaItemNext", new KeyGesture(Key.Right));
    public static RoutedUICommand MediaItemPrevious { get; } = CreateCommand("Previous", "MediaItemPrevious", new KeyGesture(Key.Left));
    public static RoutedUICommand MediaItemsSelectAll { get; } = CreateCommand("Select All", "MediaItemsSelectAll", new KeyGesture(Key.A, ModifierKeys.Control));
    public static RoutedUICommand MediaItemsSelectNotModifed { get; } = new RoutedUICommand { Text = "Select Not Modifed" };
    public static RoutedUICommand MediaItemsDelete { get; } = CreateCommand("Delete", "MediaItemsDelete", new KeyGesture(Key.Delete));
    public static RoutedUICommand MediaItemsLoadByTag { get; } = new RoutedUICommand { Text = "Load by this" };
    public static RoutedUICommand Presentation { get; } = CreateCommand("Presentation", "Presentation", new KeyGesture(Key.P, ModifierKeys.Control));
    //TreeView Commands
    public static RoutedUICommand CategoryGroupNew { get; } = new RoutedUICommand {Text = "New Group"};
    public static RoutedUICommand CategoryGroupRename { get; } = new RoutedUICommand {Text = "Rename Group"};
    public static RoutedUICommand CategoryGroupDelete { get; } = new RoutedUICommand {Text = "Delete Group"};
    public static RoutedUICommand TagItemNew { get; } = new RoutedUICommand {Text = "New"};
    public static RoutedUICommand TagItemRename { get; } = new RoutedUICommand {Text = "Rename"};
    public static RoutedUICommand TagItemDelete { get; } = new RoutedUICommand {Text = "Delete"};
    public static RoutedUICommand FolderNew { get; } = new RoutedUICommand {Text = "New folder"};
    public static RoutedUICommand FolderRename { get; } = new RoutedUICommand {Text = "Rename"};
    public static RoutedUICommand FolderDelete { get; } = new RoutedUICommand {Text = "Delete"};
    public static RoutedUICommand FolderAddToFavorites { get; } = new RoutedUICommand {Text = "Add to Favorites"};
    public static RoutedUICommand FolderRemoveFromFavorites { get; } = new RoutedUICommand {Text = "Remove from Favorites"};
    public static RoutedUICommand FolderSetAsFolderKeyword { get; } = new RoutedUICommand { Text = "Set as Folder Keyword" };
    public static RoutedUICommand FilterNew { get; } = new RoutedUICommand {Text = "New"};
    public static RoutedUICommand FilterEdit { get; } = new RoutedUICommand {Text = "Edit"};
    public static RoutedUICommand FilterDelete { get; } = new RoutedUICommand {Text = "Delete"};
    public static RoutedUICommand ViewerEdit { get; } = new RoutedUICommand {Text = "Edit"};
    public static RoutedUICommand ViewerIncludeFolder { get; } = new RoutedUICommand {Text = "Include for Viewer"};
    public static RoutedUICommand ViewerExcludeFolder { get; } = new RoutedUICommand {Text = "Exclude for Viewer"};
    public static RoutedUICommand ViewerRemoveFolder { get; } = new RoutedUICommand {Text = "Remove"};
    public static RoutedUICommand GeoNameNew { get; } = new RoutedUICommand {Text = "New"};
    //Menu Commands
    public static RoutedUICommand KeywordsEdit { get; } = CreateCommand("Edit", "KeywordsEdit", new KeyGesture(Key.E, ModifierKeys.Control));
    public static RoutedUICommand KeywordsSave { get; } = CreateCommand("Save", "KeywordsSave", new KeyGesture(Key.S, ModifierKeys.Control));
    public static RoutedUICommand KeywordsCancel { get; } = CreateCommand("Cancel", "KeywordsCancel", new KeyGesture(Key.Q, ModifierKeys.Control));
    public static RoutedUICommand KeywordsComment { get; } = CreateCommand("Comment", "KeywordsComment", new KeyGesture(Key.K, ModifierKeys.Control));

    public static RoutedUICommand CompressPictures { get; } = new RoutedUICommand {Text = "Compress Pictures"};
    public static RoutedUICommand RotatePictures { get; } = CreateCommand("Rotate Pictures", "RotatePictures", new KeyGesture(Key.R, ModifierKeys.Control));
    public static RoutedUICommand TestButton { get; } = CreateCommand("Test Button", "TestButton", new KeyGesture(Key.D, ModifierKeys.Control));
    public static RoutedUICommand ReloadMetadata { get; } = new RoutedUICommand {Text = "Reload Metadata"};
    public static RoutedUICommand RebuildThumbnails { get; } = new RoutedUICommand { Text = "Rebuild Thumbnails" };
    public static RoutedUICommand OpenSettings { get; } = new RoutedUICommand {Text = "Settings"};
    public static RoutedUICommand OpenAbout { get; } = new RoutedUICommand {Text = "About"};
    public static RoutedUICommand ShowHideTabMain { get; } = CreateCommand("S/H", "ShowHideTabMain", new KeyGesture(Key.T, ModifierKeys.Control));
    public static RoutedUICommand AddGeoNamesFromFiles { get; } = new RoutedUICommand {Text = "GeoNames"};
    public static RoutedUICommand ViewerChange { get; } = new RoutedUICommand {Text = ""};
    public static RoutedUICommand OpenFolderKeywordsList { get; } = new RoutedUICommand { Text = "Folder Keyword List" };

    private static RoutedUICommand CreateCommand(string text, string name, InputGesture inputGesture) {
      return new RoutedUICommand(text, name, typeof(Commands),
        inputGesture == null ? null : new InputGestureCollection(new List<InputGesture> { inputGesture }));
    }
  }
}
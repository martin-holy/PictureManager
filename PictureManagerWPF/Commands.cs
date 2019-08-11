using System.Windows.Input;

namespace PictureManager {
  public static class Commands {
    //Window Commands
    public static RoutedUICommand SwitchToFullScreen { get; } = new RoutedUICommand();
    public static RoutedUICommand SwitchToBrowser { get; } = new RoutedUICommand();
    //MediaItems Commands
    public static RoutedUICommand MediaItemNext { get; } = new RoutedUICommand {Text = "Next"};
    public static RoutedUICommand MediaItemPrevious { get; } = new RoutedUICommand {Text = "Previous"};
    public static RoutedUICommand MediaItemsSelectAll { get; } = new RoutedUICommand {Text = "Select All"};
    public static RoutedUICommand MediaItemsSelectNotModifed { get; } = new RoutedUICommand { Text = "Select Not Modifed" };
    public static RoutedUICommand MediaItemsDelete { get; } = new RoutedUICommand {Text = "Delete"};
    public static RoutedUICommand MediaItemsLoadByTag { get; } = new RoutedUICommand { Text = "Load by this" };
    public static RoutedUICommand Presentation { get; } = new RoutedUICommand { Text = "Presentation" };
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
    public static RoutedUICommand KeywordsEdit { get; } = new RoutedUICommand {Text = "Edit"};
    public static RoutedUICommand KeywordsSave { get; } = new RoutedUICommand {Text = "Save"};
    public static RoutedUICommand KeywordsCancel { get; } = new RoutedUICommand {Text = "Cancel"};
    public static RoutedUICommand KeywordsComment { get; } = new RoutedUICommand {Text = "Comment"};
    public static RoutedUICommand CompressPictures { get; } = new RoutedUICommand {Text = "Compress Pictures"};
    public static RoutedUICommand TestButton { get; } = new RoutedUICommand {Text = "Test Button"};
    public static RoutedUICommand ReloadMetadata { get; } = new RoutedUICommand {Text = "Reload Metadata"};
    public static RoutedUICommand OpenSettings { get; } = new RoutedUICommand {Text = "Settings"};
    public static RoutedUICommand OpenAbout { get; } = new RoutedUICommand {Text = "About"};
    public static RoutedUICommand OpenCatalog { get; } = new RoutedUICommand {Text = "Catalog"};
    public static RoutedUICommand ShowHideTabMain { get; } = new RoutedUICommand {Text = "S/H"};
    public static RoutedUICommand AddGeoNamesFromFiles { get; } = new RoutedUICommand {Text = "GeoNames"};
    public static RoutedUICommand ViewerChange { get; } = new RoutedUICommand {Text = ""};
    public static RoutedUICommand OpenFolderKeywordsList { get; } = new RoutedUICommand { Text = "Folder Keyword List" };
  }
}
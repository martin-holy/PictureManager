using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mshtml;
using PictureManager.Properties;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    public string WbFullPicHtmlPath;
    public string WbThumbsHtmlPath;
    readonly string _argPicFile;
    private readonly WFullPic _wFullPic;
    public AppCore ACore;

    public WMain(string picFile) {
      InitializeComponent();

      ACore = new AppCore {WbThumbs = WbThumbs};
      MainStatusBar.DataContext = ACore.AppInfo;

      WbFullPicHtmlPath = System.IO.Path.Combine(Environment.CurrentDirectory, "html\\FullPic.html");
      WbThumbsHtmlPath = System.IO.Path.Combine(Environment.CurrentDirectory, "html\\index.html");

      WbThumbs.ObjectForScripting = new ScriptManager(this);
      WbThumbs.Navigate(WbThumbsHtmlPath);
      WbThumbs.LoadCompleted += (o, args) => {
        var doc = (HTMLDocumentEvents2_Event) WbThumbs.Document;
        doc.oncontextmenu += obj => false;
      };

      _wFullPic = new WFullPic(this);
      _argPicFile = picFile;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      //app opened with argument
      if (System.IO.File.Exists(_argPicFile)) {
        ACore.ViewerOnly = true;
        ACore.OneFileOnly = true;
        ACore.Pictures.Add(new Data.Picture(_argPicFile, ACore.Db, 0));
        ACore.CurrentPicture = ACore.Pictures[0];
        ShowFullPicture();
      } else {
        InitUi();
      }
    }

    public void InitUi() {
      if (Settings.Default.FolderFavorites == null)
        Settings.Default.FolderFavorites = new List<string>();

      SetKeywordsButtonsVisibility();

      ACore.Init();
      TvFolders.ItemsSource = ACore.FolderBase.Items;
      ACore.KeywordsCategoryKeywords.IsExpanded = true;
      TvKeywords.ItemsSource = ACore.KeywordsBase.Items;
    }

    public void ShowFullPicture() {
      if (ACore.CurrentPicture != null) {
        _wFullPic.SetCurrentImage();
        if (!_wFullPic.IsActive)
          _wFullPic.Show();
      }
    }

    private void TvFolders_OnSelected(object sender, RoutedEventArgs e) {
      Data.Folder item = (Data.Folder) ((TreeViewItem) e.OriginalSource).DataContext;
      ACore.TreeView_FoldersOnSelected(item);
    }

    public void SwitchToBrowser() {
      if (ACore.ViewerOnly) {
        //App is first time loaded to main window
        ACore.ViewerOnly = false;
        InitUi();
        string folderName = System.IO.Path.GetDirectoryName(_argPicFile);
        ACore.TreeView_ExpandTo((ObservableCollection<Data.DataBase>)TvFilters.DataContext, folderName);
      }
      ACore.ScrollToCurrent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      _wFullPic.Close();
    }

    private void MnuKeyword_ShowAll_Click(object sender, RoutedEventArgs e) {
      
    }

    private void MnuFolder_AddToFavorites_Click(object sender, RoutedEventArgs e) {
      string path = ((Data.Folder) ((MenuItem) sender).DataContext).FullPath;
      bool found = Settings.Default.FolderFavorites.Any(folderPath => path.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
      if (!found) {
        Settings.Default.FolderFavorites.Add(path);
        Settings.Default.Save();
      }
      ACore.LoadFavorites();
    }

    private void MnuFolder_RemoveFromFavorites_Click(object sender, RoutedEventArgs e) {
      string path = ((Data.Folder)((MenuItem)sender).DataContext).FullPath;
      Settings.Default.FolderFavorites.Remove(path);
      Settings.Default.Save();
      ACore.LoadFavorites();
    }

    private void MnuFolder_Rename_Click(object sender, RoutedEventArgs e) {
      MenuItem menuItem = (MenuItem) sender;
      StackPanel stackPanel = (StackPanel) menuItem.DataContext;
      TextBlock textBlock = (TextBlock)stackPanel.Children[1];
      TextBox textBox = (TextBox)stackPanel.Children[2];
      textBlock.Visibility = Visibility.Collapsed;
      textBox.Text = textBlock.Text;
      textBox.Visibility = Visibility.Visible;
      textBox.Focus();
      textBox.SelectAll();
      textBox.Tag = textBlock;
    }

    private void TvFoldersEdit_OnKeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape || e.Key == Key.Enter) {
        TextBox textBox = (TextBox)sender;
        TextBlock textBlock = (TextBlock)textBox.Tag;
        if (e.Key == Key.Enter) {
          if (!string.IsNullOrEmpty(textBox.Text)) {
            ((Data.Folder)textBox.DataContext).Rename(textBox.Text);
          } 
        }
        textBlock.Visibility = Visibility.Visible;
        textBox.Visibility = Visibility.Collapsed;
      }
    }

    private void TreeViewCancelEdit_LostFocus(object sender, RoutedEventArgs e) {
      TextBox textBox = (TextBox)sender;
      TextBlock textBlock = (TextBlock)textBox.Tag;
      textBlock.Visibility = Visibility.Visible;
      textBox.Visibility = Visibility.Collapsed;
    }

    private void TvKeywordsEdit_OnKeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape || e.Key == Key.Enter) {
        TextBox textBox = (TextBox)sender;
        TextBlock textBlock = (TextBlock)textBox.Tag;
        if (e.Key == Key.Enter) {
          if (!string.IsNullOrEmpty(textBox.Text)) {
            ACore.RenameKeyword((Data.Keyword)textBox.DataContext, textBox.Text);
          }
        }
        textBlock.Visibility = Visibility.Visible;
        textBox.Visibility = Visibility.Collapsed;
      }
    }

    public void WbThumbsShowContextMenu() {
      /*ContextMenu cm = FindResource("MnuFolder") as ContextMenu;
      if (cm == null) return;
      cm.PlacementTarget = WbThumbs;
      cm.IsOpen = true;*/

    }

    private void BtnTest_OnClick(object sender, RoutedEventArgs e) {
      ACore.WbUpdatePictureInfo(0);


    }

    private void BtnKeywordsEditMode_OnClick(object sender, RoutedEventArgs e) {
      ACore.KeywordsEditMode = true;
      ACore.LastSelectedSource.IsSelected = false;
      SetKeywordsButtonsVisibility();
    }

    private void BtnKeywordsEditModeSave_OnClick(object sender, RoutedEventArgs e) {
      var pictures = ACore.Pictures.Where(p => p.IsModifed).ToList();
      StatusProgressBar.Maximum = pictures.Count;
      StatusProgressBar.Value = 0;
      foreach (Data.Picture picture in pictures) {
        ACore.SavePictureInToDb(picture);
        ACore.SetPictureMetadata(picture);
        StatusProgressBar.Value++;
      }
      StatusProgressBar.Value = 0;
      ACore.KeywordsEditMode = false;
      SetKeywordsButtonsVisibility();
    }

    private void BtnKeywordsEditModeCancel_OnClick(object sender, RoutedEventArgs e) {
      ACore.KeywordsEditMode = false;
      SetKeywordsButtonsVisibility();
      foreach (Data.Picture picture in ACore.Pictures) {
        if (picture.IsModifed) {
          ACore.RefreshPictureFromDb(picture);
          ACore.WbUpdatePictureInfo(picture.Index);
        }
      }
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void SetKeywordsButtonsVisibility() {
      BtnKeywordsEditMode.Visibility = TabKeywords.IsSelected && !ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
      BtnKeywordsEditModeSave.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
      BtnKeywordsEditModeCancel.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TcMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      SetKeywordsButtonsVisibility();
    }

    private void CmdKeywordShowAll_OnExecuted(object sender, ExecutedRoutedEventArgs e) {
      ACore.TreeView_KeywordsStackPanel_PreviewMouseDown((StackPanel)e.Parameter, MouseButton.Left, true);
    }

    private void CmdKeywordNew_OnExecuted(object sender, ExecutedRoutedEventArgs e) {
      ACore.CreateKeyword((Data.DataBase) e.Parameter, "New Keyword");
    }

    private void CmdKeywordRename_OnExecuted(object sender, ExecutedRoutedEventArgs e) {
      StackPanel stackPanel = (StackPanel) e.Parameter;
      TextBlock textBlock = (TextBlock)stackPanel.Children[1];
      TextBox textBox = (TextBox)stackPanel.Children[2];
      textBlock.Visibility = Visibility.Collapsed;
      textBox.Text = textBlock.Text;
      textBox.Visibility = Visibility.Visible;
      textBox.Focus();
      textBox.SelectAll();
      textBox.Tag = textBlock;
    }

    private void CmdKeywordDelete_OnExecuted(object sender, ExecutedRoutedEventArgs e) {
      ACore.DeleteKeyword((Data.Keyword)e.Parameter);
    }

    private void TvFoldersStackPanel_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
      ((StackPanel)sender).ContextMenu = null;
      Data.Folder item = (Data.Folder)((StackPanel)sender).DataContext;
      if (!item.IsAccessible) return;
      if (item.Parent != null && item.Parent.Title.Equals("Favorites")) {
        ((StackPanel)sender).ContextMenu = MainWindow.Resources["MnuFolderFavorites"] as ContextMenu;
      } else {
        ((StackPanel)sender).ContextMenu = MainWindow.Resources["MnuFolder"] as ContextMenu;
        foreach (MenuItem menuItem in ((StackPanel)sender).ContextMenu.Items) {
          switch (menuItem.Name) {
            case "MnuFolderRename": {
                menuItem.IsEnabled = item.Parent != null;
                break;
              }
          }
        }
      }
    }

    private void TvKeywordsStackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      ContextMenu newContectMenu = ACore.TreeView_KeywordsStackPanel_PreviewMouseDown((StackPanel)sender, e.ChangedButton, false);
      if (newContectMenu != null)
        ((StackPanel) sender).ContextMenu = newContectMenu;
    }
  }
}

using System;
using System.Collections.Generic;
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
      ACore.Folders.IsExpanded = true;
      ACore.Keywords.IsExpanded = true;
      TvFolders.ItemsSource = ACore.FoldersRoot;
      TvKeywords.ItemsSource = ACore.KeywordsRoot;
    }

    public void ShowFullPicture() {
      if (ACore.CurrentPicture == null) return;
      _wFullPic.SetCurrentImage();
      if (!_wFullPic.IsActive)
        _wFullPic.Show();
    }

    private void SetKeywordsButtonsVisibility() {
      BtnKeywordsEditMode.Visibility = TabKeywords.IsSelected && !ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
      BtnKeywordsEditModeSave.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
      BtnKeywordsEditModeCancel.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
      _wFullPic.Close();
    }

    public void SwitchToBrowser() {
      if (ACore.ViewerOnly) {
        //App is first time loaded to main window
        ACore.ViewerOnly = false;
        InitUi();
        ACore.Folders.ExpandTo(System.IO.Path.GetDirectoryName(_argPicFile));
      }
      ACore.ScrollToCurrent();
    }

    private void TvKeywordsStackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      StackPanel stackPanel = (StackPanel)sender;

      if (e.ChangedButton != MouseButton.Right) {
        ACore.TreeView_KeywordsStackPanel_PreviewMouseDown(stackPanel.DataContext, e.ChangedButton, false);
      } else {
        ContextMenu menu = stackPanel.ContextMenu;
        if (menu != null) return;
        object item = stackPanel.DataContext;
        menu = new ContextMenu { Tag = item };

        switch (item.GetType().Name) {
          case nameof(Data.Keyword): {
              menu.Items.Add(new MenuItem { Command = (ICommand)Resources["KeywordNew"], CommandParameter = item });
              if (((Data.Keyword)item).Items.Count == 0) {
                menu.Items.Add(new MenuItem { Command = (ICommand)Resources["KeywordRename"], CommandParameter = stackPanel });
                menu.Items.Add(new MenuItem { Command = (ICommand)Resources["KeywordDelete"], CommandParameter = item });
              }
              if (!ACore.KeywordsEditMode) {
                menu.Items.Add(new MenuItem { Command = (ICommand)Resources["KeywordShowAll"], CommandParameter = item });
              }
              break;
            }
          case nameof(Data.Keywords): {
              menu.Items.Add(new MenuItem { Command = (ICommand)Resources["KeywordNew"], CommandParameter = item });
              break;
            }
          case nameof(Data.Person): {
              break;
            }
          case nameof(Data.People): {
              break;
            }
        }

        stackPanel.ContextMenu = menu;
      }
    }

    private void TvFoldersStackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      StackPanel stackPanel = (StackPanel)sender;
      object item = stackPanel.DataContext;

      if (e.ChangedButton == MouseButton.Left) {
        switch (item.GetType().Name) {
          case nameof(Data.Folders):
          case nameof(Data.FavoriteFolders): {
            ((Data.BaseItem) item).IsSelected = false;
            break;
          }
          case nameof(Data.Folder): {
            var folder = (Data.Folder) item;
            if (!folder.IsAccessible) {
              folder.IsSelected = false;
              return;
            }
            ACore.LastSelectedSource = folder;
            ACore.LastSelectedSourceRecursive = false;
            ACore.GetPicturesByFolder(folder.FullPath);
            ACore.CreateThumbnailsWebPage();
            //TODO: tohle dat asi do jineho vlakna
            ACore.InitPictures(folder.FullPath);
            ACore.MarkUsedKeywordsAndPeople();
            break;
          }
          case nameof(Data.FavoriteFolder): {
            ACore.Folders.ExpandTo(((Data.FavoriteFolder) item).FullPath);
            break;
          }
        }
      }

      if (e.ChangedButton == MouseButton.Right) {
        ContextMenu menu = stackPanel.ContextMenu;
        if (menu != null) return;
        menu = new ContextMenu {Tag = item};

        switch (item.GetType().Name) {
          case nameof(Data.Folder): {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRename"], CommandParameter = stackPanel});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAddToFavorites"], CommandParameter = item});
            break;
          }
          case nameof(Data.FavoriteFolder): {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemoveFromFavorites"], CommandParameter = item});
            break;
          }
        }

        stackPanel.ContextMenu = menu;
      }
    }

    #region Commands
    private void CmdKeywordShowAll(object sender, ExecutedRoutedEventArgs e) {
      ACore.TreeView_KeywordsStackPanel_PreviewMouseDown(e.Parameter, MouseButton.Left, true);
    }

    private void CmdKeywordNew(object sender, ExecutedRoutedEventArgs e) {
      var keyword = e.Parameter as Data.Keyword;
      var keywords = e.Parameter as Data.Keywords;
      if (keyword == null && keywords == null) return;
      ACore.Keywords.CreateKeyword(keywords != null ? keywords.Items : keyword.Items, keyword, "New Keyword");
    }

    #region Rename Keyword and Folder

    private void CmdKeywordRename(object sender, ExecutedRoutedEventArgs e) {
      StackPanel stackPanel = (StackPanel)e.Parameter;
      TextBlock textBlock = (TextBlock)stackPanel.Children[1];
      TextBox textBox = (TextBox)stackPanel.Children[2];
      textBlock.Visibility = Visibility.Collapsed;
      textBox.Text = textBlock.Text;
      textBox.Visibility = Visibility.Visible;
      textBox.Focus();
      textBox.SelectAll();
      textBox.Tag = textBlock;
    }

    private void CmdFolderRename(object sender, ExecutedRoutedEventArgs e) {
      StackPanel stackPanel = (StackPanel)e.Parameter;
      TextBlock textBlock = (TextBlock)stackPanel.Children[1];
      TextBox textBox = (TextBox)stackPanel.Children[2];
      textBlock.Visibility = Visibility.Collapsed;
      textBox.Text = textBlock.Text;
      textBox.Visibility = Visibility.Visible;
      textBox.Focus();
      textBox.SelectAll();
      textBox.Tag = textBlock;
    }

    private void TreeViewCancelEdit_LostFocus(object sender, RoutedEventArgs e) {
      TextBox textBox = (TextBox)sender;
      TextBlock textBlock = (TextBlock)textBox.Tag;
      textBlock.Visibility = Visibility.Visible;
      textBox.Visibility = Visibility.Collapsed;
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

    private void TvKeywordsEdit_OnKeyDown(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape || e.Key == Key.Enter) {
        TextBox textBox = (TextBox)sender;
        TextBlock textBlock = (TextBlock)textBox.Tag;
        if (e.Key == Key.Enter) {
          if (!string.IsNullOrEmpty(textBox.Text)) {
            ((Data.Keyword)textBox.DataContext).Rename(ACore.Db, textBox.Text);
          }
        }
        textBlock.Visibility = Visibility.Visible;
        textBox.Visibility = Visibility.Collapsed;
      }
    }

    #endregion

    private void CmdKeywordDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.Keywords.DeleteKeyword((Data.Keyword) e.Parameter);
    }

    private void CmdFolderAddToFavorites(object sender, ExecutedRoutedEventArgs e) {
      string path = ((Data.Folder) e.Parameter).FullPath;
      bool found = Settings.Default.FolderFavorites.Any(folderPath => path.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
      if (!found) {
        Settings.Default.FolderFavorites.Add(path);
        Settings.Default.Save();
      }
      ACore.FavoriteFolders.Load();
    }

    private void CmdFolderRemoveFromFavorites(object sender, ExecutedRoutedEventArgs e) {
      string path = ((Data.FavoriteFolder) e.Parameter).FullPath;
      Settings.Default.FolderFavorites.Remove(path);
      Settings.Default.Save();
      ACore.FavoriteFolders.Load();
    }
    #endregion

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
        picture.SavePictureInToDb(ACore.Keywords, ACore.People);
        picture.SetPictureMetadata();
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
          picture.RefreshFromDb(ACore.Keywords, ACore.People);
          ACore.WbUpdatePictureInfo(picture.Index);
        }
      }
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void TcMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      SetKeywordsButtonsVisibility();
    }
  }
}

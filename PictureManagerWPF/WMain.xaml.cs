using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using mshtml;
using PictureManager.Dialogs;
using PictureManager.Properties;
using PictureManager.ShellStuff;

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
    private Point _dragDropStartPosition;

    public WMain(string picFile) {
      InitializeComponent();
      var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";

      ACore = new AppCore() {WbThumbs = WbThumbs};
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

    private void TreeViewKeywords_Select(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseUp on StackPanel in TreeView
      StackPanel stackPanel = (StackPanel)sender;

      if (e.ChangedButton != MouseButton.Right) {
        ACore.TreeView_KeywordsStackPanel_PreviewMouseUp(stackPanel.DataContext, e.ChangedButton, false);
      }
    }

    private void TreeViewFolders_Select(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseUp on StackPanel in TreeView
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

            _dragDropStartPosition = e.GetPosition(null);

            folder.IsSelected = true;
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
    }

    #region Commands
    private void CmdKeywordShowAll(object sender, ExecutedRoutedEventArgs e) {
      ACore.TreeView_KeywordsStackPanel_PreviewMouseUp(e.Parameter, MouseButton.Left, true);
    }

    private void CmdKeywordNew(object sender, ExecutedRoutedEventArgs e) {
      var keyword = e.Parameter as Data.Keyword;
      var keywords = e.Parameter as Data.Keywords;
      if (keyword == null && keywords == null) return;
      ACore.Keywords.CreateKeyword(keywords != null ? keywords.Items : keyword.Items, keyword, "New Keyword");
    }

    private void CmdPersonNew(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.CreatePerson("New Person");
    }

    private void CmdPersonDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.DeletePerson((Data.Person) e.Parameter);
    }

    #region Rename Keyword and Folder

    private void CmdRenameTreeViewItem(object sender, ExecutedRoutedEventArgs e) {
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

    private void TreeViewEndEdit_OnKeyDown(object sender, KeyEventArgs e) {
      if (e.Key != Key.Escape && e.Key != Key.Enter) return;
      TextBox textBox = (TextBox) sender;
      TextBlock textBlock = (TextBlock) textBox.Tag;
      if (e.Key == Key.Enter) {
        if (!string.IsNullOrEmpty(textBox.Text)) {
          switch (textBox.DataContext.GetType().Name) {
            case nameof(Data.Folder): {
              ((Data.Folder) textBox.DataContext).Rename(ACore.Db, textBox.Text);
              break;
            }
            case nameof(Data.Keyword): {
              ((Data.Keyword) textBox.DataContext).Rename(ACore.Db, textBox.Text);
              break;
            }
            case nameof(Data.Person): {
              ((Data.Person) textBox.DataContext).Rename(ACore.Db, textBox.Text);
              break;
            }
          }
        }
      }
      textBlock.Visibility = Visibility.Visible;
      textBox.Visibility = Visibility.Collapsed;
    }

    #endregion

    private void CmdKeywordDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.Keywords.DeleteKeyword((Data.Keyword) e.Parameter);
    }

    private void CmdFolderAddToFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Add(((Data.Folder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdFolderRemoveFromFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Remove(((Data.FavoriteFolder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdOpenSettings(object sender, RoutedEventArgs e) {
      var settings = new WSettings {Owner = this};
      if (settings.ShowDialog() ?? true) {
        Settings.Default.Save();
        ACore.FolderKeywords.Load();
      } else {
        Settings.Default.Reload();
      }
    }

    private void CmdAbout(object sender, RoutedEventArgs e) {
      var about = new WAbout { Owner = this };
      about.ShowDialog();
    }

    private void CmdKeywordsEditMode(object sender, RoutedEventArgs e) {
      if (ACore.Pictures.Count == 0) return;
      ACore.KeywordsEditMode = true;
      ACore.LastSelectedSource.IsSelected = false;
    }

    private void CmdKeywordsEditModeSave(object sender, RoutedEventArgs e) {
      var pictures = ACore.Pictures.Where(p => p.IsModifed).ToList();
      StatusProgressBar.Maximum = pictures.Count;
      StatusProgressBar.Value = 0;
      foreach (Data.Picture picture in pictures) {
        picture.SavePictureInToDb(ACore.Keywords, ACore.People);
        picture.WriteMetadata();
        StatusProgressBar.Value++;
      }
      StatusProgressBar.Value = 0;
      ACore.KeywordsEditMode = false;
    }

    private void CmdKeywordsEditModeCancel(object sender, RoutedEventArgs e) {
      ACore.KeywordsEditMode = false;
      foreach (Data.Picture picture in ACore.Pictures) {
        if (picture.IsModifed) {
          picture.RefreshFromDb(ACore.Keywords, ACore.People);
          ACore.WbUpdatePictureInfo(picture.Index);
        }
      }
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void CmdTestButton(object sender, RoutedEventArgs e) {
      MessageBox.Show((GC.GetTotalMemory(true) / 1024 / 1024).ToString());
      /*var inputDialog = new InputDialog {
        Owner = this,
        Title = "Test title",
        IconName = "appbar_question",
        Question = "Jak se máš",
        Answer = "Já ok"
      };

      if (inputDialog.ShowDialog() ?? true) {
        MessageBox.Show(inputDialog.Answer);
      }*/

      /*Application.Current.Properties["FileOperationResult"] = new Dictionary<string, string>();
      using (FileOperation fo = new FileOperation(new PicFileOperationProgressSink())) {
        //fo.SetOperationFlags(FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION);
        fo.CopyItem(@"d:\!test\003.jpg", @"d:\!test\aaa", "003.jpg");
        fo.PerformOperations();
      }
      var fileOperationResult = (Dictionary<string, string>) Application.Current.Properties["FileOperationResult"];*/

      /*WTestThumbnailGallery ttg = new WTestThumbnailGallery();
      ttg.Show();
      ttg.AddPhotosInFolder(ACore.Pictures);*/
    }

    #endregion

    public void WbThumbsShowContextMenu() {
      /*ContextMenu cm = FindResource("MnuFolder") as ContextMenu;
      if (cm == null) return;
      cm.PlacementTarget = WbThumbs;
      cm.IsOpen = true;*/
    }

    private void TvFolders_OnMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return;
      Vector diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      var stackPanel = e.OriginalSource as StackPanel;
      if (stackPanel == null) return;
      DragDrop.DoDragDrop(stackPanel, stackPanel.DataContext, DragDropEffects.All);
    }

    private void TvFolders_AllowDropCheck(object sender, DragEventArgs e) {
      var srcData = (Data.Folder) e.Data.GetData(typeof (Data.Folder));
      var destData = (Data.Folder) ((StackPanel) sender).DataContext;
      if (srcData != null && destData != null && srcData != destData && destData.IsAccessible) return;
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void TvFolders_OnDrop(object sender, DragEventArgs e) {
      var srcData = (Data.Folder) e.Data.GetData(typeof (Data.Folder));
      var destData = (Data.Folder) ((StackPanel) sender).DataContext;

      var flag = e.KeyStates == DragDropKeyStates.ControlKey ? 
        ACore.CopyItem(srcData.FullPath, destData.FullPath) : 
        ACore.MoveItem(srcData.FullPath, destData.FullPath);
      if (!flag) return;

      if (e.KeyStates != DragDropKeyStates.ControlKey) {
        UpdateFullPath(srcData, srcData.Parent.FullPath, destData.FullPath);
        srcData.Parent.Items.Remove(srcData);
        srcData.Parent = destData;
        destData.Items.Add(srcData);
      } else {
        destData.GetSubFolders(true);
      }
    }

    private static void UpdateFullPath(Data.Folder data, string oldParentPath, string newParentPath) {
      data.FullPath = data.FullPath?.Replace(oldParentPath, newParentPath);
      foreach (var item in data.Items) {
        UpdateFullPath(item, oldParentPath, newParentPath);
      }
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      StackPanel stackPanel = (StackPanel) sender;
      object item = stackPanel.DataContext;

      if (stackPanel.ContextMenu != null) return;
      ContextMenu menu = new ContextMenu {Tag = item};

      switch (item.GetType().Name) {
        case nameof(Data.Folder): {
          if (((Data.Folder) item).Parent != null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRename"], CommandParameter = stackPanel});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAddToFavorites"], CommandParameter = item});
          }
          break;
        }
        case nameof(Data.FavoriteFolder): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemoveFromFavorites"], CommandParameter = item});
          break;
        }
        case nameof(Data.Keyword): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordNew"], CommandParameter = item});
          if (((Data.Keyword) item).Items.Count == 0) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordRename"], CommandParameter = stackPanel});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordDelete"], CommandParameter = item});
          }
          if (!ACore.KeywordsEditMode) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordShowAll"], CommandParameter = item});
          }
          break;
        }
        case nameof(Data.Keywords): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordNew"], CommandParameter = item});
          break;
        }
        case nameof(Data.Person): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonRename"], CommandParameter = stackPanel});
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonDelete"], CommandParameter = item});
          break;
        }
        case nameof(Data.People): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonNew"], CommandParameter = item});
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void MainMenuButton_OnClick(object sender, RoutedEventArgs e) {
      ContextMenu menu = ((Button) sender).ContextMenu;

      McmKeywordsEditMode.Visibility = TabKeywords.IsSelected && !ACore.KeywordsEditMode && ACore.Pictures.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
      McmKeywordsEditModeSave.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;
      McmKeywordsEditModeCancel.Visibility = TabKeywords.IsSelected && ACore.KeywordsEditMode ? Visibility.Visible : Visibility.Collapsed;

      menu.Placement = PlacementMode.Absolute;
      menu.VerticalOffset = SystemParameters.WindowCaptionHeight + 9;
      menu.HorizontalOffset = 1;

      menu.IsOpen = true;
    }

    private void BtnMainContextMenu_OnContextMenuOpening(object sender, ContextMenuEventArgs e) {
      ((Button)sender).ContextMenu.IsOpen = false;
      e.Handled = true;
    }
  }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PictureManager.Properties;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    readonly string _argPicFile;
    private readonly WFullPic _wFullPic;
    public AppCore ACore;
    private Point _dragDropStartPosition;

    public WMain(string picFile) {
      System.Windows.Forms.Application.EnableVisualStyles();
      InitializeComponent();
      var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";

      ACore = new AppCore {WbThumbs = WbThumbs, WMain = this};
      MainStatusBar.DataContext = ACore.AppInfo;

      WbThumbs.ObjectForScripting = new ScriptManager(this);
      WbThumbs.DocumentCompleted += WbThumbs_DocumentCompleted;

      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PictureManager.html.Thumbs.html"))
        if (stream != null)
          using (StreamReader reader = new StreamReader(stream)) {
            WbThumbs.DocumentText = reader.ReadToEnd();
          }

      _wFullPic = new WFullPic(this);
      _argPicFile = picFile;
    }

    private void WbThumbs_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e) {
      if (WbThumbs.Document?.Body == null) return;
      WbThumbs.Document.MouseDown += WbThumbs_MouseDown;
      WbThumbs.Document.Body.DoubleClick += WbThumbs_DblClick;
      WbThumbs.Document.Body.KeyDown += WbThumbs_KeyDown;
    }

    private void WbThumbs_KeyDown(object sender, System.Windows.Forms.HtmlElementEventArgs e) {
      if (e.KeyPressedCode == 46) {//Delete 
        var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
          if (ACore.FileOperation(AppCore.FileOperations.Delete, !e.ShiftKeyPressed))
            ACore.RemoveSelectedFromWeb();
      }
    }

    private void WbThumbs_DblClick(object sender, System.Windows.Forms.HtmlElementEventArgs e) {
      var doc = WbThumbs.Document;
      var src = doc?.GetElementFromPoint(e.ClientMousePosition);
      var thumb = src?.Parent;
      if (thumb == null) return;
      if (thumb.Id == "content") return;
      ACore.CurrentPicture = ACore.Pictures[int.Parse(thumb.Id)];
      ShowFullPicture();
    }

    private void WbThumbs_MouseDown(object sender, System.Windows.Forms.HtmlElementEventArgs e) {
      if (e.MouseButtonsPressed == System.Windows.Forms.MouseButtons.Left) {
        var doc = WbThumbs.Document;
        var src = doc?.GetElementFromPoint(e.ClientMousePosition);

        var thumb = src?.Parent;
        if (thumb == null) return;

        if (thumb.Id == "content") {
          DeselectThumbnails();
          return;
        }

        if (!thumb.GetAttribute("className").Contains("thumbBox")) return;
        var picture = ACore.Pictures[int.Parse(thumb.Id)];

        if (e.CtrlKeyPressed) {
          if (thumb.GetAttribute("className").Contains("selected")) {
            thumb.SetAttribute("className", "thumbBox");
            ACore.SelectedPictures.Remove(picture);
          } else {
            thumb.SetAttribute("className", "thumbBox selected");
            ACore.SelectedPictures.Add(picture);
          }

          ACore.CurrentPicture = ACore.SelectedPictures.Count == 0 ? null : ACore.SelectedPictures[0];
          return;
        }

        if (e.ShiftKeyPressed && ACore.CurrentPicture != null) {
          ACore.SelectedPictures.Clear();
          var start = picture.Index > ACore.CurrentPicture.Index ? ACore.CurrentPicture.Index : picture.Index;
          var stop = picture.Index > ACore.CurrentPicture.Index ? picture.Index : ACore.CurrentPicture.Index;
          for (var i = start; i < stop + 1; i++) {
            ACore.SelectedPictures.Add(ACore.Pictures[i]);
            var elm = doc.GetElementById(i.ToString());
            elm?.SetAttribute("className", "thumbBox selected");
          }
        }

        if (!e.CtrlKeyPressed && !e.ShiftKeyPressed && !ACore.SelectedPictures.Contains(picture)) {
          DeselectThumbnails();
          thumb.SetAttribute("className", "thumbBox selected");
          ACore.CurrentPicture = picture;
          ACore.SelectedPictures.Add(picture);
        }

        ACore.MarkUsedKeywordsAndPeople();
      }
    }

    public void DeselectThumbnails() {
      if (ACore.SelectedPictures.Count == 0) return;
      var doc = WbThumbs.Document;
      if (doc == null) return;
      foreach (var thumb in ACore.SelectedPictures.Select(picture => doc.GetElementById(picture.Index.ToString()))) {
        thumb?.SetAttribute("className", "thumbBox");
      }
      ACore.SelectedPictures.Clear();
      ACore.CurrentPicture = null;
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      //app opened with argument
      if (File.Exists(_argPicFile)) {
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

    private void Window_Closing(object sender, CancelEventArgs e) {
      _wFullPic.Close();
    }

    public void SwitchToBrowser() {
      if (ACore.ViewerOnly) {
        //App is first time loaded to main window
        ACore.ViewerOnly = false;
        InitUi();
        ACore.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile));
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
            ACore.CreateThumbnailsWebPage(folder.FullPath);
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
      var k = ACore.Keywords.CreateKeyword(keywords != null ? keywords.Items : keyword.Items, keyword, "New Keyword");
      k.IsTitleEdited = true;
    }

    private void CmdPersonNew(object sender, ExecutedRoutedEventArgs e) {
      var p = ACore.People.CreatePerson("New Person");
      p.IsTitleEdited = true;
    }

    private void CmdPersonDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.DeletePerson((Data.Person) e.Parameter);
    }

    #region Rename Keyword and Folder

    private void CmdRenameTreeViewItem(object sender, ExecutedRoutedEventArgs e) {
      StackPanel stackPanel = (StackPanel)e.Parameter;
      ((Data.BaseItem) stackPanel.DataContext).IsTitleEdited = true;
    }

    private void TreeViewCancelEdit_LostFocus(object sender, RoutedEventArgs e) {
      ((Data.BaseItem) ((TextBox) sender).DataContext).IsTitleEdited = false;
    }

    private void TreeViewEndEdit_OnKeyDown(object sender, KeyEventArgs e) {
      if (e.Key != Key.Escape && e.Key != Key.Enter) return;
      TextBox textBox = (TextBox) sender;

      if (e.Key == Key.Enter) {
        if (!string.IsNullOrEmpty(textBox.Text)) {
          switch (textBox.DataContext.GetType().Name) {
            case nameof(Data.Folder): {
              ((Data.Folder) textBox.DataContext).Rename(ACore, textBox.Text);
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
      ((Data.BaseItem) textBox.DataContext).IsTitleEdited = false;
    }

    #endregion

    private void CmdKeywordDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.Keywords.DeleteKeyword((Data.Keyword) e.Parameter);
    }

    private void CmdFolderNew(object sender, ExecutedRoutedEventArgs e) {
      var newFolder = ((Data.Folder) e.Parameter).New();
      newFolder.IsTitleEdited = true;
    }

    private void CmdFolderDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ((Data.Folder) e.Parameter).Delete(ACore, true);
    }

    private void CmdFolderAddToFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Add(((Data.Folder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdFolderRemoveFromFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Remove(((Data.FavoriteFolder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdCompressPictures(object sender, RoutedEventArgs e) {
      var compress = new WCompress(ACore) {Owner = this};
      compress.ShowDialog();
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

      StatusProgressBar.Value = 0;
      StatusProgressBar.Maximum = 100;

      BackgroundWorker bw = new BackgroundWorker {WorkerReportsProgress = true};

      bw.ProgressChanged += delegate(object bwsender, ProgressChangedEventArgs bwe) {
        StatusProgressBar.Value = bwe.ProgressPercentage;
      };

      bw.DoWork += delegate(object bwsender, DoWorkEventArgs bwe) {
        var worker = (BackgroundWorker) bwsender;
        var acore = (AppCore) bwe.Argument;
        var count = pictures.Count;
        var done = 0;

        foreach (var picture in pictures) {
          picture.SavePictureInToDb(acore.Keywords, acore.People, false);
          picture.WriteMetadata();
          done++;
          worker.ReportProgress(Convert.ToInt32(((double) done/count)*100), picture.Index);
        }
      };

      bw.RunWorkerCompleted += delegate {
        ACore.KeywordsEditMode = false;
      };

      bw.RunWorkerAsync(ACore);
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

    private void CmdReloadMetadata(object sender, RoutedEventArgs e) {
      var pictures = ACore.SelectedPictures.Count > 0 ? ACore.SelectedPictures : ACore.Pictures;
      foreach (var picture in pictures) {
        picture.SavePictureInToDb(ACore.Keywords, ACore.People, true);
        ACore.WbUpdatePictureInfo(picture.Index);
      }
    }

    private void CmdTestButton(object sender, RoutedEventArgs e) {


      //MessageBox.Show((GC.GetTotalMemory(true) / 1024 / 1024).ToString());

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
      var thumbs = e.Data.GetData(DataFormats.Text)?.Equals("PictureManager"); //thumbnails drop
      var srcData = (Data.Folder) e.Data.GetData(typeof (Data.Folder));
      var destData = (Data.Folder) ((StackPanel) sender).DataContext;
      if ((srcData != null || (thumbs != null && thumbs.Value)) && destData != null && srcData != destData && destData.IsAccessible) return;
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void TvFolders_OnDrop(object sender, DragEventArgs e) {
      var thumbs = e.Data.GetDataPresent(DataFormats.Text) && e.Data.GetData(DataFormats.Text).Equals("PictureManager"); //thumbnails drop
      var srcData = (Data.Folder) e.Data.GetData(typeof (Data.Folder));
      var destData = (Data.Folder) ((StackPanel) sender).DataContext;
      var from = thumbs ? null : srcData.FullPath;
      var itemName = thumbs ? null : srcData.FullPath.Substring(srcData.FullPath.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1);

      var flag = e.KeyStates == DragDropKeyStates.ControlKey ? 
        ACore.FileOperation(AppCore.FileOperations.Copy, from, destData.FullPath, itemName) : 
        ACore.FileOperation(AppCore.FileOperations.Move, from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey)
          ACore.RemoveSelectedFromWeb();
        return;
      }

      if (e.KeyStates != DragDropKeyStates.ControlKey) {
        srcData.UpdateFullPath(srcData.Parent.FullPath, destData.FullPath);
        srcData.Parent.Items.Remove(srcData);
        srcData.Parent = destData;
        destData.Items.Add(srcData);
      } else {
        destData.GetSubFolders(true);
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
          menu.Items.Add(new MenuItem {Command = (ICommand)Resources["FolderNew"], CommandParameter = item});
          if (((Data.Folder) item).Parent != null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRename"], CommandParameter = stackPanel});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderDelete"], CommandParameter = item});
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
      McmCompressPictures.Visibility = ACore.Pictures.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
      McmReloadMetadata.Visibility = ACore.Pictures.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

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

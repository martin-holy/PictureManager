using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PictureManager.AvaloniaUI.Controls;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AUI = MH.UI.AvaloniaUI;

namespace PictureManager.AvaloniaUI;

public class CoreUI : ICoreP {
  public CoreUI() {
    CoreR.FileOperationDelete = FileOperationDelete;

    AUI.Utils.Init.SetDelegates();
    AUI.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;
    AUI.Controls.DialogHost.ContentTemplateSelector = new DialogHostContentTemplateSelector();
    AUI.Controls.CollectionViewHost.GroupByDialogDataTemplateSelector = new GroupByDialogDataTemplateSelector();

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
    CoreVM.DisplayScale = _getDisplayScale();
  }

  public void AfterInit() {
    // TODO PORT
  }

  private static double _getDisplayScale() =>
    Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }
      ? window.DesktopScaling
      : 1.0;

  public static Dictionary<string, string>? FileOperationDelete(List<string> items, bool recycle, bool silent) {
    // TODO PORT recycle
    items.Where(File.Exists).ToList().ForEach(File.Delete);
    return null;
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    Utils.Imaging.CreateImageThumbnail(srcPath, destPath, desiredSize, quality);
}
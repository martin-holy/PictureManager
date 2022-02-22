﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  public partial class FileOperationCollisionDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly List<string> _tempThumbs = new();
    private bool _error;
    private string _fileName;
    private FileInfo _srcFileInfo;
    private FileInfo _destFileInfo;
    private MediaItemM _srcMediaItem;
    private MediaItemM _destMediaItem;

    public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public FileInfo SrcFileInfo { get => _srcFileInfo; set { _srcFileInfo = value; OnPropertyChanged(); } }
    public FileInfo DestFileInfo { get => _destFileInfo; set { _destFileInfo = value; OnPropertyChanged(); } }
    public MediaItemM SrcMediaItem { get => _srcMediaItem; set { _srcMediaItem = value; OnPropertyChanged(); } }
    public MediaItemM DestMediaItem { get => _destMediaItem; set { _destMediaItem = value; OnPropertyChanged(); } }
    public string SrcFilePathCache => GetThumbFilePath(SrcFileInfo.FullName).Result;
    public string DestFilePathCache => GetThumbFilePath(DestFileInfo.FullName).Result;
    public string SrcFileSize => $"File size: {SrcFileInfo.Length} B";
    public string DestFileSize => $"File size: {DestFileInfo.Length} B";
    public string SrcFileModified => $"Modified: {SrcFileInfo.LastWriteTime}";
    public string DestFileModified => $"Modified: {DestFileInfo.LastWriteTime}";
    public string SrcDimensions => GetDimensions(SrcFileInfo.FullName);
    public string DestDimensions => GetDimensions(DestFileInfo.FullName);
    public static int MaxThumbSize => (int)(App.Core.ThumbnailSize * App.Core.ThumbnailsGridsM.DefaultThumbScale);
    public Visibility SrcThumbVisibility => SrcMediaItem == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SrcMediaItemThumbVisibility => SrcMediaItem != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DestThumbVisibility => DestMediaItem == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DestMediaItemThumbVisibility => DestMediaItem != null ? Visibility.Visible : Visibility.Collapsed;

    public CollisionResult Result { get; set; }

    public FileOperationCollisionDialog(string srcFilePath, string destFilePath, MediaItemM srcMediaItem, MediaItemM destMediaItem, Window owner) {
      SrcFileInfo = new(srcFilePath);
      DestFileInfo = new(destFilePath);
      SrcMediaItem = srcMediaItem;
      DestMediaItem = destMediaItem;
      FileName = SrcFileInfo.Name;
      Owner = owner;
      Result = CollisionResult.Skip;
      InitializeComponent();
    }

    private static string GetDimensions(string filePath) {
      var size = Imaging.GetImageDimensionsAsync(filePath).Result;
      return size == null ? string.Empty : $"Dimensions: {size[0]} x {size[1]}";
    }

    private async Task<string> GetThumbFilePath(string filePath) {
      var thumbPath = filePath.Replace(Path.VolumeSeparatorChar.ToString(), Settings.Default.CachePath);
      if (!File.Exists(thumbPath)) {
        _tempThumbs.Add(thumbPath);
        await Imaging.CreateThumbnailAsync(Imaging.GetMediaType(filePath), filePath, thumbPath,
          Settings.Default.ThumbnailSize, 0, Settings.Default.JpegQualityLevel);
      }

      return thumbPath;
    }

    private void BtnRename_OnClick(object sender, RoutedEventArgs e) {
      Error = false;
      TxtFileName.ToolTip = string.Empty;

      if (string.IsNullOrEmpty(FileName)) {
        Error = true;
        return;
      }

      if (Path.GetInvalidFileNameChars().Any(FileName.Contains)) {
        TxtFileName.ToolTip = "New file name contains incorrect character(s)!";
        Error = true;
        return;
      }

      var newFilePath = Path.Combine(DestFileInfo.DirectoryName, FileName);
      if (File.Exists(newFilePath)) {
        DestFileInfo = new(newFilePath);
        OnPropertyChanged(nameof(DestFilePathCache));
        OnPropertyChanged(nameof(DestFileSize));
        OnPropertyChanged(nameof(DestFileModified));
        return;
      }

      Result = CollisionResult.Rename;
      Close();
    }

    private void BtnReplace_OnClick(object sender, RoutedEventArgs e) {
      Result = CollisionResult.Replace;
      Close();
    }

    private void BtnSkip_OnClick(object sender, RoutedEventArgs e) {
      Result = CollisionResult.Skip;
      Close();
    }

    private void OnClosing(object sender, CancelEventArgs e) {
      foreach (var tempThumb in _tempThumbs) {
        if (File.Exists(tempThumb))
          File.Delete(tempThumb);
      }
    }
  }
}

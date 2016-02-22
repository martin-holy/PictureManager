using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PictureManager.Data;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WTestThumbnailGallery.xaml
  /// </summary>
  public partial class WTestThumbnailGallery {
    public WTestThumbnailGallery() {
      InitializeComponent();
    }

    public void AddPhotosInFolder(ObservableCollection<Picture> pictures) {
      foreach (Picture picture in pictures) {
        ListBoxItem item = new ListBoxItem();
        item.Padding = new Thickness(1, 2, 1, 2);

        Image image = new Image();
        using (var stream = new FileStream(picture.FilePathCache, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
          image.Width = bitmapFrame.PixelWidth;
          image.Height = bitmapFrame.PixelHeight;
        }

        BitmapImage bi = new BitmapImage(new Uri(picture.FilePathCache));
        bi.Freeze();
        image.Stretch = Stretch.None;
        image.Source = new BitmapImage(new Uri(picture.FilePathCache));
        item.Content = image;

        pictureBox.Items.Add(item);
      }
    }
  }
}

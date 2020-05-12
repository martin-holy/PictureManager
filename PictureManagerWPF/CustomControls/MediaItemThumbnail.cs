using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class MediaItemThumbnail: Control {
    private Grid _grid;
    static MediaItemThumbnail() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaItemThumbnail),
        new FrameworkPropertyMetadata(typeof(MediaItemThumbnail)));
    }

    public override void OnApplyTemplate() {
      _grid = Template.FindName("PART_Grid", this) as Grid;
      base.OnApplyTemplate();
    }

    public void InsertPlayer(UIElement player) {
      _grid.Children.Insert(2, player);
    }
  }
}

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager {
  public partial class WTest {
    public ObservableCollection<ICatTreeViewItem> Items { get; set; }
    
    public WTest() {
      InitializeComponent();
      //LoadCatTreeViewData();
      //SetUpMediaItemTest();
      //StrToDoubleTest();
      App.WMain.CommandsController.AddCommandBindings(CommandBindings);
    }

    private void StrToDoubleTest() {
      "1500.10".TryParseDoubleUniversal(out var res1);
      "1500,14".TryParseDoubleUniversal(out var res2);
      "1,500.14 kè".TryParseDoubleUniversal(out var res3);
      "$1.500,14".TryParseDoubleUniversal(out var res4);
      "".TryParseDoubleUniversal(out var res5);
    }

    private void SetUpMediaItemTest() {
      App.Ui.MediaItemClipsCategory.SetMediaItem(
        App.Core.MediaItems.All.Cast<MediaItem>().FirstOrDefault(mi => mi.Id == 138791));

      TvCategories.ItemsSource = new ObservableCollection<ICatTreeViewItem> {App.Ui.MediaItemClipsCategory};
    }

    public void AllowDropCheck(object sender, DragEventArgs e) {
      //TvCategories.AllowDropCheck(sender, e);
    }

    private void EventSetter_OnHandler(object sender, MouseEventArgs e) {
      
    }
  }
}

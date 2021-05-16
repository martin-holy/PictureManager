using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager {
  public partial class WTest {
    public ObservableCollection<ICatTreeViewBaseItem> Items { get; set; }
    
    public WTest() {
      InitializeComponent();
      //LoadCatTreeViewData();
      //SetUpMediaItemTest();
      StrToDoubleTest();

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
      App.Core.MediaItemClipsCategory.SetMediaItem(
        Core.Instance.MediaItems.All.FirstOrDefault(mi => mi.Id == 138791));

      TvCategories.ItemsSource = new ObservableCollection<ICatTreeViewBaseItem> {App.Core.MediaItemClipsCategory};
    }

    public void AllowDropCheck(object sender, DragEventArgs e) {
      //TvCategories.AllowDropCheck(sender, e);
    }

    private void EventSetter_OnHandler(object sender, MouseEventArgs e) {
      
    }
  }
}

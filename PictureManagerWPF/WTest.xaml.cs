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
      SetUpMediaItemTest();

      App.WMain.CommandsController.AddCommandBindings(CommandBindings);
    }

    private void SetUpMediaItemTest() {
      Core.Instance.MediaItemClipsCategory.SetMediaItem(
        Core.Instance.MediaItems.All.FirstOrDefault(mi => mi.Id == 138791));

      TvCategories.ItemsSource = new ObservableCollection<ICatTreeViewBaseItem> {Core.Instance.MediaItemClipsCategory};
    }

    public void AllowDropCheck(object sender, DragEventArgs e) {
      //TvCategories.AllowDropCheck(sender, e);
    }

    private void EventSetter_OnHandler(object sender, MouseEventArgs e) {
      
    }
  }
}

using PictureManager.Commands;
using PictureManager.Domain.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Interfaces;

namespace PictureManager {
  public partial class WTest {
    public ObservableCollection<ICatTreeViewItem> Items { get; set; }

    public WTest() {
      InitializeComponent();
      //LoadCatTreeViewData();
      //SetUpMediaItemTest();
      //StrToDoubleTest();
      CommandsController.AddCommandBindings(CommandBindings);
    }

    private void StrToDoubleTest() {
      "1500.10".TryParseDoubleUniversal(out var res1);
      "1500,14".TryParseDoubleUniversal(out var res2);
      "1,500.14 k�".TryParseDoubleUniversal(out var res3);
      "$1.500,14".TryParseDoubleUniversal(out var res4);
      "".TryParseDoubleUniversal(out var res5);
    }

    public void AllowDropCheck(object sender, DragEventArgs e) {
      //TvCategories.AllowDropCheck(sender, e);
    }

    private void EventSetter_OnHandler(object sender, MouseEventArgs e) {

    }

    private void IconButton_Click(object sender, RoutedEventArgs e) {

    }
  }
}

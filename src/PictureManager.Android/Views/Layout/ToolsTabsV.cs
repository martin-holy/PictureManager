using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public sealed class ToolsTabsV : TabControlHost {
  public ToolsTabsV(Context context, TabControl dataContext) : base(context, dataContext) {
    _initialize();
  }

  protected override View? _viewFactory(Context context, object? item) =>
    item switch {
      PeopleToolsTabVM peopleToolsTab => new PeopleToolsTabV(context, peopleToolsTab),
      PersonDetailVM personDetail => new PersonDetailV(context, personDetail),
      SegmentsDrawerVM segmentsDrawer => new SegmentsDrawerV(context, segmentsDrawer),
      VideoVM videoVM => new VideoDetailV(context, videoVM),
      _ => null
    };

  protected override View? _slotFactory(Context context, object? item) =>
    item is MainMenuVM mainMenuVM ? new MainMenuV(context, mainMenuVM) : null;

  private sealed class MainMenuV : FrameLayout {
    public MainMenuV(Context context, MainMenuVM mainMenuVM) : base(context) {
      AddView(new ButtonMenu(context, mainMenuVM, mainMenuVM.Icon), LPU.FrameWrap()
        .WithMargin(DimensU.Spacing, DimensU.Spacing, 0, 0));
    }
  }
}
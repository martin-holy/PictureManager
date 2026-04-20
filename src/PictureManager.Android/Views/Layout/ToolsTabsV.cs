using Android.Content;
using Android.Views;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Controls;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Layout;

public sealed class ToolsTabsV : TabControlHost {
  public ToolsTabsV(Context context, TabControl dataContext) : base(context, dataContext) { }

  protected override View? _getItemView(Context context, object? item) =>
    item switch {
      PeopleToolsTabVM peopleToolsTab => new PeopleToolsTabV(context, peopleToolsTab),
      PersonDetailVM personDetail => new PersonDetailV(context, personDetail),
      SegmentsDrawerVM segmentsDrawer => new SegmentsDrawerV(context, segmentsDrawer),
      VideoVM videoVM => new VideoDetailV(context, videoVM),
      _ => null
    };
}
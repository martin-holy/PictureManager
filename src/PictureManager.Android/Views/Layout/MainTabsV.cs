using Android.Content;
using Android.Views;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Android.Views;
using MH.UI.Controls;
using MH.UI.ViewModels;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Layout;

public sealed class MainTabsV : TabControlHost {
  public MainTabsV(Context context, TabControl dataContext) : base(context, dataContext) {
    _initialize();
  }

  protected override View? _viewFactory(Context context, object? item) =>
    item switch {
      AllSettings allSettings => new SettingsV(context, allSettings),
      MediaItemsViewVM miv => new MediaItemsViewV(context, miv),
      PeopleVM people => new PeopleV(context, people),
      SegmentsViewsVM segmentsViews => new SegmentsViewsV(context, segmentsViews),
      LogVM log => new LogV(context, log),
      View view => view,
      _ => null
    };
}
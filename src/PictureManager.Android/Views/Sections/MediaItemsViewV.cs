using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Sections;

public class MediaItemsViewV : FrameLayout {
  private readonly CollectionViewHost _host;
  private readonly TextView _loadingText;
  private readonly LinearLayout _importContainer;
  private readonly TextView _importText;
  private readonly ProgressBar _importProgress;
  private readonly Button _importCancelButton;
  private readonly BindingScope _bindings = new();

  public MediaItemsViewVM DataContext { get; }

  public MediaItemsViewV(Context context, MediaItemsViewVM dataContext) : base(context) {
    DataContext = dataContext;
    SetBackgroundResource(Resource.Color.c_static_ba);

    _loadingText = new TextView(context) { Text = "Loading ...", TextSize = 18 };
    _importText = new TextView(context);
    _importProgress = new ProgressBar(context);
    _importCancelButton = new Button(new ContextThemeWrapper(context, Resource.Style.mh_DialogButton), null, 0)
      .WithClickCommand(DataContext.Import.CancelCommand, _bindings);

    _importContainer = LayoutU.Vertical(context)
      .Add(_importText, LPU.LinearWrap().WithDpMargin(0, 0, 0, 6))
      .Add(_importProgress, LPU.LinearMatchWrap().WithDpMargin(6, 0, 6, 0))
      .Add(_importCancelButton, LPU.Linear(LPU.Wrap, LPU.Wrap, GravityFlags.End).WithDpMargin(0, 6, 6, 0));
    _importContainer.SetGravity(GravityFlags.Center);

    _host = new CollectionViewHost(context, dataContext, _createItemContent);

    AddView(_loadingText, LPU.Frame(LPU.Wrap, LPU.Wrap, GravityFlags.Center));
    AddView(_importContainer, LPU.FrameMatch().WithMargin(DimensU.Spacing));
    AddView(_host, LPU.FrameMatch());

    _bindings.AddRange([
      dataContext.Bind(nameof(MediaItemsViewVM.IsLoading), x => x.IsLoading, _ => _updateVisibility()),
      dataContext.Import.Bind(nameof(MediaItemsImport.IsImporting), x => x.IsImporting, _ => _updateVisibility()),
      dataContext.Import.Bind(nameof(MediaItemsImport.Count), x => x.Count, count => {
        _importText.Text = $"Importing {count} new items ...";
        _importProgress.Max = count;
      }),
      dataContext.Import.Bind(nameof(MediaItemsImport.DoneCount), x => x.DoneCount, x => _importProgress.Progress = x)]);
  }

  private MediaItemThumbFullV _createItemContent(Context context, ICollectionViewGroup group) =>
    new(context);

  private void _updateVisibility() {
    _loadingText.Visibility = DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _importContainer.Visibility = DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _host.Visibility = !DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}
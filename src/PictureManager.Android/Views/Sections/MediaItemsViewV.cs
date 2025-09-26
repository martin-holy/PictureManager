﻿using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.MediaItem;
using System;

namespace PictureManager.Android.Views.Sections;

public class MediaItemsViewV : LinearLayout, IDisposable {
  private bool _disposed;
  private readonly CollectionViewHost _host;
  private readonly TextView _loadingText;
  private readonly LinearLayout _importContainer;
  private readonly TextView _importText;
  private readonly ProgressBar _importProgress;
  private readonly Button _importCancelButton;

  public MediaItemsViewVM DataContext { get; }

  public MediaItemsViewV(Context context, MediaItemsViewVM dataContext) : base(context) {
    DataContext = dataContext;
    Orientation = Orientation.Vertical;
    SetBackgroundResource(Resource.Color.c_static_ba);

    _loadingText = new TextView(context) { Text = "Loading ...", TextSize = 18 };
    AddView(_loadingText, new LayoutParams(LPU.Match, LPU.Match) { Gravity = GravityFlags.Center });

    _importText = new TextView(context);
    _importProgress = new ProgressBar(context);
    _importCancelButton = new Button(new ContextThemeWrapper(context, Resource.Style.mh_DialogButton), null, 0);
    _importCancelButton.Bind(DataContext.Import.CancelCommand);
    _importContainer = new LinearLayout(context) { Orientation = Orientation.Vertical };
    _importContainer.SetGravity(GravityFlags.Center);
    _importContainer.SetPadding(DimensU.Spacing);
    _importContainer.AddView(_importText, new LayoutParams(LPU.Wrap, LPU.Wrap).WithDpMargin(0, 0, 0, 6));
    _importContainer.AddView(_importProgress, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(6, 0, 6, 0));
    _importContainer.AddView(_importCancelButton, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.End }.WithDpMargin(0, 6, 6, 0));
    AddView(_importContainer, new LayoutParams(LPU.Match, LPU.Match));

    _host = new CollectionViewHost(context, dataContext, _getItemView);
    AddView(_host);

    this.Bind(dataContext, x => x.IsLoading, (_, _) => _updateVisibility());
    this.Bind(dataContext.Import, x => x.IsImporting, (_, _) => _updateVisibility());
    this.Bind(dataContext.Import, x => x.Count, (_, count) => {
      _importText.Text = $"Importing {count} new items ...";
      _importProgress.Max = count;
    });
    _importProgress.Bind(dataContext.Import, x => x.DoneCount, (view, doneCount) => view.Progress = doneCount);
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _host.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;
    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!).Bind(mi),
      _ => null
    };
  }

  private void _updateVisibility() {
    _loadingText.Visibility = DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _importContainer.Visibility = DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _host.Visibility = !DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
  }
}
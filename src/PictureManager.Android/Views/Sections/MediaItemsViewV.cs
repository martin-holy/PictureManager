using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using MH.Utils.Extensions;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.MediaItem;
using System;
using System.ComponentModel;

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
    LayoutParameters = new(LayoutParams.MatchParent, LayoutParams.MatchParent);
    SetBackgroundResource(Resource.Color.c_static_ba);

    _loadingText = new TextView(context) {
      LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent) {
        Gravity = GravityFlags.Center
      },
      Text = "Loading ...",
      TextSize = 18
    };
    AddView(_loadingText);

    _importContainer = new LinearLayout(context) {
      LayoutParameters = new(LayoutParams.MatchParent, LayoutParams.MatchParent),
      Orientation = Orientation.Vertical
    };
    _importContainer.SetGravity(GravityFlags.Center);
    _importContainer.SetPadding(context.Resources!.GetDimensionPixelSize(Resource.Dimension.general_padding));

    _importText = new TextView(context) {
      LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent) {
        BottomMargin = DisplayU.DpToPx(6)
      }
    };

    _importProgress = new ProgressBar(context) {
      LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent) {
        LeftMargin = DisplayU.DpToPx(6),
        RightMargin = DisplayU.DpToPx(6)
      }
    };

    _importCancelButton = new Button(context) {
      LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent) {
        Gravity = GravityFlags.End,
        TopMargin = DisplayU.DpToPx(6),
        RightMargin = DisplayU.DpToPx(6)
      },
      Text = "Cancel"
    };
    _importCancelButton.Click += _onImportCancelButtonClick;

    _importContainer.AddView(_importText);
    _importContainer.AddView(_importProgress);
    _importContainer.AddView(_importCancelButton);
    AddView(_importContainer);

    _host = new CollectionViewHost(context, dataContext, _getItemView);
    AddView(_host);

    dataContext.PropertyChanged += _onDataContextPropertyChanged;
    dataContext.Import.PropertyChanged += _onImportPropertyChanged;

    _updateVisibility();
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _importCancelButton.Click -= _onImportCancelButtonClick;
      DataContext.PropertyChanged -= _onDataContextPropertyChanged;
      DataContext.Import.PropertyChanged -= _onImportPropertyChanged;

      _host.Dispose();
      _loadingText.Dispose();      
      _importText.Dispose();
      _importProgress.Dispose();
      _importCancelButton.Dispose();
      _importContainer.RemoveAllViews();
      _importContainer.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private void _onImportCancelButtonClick(object? sender, EventArgs e) =>
    DataContext.Import.CancelCommand.Execute(null);

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;
    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!).Bind(mi),
      _ => null
    };
  }

  private void _onDataContextPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MediaItemsViewVM.IsLoading)))
      _updateVisibility();
  }

  private void _onImportPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    switch (e.PropertyName) {
      case nameof(MediaItemsViewVM.Import.IsImporting):
        _updateVisibility();
        break;
      case nameof(MediaItemsViewVM.Import.Count):
        _importText.Text = $"Importing {DataContext!.Import.Count} new items ...";
        _importProgress.Max = DataContext.Import.Count;    
        break;
      case nameof(MediaItemsViewVM.Import.DoneCount):
        _importProgress.Progress = DataContext!.Import.DoneCount;
        break;
    }
  }

  private void _updateVisibility() {
    _loadingText.Visibility = DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _importContainer.Visibility = DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _host.Visibility = !DataContext.IsLoading && !DataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
  }
}
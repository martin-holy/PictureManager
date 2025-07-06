using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using MH.Utils.Extensions;
using PictureManager.Common.Features.MediaItem;
using System.ComponentModel;

namespace PictureManager.Android.Views.Sections;

public class MediaItemsViewV : LinearLayout {
  private CollectionViewHost _host = null!;
  private TextView _loadingText = null!;
  private LinearLayout _importContainer = null!;
  private TextView _importText = null!;
  private ProgressBar _importProgress = null!;
  private Button _importCancelButton = null!;
  private MediaItemsViewVM? _dataContext;

  public MediaItemsViewVM? DataContext {
    get => _dataContext;
    private set {
      if (_dataContext != null) {
        _dataContext.PropertyChanged -= _onDataContextPropertyChanged;
        _dataContext.Import.PropertyChanged -= _onImportPropertyChanged;
      }
      _dataContext = value;
      if (_dataContext != null) {
        _dataContext.PropertyChanged += _onDataContextPropertyChanged;
        _dataContext.Import.PropertyChanged += _onImportPropertyChanged;
      }
    }
  }

  public MediaItemsViewV(Context context) : base(context) => _initialize(context);
  public MediaItemsViewV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MediaItemsViewV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = new(LayoutParams.MatchParent, LayoutParams.MatchParent);
    SetBackgroundResource(Resource.Color.c_static_ba);
    var genPadding = context.Resources!.GetDimensionPixelSize(Resource.Dimension.general_padding);

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
    _importContainer.SetPadding(genPadding, genPadding, genPadding, genPadding);

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

    _importContainer.AddView(_importText);
    _importContainer.AddView(_importProgress);
    _importContainer.AddView(_importCancelButton);
    AddView(_importContainer);

    _host = new CollectionViewHost(context) { GetItemView = _getItemView };
    AddView(_host);
  }

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;
    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!).Bind(mi, DataContext!, group),
      _ => null
    };
  }

  public MediaItemsViewV Bind(MediaItemsViewVM? dataContext) {
    DataContext = dataContext;
    if (dataContext == null) return this;
    _host.Bind(dataContext);
    _importCancelButton.Click += delegate { dataContext.Import.CancelCommand.Execute(null); };
    _updateVisibility();
    return this;
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
        _importText.Text = $"Importing {_dataContext!.Import.Count} new items ...";
        _importProgress.Max = _dataContext.Import.Count;    
        break;
      case nameof(MediaItemsViewVM.Import.DoneCount):
        _importProgress.Progress = _dataContext!.Import.DoneCount;
        break;
    }
  }

  private void _updateVisibility() {
    if (_dataContext == null) return;
    _loadingText.Visibility = _dataContext.IsLoading && !_dataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _importContainer.Visibility = _dataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
    _host.Visibility = !_dataContext.IsLoading && !_dataContext.Import.IsImporting ? ViewStates.Visible : ViewStates.Gone;
  }
}
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Common;
using System.Collections.ObjectModel;

namespace PictureManager.Android.Views.Sections;

public sealed class TreeViewSearchV : LinearLayout {
  private readonly EditText _searchText;
  private readonly RecyclerView _searchResult;
  private readonly SearchAdapter _adapter;
  private bool _disposed;

  public TreeViewSearchVM DataContext { get; }

  public TreeViewSearchV(Context context, TreeViewSearchVM dataContext, BindingScope bindings) : base(context) {
    DataContext = dataContext;
    _adapter = new(dataContext.SearchResult, this);
    Orientation = Orientation.Vertical;
    SetBackgroundResource(Resource.Color.c_black5);

    _searchText = new EditText(context)
      .BindText(dataContext, nameof(TreeViewSearchVM.SearchText), x => x.SearchText, (s, p) => s.SearchText = p, bindings);

    var searchBar = new LinearLayout(context) { Orientation = Orientation.Horizontal };
    searchBar.SetBackgroundResource(Resource.Color.c_static_ba);
    searchBar.AddView(_searchText, new LinearLayout.LayoutParams(0, LPU.Wrap, 1f));
    searchBar.AddView(new IconButton(context).WithClickCommand(dataContext.CloseCommand, bindings));

    _searchResult = new(context);
    _searchResult.SetLayoutManager(new LinearLayoutManager(context, LinearLayoutManager.Vertical, false));
    _searchResult.SetAdapter(_adapter);
    _searchResult.BindVisibility(DataContext.SearchResult, "Count", x => x.Count > 0, bindings);

    AddView(searchBar, new LayoutParams(LPU.Match, LPU.Wrap));
    AddView(_searchResult, new LayoutParams(LPU.Match, 0, 1).WithMargin(DimensU.Spacing));

    dataContext.SearchResult.Bind((c, e) => Tasks.Dispatch(_adapter.NotifyDataSetChanged)).DisposeWith(bindings);
  }

  protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility) {
    base.OnVisibilityChanged(changedView, visibility);
    if (!ReferenceEquals(this, changedView)) return;
    if (Context?.GetSystemService(Context.InputMethodService) is not InputMethodManager imm) return;

    if (visibility == ViewStates.Visible) {
      _searchText.RequestFocus();
      imm.ShowSoftInput(_searchText, ShowFlags.Implicit);
    } else {
      _searchText.ClearFocus();
      imm.HideSoftInputFromWindow(_searchText.WindowToken, HideSoftInputFlags.None);
    }
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _searchResult.SetAdapter(null);
      _adapter.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private class SearchAdapter(ObservableCollection<TreeViewSearchItemM> items, TreeViewSearchV treeViewSearchV) : RecyclerView.Adapter {
    public override int ItemCount => items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
      new TreeViewSearchItemViewHolder(parent.Context!, treeViewSearchV);

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) =>
      ((TreeViewSearchItemViewHolder)holder).Bind(items[position]);

    public override void OnViewRecycled(Java.Lang.Object holder) {
      ((TreeViewSearchItemViewHolder)holder).Unbind();
      base.OnViewRecycled(holder);
    }
  }

  private class TreeViewSearchItemViewHolder : RecyclerView.ViewHolder {
    private readonly TreeViewSearchV _treeViewSearchV;
    private readonly LinearLayout _container;
    private readonly IconButton _icon;
    private readonly TextView _name;
    private readonly CommandBinding _navigateToCommandBinding;
    private bool _disposed;

    public TreeViewSearchItemM? DataContext { get; private set; }

    public TreeViewSearchItemViewHolder(Context context, TreeViewSearchV treeViewSearchV) : base(_createContainerView(context)) {
      _treeViewSearchV = treeViewSearchV;
      _icon = new IconButton(context);
      _name = new TextView(context);
      _container = (LinearLayout)ItemView;
      _container.AddView(_icon);
      _container.AddView(_name);
      _navigateToCommandBinding = new(_container);
    }

    public void Bind(TreeViewSearchItemM? item) {
      DataContext = item;
      if (item == null) return;

      _navigateToCommandBinding.Bind(_treeViewSearchV.DataContext.NavigateToCommand, item);
      _icon.SetImageDrawable(IconU.GetIcon(ItemView.Context, item.Icon));
      _name.Text = item.Name;
    }

    public void Unbind() {
      _navigateToCommandBinding.Unbind();
    }

    private static LinearLayout _createContainerView(Context context) {
      var container = new LinearLayout(context) {
        Orientation = Orientation.Horizontal,
        LayoutParameters = new RecyclerView.LayoutParams(LPU.Match, LPU.Wrap),
        Clickable = true,
        Focusable = true
      };
      container.SetGravity(GravityFlags.CenterVertical);
      container.SetPadding(DimensU.Spacing);
      container.SetBackgroundResource(Resource.Color.c_static_ba);

      return container;
    }

    protected override void Dispose(bool disposing) {
      if (_disposed) return;
      if (disposing) {
        Unbind();
        _navigateToCommandBinding.Dispose();
      }
      _disposed = true;
      base.Dispose(disposing);
    }
  }
}
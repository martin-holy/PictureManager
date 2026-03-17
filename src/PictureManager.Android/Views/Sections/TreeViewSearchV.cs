using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Recycler;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Sections;

public sealed class TreeViewSearchV : LinearLayout {
  private readonly EditText _searchText;
  private readonly RecyclerView _searchResult;
  private readonly BindableAdapter<TreeViewSearchItemM> _adapter;
  private bool _disposed;

  public TreeViewSearchVM DataContext { get; }

  public TreeViewSearchV(Context context, TreeViewSearchVM dataContext, BindingScope bindings) : base(context) {
    DataContext = dataContext;
    Orientation = Orientation.Vertical;
    SetBackgroundResource(Resource.Color.c_black5);

    _adapter = new(
      () => dataContext.SearchResult,
      ctx => new TreeViewSearchItemV(ctx, this),
      () => new(LPU.Match, LPU.Wrap));

    _searchText = new EditText(context)
      .BindText(dataContext, nameof(TreeViewSearchVM.SearchText), x => x.SearchText, (s, p) => s.SearchText = p, bindings);
    var closeBtn = new IconButton(context).WithClickCommand(dataContext.CloseCommand, bindings);

    var searchBar = LayoutU.Horizontal(context)
      .Add(_searchText, LPU.Linear(0, LPU.Wrap, 1f))
      .Add(closeBtn, LPU.LinearWrap());
    searchBar.SetBackgroundResource(Resource.Color.c_static_ba);

    _searchResult = new(context);
    _searchResult.SetLayoutManager(new LinearLayoutManager(context, LinearLayoutManager.Vertical, false));
    _searchResult.SetAdapter(_adapter);
    _searchResult.BindVisibility(DataContext.SearchResult, "Count", x => x.Count > 0, bindings);

    AddView(searchBar, LPU.LinearMatchWrap());
    AddView(_searchResult, LPU.Linear(LPU.Match, 0, 1).WithMargin(DimensU.Spacing));

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

  private class TreeViewSearchItemV : LinearLayout, IBindable<TreeViewSearchItemM> {
    private readonly TreeViewSearchV _treeViewSearchV;
    private readonly IconButton _icon;
    private readonly TextView _name;
    private readonly CommandBinding _navigateToCommandBinding;
    private bool _disposed;

    public TreeViewSearchItemM? DataContext { get; private set; }

    public TreeViewSearchItemV(Context context, TreeViewSearchV treeViewSearchV) : base(context) {
      Orientation = Orientation.Horizontal;
      Clickable = true;
      Focusable = true;
      SetGravity(GravityFlags.CenterVertical);
      this.SetPadding(DimensU.Spacing);
      SetBackgroundResource(Resource.Color.c_static_ba);

      _treeViewSearchV = treeViewSearchV;
      _icon = new IconButton(context);
      _name = new TextView(context);
      AddView(_icon);
      AddView(_name);
      _navigateToCommandBinding = new(this);
    }

    public void Bind(TreeViewSearchItemM? item) {
      DataContext = item;
      if (item == null) return;

      _navigateToCommandBinding.Bind(_treeViewSearchV.DataContext.NavigateToCommand, item);
      _icon.SetImageDrawable(IconU.GetIcon(Context, item.Icon));
      _name.Text = item.Name;
    }

    public void Unbind() {
      _navigateToCommandBinding.Unbind();
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
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.DataViews {
  public sealed class MediaItemsViews : ObservableObject {
    private readonly Core _core;
    private List<MediaItemsView> _all { get; } = new();
    private MediaItemsView _current;
    
    public MediaItemsView Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public static double DefaultThumbScale { get; set; } = 1.0;

    public RelayCommand<string> AddViewCommand { get; }
    public RelayCommand<object> CopyPathsCommand { get; }
    public RelayCommand<object> LoadByTagCommand { get; }
    public RelayCommand<object> ShuffleCommand { get; }

    public MediaItemsViews(Core core) {
      _core = core;
      AddViewCommand = new(AddView);
      CopyPathsCommand = new(
        () => Clipboard.SetText(string.Join("\n", Current.Selected.Items.Select(x => x.FilePath))),
        () => Current?.Selected.Items.Any() == true);
      LoadByTagCommand = new(async item => await LoadByTag(item));
      ShuffleCommand = new(
        () => Current.Shuffle(),
        () => Current?.FilteredItems.Count > 0);
    }

    public void RemoveMediaItems(List<MediaItemM> items) {
      foreach (var view in _all)
        view.Remove(items, Current == view);
    }

    public void CloseView(MediaItemsView view) {
      view.Clear();
      view.SelectionChangedEventHandler -= OnViewSelectionChanged;
      view.FilteredChangedEventHandler -= OnViewFilteredChanged;
      _all.Remove(view);

      if (view.Equals(Current)) {
        Current = null;
        _core.MediaItemsM.Current = null;
      }
    }

    public void SetCurrentView(MediaItemsView view) {
      Current = view;
      Current?.UpdateSelected();
      _core.MediaItemsM.Current = Current?.Selected.Items.Count > 0
        ? Current.Selected.Items[0]
        : null;
    }

    public void AddViewIfNotActive(string tabName) {
      if (_core.MainTabs.Selected?.Data is MediaItemsView) {
        if (tabName != null)
          _core.MainTabs.Selected.Name = tabName;

        return;
      }

      AddView(tabName);
    }

    public void AddView(string tabName) {
      var view = new MediaItemsView(_core, DefaultThumbScale);
      _all.Add(view);
      Current = view;
      view.SelectionChangedEventHandler += OnViewSelectionChanged;
      view.FilteredChangedEventHandler += OnViewFilteredChanged;
      _core.MainTabs.Add(Res.IconImageMultiple, tabName, view);
    }

    private void OnViewSelectionChanged(object o, EventArgs e) {
      _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
      _core.StatusPanelM.Update();
    }

    private void OnViewFilteredChanged(object o, EventArgs e) {
      _core.TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
    }

    public void ReloadViewIfContains(MediaItemM[] mediaItems) {
      foreach (var view in _all)
        view.ReGroupItems(view.FilteredItems.Where(mediaItems.Contains).ToArray(), false);
    }

    public async Task LoadByFolder(ITreeItem item) {
      if (item is FolderM { IsAccessible: false }) return;
      var and = Keyboard.IsCtrlOn();
      var hide = Keyboard.IsAltOn();
      var recursive = Keyboard.IsShiftOn();

      item.IsSelected = true;
      AddViewIfNotActive(and || hide ? null : item.Name);
      await Current.LoadByFolder(item, and, hide, recursive);
    }

    public async Task LoadByTag(object item) {
      var and = Keyboard.IsCtrlOn();
      var hide = Keyboard.IsAltOn();
      var recursive = Keyboard.IsShiftOn();
      var items = item switch {
        RatingTreeM rating => _core.MediaItemsM.DataAdapter.All.Where(x => x.Rating == rating.Rating.Value),
        PersonM person => _core.MediaItemsM.GetMediaItems(person),
        KeywordM keyword => _core.MediaItemsM.GetMediaItems(keyword, recursive),
        GeoNameM geoName => _core.MediaItemsM.GetMediaItems(geoName, recursive),
        _ => Array.Empty<MediaItemM>()
      };

      // if CTRL is pressed, add new items to already loaded items
      if (and) items = Current.LoadedItems.Union(items);
      // if ALT is pressed, remove new items from already loaded items
      if (hide) items = Current.LoadedItems.Except(items);

      var tabTitle = and || hide
        ? null
        : item switch {
          RatingTreeM rating => rating.Rating.Value.ToString(),
          PersonM person => person.Name,
          KeywordM keyword => keyword.Name,
          GeoNameM geoName => geoName.Name,
          _ => string.Empty
        };

      if (and || hide)
        AddViewIfNotActive(tabTitle);
      else
        AddView(tabTitle);

      await Current.LoadByTag(items.ToArray());
    }
  }
}

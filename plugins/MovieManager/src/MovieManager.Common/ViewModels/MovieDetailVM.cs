using MH.UI;
using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.CollectionViews;
using MovieManager.Common.Models;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using PictureManager.Interfaces.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MovieManager.Common.ViewModels;

public sealed class MovieDetailVM : ObservableObject {
  private readonly IPMCoreVM _pmCoreVM;
  private readonly CoreR _coreR;
  private readonly CoreS _coreS;
  private MovieM _movieM;

  public MovieM MovieM { get => _movieM; set { _movieM = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastSeenDate)); } }
  public CollectionViewCharacters Characters { get; } = new();
  public string LastSeenDate => MovieM == null || MovieM.Seen.Count == 0 ? string.Empty : MovieM.Seen.Last().ToShortDateString();

  public RelayCommand AddMediaItemsCommand { get; }
  public RelayCommand RemoveMediaItemsCommand { get; }
  public AsyncRelayCommand ViewMediaItemsCommand { get; }
  public RelayCommand SetCharacterSegmentCommand { get; }
  public RelayCommand<ObservableCollection<DateTime>> AddSeenDateCommand { get; }
  public RelayCommand<DateOnly> RemoveSeenDateCommand { get; }
  public RelayCommand MyRatingChangedCommand { get; }

  public MovieDetailVM(IPMCoreVM pmCoreVM, CoreR coreR, CoreS coreS) {
    _pmCoreVM = pmCoreVM;
    _coreR = coreR;
    _coreS = coreS;

    AddMediaItemsCommand = new(AddMediaItems, CanAddMediaItems, "IconImageMultiple", "Add Media items");
    RemoveMediaItemsCommand = new(RemoveMediaItems, CanRemoveMediaItems, "IconImageMultiple", "Remove Media items");
    ViewMediaItemsCommand = new(ViewMediaItems, () => MovieM.MediaItems?.Count > 0, "IconImageMultiple", "View Media items");
    SetCharacterSegmentCommand = new(SetCharacterSegment, CanSetCharacterSegment, "IconSegment", "Set Character Segment");
    AddSeenDateCommand = new(AddSeenDate);
    RemoveSeenDateCommand = new(RemoveSeenDate, Res.IconXCross, "Remove");
    MyRatingChangedCommand = new(() => _coreR.Movie.IsModified = true);
  }

  public void Reload(MovieM movie) {
    MovieM = movie;

    if (MovieM == null) {
      Characters.Root?.Clear();
      return;
    }

    var charSource = Core.R.Character.All.Where(x => ReferenceEquals(x.Movie, movie)).ToList();
    Characters.Reload(charSource, GroupMode.ThenByRecursive, null, true);
  }

  private void AddMediaItems() {
    var mis = _pmCoreVM.GetActive();
    if (Dialog.Show(new MessageDialog(
          "Adding Media items to Movie",
          "Do you really want to add {0} Media item{1} to Movie?".Plural(mis.Length),
          "IconMovieClapper",
          true)) != 1) return;

    _coreR.Movie.AddMediaItems(MovieM, mis);
  }

  private bool CanAddMediaItems() {
    var mis = _pmCoreVM.GetActive();
    return mis.Length > 0 && (MovieM.MediaItems == null || mis.Any(x => !MovieM.MediaItems.Contains(x)));
  }

  private void RemoveMediaItems() {
    var mis = _pmCoreVM.GetActive().Intersect(MovieM.MediaItems).ToArray();
    if (Dialog.Show(new MessageDialog(
          "Removing Media items from Movie",
          "Do you really want to remove {0} Media item{1} from Movie?".Plural(mis.Length),
          "IconMovieClapper",
          true)) != 1) return;

    _coreR.Movie.RemoveMediaItems(MovieM, mis);
  }

  private bool CanRemoveMediaItems() =>
    MovieM.MediaItems?.Count > 0 && _pmCoreVM.GetActive().Any(MovieM.MediaItems.Contains);

  private Task ViewMediaItems() =>
    _pmCoreVM.MediaItem.ViewMediaItems([.. MovieM.MediaItems], MovieM.Title);

  private void SetCharacterSegment() {
    if (_coreS.Character.Selected.Items.FirstOrDefault() is not { } character) return;
    _coreR.Character.SetSegment(character, _coreS.PMCoreS.Segment.GetSelected().FirstOrDefault());
    character.OnPropertyChanged(nameof(character.DisplaySegment));
  }

  private bool CanSetCharacterSegment() {
    var segments = _coreS.PMCoreS.Segment.GetSelected();
    var characters = _coreS.Character.Selected.Items;

    return segments.Length == 1
           && segments[0].Person != null
           && characters.Count == 1
           && ReferenceEquals(segments[0].Person, characters[0].Actor.Person);
  }

  private void AddSeenDate(ObservableCollection<DateTime> selectedDates) {
    if (selectedDates?.FirstOrDefault() is not { } dt) return;
    _coreR.Movie.AddSeenDate(MovieM, new(dt.Year, dt.Month, dt.Day));
    OnPropertyChanged(nameof(LastSeenDate));
  }

  private void RemoveSeenDate(DateOnly date) {
    _coreR.Movie.RemoveSeenDate(MovieM, date);
    OnPropertyChanged(nameof(LastSeenDate));
  }
}
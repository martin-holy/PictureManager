using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.CollectionViews;
using MovieManager.Common.Models;
using MovieManager.Common.Repositories;
using PictureManager.Interfaces.ViewModels;
using System.Linq;
using MovieManager.Common.Services;

namespace MovieManager.Common.ViewModels;

public sealed class MovieDetailVM : ObservableObject {
  private readonly ICoreVM _phCoreVM;
  private readonly CoreR _coreR;
  private readonly CoreS _coreS;
  private MovieM _movieM;

  public MovieM MovieM { get => _movieM; set { _movieM = value; OnPropertyChanged(); } }
  public CollectionViewCharacters Characters { get; } = new();

  public RelayCommand AddMediaItemsCommand { get; }
  public RelayCommand SetCharacterSegmentCommand { get; }

  public MovieDetailVM(ICoreVM phCoreVM, CoreR coreR, CoreS coreS) {
    _phCoreVM = phCoreVM;
    _coreR = coreR;
    _coreS = coreS;
    AddMediaItemsCommand = new(AddMediaItems, phCoreVM.AnyActive, "IconImageMultiple", "Add selected Media items");
    SetCharacterSegmentCommand = new(SetCharacterSegment, CanSetCharacterSegment, "IconSegment", "Set Character Segment");
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
    var mis = _phCoreVM.GetActive();
    if (Dialog.Show(new MessageDialog(
          "Adding selected Media items to Movie",
          "Do you really want to add {0} Media item{1} to Movie?".Plural(mis.Length),
          "IconMovieClapper",
          true)) != 1) return;

    _coreR.Movie.AddMediaItems(_movieM, mis);
  }

  private void SetCharacterSegment() =>
    _coreR.Character.SetSegment(
      _coreS.Character.Selected.Items.FirstOrDefault(),
      _coreS.PhCoreS.Segment.GetSelected().FirstOrDefault());

  private bool CanSetCharacterSegment() =>
    _coreS.Character.Selected.Items.Count == 1
    && _coreS.PhCoreS.Segment.GetSelected().Length == 1;
}
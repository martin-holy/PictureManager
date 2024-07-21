using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Import;
using MovieManager.Common.Features.Movie;
using PictureManager.Common.Interfaces.Plugin;
using PictureManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common;

public sealed class CoreVM : ObservableObject, IPluginCoreVM {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public PM.ViewModels.CoreVM PMCoreVM { get; }
  public string PluginIcon => MH.UI.Res.IconMovieClapper;
  public string PluginTitle => "Movie Manager";

  public ImportVM? Import { get; private set; }
  public MoviesVM? Movies { get; private set; }
  public MovieDetailVM? MovieDetail { get; private set; }
  public MoviesFilterVM? MoviesFilter { get; private set; }

  public List<RelayCommand> MainMenuCommands { get; }

  public RelayCommand DeleteSelectedMoviesCommand { get; }
  public RelayCommand ImportMoviesCommand { get; }
  public RelayCommand OpenMoviesCommand { get; }
  public RelayCommand OpenMoviesFilterCommand { get; }
  public RelayCommand SaveDbCommand { get; }
  public RelayCommand ScrollToRootFolderCommand { get; }

  public CoreVM(PM.ViewModels.CoreVM pmCoreVM, CoreS coreS, CoreR coreR) {
    PMCoreVM = pmCoreVM;
    _coreS = coreS;
    _coreR = coreR;

    pmCoreVM.AppClosingEvent += OnAppClosing;

    InitToggleDialog();

    DeleteSelectedMoviesCommand = new(DeleteSelectedMovies, () => _coreS.Movie.Selected.Items.Count > 0, MH.UI.Res.IconXCross, "Delete selected Movies");
    ImportMoviesCommand = new(OpenImportMovies, PM.Res.IconImport, "Import");
    OpenMoviesCommand = new(OpenMovies, MH.UI.Res.IconMovieClapper, "Movies");
    OpenMoviesFilterCommand = new(OpenMoviesFilter, PM.Res.IconFilter, "Movies filter");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, PM.Res.IconDatabase, "Save changes");
    ScrollToRootFolderCommand = new(() => _coreR.PMCoreR.Folder.Tree.ScrollTo(_coreR.RootFolder), PM.Res.IconFolder, "Scroll to root folder");

    MainMenuCommands = [
      DeleteSelectedMoviesCommand,
      ImportMoviesCommand,
      OpenMoviesCommand,
      OpenMoviesFilterCommand,
      SaveDbCommand,
      ScrollToRootFolderCommand
    ];
  }

  public void AttachEvents() {
    _coreR.Actor.ActorPersonChangedEvent += OnActorPersonChanged;
    _coreR.Movie.ItemDeletedEvent += OnMovieDeleted;
    _coreR.Movie.ItemsDeletedEvent += OnMoviesDeleted;
    _coreR.Movie.MoviesKeywordsChangedEvent += OnMoviesKeywordsChanged;
    _coreR.Movie.PosterChangedEvent += OnMoviePosterChanged;
    _coreS.Import.MovieImportedEvent += OnMovieImported;

    PMCoreVM.MainTabs.TabClosedEvent += OnMainTabsTabClosed;
    PMCoreVM.ToolsTabs.TabClosedEvent += OnToolsTabsTabClosed;
  }

  private void OnActorPersonChanged(object? sender, ActorM e) {
    foreach (var character in _coreR.Character.All.Where(x =>
               ReferenceEquals(x.Actor, e) && ReferenceEquals(x.Movie, MovieDetail?.MovieM))) character.OnPropertyChanged(nameof(character.DisplaySegment));
  }

  private void OnMovieImported(object? sender, MovieM e) {
    if (Movies != null) {
      e.Poster?.SetThumbSize();
      Movies.Insert(e);
    }

    MoviesFilter?.Update(_coreR.Movie.All, _coreR.Genre.All);
  }

  private void OnMovieDeleted(object? sender, MovieM e) {
    if (ReferenceEquals(e, MovieDetail?.MovieM))
      PMCoreVM.ToolsTabs.Close(MovieDetail);
  }

  private void OnMoviesDeleted(object? sender, IList<MovieM> e) {
    Movies?.Remove(e.ToArray());
    MoviesFilter?.Update(_coreR.Movie.All, _coreR.Genre.All);
  }

  private void OnMoviesKeywordsChanged(object? sender, MovieM[] items) {
    MovieDetail?.UpdateDisplayKeywordsIfContains(items);
  }

  private void OnMoviePosterChanged(object? sender, MovieM e) {
    Movies?.ReWrapAll();
  }

  private void OnMainTabsTabClosed(IListItem tab) {
    if (tab.Data is MoviesVM)
      _coreS.Movie.Selected.DeselectAll();
  }

  private void OnToolsTabsTabClosed(IListItem tab) {
    if (tab.Data is MovieDetailVM) {
      _coreS.Actor.Selected.DeselectAll();
      _coreS.Character.Selected.DeselectAll();
    }
  }

  private void OnAppClosing(object? sender, EventArgs e) {
    if (_coreR.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in Movie Manager database.\nDo you want to save them?",
          PM.Res.IconDatabase,
          true)) == 1)
      _coreR.SaveAllTables();

    _coreR.BackUp();
  }

  private void DeleteSelectedMovies() {
    if (Dialog.Show(new MessageDialog(
          "Delete Movies",
          "Do you really want to delete {0} Movie{1}?".Plural(_coreS.Movie.Selected.Items.Count),
          MH.UI.Res.IconMovieClapper,
          true)) == 1)
      _coreR.Movie.ItemsDelete(_coreS.Movie.Selected.Items);
  }

  private void OpenImportMovies() {
    Import ??= new(_coreS.Import);
    PMCoreVM.MainTabs.Activate(PM.Res.IconImport, "Import", Import);
  }

  private void OpenMovies() {
    Movies ??= new();
    Movies.Open(MoviesFilter == null
      ? _coreR.Movie.All
      : _coreR.Movie.All.Where(MoviesFilter.Filter));
    PMCoreVM.MainTabs.Activate(MH.UI.Res.IconMovieClapper, "Movies", Movies);
  }

  private void OpenMoviesFilter() {
    if (MoviesFilter == null) {
      MoviesFilter = new();
      MoviesFilter.FilterChangedEvent += OnMoviesFilterChanged;
    }

    MoviesFilter.Update(_coreR.Movie.All, _coreR.Genre.All);
    PMCoreVM.ToolsTabs.Activate(PM.Res.IconFilter, "Movies filter", MoviesFilter);
  }

  private void OnMoviesFilterChanged(object? sender, EventArgs e) {
    Movies?.Open(_coreR.Movie.All.Where(MoviesFilter!.Filter));
  }

  public void OpenMovieDetail(MovieM? movie) {
    if (movie == null) {
      if (MovieDetail != null) {
        PMCoreVM.ToolsTabs.Close(MovieDetail);
        MovieDetail = null;
      }

      return;
    }

    MovieDetail ??= new(PMCoreVM, _coreR, _coreS, movie);
    MovieDetail.Reload(movie);
    PMCoreVM.ToolsTabs.Activate(MH.UI.Res.IconMovieClapper, "Movie", MovieDetail);
  }

  private void InitToggleDialog() {
    var sts = PMCoreVM.ToggleDialog.SourceTypes;
    var ttActor = new ToggleDialogTargetType<ActorM>(
      PM.Res.IconPeople,
      _ => _coreS.Actor.Selected.Items.Count == 0 ? [] : [_coreS.Actor.Selected.Items[0]],
      _ => "Actor");
    var ttMovie = new ToggleDialogTargetType<MovieM>(
      MH.UI.Res.IconMovieClapper,
      _ => _coreS.Movie.Selected.Items.Count == 0 ? [] : [.. _coreS.Movie.Selected.Items],
      "{0} Movie{1}".Plural);

    if (sts.SingleOrDefault(x => x.Type.IsAssignableTo(typeof(PersonM))) is { } stPerson) stPerson.Options.Add(new ToggleDialogOption<PersonM, ActorM>(ttActor,
          (items, item) => _coreR.Actor.SetPerson(items.First(), item)));

    if (sts.SingleOrDefault(x => x.Type.IsAssignableTo(typeof(KeywordM))) is { } stKeyword)
      stKeyword.Options.Add(new ToggleDialogOption<KeywordM, MovieM>(ttMovie, _coreR.Movie.ToggleKeyword));
  }
}
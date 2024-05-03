using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Plugin;
using PictureManager.Interfaces.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MovieManager.Common.ViewModels;

public sealed class CoreVM : ObservableObject, IPluginCoreVM {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public ICoreVM PhCoreVM { get; }
  public string PluginIcon => "IconMovieClapper";
  public string PluginTitle => "Movie Manager";

  public ImportVM Import { get; private set; }
  public MoviesVM Movies { get; private set; }
  public MovieDetailVM MovieDetail { get; private set; }

  public List<RelayCommand> MainMenuCommands { get; }

  public RelayCommand ImportMoviesCommand { get; }
  public RelayCommand OpenMoviesCommand { get; }
  public RelayCommand SaveDbCommand { get; }
  public RelayCommand ScrollToRootFolderCommand { get; }

  public CoreVM(ICoreVM phCoreVM, CoreS coreS, CoreR coreR) {
    PhCoreVM = phCoreVM;
    _coreS = coreS;
    _coreR = coreR;

    phCoreVM.AppClosingEvent += OnAppClosing;

    InitToggleDialog();

    ImportMoviesCommand = new(OpenImportMovies, "IconImport", "Import");
    OpenMoviesCommand = new(OpenMovies, "IconMovieClapper", "Movies");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, "IconDatabase", "Save changes");
    ScrollToRootFolderCommand = new(() => PhCoreVM.ScrollToFolder(_coreR.RootFolder), "IconFolder", "Scroll to root folder");

    MainMenuCommands = [ImportMoviesCommand, OpenMoviesCommand, SaveDbCommand, ScrollToRootFolderCommand];
  }

  private void OnAppClosing(object sender, System.EventArgs e) {
    if (_coreR.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in Movie Manager database.\nDo you want to save them?",
          MH.UI.Res.IconQuestion,
          true)) == 1)
      _coreR.SaveAllTables();

    _coreR.BackUp();
  }

  private void OpenImportMovies() {
    Import ??= new(_coreS.Import);
    Import.Open();
    PhCoreVM.MainTabs.Activate("IconImport", "Import", Import);
  }

  private void OpenMovies() {
    Movies ??= new();
    Movies.Open(_coreR.Movie.All);
    PhCoreVM.MainTabs.Activate("IconMovieClapper", "Movies", Movies);
  }

  public void OpenMovieDetail(MovieM movie) {
    MovieDetail ??= new(PhCoreVM, _coreR, _coreS);
    MovieDetail.Reload(movie);
    PhCoreVM.ToolsTabs.Activate("IconMovieClapper", "Movie", MovieDetail);
  }

  private void InitToggleDialog() {
    var sts = PhCoreVM.ToggleDialog.SourceTypes;
    var ttActor = new ToggleDialogTargetType<ActorM>(
      "IconPeople",
      _ => Core.S.Actor.Selected.Items.Count == 0 ? [] : [Core.S.Actor.Selected.Items.First()],
      _ => "Actor");

    if (sts.SingleOrDefault(x => x.Type.IsAssignableTo(typeof(IPersonM))) is { } stPerson) {
      stPerson.Options.Add(new ToggleDialogOption<IPersonM, ActorM>(ttActor,
        (items, item) => Core.R.Actor.SetPerson(items.First(), item)));
    }
  }
}
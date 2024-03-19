using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using PictureManager.Plugins.Common.Interfaces.ViewModels;
using System.Collections.Generic;

namespace MovieManager.Common.ViewModels;

public sealed class CoreVM : ObservableObject, IPluginCoreVM {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public string PluginIcon => "IconMovieClapper";
  public string PluginTitle => "Movie Manager";

  public List<RelayCommand> MainMenuCommands { get; } = [];

  public RelayCommand ImportMoviesCommand { get; }
  public RelayCommand SaveDbCommand { get; }

  public CoreVM(CoreS coreS, CoreR coreR) {
    _coreS = coreS;
    _coreR = coreR;

    ImportMoviesCommand = new(_coreR.Movie.ImportFromJson, "IconBug", "Import");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, "IconDatabase", "Save changes");

    MainMenuCommands.AddRange(new[] { ImportMoviesCommand, SaveDbCommand });
  }
}
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MovieManager.Common.Features.Import;

public class ImportVM : ObservableObject {
  private readonly ImportS _importS;
  private readonly List<string> _searchQueue = [];
  private bool _isSearchInProgress;
  private bool _isImportInProgress;
  private string _searchTitle = string.Empty;

  public ObservableCollection<SearchResult> SearchResults { get; } = [];
  public ObservableCollection<string> ProgressCollection { get; } = [];
  public IProgress<string> Progress { get; }
  public string SearchTitle { get => _searchTitle; set { _searchTitle = value; OnPropertyChanged(); } }

  public AsyncRelayCommand<string> SearchCommand { get; }
  public AsyncRelayCommand<SearchResult> ImportCommand { get; }

  public ImportVM(ImportS importS) {
    _importS = importS;
    Progress = new Progress<string>(x => ProgressCollection.Insert(0, x));

    SearchCommand = new((x, y) => Search(x!, y), CanSearch, null, "Search");
    ImportCommand = new((x, y) => Import(x!, y), x => x != null, null, "Import");
  }

  private bool CanSearch(string? query) =>
    !string.IsNullOrEmpty(query) && !_isSearchInProgress && !_isImportInProgress;

  private Task Search(string titles, CancellationToken token) {
    ProgressCollection.Clear();
    _searchQueue.Clear();
    _searchQueue.AddRange(titles.Split(
      [Environment.NewLine],
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    ));

    return SearchQueue(token);
  }

  private async Task Import(SearchResult result, CancellationToken token) {
    SearchResults.Clear();
    SearchTitle = string.Empty;
    _isImportInProgress = true;
    await _importS.ImportMovie(result, Progress, Core.Inst.ImportPlugin!, token);
    _isImportInProgress = false;
    Progress.Report(string.Empty);
    await SearchQueue(SearchCommand.CancelCommand.Token);
    RelayCommandBase.RaiseCanExecuteChanged();
  }

  private async Task SearchQueue(CancellationToken token) {
    SearchResults.Clear();
    SearchTitle = string.Empty;

    if (token.IsCancellationRequested) return;

    if (_searchQueue.Pop() is not { } title) {
      Progress.Report("Importing completed.", true);
      return;
    }

    Progress.Report($"Searching for '{title}' ...", true);
    SearchTitle = title;
    _isSearchInProgress = true;
    var results = await Core.Inst.ImportPlugin!.SearchMovie(title, token);
    _isSearchInProgress = false;

    if (results.Length == 0) {
      Progress.Report("No results were found.", true);
      await SearchQueue(token);
    }
    else {
      foreach (var result in results) SearchResults.Add(result);
      Progress.Report("Waiting for resolving search results ...", true);
    }
  }
}
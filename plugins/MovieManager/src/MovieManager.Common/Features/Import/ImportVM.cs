﻿using MH.Utils.BaseClasses;
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
  private CancellationTokenSource? _cts;
  private string _searchTitle = string.Empty;

  public ObservableCollection<SearchResult> SearchResults { get; } = [];
  public ObservableCollection<string> ProgressCollection { get; } = [];
  public IProgress<string> Progress { get; }
  public string SearchTitle { get => _searchTitle; set { _searchTitle = value; OnPropertyChanged(); } }

  public AsyncRelayCommand<string> SearchCommand { get; }
  public AsyncRelayCommand<SearchResult> ImportCommand { get; }
  public AsyncRelayCommand CancelCommand { get; }

  public ImportVM(ImportS importS) {
    _importS = importS;
    Progress = new Progress<string>(x => ProgressCollection.Insert(0, x));

    SearchCommand = new(x => Search(x!), x => !string.IsNullOrEmpty(x) && !_isSearchInProgress && !_isImportInProgress, null, "Search");
    ImportCommand = new(x => Import(x!), x => x != null, null, "Import");
    CancelCommand = new(Cancel, () => _isImportInProgress, null, "Cancel");
  }

  private Task Search(string titles) {
    ProgressCollection.Clear();
    _searchQueue.Clear();
    _searchQueue.AddRange(titles.Split(
      [Environment.NewLine],
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    ));

    return SearchQueue();
  }

  private async Task Import(SearchResult result) {
    SearchResults.Clear();
    SearchTitle = string.Empty;
    _isImportInProgress = true;

    _cts = new();
    try {
      await _importS.ImportMovie(result, Progress, _cts.Token);
    }
    catch (OperationCanceledException) { }
    finally {
      _cts.Dispose();
      _cts = null;
    }

    _isImportInProgress = false;
    Progress.Report(string.Empty);

    await SearchQueue();
    RelayCommandBase.InvokeCanExecuteChanged(null, EventArgs.Empty);
  }

  private Task Cancel() =>
    _cts == null ? Task.CompletedTask : _cts.CancelAsync();

  private async Task SearchQueue() {
    SearchResults.Clear();
    SearchTitle = string.Empty;

    if (_searchQueue.Pop() is not { } title) {
      Progress.Report("Importing completed.", true);
      return;
    }

    Progress.Report($"Searching for '{title}' ...", true);
    SearchTitle = title;
    _isSearchInProgress = true;
    var results = await Core.Inst.ImportPlugin!.SearchMovie(title);
    _isSearchInProgress = false;

    if (results.Length == 0) {
      Progress.Report("No results were found.", true);
      await SearchQueue();
    }
    else {
      foreach (var result in results) SearchResults.Add(result);
      Progress.Report("Waiting for resolving search results ...", true);
    }
  }
}
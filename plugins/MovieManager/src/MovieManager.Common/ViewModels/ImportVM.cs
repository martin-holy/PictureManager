using MH.Utils.BaseClasses;
using MovieManager.Common.Services;

namespace MovieManager.Common.ViewModels;

public class ImportVM : ObservableObject {
  public RelayCommand<string> ImportCommand { get; }

  public ImportVM(ImportS importS) {
    ImportCommand = new(importS.Import, "IconBug", "Import");
  }

  public void Open() {

  }
}
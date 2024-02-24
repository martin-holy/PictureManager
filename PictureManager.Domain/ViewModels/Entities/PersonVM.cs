using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;
using System.Linq;

namespace PictureManager.Domain.ViewModels.Entities;

public sealed class PersonVM {
  private readonly CoreVM _coreVM;
  private readonly PersonR _r;
  public static RelayCommand<KeywordM> LoadByKeywordCommand { get; set; }
  

  public PersonVM(CoreVM coreVM, PersonR r) {
    _coreVM = coreVM;
    _r = r;
    LoadByKeywordCommand = new(LoadBy, Res.IconPeopleMultiple, "Load People");
  }

  private void LoadBy(KeywordM k) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray());
}
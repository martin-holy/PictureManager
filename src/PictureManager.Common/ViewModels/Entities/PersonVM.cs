using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using System.Linq;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class PersonVM {
  private readonly CoreVM _coreVM;
  private readonly PersonR _r;
  public static RelayCommand<CategoryGroupM> LoadByCategoryGroupCommand { get; set; }
  public static RelayCommand<KeywordM> LoadByKeywordCommand { get; set; }

  public PersonVM(CoreVM coreVM, PersonR r) {
    _coreVM = coreVM;
    _r = r;
    LoadByCategoryGroupCommand = new(LoadBy, Res.IconPeopleMultiple, "Load People");
    LoadByKeywordCommand = new(LoadBy, Res.IconPeopleMultiple, "Load People");
  }

  private void LoadBy(CategoryGroupM cg) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(cg.Items.OfType<PersonM>().ToArray());

  private void LoadBy(KeywordM k) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray());
}
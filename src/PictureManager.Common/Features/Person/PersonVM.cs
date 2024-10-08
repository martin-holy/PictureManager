﻿using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Keyword;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PersonVM {
  private readonly CoreVM _coreVM;
  private readonly PersonR _r;

  public static int PersonTileSegmentWidth { get; set; }
  public static RelayCommand<CategoryGroupM> LoadByCategoryGroupCommand { get; set; } = null!;
  public static RelayCommand<KeywordM> LoadByKeywordCommand { get; set; } = null!;

  public PersonVM(CoreVM coreVM, PersonR r) {
    _coreVM = coreVM;
    _r = r;
    LoadByCategoryGroupCommand = new(x => LoadBy(x!), x => x != null, Res.IconPeopleMultiple, "Load People");
    LoadByKeywordCommand = new(x => LoadBy(x!), x => x != null, Res.IconPeopleMultiple, "Load People");
  }

  private void LoadBy(CategoryGroupM cg) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(cg.Items.OfType<PersonM>().ToArray());

  private void LoadBy(KeywordM k) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray());
}
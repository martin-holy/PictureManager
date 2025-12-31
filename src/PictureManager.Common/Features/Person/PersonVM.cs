using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Segment;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Person;

public sealed class PersonVM {
  private readonly CoreVM _coreVM;
  private readonly PersonR _r;

  public static int PersonTileSegmentWidth { get; set; }
  public static int PersonTileTextWidth { get; set; }
  public static int PersonListWidth { get; set; }
  public static int PersonListHeight { get; set; }
  public static AsyncRelayCommand<CategoryGroupM> LoadByCategoryGroupCommand { get; set; } = null!;
  public static AsyncRelayCommand<KeywordM> LoadByKeywordCommand { get; set; } = null!;

  public PersonVM(CoreVM coreVM, PersonR r) {
    _coreVM = coreVM;
    _r = r;
    LoadByCategoryGroupCommand = new((x, _) => _loadBy(x!), x => x != null, Res.IconPeopleMultiple, "Load People");
    LoadByKeywordCommand = new((x, _) => _loadBy(x!), x => x != null, Res.IconPeopleMultiple, "Load People");
  }

  private Task _loadBy(CategoryGroupM cg) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(cg.Items.OfType<PersonM>().ToArray());

  private Task _loadBy(KeywordM k) =>
    _coreVM.MainWindow.ToolsTabs.OpenPeopleTab(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray());

  public static void SetPersonUiSize(double scale) {
    PersonTileSegmentWidth = SegmentVM.SegmentUiSize + 2;
    PersonTileTextWidth = (int)(173 / scale);
    PersonListWidth = (int)(200 / scale);
    PersonListHeight = (int)(38 / scale);
  }
}
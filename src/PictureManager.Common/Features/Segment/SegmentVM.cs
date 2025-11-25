using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private readonly SegmentS _s;
  private readonly SegmentR _r;

  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }
  public static IImageSourceConverter<SegmentM> ThumbConverter { get; set; } = null!;

  public SegmentsViewsVM Views { get; }
  public SegmentRectVM Rect { get; }

  public static AsyncRelayCommand<KeywordM> LoadByKeywordCommand { get; set; } = null!;
  public static AsyncRelayCommand<PersonM> LoadByPersonCommand { get; set; } = null!;
  public static RelayCommand<PersonM> AddSelectedToPersonsTopSegmentsCommand { get; set; } = null!;
  public static RelayCommand<PersonM> RemoveSelectedFromPersonsTopSegmentsCommand { get; set; } = null!;
  public static AsyncRelayCommand SetSelectedAsSamePersonCommand { get; set; } = null!;
  public static AsyncRelayCommand SetSelectedAsUnknownCommand { get; set; } = null!;

  public SegmentVM(CoreVM coreVM, SegmentS s, SegmentR r) {
    _coreVM = coreVM;
    _s = s;
    _r = r;
    Views = new(_s);
    Rect = new(_s.Rect);

    LoadByKeywordCommand = new((x, _) => _loadBy(x!), x => x != null, Res.IconSegment, "Load Segments");
    LoadByPersonCommand = new((x, _) => _loadBy(x!), x => x != null, Res.IconSegment, "Load Segments");
    AddSelectedToPersonsTopSegmentsCommand = new(_addSelectedToPersonsTopSegments, _canAddSelectedToPersonsTopSegments, null, "Add selected to Top segments");
    RemoveSelectedFromPersonsTopSegmentsCommand = new(_removeSelectedFromPersonsTopSegments, _canRemoveSelectedFromPersonsTopSegments, null, "Remove selected from Top segments");
    SetSelectedAsSamePersonCommand = new(_setSelectedAsSamePerson, Res.IconEquals, "Set selected as same person");
    SetSelectedAsUnknownCommand = new(_setSelectedAsUnknown, _canSetSelectedAsUnknown, Res.IconUnknownSegment, "Set selected as Unknown");

    _s.Selected.ItemsChangedEvent += _onSelectedSegmentsChanged;
  }

  private Task _loadBy(KeywordM k) =>
    _coreVM.OpenSegmentsViews(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray(), k.GetFullName("-", x => x.Name));

  private Task _loadBy(PersonM p) =>
    _coreVM.OpenSegmentsViews(_r.GetBy(p).ToArray(), p.Name);

  private void _addSelectedToPersonsTopSegments(PersonM? person) {
    Core.S.Person.AddToTopSegments(person!, _s.Selected.Items);
  }

  private bool _canAddSelectedToPersonsTopSegments(PersonM? person) =>
    person != null && _s.Selected.Items.Any(x => ReferenceEquals(person, x.Person)
      && (person.TopSegments == null || !person.TopSegments.Contains(x)));

  private void _removeSelectedFromPersonsTopSegments(PersonM? person) {
    Core.S.Person.RemoveFromTopSegments(person!, _s.Selected.Items);
  }

  private bool _canRemoveSelectedFromPersonsTopSegments(PersonM? person) =>
     person?.TopSegments?.Any(_s.Selected.Items.Contains) == true;

  private Task _setSelectedAsSamePerson(CancellationToken token) =>
    _s.SetSelectedAsSamePerson(_s.Selected.Items.ToArray());

  private Task _setSelectedAsUnknown(CancellationToken token) =>
    _setAsUnknown(_s.Selected.Items.ToArray(), _r);

  private bool _canSetSelectedAsUnknown() =>
    _s.Selected.Items.Count > 0;

  private static async Task _setAsUnknown(SegmentM[] segments, SegmentR r) {
    var msg = "Do you want to set {0} segment{1} as unknown?".Plural(segments.Length);
    if (await Dialog.ShowAsync(new MessageDialog("Set as unknown", msg, MH.UI.Res.IconQuestion, true)) != 1) return;
    r.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }

  private void _onSelectedSegmentsChanged(object? sender, SegmentM[] e) {
    AddSelectedToPersonsTopSegmentsCommand.RaiseCanExecuteChanged();
    RemoveSelectedFromPersonsTopSegmentsCommand.RaiseCanExecuteChanged();
    SetSelectedAsSamePersonCommand.RaiseCanExecuteChanged();
    SetSelectedAsUnknownCommand.RaiseCanExecuteChanged();
  }

  public static void SetSegmentUiSize(double scale) {
    var size = (int)(SegmentSize / scale);
    SegmentUiSize = size;
    SegmentUiFullWidth = size + 6; // + border, margin
  }
}
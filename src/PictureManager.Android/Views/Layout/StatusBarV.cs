using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Items;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public sealed class StatusBarV : WrapLayout {
  private readonly StatusBarVM _statusBarVM;

  private readonly IconItemsView _people;
  private readonly IconItemsView _keywords;

  public StatusBarV(Context context, StatusBarVM statusBarVM, BindingScope bindings) : base(context) {
    _statusBarVM = statusBarVM;

    _people = new IconItemsView(context, Res.IconPeople, _getPersonView);
    _keywords = new IconItemsView(context, Res.IconTag, _getKeywordView);

    AddView(_people, LPU.ViewGroupWrap());
    AddView(_keywords, LPU.ViewGroupWrap());

    _statusBarVM.Bind<PersonM[]>(
      [nameof(StatusBarVM.Current), nameof(MediaItemM.DisplayPeople)],
      [x => (x as StatusBarVM)?.Current, x => (x as MediaItemM)?.DisplayPeople],
      people => {
        _people.Items = people;
        _people.Visibility = people?.Length > 0 ? ViewStates.Visible : ViewStates.Gone;
      });

    _statusBarVM.Bind<string[]>(
      [nameof(StatusBarVM.Current), nameof(MediaItemM.DisplayKeywords)],
      [x => (x as StatusBarVM)?.Current, x => (x as MediaItemM)?.DisplayKeywords],
      keywords => {
        _keywords.Items = keywords;
        _keywords.Visibility = keywords?.Length > 0 ? ViewStates.Visible : ViewStates.Gone;
      });
  }

  private TextView? _getPersonView(object item) =>
    item is PersonM p
      ? new TextView(Context) { Text = p.Name, Background = BackgroundFactory.RoundDarker() }
      : null;

  private TextView? _getKeywordView(object item) =>
    item is string s
      ? new TextView(Context) { Text = s, Background = BackgroundFactory.RoundDarker() }
      : null;
}
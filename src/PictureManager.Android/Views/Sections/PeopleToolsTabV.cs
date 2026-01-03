using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Sections;

public sealed class PeopleToolsTabV : FrameLayout {
  public PeopleToolsTabV(Context context, PeopleToolsTabVM dataContext) : base(context) {
    AddView(new CollectionViewHost(context, dataContext, PeopleV.GetPersonV), new LayoutParams(LPU.Match, LPU.Match));
  }
}
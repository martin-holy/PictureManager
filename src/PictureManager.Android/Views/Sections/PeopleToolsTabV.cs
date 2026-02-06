using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Sections;

public sealed class PeopleToolsTabV : FrameLayout {
  public PeopleToolsTabV(Context context, PeopleToolsTabVM dataContext) : base(context) {
    AddView(new CollectionViewHost(context, dataContext, PeopleV.CreatePersonV), new LayoutParams(LPU.Match, LPU.Match));
  }
}
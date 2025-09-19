using MH.Utils.BaseClasses;
using System.Collections.ObjectModel;

namespace PictureManager.Common.Features.Common;
public sealed class LogVM {
  public ObservableCollection<LogItem> Items => MH.Utils.Log.Items;
}
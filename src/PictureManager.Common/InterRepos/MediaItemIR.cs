using MH.Utils.BaseClasses;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Interfaces.Models;

namespace PictureManager.Common.InterRepos;

public class MediaItemIR : InterfaceTableDataAdapter<MediaItemM, IMediaItemM> {
  public MediaItemIR(TableDataAdapter<MediaItemM> tableDataAdapter) : base(tableDataAdapter) { }
}
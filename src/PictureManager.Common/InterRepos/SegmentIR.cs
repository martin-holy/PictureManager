using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Interfaces.Models;

namespace PictureManager.Common.InterRepos;

public class SegmentIR : InterfaceTableDataAdapter<SegmentM, ISegmentM> {
  public SegmentIR(TableDataAdapter<SegmentM> tableDataAdapter) : base(tableDataAdapter) { }
}
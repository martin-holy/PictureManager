using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Interfaces.Models;

namespace PictureManager.Common.InterRepos;

public class KeywordIR : InterfaceTableDataAdapter<KeywordM, IKeywordM> {
  public KeywordIR(TableDataAdapter<KeywordM> tableDataAdapter) : base(tableDataAdapter) { }
}
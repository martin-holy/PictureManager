using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Interfaces.Models;

namespace PictureManager.Common.InterRepos;

public class PersonIR : InterfaceTableDataAdapter<PersonM, IPersonM> {
  public PersonIR(TableDataAdapter<PersonM> tableDataAdapter) : base(tableDataAdapter) { }
}
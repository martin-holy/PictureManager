using System;

namespace PictureManager.ViewModel {
  public class Viewer : BaseTreeViewTagItem {
    public string[] DirsAllowed;
    public string[] DirsDenied;
    public string[] FilesAllowed;
    public string[] FilesDenied;
    public DataModel.Viewer Data;
    public DataModel.PmDataContext Db;

    public Viewer() {
      DirsAllowed = new string[0];
      DirsDenied = new string[0];
      FilesAllowed = new string[0];
      FilesDenied = new string[0];

      IconName = "appbar_eye";
    }

    public Viewer(DataModel.PmDataContext db, DataModel.Viewer data) : this() {
      Db = db;
      Data = data;
      Id = data.Id;
      Title = data.Name;

      DirsAllowed = data.DirsAllowed.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
      DirsDenied = data.DirsDenied.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
      FilesAllowed = data.FilesAllowed.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
      FilesDenied = data.FilesDenied.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
    }

    public void Save() {
      if (Data == null) {
        var data = new DataModel.Viewer {
          Id = Db.GetNextIdFor("Viewers"),
          Name = Title,
          DirsAllowed = string.Join(";", DirsAllowed),
          DirsDenied = string.Join(";", DirsDenied),
          FilesAllowed = string.Join(";", FilesAllowed),
          FilesDenied = string.Join(";", FilesDenied)
        };
        Db.Viewers.InsertOnSubmit(data);
        Data = data;
        Id = data.Id;
      } else {
        Data.Name = Title;
        Data.DirsAllowed = string.Join(";", DirsAllowed);
        Data.DirsDenied = string.Join(";", DirsDenied);
        Data.FilesAllowed = string.Join(";", FilesAllowed);
        Data.FilesDenied = string.Join(";", FilesDenied);
      }
      Db.DataContext.SubmitChanges();
    }
  }
}

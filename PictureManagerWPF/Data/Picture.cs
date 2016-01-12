using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PictureManager.Data {
  public class Picture {
    public string FilePath;
    public string FileName;
    public string FileExt;
    public int Index;
    public int Id;
    public int DirId;
    public int Rating;
    public bool Modifed;
    public bool IsNew = false;
    public bool IsSelected = false;
    public DbStuff Db;
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();

    public Picture(string filePath, DbStuff db, int index) {
      FilePath = filePath;
      FileName = Path.GetFileName(filePath);
      FileExt = Path.GetExtension(filePath);
      if (!string.IsNullOrEmpty(FileExt))
        FileExt = FileExt.Replace(".", string.Empty).ToLower();
      Index = index;
      Id = -1;
      DirId = -1;
      Db = db;
    }

    public string GetKeywordsAsString() {
      StringBuilder sb = new StringBuilder();
      foreach (Person p in People.OrderBy(x => x.Title)) {
        sb.Append("<div>");
        sb.Append(p.Title);
        sb.Append("</div>");
      }
      List<string> keywordsList = new List<string>();
      foreach (Keyword keyword in Keywords.OrderBy(x => x.FullPath)) {
        foreach (string k in keyword.FullPath.Split('/')) {
          if (!keywordsList.Contains(k)) keywordsList.Add(k);
        }
      }

      foreach (var keyword in keywordsList) {
        sb.Append("<div>");
        sb.Append(keyword);
        sb.Append("</div>");
      }
      return sb.ToString();
    }

  }
}

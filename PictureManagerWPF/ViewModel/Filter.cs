using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace PictureManager.ViewModel {
  public class Filter: BaseTreeViewTagItem {
    public DataModel.Filter Data;
    public ObservableCollection<Filter> Items { get; set; }
    public Filter Parent;
    public DataModel.PmDataContext Db;
    public ObservableCollection<BaseFilterItem> FilterData;

    public override bool IsExpanded
    {
      get { return base.IsExpanded; }
      set
      {
        base.IsExpanded = value;
        if (value) GetSubFilters(false, Items, this, Db);
      }
    }

    public Filter() {
      Items = new ObservableCollection<Filter>();
      FilterData = new ObservableCollection<BaseFilterItem>();
      Id = -1;
      IconName = "appbar_filter";
    }

    public Filter(DataModel.Filter data, DataModel.PmDataContext db, Filter parent) : this() {
      Data = data;
      Parent = parent;
      Db = db;
      Id = data.Id;
      Title = data.Name;
    }

    public static void GetSubFilters(bool refresh, ObservableCollection<Filter> items, Filter parent, DataModel.PmDataContext db) {
      if (!refresh && parent != null) {
        if (items.Count <= 0) return;
        if (items[0].Title != @"...") return;
      }
      items.Clear();
      var parentId = parent?.Id;

      foreach (var f in db.Filters.ToArray().Where(x => x.ParentId.Equals(parentId))) {
        Filter item = new Filter(f, db, parent);
        item.LoadFilterData(f.Data);

        if (db.Filters.Count(x => x.ParentId == f.Id) != 0) {
          item.Items.Add(new Filter { Title = "..." });
        }

        items.Add(item);
      }
    }

    public void ReloadData() {
      if (Id == -1) return;
      LoadFilterData(Db.Filters.Single(x => x.Id == Id).Data);
    }

    public void LoadFilterData(byte[] biteArray) {
      FileInfo filterFile = new FileInfo("TempFilter.dat");

      using (FileStream writeFileStream = new FileStream(filterFile.Name, FileMode.Create)) {
        try {
          writeFileStream.Write(biteArray, 0, biteArray.Length);
        } catch (Exception) {
          //ignored
        }
      }

      if (!filterFile.Exists) return;

      using (FileStream readFileStream = new FileStream(filterFile.Name, FileMode.Open)) {
        BinaryFormatter formatter = new BinaryFormatter();
        try {
          FilterData = (ObservableCollection<BaseFilterItem>) formatter.Deserialize(readFileStream);
        } catch (System.Runtime.Serialization.SerializationException) {
          //ignored
        }
      }

      filterFile.Delete();
    }

    public void SaveFilter() {
      FileInfo filterFile = new FileInfo("TempFilter.dat");

      using (FileStream writeFileStream = new FileStream(filterFile.Name, FileMode.Create)) {
        BinaryFormatter formatter = new BinaryFormatter();
        try {
          formatter.Serialize(writeFileStream, FilterData);
        } catch (System.Runtime.Serialization.SerializationException) {
          //ignored
        }
      }

      if (!filterFile.Exists) return;
      byte[] biteArray;
      //read filder data from stream to biteArray
      using (FileStream readFileStream = new FileStream(filterFile.Name, FileMode.Open)) {
        biteArray = new byte[readFileStream.Length];
        readFileStream.Position = 0;
        readFileStream.Read(biteArray, 0, (int)readFileStream.Length);
      }
      filterFile.Delete();

      //insert or update filter to DB
      if (Id == -1) {
        var dmFilter = new DataModel.Filter {
          Id = Db.GetNextIdFor("Filters"),
          Name = Title,
          Data = biteArray,
          ParentId = Parent?.Id
        };

        Db.Filters.InsertOnSubmit(dmFilter);
        Db.Filters.Context.SubmitChanges();

        Id = dmFilter.Id;
      } else {
        Data.Data = biteArray;
        Data.Name = Title;
        Db.Filters.Context.SubmitChanges();
      }
    }
  }
}

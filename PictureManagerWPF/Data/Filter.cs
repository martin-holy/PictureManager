using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PictureManager.Data {
  public class Filter: BaseTagItem {
    public ObservableCollection<Filter> Items { get; set; }
    public Filter Parent;
    public DbStuff Db;
    public ObservableCollection<BaseFilterItem> FilterData;

    public override bool IsExpanded
    {
      get { return base.IsExpanded; }
      set
      {
        base.IsExpanded = value;
        if (value) GetSubFilters(false, Items, Db, this);
      }
    }

    public Filter() {
      Items = new ObservableCollection<Filter>();
      FilterData = new ObservableCollection<BaseFilterItem>();
      Id = -1;
    }

    public static void GetSubFilters(bool refresh, ObservableCollection<Filter> items, DbStuff db, Filter parent) {
      if (!refresh && parent != null) {
        if (items.Count <= 0) return;
        if (items[0].Title != @"...") return;
      }
      items.Clear();
      var sql =
        $"select Id, Name, Data, (select count(Id) from Filters where ParentId = F.Id) as ChildsCount from Filters F where ParentId {(parent == null ? "is null" : $"= {parent.Id}")}";
      foreach (DataRow row in db.Select(sql)) {
        Filter item = new Filter {
          Parent = parent,
          Db = db,
          Id = (int)(long)row[0],
          Title = (string)row[1],
          IconName = "appbar_filter"
        };

        item.LoadFilterData((byte[]) row[2]);

        if ((long)row[3] != 0) {
          item.Items.Add(new Filter { Title = "..." });
        }

        items.Add(item);
      }
    }

    public void ReloadData() {
      if (Id == -1) return;
      LoadFilterData((byte[]) Db.ExecuteScalar($"select Data from Filters where Id = {Id}"));
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
      var sql = Id == -1
        ? $"insert into Filters (ParentId, Name, Data) values ({Parent?.Id.ToString() ?? "null"}, '{Title}', @p1)"
        : $"update Filters set Name = '{Title}', Data = @p1 where Id = {Id}";
      if (!Db.Execute(sql, new Dictionary<string, object> { { "@p1", biteArray } })) return;

      if (Id == -1) {
        var id = Db.GetLastIdFor("Filters");
        if (id != null) Id = (int) id;
      }
    }
  }
}

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace PictureManager.ViewModel {
  public class Filter: BaseTreeViewItem, IDbItem {
    public DataModel.Filter Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }
    public ObservableCollection<BaseFilterItem> FilterData;

    public override bool IsExpanded
    {
      get => base.IsExpanded;
      set
      {
        base.IsExpanded = value;
        if (value) GetSubFilters(false, Items, this);
      }
    }

    public Filter() {
      FilterData = new ObservableCollection<BaseFilterItem>();
      Data.Id = -1;
      IconName = IconName.Filter;
    }

    public Filter(DataModel.Filter data, BaseTreeViewItem parent) : this() {
      Data = data;
      Parent = parent;
    }

    public void GetSubFilters(bool refresh, ObservableCollection<BaseTreeViewItem> items, Filter parent) {
      if (!refresh && parent != null) {
        if (items.Count <= 0) return;
        if (items[0].Title != @"...") return;
      }
      items.Clear();
      var parentId = parent?.Data.Id;

      foreach (var f in ACore.Db.Filters.Where(x => x.ParentId.Equals(parentId))) {
        var item = new Filter(f, parent);
        item.LoadFilterData(f.Data);

        if (ACore.Db.Filters.Count(x => x.ParentId == f.Id) != 0) {
          item.Items.Add(new Filter { Title = "..." });
        }

        items.Add(item);
      }
    }

    public void ReloadData() {
      if (Data.Id == -1) return;
      LoadFilterData(ACore.Db.Filters.Single(x => x.Id == Data.Id).Data);
    }

    public void LoadFilterData(byte[] biteArray) {
      //TODO: udelat bez ukladani na disk!!!
      var filterFile = new FileInfo("TempFilter.dat");

      using (var writeFileStream = new FileStream(filterFile.Name, FileMode.Create)) {
        try {
          writeFileStream.Write(biteArray, 0, biteArray.Length);
        } catch (Exception) {
          //ignored
        }
      }

      if (!filterFile.Exists) return;

      using (var readFileStream = new FileStream(filterFile.Name, FileMode.Open)) {
        var formatter = new BinaryFormatter();
        try {
          FilterData = (ObservableCollection<BaseFilterItem>) formatter.Deserialize(readFileStream);
        } catch (System.Runtime.Serialization.SerializationException) {
          //ignored
        }
      }

      filterFile.Delete();
    }

    public void SaveFilter() {
      var filterFile = new FileInfo("TempFilter.dat");

      using (var writeFileStream = new FileStream(filterFile.Name, FileMode.Create)) {
        var formatter = new BinaryFormatter();
        try {
          formatter.Serialize(writeFileStream, FilterData);
        } catch (System.Runtime.Serialization.SerializationException) {
          //ignored
        }
      }

      if (!filterFile.Exists) return;
      byte[] biteArray;
      //read filder data from stream to biteArray
      using (var readFileStream = new FileStream(filterFile.Name, FileMode.Open)) {
        biteArray = new byte[readFileStream.Length];
        readFileStream.Position = 0;
        readFileStream.Read(biteArray, 0, (int)readFileStream.Length);
      }
      filterFile.Delete();

      //insert or update filter to DB
      if (Data.Id == -1) {
        var dmFilter = new DataModel.Filter {
          Id = ACore.Db.GetNextIdFor<DataModel.Filter>(),
          Name = Title,
          Data = biteArray,
          ParentId = (Parent as Filter)?.Data.Id
        };

        ACore.Db.Insert(dmFilter);

        Data.Id = dmFilter.Id;
      } else {
        Data.Data = biteArray;
        Data.Name = Title;
        ACore.Db.Update(Data);
      }
    }
  }
}

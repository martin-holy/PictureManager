using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PictureManager.DataModel {

  public class PmDataContext : IDisposable {
    public SQLiteConnection DbConn;
    public string ConnectionString;
    public Dictionary<Type, TableInfo> TableInfos = new Dictionary<Type, TableInfo>(); 

    private readonly List<BaseTable> _toInsert = new List<BaseTable>();
    private readonly List<BaseTable> _toUpdate = new List<BaseTable>();
    private readonly List<BaseTable> _toDelete = new List<BaseTable>();
    private static readonly Mutex Mut = new Mutex();

    public List<Directory> Directories;
    public List<Filter> Filters;
    public List<Keyword> Keywords;
    public List<MediaItemKeyword> MediaItemKeywords;
    public List<MediaItemPerson> MediaItemPeople;
    public List<MediaItem> MediaItems;
    public List<Person> People;
    public List<PeopleGroup> PeopleGroups;
    public List<Viewer> Viewers;
    public List<ViewerAccess> ViewersAccess;
    public List<SQLiteSequence> SQLiteSequences;

    public PmDataContext(string connectionString) {
      ConnectionString = connectionString;
    }

    public void Dispose() {
      DbConn.Close();
      DbConn.Dispose();
      Mut.Dispose();
    }

    public bool OpenDbConnection() {
      if (DbConn == null) DbConn = new SQLiteConnection(ConnectionString);
      if (DbConn.State != ConnectionState.Open) DbConn.Open();
      return true;
    }

    public bool Load() {
      if (!OpenDbConnection()) return false;

      TableInfos.Clear();

      Directories = GetTableData<Directory>();
      Filters = GetTableData<Filter>();
      Keywords = GetTableData<Keyword>();
      MediaItemKeywords = GetTableData<MediaItemKeyword>();
      MediaItemPeople = GetTableData<MediaItemPerson>();
      MediaItems = GetTableData<MediaItem>();
      People = GetTableData<Person>();
      PeopleGroups = GetTableData<PeopleGroup>();
      Viewers = GetTableData<Viewer>();
      ViewersAccess = GetTableData<ViewerAccess>();

      return true;
    }

    public void Insert(BaseTable o) {
      if (!OpenDbConnection()) return;
      using (SQLiteCommand cmd = DbConn.CreateCommand()) {
        Insert(cmd, o);
      }
    }

    public void Insert(SQLiteCommand cmd, BaseTable o) {
      var columns = GetColumnValues(o);
      cmd.CommandText = TableInfos[o.GetType()].QueryInsert;
      cmd.Parameters.Clear();
      foreach (var column in columns) {
        cmd.Parameters.Add(new SQLiteParameter($"@{column.Key}", column.Value));
      }
      cmd.ExecuteNonQuery();
      UpdateInList(o, true);
    }

    public void InsertOnSubmit(BaseTable data) {
      _toInsert.Add(data);
    }

    public void UpdateOnSubmit(BaseTable data) {
      _toUpdate.Add(data);
    }

    public void DeleteOnSubmit(BaseTable data) {
      _toDelete.Add(data);
    }

    private void UpdateInList(BaseTable data, bool addIt) {
      if (!Mut.WaitOne()) return;

      switch (data.GetType().Name) {
        case nameof(Directory): {
            if (addIt) Directories.Add((Directory) data); else Directories.Remove((Directory)data); break;
          }
        case nameof(Filter): {
            if (addIt) Filters.Add((Filter)data); else Filters.Remove((Filter)data); break;
          }
        case nameof(Keyword): {
            if (addIt) Keywords.Add((Keyword)data); else Keywords.Remove((Keyword)data); break;
          }
        case nameof(MediaItemKeyword): {
            if (addIt) MediaItemKeywords.Add((MediaItemKeyword)data); else MediaItemKeywords.Remove((MediaItemKeyword)data); break;
          }
        case nameof(MediaItemPerson): {
            if (addIt) MediaItemPeople.Add((MediaItemPerson)data); else MediaItemPeople.Remove((MediaItemPerson)data); break;
          }
        case nameof(MediaItem): {
            if (addIt) MediaItems.Add((MediaItem)data); else MediaItems.Remove((MediaItem)data); break;
          }
        case nameof(Person): {
            if (addIt) People.Add((Person)data); else People.Remove((Person)data); break;
          }
        case nameof(PeopleGroup): {
            if (addIt) PeopleGroups.Add((PeopleGroup)data); else PeopleGroups.Remove((PeopleGroup)data); break;
          }
        case nameof(Viewer): {
            if (addIt) Viewers.Add((Viewer)data); else Viewers.Remove((Viewer)data); break;
          }
        case nameof(ViewerAccess): {
            if (addIt) ViewersAccess.Add((ViewerAccess)data); else ViewersAccess.Remove((ViewerAccess)data); break;
          }
      }

      Mut.ReleaseMutex();
    }

    public bool SubmitChanges() {
      if (!OpenDbConnection()) return false;

      using (SQLiteTransaction tr = DbConn.BeginTransaction()) {
        using (SQLiteCommand cmd = DbConn.CreateCommand()) {
          cmd.Transaction = tr;

          //Insert
          _toInsert.ForEach(o => Insert(cmd, o));
          _toInsert.Clear();

          //Update
          foreach (var o in _toUpdate) {
            var columns = GetColumnValues(o);
            cmd.CommandText = TableInfos[o.GetType()].QueryUpdate;
            cmd.Parameters.Clear();
            foreach (var column in columns) {
              cmd.Parameters.Add(new SQLiteParameter($"@{column.Key}", column.Value));
            }
            cmd.ExecuteNonQuery();
          }
          _toUpdate.Clear();

          //Delete
          foreach (var o in _toDelete) {
            cmd.CommandText = TableInfos[o.GetType()].QueryDelete;
            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SQLiteParameter("@Id", o.Id));
            cmd.ExecuteNonQuery();
            UpdateInList(o, false);
          }
          _toDelete.Clear();

        }
        tr.Commit();
      }

      return true;
    }

    private Dictionary<string, object> GetColumnValues(object o) {
      return TableInfos[o.GetType()].Columns.ToDictionary(c => c.Key, c => c.Value.GetValue(o));
    }

    private List<T> GetTableData<T>() where T : new() {
      var tableName = ((TableAttribute) typeof (T).GetCustomAttribute(typeof (TableAttribute), false))?.Name;
      var data = new List<T>();
      if (tableName == null) return data;

      var columns = new Dictionary<string, PropertyInfo>();
      var columnsToCreate = new List<string>();
      foreach (var propertyInfo in typeof(T).GetProperties()) {
        var colName = propertyInfo.GetCustomAttribute<ColumnAttribute>(true)?.Name;
        if (colName == null) continue;
        columns.Add(colName, propertyInfo);

        var attrs = propertyInfo.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault();
        if (attrs == null) continue;
        var isPrimaryKey = attrs.IsPrimaryKey ? "PRIMARY KEY AUTOINCREMENT" : string.Empty;
        var canBeNull = attrs.CanBeNull ? string.Empty : "NOT NULL";
        columnsToCreate.Add($"{colName} {attrs.DbType} {isPrimaryKey} {canBeNull}");
      }

      //create queries
      var paramNames = string.Join(", ", columns.Keys.Select(x => $"@{x}"));
      var setColumns = string.Join(", ", columns.Keys.Where(x => !x.Equals("Id")).Select(x => $"{x} = @{x}"));
      var qSelect = $"select {string.Join(", ", columns.Keys)} from {tableName}";
      var qInsert = $"insert into {tableName} ({string.Join(", ", columns.Keys)}) values ({paramNames})";
      var qUpdate = $"update {tableName} set {setColumns} where Id = @Id";
      var qDelete = $"delete from {tableName} where Id = @Id";
      var qCreate = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" ({string.Join(", ", columnsToCreate)});";

      //create table if not exists
      if (!tableName.Equals("sqlite_sequence"))
        Execute(qCreate);
      
      foreach (DataRow row in Select(qSelect)) {
        var item = new T();
        var i = 0;
        foreach (var column in columns) {
          var val = row.ItemArray[i];
          var type = column.Value.PropertyType;
          type = Nullable.GetUnderlyingType(type) ?? type;
          column.Value.SetValue(item, val is DBNull ? null : Convert.ChangeType(val, type));
          i++;
        }
        data.Add(item);
      }

      var maxId = data.Count == 0 ? 0 : data.Cast<BaseTable>().Max(x => x.Id);

      TableInfos.Add(typeof(T),
        new TableInfo {
          Columns = columns,
          QuerySelect = qSelect,
          QueryInsert = qInsert,
          QueryUpdate = qUpdate,
          QueryDelete = qDelete,
          Items = data,
          //MaxId = SQLiteSequences?.SingleOrDefault(s => s.Name == tableName)?.Seq ?? 0
          MaxId = maxId
        });

      return data;
    }

    public DataRowCollection Select(string sql) {
      if (!OpenDbConnection()) return null;
      using (SQLiteCommand cmd = DbConn.CreateCommand()) {
        cmd.CommandText = sql;
        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd)) {
          DataSet ds = new DataSet();
          adapter.Fill(ds);
          return ds.Tables[0].Rows;
        }
      }
    }

    public bool Execute(string sql) {
      if (!OpenDbConnection()) return false;
      using (SQLiteCommand cmd = DbConn.CreateCommand()) {
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
      }
      return true;
    }

    public int GetNextIdFor<T>() {
      Mut.WaitOne();
      var nextId = ++ TableInfos[typeof (T)].MaxId;
      Mut.ReleaseMutex();
      return nextId;
    }


    public int GetMaxIdFor<T>() {
      return TableInfos[typeof (T)].MaxId;
    }

    public int? GetDirectoryIdByPath(string path) {
      var dir = Directories.SingleOrDefault(x => x.Path.Equals(path));
      return dir?.Id;
    }

    public int InsertDirecotryInToDb(string path) {
      Mut.WaitOne();
      var dirId = GetDirectoryIdByPath(path);
      if (dirId != null) {
        Mut.ReleaseMutex();
        return (int) dirId;
      }
      var newDirId = GetNextIdFor<Directory>();
      Insert(new Directory { Id = newDirId, Path = path });
      Mut.ReleaseMutex();
      return newDirId;
    }
  }

  public class TableInfo {
    public string QuerySelect;
    public string QueryInsert;
    public string QueryUpdate;
    public string QueryDelete;
    public Dictionary<string, PropertyInfo> Columns;
    public object Items;
    public int MaxId;
  }

  #region Tables

  public class BaseTable {
    [Column(Name = "Id", DbType = "integer", IsPrimaryKey = true, CanBeNull = false)]
    public int Id { get; set; }
  }

  [Table(Name = "Directories")]
  public class Directory : BaseTable {
    [Column(Name = "Path", DbType = "nvarchar(256) COLLATE NOCASE", CanBeNull = false)]
    public string Path { get; set; }
  }

  [Table(Name = "Filters")]
  public class Filter : BaseTable {
    [Column(Name = "ParentId", DbType = "integer", CanBeNull = true)]
    public int? ParentId { get; set; }

    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Data", DbType = "blob", CanBeNull = false)]
    public byte[] Data { get; set; }
  }

  [Table(Name = "Keywords")]
  public class Keyword : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(128) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Idx", DbType = "integer", CanBeNull = false)]
    public int Idx { get; set; }
  }

  [Table(Name = "MediaItemKeyword")]
  public class MediaItemKeyword : BaseTable {
    [Column(Name = "MediaItemId", DbType = "integer", CanBeNull = false)]
    public int MediaItemId { get; set; }

    [Column(Name = "KeywordId", DbType = "integer", CanBeNull = false)]
    public int KeywordId { get; set; }
  }

  [Table(Name = "MediaItemPerson")]
  public class MediaItemPerson : BaseTable {
    [Column(Name = "MediaItemId", DbType = "integer", CanBeNull = false)]
    public int MediaItemId { get; set; }

    [Column(Name = "PersonId", DbType = "integer", CanBeNull = false)]
    public int PersonId { get; set; }
  }

  [Table(Name = "MediaItems")]
  public class MediaItem : BaseTable {
    [Column(Name = "DirectoryId", DbType = "integer", CanBeNull = false)]
    public int DirectoryId { get; set; }

    [Column(Name = "FileName", DbType = "nvarchar(256) COLLATE NOCASE", CanBeNull = false)]
    public string FileName { get; set; }

    [Column(Name = "Rating", DbType = "integer DEFAULT 0", CanBeNull = false)]
    public int Rating { get; set; }

    [Column(Name = "Comment", DbType = "nvarchar(256) COLLATE NOCASE", CanBeNull = true)]
    public string Comment { get; set; }

    [Column(Name = "Orientation", DbType = "integer DEFAULT 1", CanBeNull = false)]
    public int Orientation { get; set; }
  }

  [Table(Name = "People")]
  public class Person : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "PeopleGroupId", DbType = "integer", CanBeNull = true)]
    public int? PeopleGroupId { get; set; }
  }

  [Table(Name = "PeopleGroups")]
  public class PeopleGroup : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }
  }

  [Table(Name = "Viewers")]
  public class Viewer : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }
  }

  [Table(Name = "ViewersAccess")]
  public class ViewerAccess : BaseTable {
    [Column(Name = "ViewerId", DbType = "integer", CanBeNull = false)]
    public int ViewerId { get; set; }

    [Column(Name = "IsIncluded", DbType = "bit", CanBeNull = false)]
    public bool IsIncluded { get; set; }

    [Column(Name = "DirectoryId", DbType = "integer", CanBeNull = true)]
    public int? DirectoryId { get; set; }

    [Column(Name = "MediaItemId", DbType = "integer", CanBeNull = true)]
    public int? MediaItemId { get; set; }
  }

  [Table(Name = "sqlite_sequence")]
  public class SQLiteSequence {
    [Column(Name = "name")]
    public string Name { get; set; }

    [Column(Name = "seq")]
    public int Seq { get; set; }
  }

  #endregion

}

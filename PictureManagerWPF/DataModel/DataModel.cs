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

    private static readonly Mutex Mut = new Mutex();

    public List<CategoryGroup> CategoryGroups;
    public List<CategoryGroupItem> CategoryGroupsItems; 
    public List<Directory> Directories;
    public List<Filter> Filters;
    public List<Keyword> Keywords;
    public List<MediaItemKeyword> MediaItemKeywords;
    public List<MediaItemPerson> MediaItemPeople;
    public List<MediaItem> MediaItems;
    public List<Person> People;
    public List<Viewer> Viewers;
    public List<ViewerAccess> ViewersAccess;
    public List<GeoName> GeoNames;
    public List<SqlQuery> SqlQueries;
    public List<SQLiteSequence> SQLiteSequences;

    public PmDataContext(string connectionString) {
      ConnectionString = connectionString;
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
      if (!disposing) return;

      if (DbConn != null) {
        DbConn.Close();
        DbConn.Dispose();
        DbConn = null;
      }

      Mut?.Dispose();
    }

    public bool OpenDbConnection() {
      if (DbConn == null) DbConn = new SQLiteConnection(ConnectionString);
      if (DbConn.State != ConnectionState.Open) DbConn.Open();
      return true;
    }

    public bool Load() {
      if (!OpenDbConnection()) return false;

      TableInfos.Clear();

      CategoryGroups = GetTableData<CategoryGroup>();
      CategoryGroupsItems = GetTableData<CategoryGroupItem>();
      Directories = GetTableData<Directory>();
      Filters = GetTableData<Filter>();
      Keywords = GetTableData<Keyword>();
      MediaItemKeywords = GetTableData<MediaItemKeyword>();
      MediaItemPeople = GetTableData<MediaItemPerson>();
      MediaItems = GetTableData<MediaItem>();
      People = GetTableData<Person>();
      Viewers = GetTableData<Viewer>();
      ViewersAccess = GetTableData<ViewerAccess>();
      GeoNames = GetTableData<GeoName>();
      SqlQueries = GetTableData<SqlQuery>();

      return true;
    }

    public List<BaseTable>[] GetInsertUpdateDeleteLists() {
      return new[] {new List<BaseTable>(), new List<BaseTable>(), new List<BaseTable>()};
    }

    public void Insert(BaseTable o) {
      if (!OpenDbConnection()) return;
      using (var cmd = DbConn.CreateCommand()) {
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
      if (!Mut.WaitOne()) return;
      cmd.ExecuteNonQuery();
      UpdateInList(o, true);
      Mut.ReleaseMutex();
    }

    public void Update(BaseTable o) {
      if (!OpenDbConnection()) return;
      using (var cmd = DbConn.CreateCommand()) {
        Update(cmd, o);
      }
    }

    public void Update(SQLiteCommand cmd, BaseTable o) {
      var columns = GetColumnValues(o);
      cmd.CommandText = TableInfos[o.GetType()].QueryUpdate;
      cmd.Parameters.Clear();
      foreach (var column in columns) {
        cmd.Parameters.Add(new SQLiteParameter($"@{column.Key}", column.Value));
      }
      if (!Mut.WaitOne()) return;
      cmd.ExecuteNonQuery();
      Mut.ReleaseMutex();
    }

    public void Delete(BaseTable o) {
      if (!OpenDbConnection()) return;
      using (var cmd = DbConn.CreateCommand()) {
        Delete(cmd, o);
      }
    }

    public void Delete(SQLiteCommand cmd, BaseTable o) {
      cmd.CommandText = TableInfos[o.GetType()].QueryDelete;
      cmd.Parameters.Clear();
      cmd.Parameters.Add(new SQLiteParameter("@Id", o.Id));
      if (!Mut.WaitOne()) return;
      cmd.ExecuteNonQuery();
      UpdateInList(o, false);
      Mut.ReleaseMutex();
    }

    public void InsertOnSubmit(BaseTable data, List<BaseTable>[] lists) {
      lists[0].Add(data);
    }

    public void UpdateOnSubmit(BaseTable data, List<BaseTable>[] lists) {
      lists[1].Add(data);
    }

    public void DeleteOnSubmit(BaseTable data, List<BaseTable>[] lists) {
      lists[2].Add(data);
    }

    private void UpdateInList(BaseTable data, bool addIt) {
      switch (data) {
        case CategoryGroup x: { if (addIt) CategoryGroups.Add(x); else CategoryGroups.Remove(x); break; }
        case CategoryGroupItem x: { if (addIt) CategoryGroupsItems.Add(x); else CategoryGroupsItems.Remove(x); break; }
        case Directory x: { if (addIt) Directories.Add(x); else Directories.Remove(x); break; }
        case Filter x: { if (addIt) Filters.Add(x); else Filters.Remove(x); break; }
        case Keyword x: { if (addIt) Keywords.Add(x); else Keywords.Remove(x); break; }
        case MediaItemKeyword x: { if (addIt) MediaItemKeywords.Add(x); else MediaItemKeywords.Remove(x); break; }
        case MediaItemPerson x: { if (addIt) MediaItemPeople.Add(x); else MediaItemPeople.Remove(x); break; }
        case MediaItem x: { if (addIt) MediaItems.Add(x); else MediaItems.Remove(x); break; }
        case Person x: { if (addIt) People.Add(x); else People.Remove(x); break; }
        case Viewer x: { if (addIt) Viewers.Add(x); else Viewers.Remove(x); break; }
        case ViewerAccess x: { if (addIt) ViewersAccess.Add(x); else ViewersAccess.Remove(x); break; }
        case GeoName x: { if (addIt) GeoNames.Add(x); else GeoNames.Remove(x); break; }
        case SqlQuery x: { if (addIt) SqlQueries.Add(x); else SqlQueries.Remove(x); break; }
      }
    }

    public bool SubmitChanges(List<BaseTable>[] lists) {
      if (!OpenDbConnection()) return false;
      if (!Mut.WaitOne()) return false;

      using (var tr = DbConn.BeginTransaction()) {
        using (var cmd = DbConn.CreateCommand()) {
          cmd.Transaction = tr;

          lists[0].ForEach(o => Insert(cmd, o));
          lists[0].Clear();

          lists[1].ForEach(o => Update(cmd, o));
          lists[1].Clear();

          lists[2].ForEach(o => Delete(cmd, o));
          lists[2].Clear();
        }
        tr.Commit();
      }

      Mut.ReleaseMutex();
      return true;
    }

    public void RollbackChanges(List<BaseTable>[] lists) {
      lists[0].Clear();
      lists[1].ForEach(ReloadItem);
      lists[1].Clear();
      lists[2].Clear();
    }

    public void ReloadItem(BaseTable item) {
      var tableInfo = TableInfos[item.GetType()];
      var dbItem = Select($"{tableInfo.QuerySelect} where Id = {item.Id}");
      var i = 0;
      foreach (var column in tableInfo.Columns) {
        var val = dbItem[0].ItemArray[i];
        var type = column.Value.PropertyType;
        type = Nullable.GetUnderlyingType(type) ?? type;
        column.Value.SetValue(item, val is DBNull ? null : Convert.ChangeType(val, type));
        i++;
      }
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
          MaxId = maxId
        });

      return data;
    }

    public DataRowCollection Select(string sql) {
      if (!OpenDbConnection()) return null;
      using (var cmd = DbConn.CreateCommand()) {
        cmd.CommandText = sql;
        using (var adapter = new SQLiteDataAdapter(cmd)) {
          var ds = new DataSet();
          adapter.Fill(ds);
          return ds.Tables[0].Rows;
        }
      }
    }

    public bool Execute(string sql) {
      if (!OpenDbConnection()) return false;
      using (var cmd = DbConn.CreateCommand()) {
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
      return Directories.SingleOrDefault(x => x.Path.Equals(path, StringComparison.OrdinalIgnoreCase))?.Id;
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

  [Table(Name = "CategoryGroups")]
  public class CategoryGroup : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Category", DbType = "integer", CanBeNull = false)]
    public int Category { get; set; }
  }

  [Table(Name = "CategoryGroupsItems")]
  public class CategoryGroupItem : BaseTable {
    [Column(Name = "CategoryGroupId", DbType = "integer", CanBeNull = false)]
    public int CategoryGroupId { get; set; }

    [Column(Name = "ItemId", DbType = "integer", CanBeNull = false)]
    public int ItemId { get; set; }
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

    [Column(Name = "Width", DbType = "integer DEFAULT 0", CanBeNull = false)]
    public int Width { get; set; }

    [Column(Name = "Height", DbType = "integer DEFAULT 0", CanBeNull = false)]
    public int Height { get; set; }

    [Column(Name = "GeoNameId", DbType = "integer", CanBeNull = true)]
    public int? GeoNameId { get; set; }
  }

  [Table(Name = "People")]
  public class Person : BaseTable {
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

  [Table(Name = "GeoNames")]
  public class GeoName : BaseTable
  {
    [Column(Name = "ParentGeoNameId", DbType = "integer", CanBeNull = true)]
    public int? ParentGeoNameId { get; set; }

    [Column(Name = "ToponymName", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string ToponymName { get; set; }

    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "GeoNameId", DbType = "integer", CanBeNull = false)]
    public int GeoNameId { get; set; }

    [Column(Name = "Fcode", DbType = "nvarchar(5)", CanBeNull = false)]
    public string Fcode { get; set; }
  }

  [Table(Name = "SqlQueries")]
  public class SqlQuery : BaseTable {
    [Column(Name = "Name", DbType = "nvarchar(64) COLLATE NOCASE", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Query", DbType = "ntext", CanBeNull = false)]
    public string Query { get; set; }
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;

namespace PictureManager.DataModel {

  public class PmDataContext : IDisposable {
    public SQLiteConnection DbConn;
    public string ConnectionString;
    public Dictionary<string, int> TableIds = new Dictionary<string, int>();
    public Dictionary<Type, TableInfo> TableInfos = new Dictionary<Type, TableInfo>(); 

    private readonly List<BaseTable> _toInsert = new List<BaseTable>();
    private readonly List<BaseTable> _toUpdate = new List<BaseTable>();
    private readonly List<BaseTable> _toDelete = new List<BaseTable>();

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
      SQLiteSequences = GetTableData<SQLiteSequence>();

      TableIds.Clear();
      SQLiteSequences.ForEach(s => TableIds.Add(s.Name, s.Seq));

      return true;
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

    public bool SubmitChanges() {
      if (!OpenDbConnection()) return false;

      using (SQLiteTransaction tr = DbConn.BeginTransaction()) {
        using (SQLiteCommand cmd = DbConn.CreateCommand()) {
          cmd.Transaction = tr;

          //Insert
          foreach (var o in _toInsert) {
            var columns = GetColumnValues(o);
            cmd.CommandText = TableInfos[o.GetType()].QueryInsert;
            cmd.Parameters.Clear();
            foreach (var column in columns) {
              cmd.Parameters.Add(new SQLiteParameter($"@{column.Key}", column.Value));
            }
            cmd.ExecuteNonQuery();
          }
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

      var columns = new Dictionary<string, PropertyInfo>();
      foreach (var propertyInfo in typeof(T).GetProperties()) {
        var colName = propertyInfo.GetCustomAttribute<ColumnAttribute>(true)?.Name;
        if (colName == null) continue;
        columns.Add(colName, propertyInfo);
      }

      //create queries
      var paramNames = string.Join(", ", columns.Keys.Select(x => $"@{x}"));
      var setColumns = string.Join(", ", columns.Keys.Where(x => !x.Equals("Id")).Select(x => $"{x} = @{x}"));
      var qSelect = $"select {string.Join(", ", columns.Keys)} from {tableName}";
      var qInsert = $"insert into {tableName} ({string.Join(", ", columns.Keys)}) values ({paramNames})";
      var qUpdate = $"update {tableName} set {setColumns} where Id = @Id";
      var qDelete = $"delete from {tableName} where Id = @Id";
      TableInfos.Add(typeof (T),
        new TableInfo {
          Columns = columns,
          QuerySelect = qSelect,
          QueryInsert = qInsert,
          QueryUpdate = qUpdate,
          QueryDelete = qDelete
        });

      var data = new List<T>();
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

    public int GetNextIdFor(string tableName) {
      if (!TableIds.ContainsKey(tableName)) {
        TableIds.Add(tableName, 0);
      }
      var nextId = TableIds[tableName] + 1;
      TableIds[tableName] = nextId;
      return nextId;
    }

    public int GetMaxIdFor(string tableName) {
      return !TableIds.ContainsKey(tableName) ? 0 : TableIds[tableName];
    }
  }

  public class TableInfo {
    public string QuerySelect;
    public string QueryInsert;
    public string QueryUpdate;
    public string QueryDelete;
    public Dictionary<string, PropertyInfo> Columns;
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

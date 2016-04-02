using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

namespace PictureManager.DataModel {

  public class PmDataContext : IDisposable {
    public SQLiteConnection DbConn;
    public string ConnectionString;
    public DataContext DataContext;
    public Dictionary<string, long> TableIds;

    private List<object> _toDelete = new List<object>(); 

    public Table<Directory> Directories;
    public Table<Filter> Filters;
    public Table<Keyword> Keywords;
    public Table<MediaItemKeyword> MediaItemKeywords;
    public Table<MediaItemPerson> MediaItemPeople;
    public Table<MediaItem> MediaItems;
    public Table<Person> People;
    public Table<PeopleGroup> PeopleGroups;
    public Table<Viewer> Viewers; 
    public Table<SQLiteSequence> SQLiteSequences;

    private List<Directory> _listDirectories;
    private List<Filter> _listFilters;
    private List<Keyword> _listKeywords;
    private List<MediaItemKeyword> _listMediaItemKeywords;
    private List<MediaItemPerson> _listMediaItemPeople;
    private List<MediaItem> _listMediaItems;
    private List<Person> _listPeople;
    private List<PeopleGroup> _listPeopleGroups;
    private List<Viewer> _listViewers;

    public List<Directory> ListDirectories => _listDirectories ?? (_listDirectories = Directories.ToList());
    public List<Filter> ListFilters => _listFilters ?? (_listFilters = Filters.ToList());
    public List<Keyword> ListKeywords => _listKeywords ?? (_listKeywords = Keywords.ToList());
    public List<MediaItemKeyword> ListMediaItemKeywords => _listMediaItemKeywords ?? (_listMediaItemKeywords = MediaItemKeywords.ToList());
    public List<MediaItemPerson> ListMediaItemPeople => _listMediaItemPeople ?? (_listMediaItemPeople = MediaItemPeople.ToList());
    public List<MediaItem> ListMediaItems => _listMediaItems ?? (_listMediaItems = MediaItems.ToList());
    public List<Person> ListPeople => _listPeople ?? (_listPeople = People.ToList());
    public List<PeopleGroup> ListPeopleGroups => _listPeopleGroups ?? (_listPeopleGroups = PeopleGroups.ToList());
    public List<Viewer> ListViewers => _listViewers ?? (_listViewers = Viewers.ToList());

    public PmDataContext(string connectionString) {
      ConnectionString = connectionString;
    }

    public void Dispose() {
      DataContext.Dispose();
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
      DataContext = new DataContext(DbConn);

      Directories = DataContext.GetTable<Directory>();
      Filters = DataContext.GetTable<Filter>();
      Keywords = DataContext.GetTable<Keyword>();
      MediaItemKeywords = DataContext.GetTable<MediaItemKeyword>();
      MediaItemPeople = DataContext.GetTable<MediaItemPerson>();
      MediaItems = DataContext.GetTable<MediaItem>();
      People = DataContext.GetTable<Person>();
      PeopleGroups = DataContext.GetTable<PeopleGroup>();
      Viewers = DataContext.GetTable<Viewer>();
      SQLiteSequences = DataContext.GetTable<SQLiteSequence>();

      TableIds = new Dictionary<string, long>();
      foreach (var s in SQLiteSequences) {
        TableIds.Add(s.Name, s.Seq);
      }
      return true;
    }

    public void SubmitChanges() {
      SubmitDelete();
      DataContext.SubmitChanges();
    }

    private void SubmitDelete() {
      foreach (var data in _toDelete) {
        switch (data.GetType().Name) {
          case nameof(Directory): { ListDirectories.Remove((Directory)data); break; }
          case nameof(Filter): { ListFilters.Remove((Filter) data); break; }
          case nameof(Keyword): { ListKeywords.Remove((Keyword)data); break; }
          case nameof(MediaItemKeyword): { ListMediaItemKeywords.Remove((MediaItemKeyword)data); break; }
          case nameof(MediaItemPerson): { ListMediaItemPeople.Remove((MediaItemPerson)data); break; }
          case nameof(MediaItem): { ListMediaItems.Remove((MediaItem)data); break; }
          case nameof(Person): { ListPeople.Remove((Person)data); break; }
          case nameof(PeopleGroup): { ListPeopleGroups.Remove((PeopleGroup)data); break; }
          case nameof(Viewer): { ListViewers.Remove((Viewer)data); break; }
        }
      }
      _toDelete.Clear();
    }

    public void InsertOnSubmit(object data) {
      switch (data.GetType().Name) {
        case nameof(Directory): {
          Directories.InsertOnSubmit((Directory) data);
          ListDirectories.Add((Directory) data);
          break;
        }
        case nameof(Filter): {
          Filters.InsertOnSubmit((Filter) data);
          ListFilters.Add((Filter) data);
          break;
        }
        case nameof(Keyword): {
          Keywords.InsertOnSubmit((Keyword) data);
          ListKeywords.Add((Keyword) data);
          break;
        }
        case nameof(MediaItemKeyword): {
          MediaItemKeywords.InsertOnSubmit((MediaItemKeyword) data);
          ListMediaItemKeywords.Add((MediaItemKeyword) data);
          break;
        }
        case nameof(MediaItemPerson): {
          MediaItemPeople.InsertOnSubmit((MediaItemPerson) data);
          ListMediaItemPeople.Add((MediaItemPerson) data);
          break;
        }
        case nameof(MediaItem): {
          MediaItems.InsertOnSubmit((MediaItem) data);
          ListMediaItems.Add((MediaItem) data);
          break;
        }
        case nameof(Person): {
          People.InsertOnSubmit((Person) data);
          ListPeople.Add((Person) data);
          break;
        }
        case nameof(PeopleGroup): {
          PeopleGroups.InsertOnSubmit((PeopleGroup) data);
          ListPeopleGroups.Add((PeopleGroup) data);
          break;
        }
        case nameof(Viewer): {
          Viewers.InsertOnSubmit((Viewer) data);
          ListViewers.Add((Viewer) data);
          break;
        }
      }
    }

    public void DeleteOnSubmit(object data) {
      _toDelete.Add(data);
      switch (data.GetType().Name) {
        case nameof(Directory): { Directories.DeleteOnSubmit((Directory) data); break; }
        case nameof(Filter): { Filters.DeleteOnSubmit((Filter) data); break; }
        case nameof(Keyword): { Keywords.DeleteOnSubmit((Keyword) data); break; }
        case nameof(MediaItemKeyword): { MediaItemKeywords.DeleteOnSubmit((MediaItemKeyword) data); break; }
        case nameof(MediaItemPerson): { MediaItemPeople.DeleteOnSubmit((MediaItemPerson) data); break; }
        case nameof(MediaItem): { MediaItems.DeleteOnSubmit((MediaItem) data); break; }
        case nameof(Person): { People.DeleteOnSubmit((Person) data); break; }
        case nameof(PeopleGroup): { PeopleGroups.DeleteOnSubmit((PeopleGroup) data); break; }
        case nameof(Viewer): { Viewers.DeleteOnSubmit((Viewer) data); break; }
      }
    }

    public long GetNextIdFor(string tableName) {
      if (!TableIds.ContainsKey(tableName)) {
        TableIds.Add(tableName, 0);
      }
      var nextId = TableIds[tableName] + 1;
      TableIds[tableName] = nextId;
      return nextId;
    }

    public long GetMaxIdFor(string tableName) {
      return !TableIds.ContainsKey(tableName) ? 0 : TableIds[tableName];
    }

    public bool Execute(string sql) {
      if (!OpenDbConnection()) return false;
      using (SQLiteCommand cmd = DbConn.CreateCommand()) {
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
      }
      return true;
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

    public void CreateDbStructure() {
      Execute("CREATE TABLE IF NOT EXISTS \"Directories\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Path] nvarchar(256) NOT NULL COLLATE NOCASE);");

      Execute("CREATE TABLE IF NOT EXISTS \"Filters\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[ParentId] integer"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE"
              + ",[Data] blob NOT NULL);");

      Execute("CREATE TABLE IF NOT EXISTS \"Keywords\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(128) NOT NULL COLLATE NOCASE"
              + ",[Idx] integer NOT NULL DEFAULT 0);");

      Execute("CREATE TABLE IF NOT EXISTS \"MediaItemKeyword\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[MediaItemId] integer NOT NULL"
              + ",[KeywordId] integer NOT NULL);");

      Execute("CREATE TABLE IF NOT EXISTS \"MediaItems\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[DirectoryId] integer NOT NULL"
              + ",[FileName] nvarchar(256) NOT NULL COLLATE NOCASE"
              + ",[Rating] integer DEFAULT 0"
              + ",[Comment] nvarchar(256) COLLATE NOCASE"
              + ",[Orientation] integer NOT NULL DEFAULT 1);");

      Execute("CREATE TABLE IF NOT EXISTS \"People\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE"
              + ",[PeopleGroupId] integer);");

      Execute("CREATE TABLE IF NOT EXISTS \"PeopleGroups\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE);");

      Execute("CREATE TABLE IF NOT EXISTS \"MediaItemPerson\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[MediaItemId] integer NOT NULL"
              + ",[PersonId] integer NOT NULL); ");

      Execute("CREATE TABLE IF NOT EXISTS \"Viewers\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE"
              + ",[DirsAllowed] ntext NOT NULL"
              + ",[DirsDenied] ntext NOT NULL"
              + ",[FilesAllowed] ntext NOT NULL"
              + ",[FilesDenied] ntext NOT NULL); ");
    }
  }

  [Table(Name = "Directories")]
  public class Directory {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "Path", CanBeNull = false)]
    public string Path { get; set; }
  }

  [Table(Name = "Filters")]
  public class Filter {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "ParentId", CanBeNull = true)]
    public long? ParentId { get; set; }

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Data", CanBeNull = false)]
    public byte[] Data { get; set; }
  }

  [Table(Name = "Keywords")]
  public class Keyword {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "Idx", CanBeNull = false)]
    public long Idx { get; set; }
  }

  [Table(Name = "MediaItemKeyword")]
  public class MediaItemKeyword {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "MediaItemId", CanBeNull = false)]
    public long MediaItemId { get; set; }

    [Column(Name = "KeywordId", CanBeNull = false)]
    public long KeywordId { get; set; }
  }

  [Table(Name = "MediaItemPerson")]
  public class MediaItemPerson {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "MediaItemId", CanBeNull = false)]
    public long MediaItemId { get; set; }

    [Column(Name = "PersonId", CanBeNull = false)]
    public long PersonId { get; set; }
  }

  [Table(Name = "MediaItems")]
  public class MediaItem {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "DirectoryId", CanBeNull = false)]
    public long DirectoryId { get; set; }

    [Column(Name = "FileName", CanBeNull = false)]
    public string FileName { get; set; }

    [Column(Name = "Rating", CanBeNull = false)]
    public long Rating { get; set; }

    [Column(Name = "Comment", CanBeNull = true)]
    public string Comment { get; set; }

    [Column(Name = "Orientation", CanBeNull = false)]
    public long Orientation { get; set; }
  }

  [Table(Name = "People")]
  public class Person {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "PeopleGroupId", CanBeNull = true)]
    public long? PeopleGroupId { get; set; }
  }

  [Table(Name = "PeopleGroups")]
  public class PeopleGroup {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }
  }

  [Table(Name = "Viewers")]
  public class Viewer {
    [Column(Name = "Id", IsPrimaryKey = true, CanBeNull = false)]
    public long Id { get; set; }

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    [Column(Name = "DirsAllowed", CanBeNull = false)]
    public string DirsAllowed { get; set; }

    [Column(Name = "DirsDenied", CanBeNull = false)]
    public string DirsDenied { get; set; }

    [Column(Name = "FilesAllowed", CanBeNull = false)]
    public string FilesAllowed { get; set; }

    [Column(Name = "FilesDenied", CanBeNull = false)]
    public string FilesDenied { get; set; }
  }

  [Table(Name = "sqlite_sequence")]
  public class SQLiteSequence {
    [Column(Name = "name")]
    public string Name { get; set; }

    [Column(Name = "seq")]
    public long Seq { get; set; }
  }
}

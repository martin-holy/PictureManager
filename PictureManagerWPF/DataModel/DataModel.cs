using System;
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

    public Table<Directory> Directories;
    public Table<Filter> Filters;
    public Table<Keyword> Keywords;
    public Table<MediaItemKeyword> MediaItemKeywords;
    public Table<MediaItemPerson> MediaItemPeople;
    public Table<MediaItem> MediaItems;
    public Table<Person> People;
    public Table<PeopleGroup> PeopleGroups;
    public Table<SQLiteSequence> SQLiteSequences;

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
      SQLiteSequences = DataContext.GetTable<SQLiteSequence>();

      return true;
    }

    public long GetNextIdFor(string tableName) {
      return SQLiteSequences.Single(x => x.Name.Equals(tableName)).Seq + 1;
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

  [Table(Name = "sqlite_sequence")]
  public class SQLiteSequence {
    [Column(Name = "name")]
    public string Name { get; set; }

    [Column(Name = "seq")]
    public long Seq { get; set; }
  }
}

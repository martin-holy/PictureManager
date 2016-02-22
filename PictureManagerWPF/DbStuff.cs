﻿using System;
using System.Data;
using System.Data.SQLite;

namespace PictureManager {
  public class DbStuff : IDisposable {
    public SQLiteConnection DbConn;
    public string ConnectionString;

    public DbStuff(string connectionString) {
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

    public void CloseDbConnection() {
      if (DbConn.State == ConnectionState.Open) DbConn.Close();
    }

    public int? GetLastIdFor(string tableName) {
      return (int?) (long?) ExecuteScalar($"select seq from sqlite_sequence where name = '{tableName}'");
    }

    public object ExecuteScalar(string sql) {
      if (!OpenDbConnection()) return null;
      using (SQLiteCommand cmd = DbConn.CreateCommand()) {
        cmd.CommandText = sql;
        return cmd.ExecuteScalar();
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

    public int? GetDirectoryIdByPath(string path) {
      return (int?) (long?) ExecuteScalar($"select Id from Directories where Path = '{path}'");
    }

    public int? InsertDirecotryInToDb(string path) {
      int? dirId = GetDirectoryIdByPath(path);
      if (dirId != null) return dirId;
      return Execute($"insert into Directories (Path) values ('{path}')") ? GetLastIdFor("Directories") : null;
    }

    public void CreateDbStructure() {
      Execute("CREATE TABLE IF NOT EXISTS \"Directories\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Path] nvarchar(256) NOT NULL COLLATE NOCASE);");

      Execute("CREATE TABLE IF NOT EXISTS \"FilterFolders\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE"
              + ",[ParentId] integer NOT NULL DEFAULT 0);");

      Execute("CREATE TABLE IF NOT EXISTS \"Filters\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE"
              + ",[Description] nvarchar(256)"
              + ",[FilterFolderId] integer NOT NULL"
              + ",[DirectoryId] integer"
              + ",[MatchType] integer NOT NULL DEFAULT 0"
              + ",[IncludedKeywords] varchar(256)"
              + ",[ExcludedKeywords] varchar(256)"
              + ",[IncludedPeople] varchar(256)"
              + ",[ExcludedPeople] varchar(256));");

      Execute("CREATE TABLE IF NOT EXISTS \"Keywords\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Keyword] nvarchar(128) NOT NULL COLLATE NOCASE"
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
              + ",[Comment] nvarchar(256) COLLATE NOCASE);");

      Execute("CREATE TABLE IF NOT EXISTS \"People\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Name] nvarchar(64) NOT NULL COLLATE NOCASE);");

      Execute("CREATE TABLE IF NOT EXISTS \"MediaItemPerson\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[MediaItemId] integer NOT NULL"
              + ",[PersonId] integer NOT NULL); ");

      Execute("CREATE TABLE IF NOT EXISTS \"FavoriteFolders\"("
              + "[Id] integer PRIMARY KEY AUTOINCREMENT NOT NULL"
              + ",[Path] nvarchar(256) NOT NULL COLLATE NOCASE); ");
    }
  }
}
using System.Data.Entity;
using System.Data.Linq.Mapping;

namespace PictureManager.DataModel {

  public class PmDbContext : DbContext {
    //public PmDbContext() : base("name=PictureManagerContext") { }
    
    public DbSet<Directory> Directories { get; set; }
    public DbSet<Filter> Filters { get; set; }
    public DbSet<Keyword> Keywords { get; set; }
    public DbSet<MediaItemKeyword> MediaItemKeywords { get; set; }
    public DbSet<MediaItemPerson> MediaItemPeople { get; set; }
    public DbSet<MediaItem> MediaItems { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<PeopleGroup> PeopleGroups { get; set; }
  }

  [Table(Name = "Directories")]
  public class Directory {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "Path")]
    public string Path { get; set; }
  }

  [Table(Name = "Filters")]
  public class Filter {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "ParentId")]
    public int ParentId { get; set; }

    [Column(Name = "Name")]
    public string Name { get; set; }

    [Column(Name = "Data")]
    public byte[] Data { get; set; }
  }

  [Table(Name = "Keywords")]
  public class Keyword {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "Name")]
    public string Name { get; set; }

    [Column(Name = "Idx")]
    public int Idx { get; set; }
  }

  [Table(Name = "MediaItemKeyword")]
  public class MediaItemKeyword {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "MediaItemId")]
    public int MediaItemId { get; set; }

    [Column(Name = "KeywordId")]
    public int KeywordId { get; set; }
  }

  [Table(Name = "MediaItemPerson")]
  public class MediaItemPerson {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "MediaItemId")]
    public int MediaItemId { get; set; }

    [Column(Name = "PersonId")]
    public int PersonId { get; set; }
  }

  [Table(Name = "MediaItems")]
  public class MediaItem {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "DirectoryId")]
    public int DirectoryId { get; set; }

    [Column(Name = "FileName")]
    public string FileName { get; set; }

    [Column(Name = "Rating")]
    public int Rating { get; set; }

    [Column(Name = "Comment")]
    public string Comment { get; set; }

    [Column(Name = "Orientation")]
    public int Orientation { get; set; }
  }

  [Table(Name = "People")]
  public class Person {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "Name")]
    public string Name { get; set; }

    [Column(Name = "PeopleGroupId")]
    public int PeopleGroupId { get; set; }
  }

  [Table(Name = "PeopleGroups")]
  public class PeopleGroup {
    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "Name")]
    public string Name { get; set; }
  }
}

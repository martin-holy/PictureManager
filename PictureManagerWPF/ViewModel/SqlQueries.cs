using System.Collections.Generic;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public sealed class SqlQueries : BaseCategoryItem {
    public List<SqlQuery> AllSqlQueries;

    public SqlQueries() : base(Categories.SqlQueries) {
      AllSqlQueries = new List<SqlQuery>();
      Title = "SQL Queries";
      IconName = "appbar_database_sql";
    }

    public void Load() {
      Items.Clear();
      AllSqlQueries.Clear();

      LoadGroups();

      //Add Queries in Group
      foreach (var g in Items.Cast<CategoryGroup>()) {
        foreach (var sqlQuery in (from q in ACore.Db.SqlQueries
          join cgi in ACore.Db.CategoryGroupsItems
          on new { qid = q.Id, gid = g.Data.Id } equals new { qid = cgi.ItemId, gid = cgi.CategoryGroupId }
          select q).OrderBy(x => x.Name).Select(x => new SqlQuery(x) { Parent = g })) {
          g.Items.Add(sqlQuery);
          AllSqlQueries.Add(sqlQuery);
        }
      }

      //Add Queries without Group
      foreach (var sqlQuery in (from q in ACore.Db.SqlQueries where AllSqlQueries.All(aq => aq.Data.Id != q.Id) select q)
        .OrderBy(x => x.Name).Select(x => new SqlQuery(x) { Parent = this })) {
        Items.Add(sqlQuery);
        AllSqlQueries.Add(sqlQuery);
      }
    }

    public SqlQuery CreateSqlQuery(BaseTreeViewItem root, string name, string query) {
      if (root == null) return null;

      var dmSqlQuery = new DataModel.SqlQuery {
        Id = ACore.Db.GetNextIdFor<DataModel.SqlQuery>(),
        Name = name,
        Query = query
      };

      ACore.Db.Insert(dmSqlQuery);

      InsertCategoryGroupItem(root, dmSqlQuery.Id);

      var vmSqlQuery = new SqlQuery(dmSqlQuery) { Parent = root };
      AllSqlQueries.Add(vmSqlQuery);
      ACore.SqlQueries.ItemSetInPlace(root, true, vmSqlQuery);
      return vmSqlQuery;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var sqlQueryDialog = new SqlQueryDialog {
        Owner = ACore.WMain,
        IconName = IconName,
        SqlQueryName = rename ? ((SqlQuery) item).Title : string.Empty,
        SqlQueryQuery = rename ? ((SqlQuery) item).Data.Query : string.Empty
      };

      sqlQueryDialog.BtnDialogOk.Click += delegate {
        var root = rename ? item.Parent : item;
        if (root.Items.Where(x => !(x is CategoryGroup))
              .SingleOrDefault(x => x.Title.Equals(sqlQueryDialog.SqlQueryName)) != null) {
          sqlQueryDialog.ShowErrorMessage("SQL Query name already exists!");
          return;
        }

        if (sqlQueryDialog.SqlQueryName == string.Empty) {
          sqlQueryDialog.ShowErrorMessage("Enter name of SQL Query!");
          return;
        }

        sqlQueryDialog.DialogResult = true;
      };

      sqlQueryDialog.TxtName.SelectAll();

      if (!(sqlQueryDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var sqlQuery = (SqlQuery) item;
        sqlQuery.Title = sqlQueryDialog.SqlQueryName;
        sqlQuery.Data.Query = sqlQueryDialog.SqlQueryQuery;
        ACore.Db.Update(sqlQuery.Data);
        ACore.SqlQueries.ItemSetInPlace(sqlQuery.Parent, false, sqlQuery);
      }
      else CreateSqlQuery(item, sqlQueryDialog.SqlQueryName, sqlQueryDialog.SqlQueryQuery);
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is SqlQuery sqlQuery)) return;
      var lists = ACore.Db.GetInsertUpdateDeleteLists();

      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(
        x => x.ItemId == sqlQuery.Data.Id && x.CategoryGroupId == (item.Parent as CategoryGroup)?.Data.Id);
      if (cgi != null) {
        ACore.Db.DeleteOnSubmit(cgi, lists);
      }

      ACore.Db.DeleteOnSubmit(sqlQuery.Data, lists);
      ACore.Db.SubmitChanges(lists);

      item.Parent.Items.Remove(sqlQuery);
      AllSqlQueries.Remove(sqlQuery);
    }
  }
}

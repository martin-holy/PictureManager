using System.Collections.ObjectModel;

namespace PictureManager.ViewModel {
  public class PeopleGroup : BaseTreeViewTagItem {
    public DataModel.PeopleGroup Data;
    public ObservableCollection<Person> Items { get; set; }

    public PeopleGroup() {
      Items = new ObservableCollection<Person>();
    }

    public PeopleGroup(DataModel.PeopleGroup data) : this() {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      IconName = "appbar_people_multiple";
    }
  }
}

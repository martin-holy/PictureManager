using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class PersonDeletedEventArgs : EventArgs {
    public PersonM Person { get; }

    public PersonDeletedEventArgs(PersonM person) {
      Person = person;
    }
  }
}

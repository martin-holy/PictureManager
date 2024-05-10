using PictureManager.Interfaces.Models;
using System.Threading.Tasks;

namespace PictureManager.Interfaces.ViewModels;

public interface IMediaItemVM {
  public Task ViewMediaItems(IMediaItemM[] items, string name);
}
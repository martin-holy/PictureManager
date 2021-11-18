namespace PictureManager.Domain.Interfaces {
  public interface IViewModel<out T> {
    public T ToModel();
  }
}

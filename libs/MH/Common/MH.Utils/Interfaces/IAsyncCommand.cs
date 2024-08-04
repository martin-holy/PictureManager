using System.Threading.Tasks;
using System.Windows.Input;

namespace MH.Utils.Interfaces;

public interface IAsyncCommand : ICommand {
  Task ExecuteAsync(object? parameter, Task task);
}
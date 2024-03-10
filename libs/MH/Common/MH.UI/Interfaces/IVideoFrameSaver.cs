using MH.UI.HelperClasses;
using System;

namespace MH.UI.Interfaces;

public interface IVideoFrameSaver {
  public void Save(VfsVideo[] videos, Action<VfsFrame> onSaveAction, Action<VfsFrame, Exception> onErrorAction, Action onFinishedAction);
}
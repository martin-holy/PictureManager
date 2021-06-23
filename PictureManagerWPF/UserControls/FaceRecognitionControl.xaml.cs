using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels;

namespace PictureManager.UserControls {
  public partial class FaceRecognitionControl: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";
    }

    public async void Recognize(List<MediaItem> mediaItems) {
      ThumbsGrid.ClearRows();
      App.Core.Faces.LoadFromFile();
      App.Core.Faces.LinkReferences();

      var detectedFaces = App.Core.Faces.All.Cast<Face>().Where(x => mediaItems.Contains(x.MediaItem)).ToArray();
      var notRecognizedFaces = detectedFaces.Where(x => x.PersonId == 0);
      var mediaItemsToDetect = mediaItems.Except(detectedFaces.Select(x => x.MediaItem).Distinct());
      var facesToDisplay = new Dictionary<object, long>();

      foreach (var face in notRecognizedFaces)
        facesToDisplay.Add(AddFaceToGrid(face), face.AvgHash);

      foreach (var mi in mediaItemsToDetect) {
        var filePath = mi.MediaType == MediaType.Image ? mi.FilePath : mi.FilePathCache;
        var faces = await Imaging.DetectFaces(filePath, 40);
        
        foreach (var face in faces) {
          var rect = new Rectangle(face.X, face.Y, face.Width, face.Height);
          var rect2 = new Int32Rect(face.X, face.Y, face.Width, face.Height);
          var avgHash = Imaging.GetAvgHash(filePath, rect2);
          var newFace = new Face(App.Core.Faces.Helper.GetNextId(), 0, rect, avgHash) {MediaItem = mi};
          facesToDisplay.Add(AddFaceToGrid(newFace), newFace.AvgHash);
          App.Core.Faces.All.Add(newFace);
        }
      }

      // sort by similarity
      var sorted = Imaging.GetSimilarImages(facesToDisplay, -1);

      // display faces sorted
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      ThumbsGrid.ClearRows();
      foreach (var face in sorted)
        ThumbsGrid.AddItem(face, 100 + itemOffset, new VirtualizingWrapPanelGroupItem[0]);
    }

    private FaceViewModel AddFaceToGrid(Face face) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rect = new Int32Rect(face.FaceBox.X, face.FaceBox.Y, face.FaceBox.Width, face.FaceBox.Height);
      var filePath = face.MediaItem.MediaType == MediaType.Image ? face.MediaItem.FilePath : face.MediaItem.FilePathCache;
      var picture = Imaging.GetCroppedBitmapSource(filePath, rect, 100);
      var faceVm = new FaceViewModel {Face = face, Picture = picture};

      ThumbsGrid.AddItem(faceVm, 100 + itemOffset, new VirtualizingWrapPanelGroupItem[0]);

      return faceVm;
    }
  }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PictureManager.Domain.Models;

namespace PictureManagerWPF.UnitTests.Database {
  [TestClass]
  public class FolderTests {
    public Folder GetByPath_GetTestData() {
      var folderA = new Folder(1, "D:", null);
      var folderB = new Folder(2, "subFolder", folderA);
      var folderC = new Folder(3, "sub folder", folderB);
      var folderD = new Folder(4, "sub folder 2", folderC);

      folderA.Items.Add(folderB);
      folderB.Items.Add(folderC);
      folderC.Items.Add(folderD);

      return folderA;
    }

    [TestMethod]
    // get folder "D:" on folder "D:"
    public void GetByPath_FullPathA_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = root.GetByPath("D:");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "D:\\subFolder" on folder "D:"
    public void GetByPath_FullPathB_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = root.GetByPath("D:\\subFolder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "D:\\subFolder\\sub folder" on folder "D:"
    public void GetByPath_FullPathC_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = root.GetByPath("D:\\subFolder\\sub folder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "D:\\subFolder" on folder "D:\\subFolder"
    public void GetByPath_FullPathD_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = ((Folder)root.Items[0]).GetByPath("D:\\subFolder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "D:\\subFolder\\sub folder" on folder "D:\\subFolder"
    public void GetByPath_FullPathE_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = ((Folder)root.Items[0]).GetByPath("D:\\subFolder\\sub folder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "D:\\other" on folder "D:\\subFolder"
    public void GetByPath_FullPathF_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = ((Folder)root.Items[0]).GetByPath("D:\\other");

      Assert.IsNull(result);
    }

    [TestMethod]
    // get folder "D:\\subFolder2" on folder "D:\\subFolder"
    public void GetByPath_FullPathG_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = ((Folder)root.Items[0]).GetByPath("D:\\subFolder2\\neco");

      Assert.IsNull(result);
    }

    [TestMethod]
    // get folder "subFolder" on folder "D:"
    public void GetByPath_PartialPathA_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = root.GetByPath("subFolder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "subFolder\\sub folder" on folder "D:"
    public void GetByPath_PartialPathB_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = root.GetByPath("subFolder\\sub folder");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }

    [TestMethod]
    // get folder "sub folder\\sub folder 2" on folder "D:\\subFolder"
    public void GetByPath_PartialPathC_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = ((Folder)root.Items[0]).GetByPath("sub folder\\sub folder 2");

      Assert.IsInstanceOfType(result, typeof(Folder));
    }
  }
}

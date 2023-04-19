using MH.Utils;
using MH.Utils.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureManagerWPF.UnitTests {
  [TestClass]
  public class TreeTests {
    public FolderM GetByPath_GetTestData() {
      var folderA = new FolderM(1, "D:", null);
      var folderB = new FolderM(2, "subFolder", folderA);
      var folderC = new FolderM(3, "sub folder", folderB);
      var folderD = new FolderM(4, "sub folder 2", folderC);

      folderA.Items.Add(folderB);
      folderB.Items.Add(folderC);
      folderC.Items.Add(folderD);

      return folderA;
    }

    [TestMethod]
    // get folder "D:" on folder "D:"
    public void GetByPath_FullPathA_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root, "D:", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "D:\\subFolder" on folder "D:"
    public void GetByPath_FullPathB_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root, "D:\\subFolder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "D:\\subFolder\\sub folder" on folder "D:"
    public void GetByPath_FullPathC_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root, "D:\\subFolder\\sub folder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "D:\\subFolder" on folder "D:\\subFolder"
    public void GetByPath_FullPathD_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root.Items[0], "D:\\subFolder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "D:\\subFolder\\sub folder" on folder "D:\\subFolder"
    public void GetByPath_FullPathE_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root.Items[0], "D:\\subFolder\\sub folder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "D:\\other" on folder "D:\\subFolder"
    public void GetByPath_FullPathF_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root.Items[0], "D:\\other", Path.DirectorySeparatorChar);

      Assert.IsNull(result);
    }

    [TestMethod]
    // get folder "D:\\subFolder2" on folder "D:\\subFolder"
    public void GetByPath_FullPathG_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root.Items[0], "D:\\subFolder2\\neco", Path.DirectorySeparatorChar);

      Assert.IsNull(result);
    }

    [TestMethod]
    // get folder "subFolder" on folder "D:"
    public void GetByPath_PartialPathA_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root, "subFolder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "subFolder\\sub folder" on folder "D:"
    public void GetByPath_PartialPathB_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root, "subFolder\\sub folder", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }

    [TestMethod]
    // get folder "sub folder\\sub folder 2" on folder "D:\\subFolder"
    public void GetByPath_PartialPathC_ReturnsFolder() {
      var root = GetByPath_GetTestData();

      var result = Tree.GetByPath(root.Items[0], "sub folder\\sub folder 2", Path.DirectorySeparatorChar);

      Assert.IsInstanceOfType(result, typeof(FolderM));
    }
  }
}

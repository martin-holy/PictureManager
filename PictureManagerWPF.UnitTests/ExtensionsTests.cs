using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PictureManager.Domain;

namespace PictureManagerWPF.UnitTests {
  [TestClass]
  public class ExtensionsTests {
    [TestMethod]
    public void AddInOrder() {
      var list = new List<string>{"a", "b", "c", "d"};
      list.AddInOrder("ba", (a, b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) >= 0);

      Assert.AreEqual(list.IndexOf("ba"), 2);
    }
  }
}

using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Threading.Tasks;

namespace MovieManager.Plugins.FDbCz;

public class Core : IPluginCore, IMovieSearchPlugin, IMovieDetailPlugin {
  public static readonly string IdName = "FDbCz";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://www.fdb.cz/vyhledavani.html?hledat={query.Replace(' ', '+')}";
    var content = await Common.Core.GetWebPageContent(url, "cs");
    if (content == null) return [];

    try {
      return Parser.ParseSearch(content);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return [];
    }
  }

  public async Task<MovieDetail> GetMovieDetail(DetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.fdb.cz/film/{Parser.DetailIdToUrl(id)}";
    var content = await Common.Core.GetWebPageContent(url, "cs");
    if (content == null) return null;
    
    try {
      return Parser.ParseMovie(content, id);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}


using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Threading.Tasks;

namespace MovieManager.Plugins.CSFDcz;

// INFO CSFD doesn't have role names. So I will leave this for now.

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin, IMovieDetailPlugin {
  public static readonly string IdName = "CSFD";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://www.csfd.cz/hledat/?q={query.Replace(' ', '+')}&series=0&creators=0&users=0";
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return [];

    try {
      return Parser.ParseSearch(content);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return [];
    }
  }

  public IActorSearchResult[] SearchActor(string query) => throw new System.NotImplementedException();

  public async Task<MovieDetail> GetMovieDetail(DetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.csfd.cz/film/{id.Id}";
    var content = await Common.Core.GetWebPageContent(url);
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


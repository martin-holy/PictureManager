using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Threading.Tasks;

namespace MovieManager.Plugins.CSFDcz;

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin, IMovieDetailPlugin {
  public static readonly string IdName = "CSFD";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://www.csfd.cz/hledat/?q={query.Replace(' ', '+')}&creators=0&users=0"; // TODO url
    var content = await Common.Core.GetWebPageContent(url);
    return content == null ? [] : Parser.ParseSearch(content);
  }

  public IActorSearchResult[] SearchActor(string query) => throw new System.NotImplementedException();

  public async Task<MovieDetail> GetMovieDetail(DetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.imdb.com/title/{id.Id}"; // TODO url
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return null;
    
    try {
      return Parser.ParseMovie(content);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}


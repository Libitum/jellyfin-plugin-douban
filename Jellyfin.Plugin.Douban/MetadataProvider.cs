using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;

using Jellyfin.Plugin.Douban.Configuration;

namespace Jellyfin.Plugin.Douban
{
    public class MetadataProvider : BaseProvider, IHasOrder,
        IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public String Name => "Douban Metadata Provider";
        public int Order => 3;

        public MetadataProvider(IHttpClient httpClient,
                              IJsonSerializer jsonSerializer,
                              ILogger logger): base(httpClient, jsonSerializer, logger)
        {
            // Empty
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info,
                                                             CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata name: {info.Name}");

            // Only handle it when launguage is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required launguage is not zh, " +
                    "so just bypass DoubanProvider");
                return new MetadataResult<Movie>();
            }

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Get subject id firstly
                var sidList = await SearchSidByName(info.Name, cancellationToken).ConfigureAwait(false);
                sid = sidList.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(sid))
            {
                // Not found, just return
                return new MetadataResult<Movie>();
            }

            info.SetProviderId(ProviderID, sid);
            var result = await GetMovieItem(sid, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info,
                CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: search name {0}", info.Name);

            var results = new List<RemoteSearchResult>();

            // Only handle it when launguage is "zh"
            if (info.MetadataLanguage != "zh")
            {
                _logger.LogInformation("DoubanProvider: the required launguage is not zh, " +
                    "so just bypass DoubanProvider");
                return results;
            }

            var sidList = await SearchSidByName(info.Name, cancellationToken).ConfigureAwait(false);
            foreach (String sid in sidList)
            {
                var subject = await GetSubject(sid, cancellationToken).ConfigureAwait(false);
                if (subject.Subtype != "movie")
                {
                    continue;
                }

                var searchResult = new RemoteSearchResult()
                {
                    Name = subject.Title,
                    ImageUrl = subject.Images.Large,
                    Overview = subject.Summary,
                    ProductionYear = int.Parse(subject.Year),
                };
                searchResult.SetProviderId(ProviderID, sid);
                results.Add(searchResult);
            }

            return results;
        }

        private async Task<MetadataResult<Movie>> GetMovieItem(string sid,
                                                              CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trying to get movie item by sid: {0}", sid);
            var result = new MetadataResult<Movie>();

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogWarning("Can not get movie item, sid is empty");
                return result;
            }

            var data = await GetSubject(sid, cancellationToken);
            if (data.Subtype != "movie")
            {
                // It's not movie, could be a TV series.
                _logger.LogInformation("GetMovieItem: {0} type is {1}, so just ignore it",
                                       sid, data.Subtype);
                return result;
            }

            result.Item = TransMovieInfo(data);
            TransPersonInfo(data.Directors, PersonType.Director).ForEach(result.AddPerson);
            TransPersonInfo(data.Casts, PersonType.Actor).ForEach(result.AddPerson);
            TransPersonInfo(data.Writers, PersonType.Writer).ForEach(result.AddPerson);

            result.QueriedById = true;
            result.HasMetadata = true;

            _logger.LogInformation("The name of sid {0} is {1}", sid, result.Item.Name);
            return result;
        }

        private Movie TransMovieInfo(Response.Subject data)
        {
            var movie = new Movie
            {
                Name = data.Title,
                OriginalTitle = data.Original_Title,
                CommunityRating = data.Rating.Average,
                Overview = data.Summary,
                ProductionYear = int.Parse(data.Year),
                HomePageUrl = data.Alt,
                ProductionLocations = data.Countries.ToArray()
            };

            if (!String.IsNullOrEmpty(data.Pubdate))
            {
                movie.PremiereDate = DateTime.Parse(data.Pubdate);
            }

            data.Trailer_Urls.ForEach(item => movie.AddTrailerUrl(item));
            data.Genres.ForEach(movie.AddGenre);

            return movie;
        }

        private List<PersonInfo> TransPersonInfo(List<Response.PersonInfo> persons, string role)
        {
            var result = new List<PersonInfo>();
            foreach (var person in persons)
            {
                var personInfo = new PersonInfo
                {
                    Name = person.Name,
                    Role = role,
                    ImageUrl = person.Avatars?.Large,
                };

                personInfo.SetProviderId(ProviderID, person.Id);
                result.Add(personInfo);
            }
            return result;
        }
    }
}

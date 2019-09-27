using System;
using System.Collections.Generic;
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
    public class MetadataProvider : BaseProvider, IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        public String Name => "Douban Metadata Provider";
        public int Order => 3;

        public MetadataProvider(IHttpClient httpClient,
                              IJsonSerializer jsonSerializer,
                              ILogger logger): base(httpClient, jsonSerializer, logger)
        {
            // Empty
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Douban:GetMetadata name: {info.Name}");

            var sid = info.GetProviderId(ProviderID);
            if (string.IsNullOrWhiteSpace(sid))
            {
                // Get subject id firstly
                sid = await SearchSidByName(info.Name, cancellationToken).ConfigureAwait(false);
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

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info,
                CancellationToken cancellationToken)
        {
            _logger.LogInformation("Douban: search name {0}", info.Name);
            throw new NotImplementedException("Douban:GetSearchResults");
        }

        public async Task<MetadataResult<Movie>> GetMovieItem(string sid, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trying to get movie item by sid: {0}", sid);
            var result = new MetadataResult<Movie>();

            if (string.IsNullOrWhiteSpace(sid))
            {
                _logger.LogWarning("Can not get movie item, sid is empty");
                return result;
            }

            var data = await GetMovieSubject(sid, cancellationToken);
            if (data.Subtype != "movie")
            {
                // It's not movie, could be a TV series.
                return result;
            }

            result.Item = TransMovieInfo(data);
            TransPersonInfo(data.Directors, PersonType.Director).ForEach(item => result.AddPerson(item));
            TransPersonInfo(data.Casts, PersonType.Actor).ForEach(item => result.AddPerson(item));
            TransPersonInfo(data.Writers, PersonType.Writer).ForEach(item => result.AddPerson(item));

            result.QueriedById = true;
            result.HasMetadata = true;

            _logger.LogInformation("The name of sid {0} is {1}", sid, result.Item.Name);
            return result;
        }

        private Movie TransMovieInfo(Response.Subject data)
        {
            var movie = new Movie();
            movie.Name = data.Title;
            movie.OriginalTitle = data.Original_Title;
            movie.CommunityRating = data.Rating.Average;
            movie.Overview = data.Summary;
            movie.ProductionYear = int.Parse(data.Year);
            movie.PremiereDate = DateTime.Parse(data.Pubdate);
            movie.HomePageUrl = data.Alt;
            movie.ProductionLocations = data.Countries.ToArray();

            data.Trailer_Urls.ForEach(item => movie.AddTrailerUrl(item));
            data.Genres.ForEach(item => movie.AddGenre(item));

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
                    ImageUrl = person.Avatars.Large,
                };

                personInfo.SetProviderId(ProviderID, person.Id);
                result.Add(personInfo);
            }
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Jellyfin.Plugin.Douban.Response;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Douban
{
    /// <summary>
    /// Frodo is the secondary domain of API used by Douban APP.
    /// </summary>
    public class FrodoUtils
    {
        /// <summary>
        /// Douban frodo API base URL.
        /// </summary>
        public const string BaseDoubanUrl = "https://frodo.douban.com";

        /// <summary>
        /// Provider ID
        /// </summary>
        public const string ProviderId = "DoubanID";

        /// <summary>
        /// Douban frodo search API path.
        /// </summary>
        public const string SearchApi = "/api/v2/search/movie";

        /// <summary>
        /// Douban frodo subject item API path 
        /// </summary>
        public const string ItemApi = "/api/v2";

        /// <summary>
        /// API key to use when performing an API call.
        /// </summary>
        public const string ApiKey = "0dad551ec0f84ed02907ff5c42e8ec70";

        /// <summary>
        /// Secret key for HMACSHA1 to generate signature.
        /// </summary>
        private const string SecretKey = "bf7dddc7c9cfe6f7";

        /// <summary>
        /// User agent
        /// </summary>
        public const string UserAgent = "api-client/1 com.douban.frodo/6.42.2(194) Android/22 product/shamu vendor/OPPO model/OPPO R11 Plus  rom/android  network/wifi  platform/mobile nd/1";

        /// <summary>
        /// Maximum number of search count.
        /// </summary>
        public const int MaxSearchCount = 20;

        /// <summary>
        /// Generates timestamp for douban api
        /// </summary>
        /// <returns>Timestamp.</returns>
        public static string GetTsParam()
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000;
            return $"{ts}";
        }

        /// <summary>
        /// Generates signature for douban api
        /// </summary>
        /// <param name="api">Douban api path, e.g. /api/v2/search/movie</param>
        /// <param name="ts">Timestamp.</param>
        /// <returns>Douban signature</returns>
        public static string GetSigParam(string api, string ts)
        {
            using var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(SecretKey));
            hmacsha1.Initialize();
            byte[] data = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes($"GET&{UrlEncodeInUpperCase(api)}&{ts}"));

            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Encodes url in uppercase.
        /// </summary>
        public static string UrlEncodeInUpperCase(string input)
        {
            string lower = HttpUtility.UrlEncode(input);
            Regex reg = new Regex(@"%[a-f0-9]{2}");
            string upper = reg.Replace(lower, m => m.Value.ToUpperInvariant());
            return upper;
        }

        /// <summary>
        ///  Formats query parameters to query string.
        /// </summary>
        public static string FormatQueryString(Dictionary<string, string> queryParams)
        {
            List<string> temp = new List<string>();
            foreach (KeyValuePair<string, string> entry in queryParams)
            {
                temp.Add($"{UrlEncodeInUpperCase(entry.Key)}={UrlEncodeInUpperCase(entry.Value)}");
            }
            return "?" + String.Join("&", temp);
        }

        public static T MapSubjectToItem<T>(Subject data)
        where T : BaseItem, new()
        {
            var item = new T
            {
                Name = data.Title ?? data.Original_Title,
                OriginalTitle = data.Original_Title,
                CommunityRating = data.Rating?.Value,
                Overview = data.Intro,
                ProductionYear = int.Parse(data.Year),
                HomePageUrl = data.Url,
                ProductionLocations = data.Countries?.ToArray()
            };

            if (data.Pubdate?.Count > 0 && !String.IsNullOrEmpty(data.Pubdate[0]))
            {
                string pubdate;
                if (data.Pubdate[0].IndexOf("(") != -1)
                {
                    pubdate = data.Pubdate[0].Substring(0, data.Pubdate[0].IndexOf("("));
                }
                else
                {
                    pubdate = data.Pubdate[0];
                }
                DateTime dateValue;
                if (DateTime.TryParse(pubdate, out dateValue))
                {
                    item.PremiereDate = dateValue;
                }
            }

            if (data.Trailer != null)
            {
                item.AddTrailerUrl(data.Trailer.Video_Url);
            }

            data.Genres.ForEach(item.AddGenre);

            return item;
        }

        public static List<PersonInfo> MapCrewToPersons(
            List<Crew> crewList, string personType)
        {
            var result = new List<PersonInfo>();
            foreach (var crew in crewList)
            {
                var personInfo = new PersonInfo
                {
                    Name = crew.Name,
                    Type = personType,
                    ImageUrl = crew.Avatar?.Large ?? "",
                    Role = crew.Roles.Count > 0 ? crew.Roles[0] : ""
                };

                personInfo.SetProviderId(ProviderId, crew.Id);
                result.Add(personInfo);
            }
            return result;
        }

        public static SearchTarget MapSubjectToSearchTarget(Subject subject)
        {
            return new SearchTarget
            {
                Id = subject?.Id,
                Cover_Url = subject?.Pic?.Normal,
                Year = subject?.Year,
                Title = subject?.Title
            };
        }
    }
}

using System.Collections.Generic;

namespace Jellyfin.Plugin.Douban.Response
{
    internal class Subject
    {
        public string Title {get; set;}
        public string Original_Title {get; set;}
        public string Summary {get; set;}
        public string Year {get; set;}
        public string Pubdate {get; set;}
        public Rating Rating {get; set;}
        public Avatar Images {get; set;}
        public string Alt {get; set;}
        public List<string> Countries {get; set;}
        public List<string> Trailer_Urls {get; set;}
        public List<PersonInfo> Directors {get; set;}
        public List<PersonInfo> Writers {get; set;}
        public List<PersonInfo> Casts {get; set;}
        public List<string> Genres {get; set;}
        public string Subtype {get; set;}
        // season information
        public int? Seasons_Count {get; set;}
        public int? Current_Season {get; set;}
        public int? Episodes_Count {get; set;}
    }

    internal class Rating
    {
        public float Average {get; set;}
    }

    internal class PersonInfo 
    {
        public string Name {get; set;}
        public string Alt {get; set;}
        public string Id {get; set;}
        public Avatar Avatars {get; set;}
    }

    internal class Avatar
    {
        public string Large {get; set;}
    }
}
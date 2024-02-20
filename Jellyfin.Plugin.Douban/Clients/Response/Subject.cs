using System.Collections.Generic;

namespace Jellyfin.Plugin.Douban.Response
{
    public class Subject
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Original_Title { get; set; }
        public string Intro { get; set; }
        public string Summary { get; set; }
        public string Year { get; set; }
        public List<string> Pubdate { get; set; }
        public Rating Rating { get; set; }
        public Image Pic { get; set; }
        public string Url { get; set; }
        public List<string> Countries { get; set; }
        public Trailer Trailer { get; set; }
        public List<Crew> Directors { get; set; }
        public List<Crew> Actors { get; set; }
        public List<string> Genres { get; set; }
        public string Subtype { get; set; }
        public bool Is_Tv { get; set; }
    }

    public class Rating
    {
        public float Value { get; set; }
        public float Star_Count { get; set; }
    }

    public class Crew
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Id { get; set; }
        public Image Avatar { get; set; }
        public List<string> Roles { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Type { get; set; }
    }

    public class Image
    {
        public string Large { get; set; }
        public string Normal { get; set; }
    }

    public class Trailer
    {
        public string Video_Url { get; set; }
        public string Title { get; set; }
        public string Subject_Title { get; set; }
        public string Runtime { get; set; }
        public string Cover_Url { get; set; }
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Douban.Response
{
    public class SearchResult {
        public SubjectList Subjects {get; set;}

    }
    public class SubjectList {
        public string Target_Name {get; set;}
        public List<SearchSubject> Items { get; set; }
    }

    public class SearchSubject
    {
        public SearchTarget Target { get; set; }
        public string Target_Type { get; set; }
    }

    public class SearchTarget
    {
        public string Id { get; set; }
        public string Cover_Url { get; set; }
        public string Year { get; set; }
        public string Title { get; set; }
    }
}
using System;
using System.IO;
using System.Text.Json;
using Jellyfin.Plugin.Douban.Response;
using Xunit;

public class JsonTest {
    [Fact]
    public async void TestJson() {
        FileStream stream = File.OpenRead("/home/lei/test.json");
        JsonSerializerOptions option = new(JsonSerializerDefaults.Web);
        var result = await JsonSerializer.DeserializeAsync<SearchResult>(stream, option);
        Console.WriteLine(result.Subjects.Target_Name);
        Assert.True(1 == 1);
    }
}
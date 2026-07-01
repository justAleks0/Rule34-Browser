using System.Net.Http;
using Rule34Gallery.Core.Services;

var http = new HttpClient();
var results = await WebSearchClient.SearchAsync(http, "honkai star rail girl character that uses a bow", default);
Console.WriteLine($"Count: {results.Count}");
foreach (var r in results.Take(3)) Console.WriteLine($"- {r.Title}");

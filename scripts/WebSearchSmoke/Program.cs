using Rule34Gallery.Core.Services;

var query = args.Length > 0 ? string.Join(' ', args) : "honkai star rail female bow user";
var results = await WebSearchClient.SearchComprehensiveAsync(query);
Console.WriteLine($"Query: {query}");
Console.WriteLine($"Results: {results.Count}");
foreach (var r in results.Take(8))
{
    Console.WriteLine($"  - {r.Title}");
    if (!string.IsNullOrWhiteSpace(r.Snippet))
    {
        Console.WriteLine($"    {r.Snippet[..Math.Min(90, r.Snippet.Length)]}");
    }
}

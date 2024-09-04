using System.Text.Json;
using System.Text.RegularExpressions;

namespace UrlFromJsonExtractor
{
	internal class Program
	{
		private static readonly Regex UrlRegex = new Regex(
			@"\b(?:[a-zA-Z][a-zA-Z\d+\-.]*):\/\/[^\s/$.?#].[^\s]*\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);


		static async Task Main(string[] args)
		{
			var inFilePath = args[0];
			if (string.IsNullOrWhiteSpace(inFilePath))
			{
				Console.WriteLine(
					"Please provide a source file name as a first argument.");
			}

			if (!File.Exists(inFilePath))
			{
				Console.WriteLine($"File {inFilePath} not found.");
			}

			string inData = string.Empty;
			try
			{
				inData = await File.ReadAllTextAsync(inFilePath);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error reading file: {ex.Message}");
			}


			// Parse JSON
			using var jsonDoc = JsonDocument.Parse(inData);

			// Extract URLs
			var urls = ExtractUrls(jsonDoc.RootElement);

			Console.WriteLine($"Found total {urls.Count} links.");

			var unique = urls.Distinct();//.Select(x => new Uri(x));
			Console.WriteLine();
			Console.WriteLine($"Unique links: {unique.Count()}");

			var uniqueRoots = unique
				.Where(x => x.Scheme == "http" || x.Scheme == "https")
				.Select(x => GetRoot(x))
				.Distinct();

			// Print URLs
			Console.WriteLine($"Unique by root: {uniqueRoots.Count()}");
			foreach (var url in uniqueRoots)
			{
				Console.WriteLine(url);
			}

			var nonHttpUrls = unique
				.Where(x => x.Scheme != "http" && x.Scheme != "https")
				.OrderBy(x => x.ToString());

			// Print URLs
			Console.WriteLine();
			Console.WriteLine($"Non-HTTP URLs: {nonHttpUrls.Count()}");
			foreach (var url in nonHttpUrls)
			{
				Console.WriteLine(url);
			}
		}


		private static List<Uri> ExtractUrls(JsonElement element)
		{
			var urls = new List<Uri>();

			switch (element.ValueKind)
			{
				case JsonValueKind.Object:
					foreach (var property in element.EnumerateObject())
					{
						urls.AddRange(ExtractUrls(property.Value));
					}
					break;
				case JsonValueKind.Array:
					foreach (var item in element.EnumerateArray())
					{
						urls.AddRange(ExtractUrls(item));
					}
					break;
				case JsonValueKind.String:
					var str = element.GetString();
					if (Uri.TryCreate(str, UriKind.Absolute, out Uri? uri))
					{
						urls.Add(uri);
					}
					//////var matches = UrlRegex.Matches(str);
					//////foreach (Match match in matches)
					//////{
					//////	urls.Add(match.Value);
					//////}
					break;
			}

			return urls;
		}


		private static Uri GetRoot(Uri uri)
		{
			// Extract the base URI components
			var baseUri = $"{uri.Scheme}://{uri.Host}";

			// Include port if it's not the default for the scheme
			if (uri.Port != -1 &&
				((uri.Scheme == "http" && uri.Port != 80) ||
				 (uri.Scheme == "https" && uri.Port != 443)))
			{
				baseUri += $":{uri.Port}";
			}

			return new Uri(baseUri);
		}
	}
}

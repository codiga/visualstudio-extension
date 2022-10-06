using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQLClient
{
	public class CodigaClient
	{
		private readonly GraphQLHttpClient _client;

		private static string ShortcutQuery =>
			"query GetRecipesForClientByShortcut($fingerprint: String, $filename: String, $term: String, $dependencies: [String!]!, $parameters: String, $language: LanguageEnumeration!, $onlyPublic: Boolean, $onlyPrivate: Boolean, $onlySubscribed: Boolean){\r\n  getRecipesForClientByShortcut(fingerprint: $fingerprint, term: $term, filename: $filename, dependencies:$dependencies, parameters:$parameters, language:$language, onlyPublic: $onlyPublic, onlyPrivate: $onlyPrivate, onlySubscribed: $onlySubscribed){\r\n    id\r\n    name\r\n    code\r\n    keywords\r\n    imports\r\n    language\r\n    description\r\n    shortcut\r\n  }\r\n}";

		private static string ShortcutLastTimestampQuery =>
			"query GetRecipesForClientByShortcutLastTimestamp($fingerprint: String, $dependencies: [String!]!, $language: LanguageEnumeration!){\r\n  getRecipesForClientByShortcutLastTimestamp(fingerprint: $fingerprint, dependencies:$dependencies, language:$language)\r\n}";

		public CodigaClient()
		{
			_client = new GraphQLHttpClient("https://api.codiga.io/graphql", new SystemTextJsonSerializer());
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>> GetRecipesForClientByShortcutAsync(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["filename"] = "";
			variablesDict["term"] = "";
			variablesDict["dependencies"] = "";
			variablesDict["parameters"] = "";
			variablesDict["language"] = language;
			variablesDict["onlyPublic"] = true;
			variablesDict["onlyPrivate"] = null;
			variablesDict["onlySubscribed"] = false;


			var request = new GraphQLHttpRequest(ShortcutQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesByShortcutResult>(request);

			return result.Data.GetRecipesForClientByShortcut;
		}

		public async Task<long> GetRecipesForClientByShortcutLastTimestamp(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["dependencies"] = "";
			variablesDict["language"] = language;

			var request = new GraphQLHttpRequest(ShortcutLastTimestampQuery, variables);

			var result = await _client.SendQueryAsync<GetRecipesByShortcutLastTimestampResult>(request);

			return result.Data.GetRecipesForClientByShortcutLastTimestamp;
		}
	}

	internal class GetRecipesByShortcutResult
	{
		public IReadOnlyCollection<CodigaSnippet> GetRecipesForClientByShortcut { get; set; }
	}

	internal class GetRecipesByShortcutLastTimestampResult
	{
		public long GetRecipesForClientByShortcutLastTimestamp { get; set; }
	}

	/// <summary>
	/// Represents the structure of a Codiga Recipe/Snippet
	/// </summary>
	public class CodigaSnippet
	{
		/// <summary>
		/// identifier
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// name
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// The Base64 encoded code string
		/// </summary>
		public string? Code { get; set; }

		/// <summary>
		/// keywords
		/// </summary>
		public IReadOnlyList<string> Keywords { get; set; }

		/// <summary>
		/// imports to add when adding the recipe
		/// </summary>
		public IReadOnlyList<string> Imports { get; set; }

		/// <summary>
		/// language of the recipe
		/// </summary>
		public string? Language { get; set; }

		/// <summary>
		/// description
		/// </summary>
		public string? Description { get; set; }

		/// <summary>
		/// shortcut
		/// </summary>
		public string? Shortcut { get; set; }
	}
}
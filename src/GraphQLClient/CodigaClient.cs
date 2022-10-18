using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GraphQLClient
{
	public class CodigaClient
	{
		private readonly GraphQLHttpClient _client;
		private const string CodigaEndpoint = "https://api.codiga.io/graphql";
		private const string AuthHeaderScheme = "X-Api-Token";

		public CodigaClient()
		{
			_client = new GraphQLHttpClient(CodigaEndpoint, new SystemTextJsonSerializer());
		}

		public CodigaClient(string apiToken)
		{
			_client = new GraphQLHttpClient(CodigaEndpoint, new SystemTextJsonSerializer());

			if(!string.IsNullOrEmpty(apiToken))
				_client.HttpClient.DefaultRequestHeaders.Add(AuthHeaderScheme, apiToken);
		}

		public void SetApiToken(string apiToken)
		{
			_client.HttpClient.DefaultRequestHeaders.Remove(AuthHeaderScheme);
			if (!string.IsNullOrEmpty(apiToken))
				_client.HttpClient.DefaultRequestHeaders.Add(AuthHeaderScheme, apiToken);
		}

		public async Task<User> GetUserAsync()
		{
			var request = new GraphQLHttpRequest(QueryProvider.GetUserQuery);
			var result = await _client.SendQueryAsync<GetUserResult>(request);

			return result.Data.User;
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["filename"] = "";
			variablesDict["term"] = "";
			variablesDict["dependencies"] = "";
			variablesDict["parameters"] = "";
			variablesDict["language"] = language;
			variablesDict["onlyPublic"] = null;
			variablesDict["onlyPrivate"] = null;
			variablesDict["onlySubscribed"] = false;

			var request = new GraphQLHttpRequest(QueryProvider.ShortcutQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesByShortcutResult>(request);

			return result.Data.GetRecipesForClientByShortcut;
		}

		public async Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["dependencies"] = "";
			variablesDict["language"] = language;

			var request = new GraphQLHttpRequest(QueryProvider.ShortcutLastTimestampQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesByShortcutLastTimestampResult>(request);
			return result.Data.GetRecipesForClientByShortcutLastTimestamp;
		}

		public async Task<string> RecordRecipeUseAsync(long recipeId)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["recipeId"] = recipeId;

			var request = new GraphQLHttpRequest(QueryProvider.RecordRecipeUseMutation, variables);
			var result = await _client.SendMutationAsync<RecordRecipeUseMutationResult>(request);
			
			return result.Data.RecordAccess;
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords, IReadOnlyCollection<string> languages, bool onlyPublic, int howMany, int skip)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = "5fff6cfc-bfd2-45db-9cd1-d9821ec9628c";
			variablesDict["filename"] = "";
			variablesDict["term"] = keywords;
			variablesDict["dependencies"] = "";
			variablesDict["parameters"] = "";
			variablesDict["languages"] = languages;
			variablesDict["onlyPublic"] = onlyPublic;
			variablesDict["onlyPrivate"] = null;
			variablesDict["onlySubscribed"] = false;
			variablesDict["howmany"] = howMany;
			variablesDict["skip"] = skip;

			var request = new GraphQLHttpRequest(QueryProvider.SemanticQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesSemanticResult>(request);
			return result.Data.AssistantRecipesSemanticSearch;
		}
	}

	internal class GetUserResult
	{
		public User User { get; set; }
	}

	internal class GetRecipesByShortcutResult
	{
		public IReadOnlyCollection<CodigaSnippet>? GetRecipesForClientByShortcut { get; set; }
	}

	internal class GetRecipesSemanticResult
	{
		public IReadOnlyCollection<CodigaSnippet>? AssistantRecipesSemanticSearch { get; set; }
	}

	internal class GetRecipesByShortcutLastTimestampResult
	{
		public long GetRecipesForClientByShortcutLastTimestamp { get; set; }
	}

	internal class RecordRecipeUseMutationResult
	{
		public string? RecordAccess { get; set; }
	}

	public class User
	{
		public string UserName { get; set; }
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
		public IReadOnlyList<string>? Keywords { get; set; }

		/// <summary>
		/// imports to add when adding the recipe
		/// </summary>
		public IReadOnlyList<string>? Imports { get; set; }

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
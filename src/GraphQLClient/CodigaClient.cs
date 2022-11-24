using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQLClient.Model.Rosie;

namespace GraphQLClient
{
	/// <summary>
	/// GraphQL client to query the Codiga API.
	/// </summary>
	public interface ICodigaClient : IDisposable
	{
		public string Fingerprint { get; }

		public void SetApiToken(string apiToken);

		/// <summary>
		/// Get the logged user identified by the configured API token.
		/// </summary>
		/// <returns></returns>
		public Task<GraphQLResponse<GetUserResult>> GetUserAsync();

		/// <summary>
		/// Get shortcut snippets for the specified language
		/// </summary>
		/// <param name="language"></param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns></returns>
		public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language);

		/// <summary>
		/// Get the timestamp of the latest snippet for the specified language.
		/// </summary>
		/// <param name="language"></param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns></returns>
		public Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language);

		/// <summary>
		/// Reports usage of the specified recipe to the Codiga service.
		/// </summary>
		/// <param name="recipeId"></param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns></returns>
		public Task<string> RecordRecipeUseAsync(long recipeId);

		/// <summary>
		/// Get recipes by matching the keywords semantically.
		/// </summary>
		/// <param name="keywords"></param>
		/// <param name="languages"></param>
		/// <param name="onlyPublic"></param>
		/// <param name="howMany"></param>
		/// <param name="skip"></param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns></returns>
		public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords, IReadOnlyCollection<string> languages, bool onlyPublic, int howMany, int skip);

		/// <summary>
		/// Get recipes by matching the keywords semantically.
		/// </summary>
		/// <param name="keywords"></param>
		/// <param name="languages"></param>
		/// <param name="onlyPublic"></param>
		/// <param name="onlyPrivate"></param>
		/// <param name="onlySubscribed"></param>
		/// <param name="howMany"></param>
		/// <param name="skip"></param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns></returns>
		public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords, IReadOnlyCollection<string> languages, bool onlyPublic, bool onlyPrivate, bool onlySubscribed, int howMany, int skip);
		
		/// <summary>
		/// Get ruleset information based on the ruleset names used in Codiga config file (codiga.yml).
		/// </summary>
		/// <param name="names">The ruleset names to send</param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns>The collection of rulesets.</returns>
		public Task<IReadOnlyCollection<RuleSetsForClient>?> GetRulesetsForClientAsync(IReadOnlyCollection<string> names);

		/// <summary>
		/// Get the most recent timestamp of the given rulesets in the Codiga config file (codiga.yml)
		/// </summary>
		/// <param name="names">The ruleset names to send</param>
		/// <exception cref="CodigaAPIException"></exception>
		/// <returns>The timestamp when the sent rulesets were last updated.</returns>
		public Task<long> GetRulesetsLastUpdatedTimestampAsync(IReadOnlyCollection<string> names);
	}

	public class CodigaClient : ICodigaClient, IDisposable
	{
		private GraphQLHttpClient _client;
		private const string CodigaEndpoint = "https://api.codiga.io/graphql";
		private const string AuthHeaderScheme = "X-Api-Token";

		public string Fingerprint { get; }

		public CodigaClient(string fingerprint)
		{
			_client = new GraphQLHttpClient(CodigaEndpoint, new SystemTextJsonSerializer());
			Fingerprint = fingerprint;
		}

		public CodigaClient(string apiToken, string fingerprint)
		{
			Fingerprint = fingerprint;
			_client = new GraphQLHttpClient(CodigaEndpoint, new SystemTextJsonSerializer());

			if (!string.IsNullOrEmpty(apiToken))
				_client.HttpClient.DefaultRequestHeaders.Add(AuthHeaderScheme, apiToken);
		}

		public void SetApiToken(string apiToken)
		{
			_client.HttpClient.DefaultRequestHeaders.Remove(AuthHeaderScheme);
			if (!string.IsNullOrEmpty(apiToken))
				_client.HttpClient.DefaultRequestHeaders.Add(AuthHeaderScheme, apiToken);
		}

		public async Task<GraphQLResponse<GetUserResult>> GetUserAsync()
		{
			var request = new GraphQLHttpRequest(QueryProvider.GetUserQuery);
			var result = await _client.SendQueryAsync<GetUserResult>(request);

			return result;
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
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

			ThrowOnErrors(result);

			return result.Data.GetRecipesForClientByShortcut;
		}

		public async Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
			variablesDict["dependencies"] = "";
			variablesDict["language"] = language;

			var request = new GraphQLHttpRequest(QueryProvider.ShortcutLastTimestampQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesByShortcutLastTimestampResult>(request);

			ThrowOnErrors(result);

			return result.Data.GetRecipesForClientByShortcutLastTimestamp;
		}

		public async Task<string> RecordRecipeUseAsync(long recipeId)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
			variablesDict["recipeId"] = recipeId;

			var request = new GraphQLHttpRequest(QueryProvider.RecordRecipeUseMutation, variables);
			var result = await _client.SendMutationAsync<RecordRecipeUseMutationResult>(request);

			ThrowOnErrors(result);
			
			return result.Data.RecordAccess;
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords, IReadOnlyCollection<string> languages, bool onlyPublic, int howMany, int skip)
		{
			return await GetRecipesForClientSemanticAsync(keywords, languages, onlyPublic, false, false, howMany, skip);
		}

		public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords, IReadOnlyCollection<string> languages, bool onlyPublic, bool onlyPrivate, bool onlySubscribed, int howMany, int skip)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
			variablesDict["filename"] = "";
			variablesDict["term"] = keywords;
			variablesDict["dependencies"] = "";
			variablesDict["parameters"] = "";
			variablesDict["languages"] = languages;
			if(onlyPublic)
				variablesDict["onlyPublic"] = onlyPublic;
			if(onlyPrivate)
				variablesDict["onlyPrivate"] = onlyPrivate;
			if(onlySubscribed)
				variablesDict["onlySubscribed"] = onlySubscribed;
			variablesDict["howmany"] = howMany;
			variablesDict["skip"] = skip;

			var request = new GraphQLHttpRequest(QueryProvider.SemanticQuery, variables);
			var result = await _client.SendQueryAsync<GetRecipesSemanticResult>(request);

			ThrowOnErrors(result);

			return result.Data.AssistantRecipesSemanticSearch;
		}
		
		public async Task<IReadOnlyCollection<RuleSetsForClient>?> GetRulesetsForClientAsync(IReadOnlyCollection<string> names)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
			variablesDict["names"] = names;

			var request = new GraphQLHttpRequest(QueryProvider.GetRulesetsForClientQuery, variables);
			var result = await _client.SendQueryAsync<GetRulesetsForClientResult>(request);

			ThrowOnErrors(result);

			return result.Data.RuleSetsForClient;
		}

		public async Task<long> GetRulesetsLastUpdatedTimestampAsync(IReadOnlyCollection<string> names)
		{
			dynamic variables = new System.Dynamic.ExpandoObject();
			var variablesDict = (IDictionary<string, object?>)variables;
			variablesDict["fingerprint"] = Fingerprint;
			variablesDict["names"] = names;

			var request = new GraphQLHttpRequest(QueryProvider.GetRulesetsLastUpdatedTimestampQuery, variables);
			var result = await _client.SendQueryAsync<GetRulesetsLastUpdatedTimestampResult>(request);

			ThrowOnErrors(result);

			return result.Data.RuleSetsLastUpdatedTimestamp;
		}

		public void Dispose()
		{
			_client?.Dispose();
		}

		public static string GetReadableErrorMessage(string errorMessage)
		{
			return errorMessage switch
			{
				"user-not-logged" => "User is not logged or the token is invalid.",
				_ => errorMessage,
			};
		}
		private void ThrowOnErrors(IGraphQLResponse response)
		{
			if (response.Errors == null || !response.Errors.Any() || response.Data != null)
				return;

			var error = response.Errors.First();
			var message = $"{error.Message} from {error.Path} at {error.Locations}";
			throw new CodigaAPIException(message);
		}
	}

	public class CodigaAPIException : Exception
	{
		public CodigaAPIException()
		{
		}

		public CodigaAPIException(string message) : base(message)
		{
		}
	}

	public class GetUserResult
	{
		public User? User { get; set; }
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
	
	internal class GetRulesetsForClientResult
	{
		public IReadOnlyCollection<RuleSetsForClient>? RuleSetsForClient { get; set; }
	}

	internal class GetRulesetsLastUpdatedTimestampResult
	{
		public long RuleSetsLastUpdatedTimestamp { get; set; }
	}

	internal class RecordRecipeUseMutationResult
	{
		public string? RecordAccess { get; set; }
	}

	public class User
	{
		public string? UserName { get; set; }
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

		public bool? IsPublic { get; set; }
		public bool? IsSubscribed { get; set; }

		public Owner? Owner { get; set; }
	}

	public class Owner
	{
		public long? Id { get; set; }

		public string? DisplayName { get; set; }
	}
}

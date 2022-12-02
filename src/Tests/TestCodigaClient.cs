using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQLClient;
using GraphQLClient.Model.Rosie;
using Tests.Rosie;

namespace Tests
{
    /// <summary>
    /// Codiga client implementation for testing.
    /// </summary>
    public class TestCodigaClient : ICodigaClient
    {
        public string Fingerprint { get; }

        public void SetApiToken(string apiToken)
        {
        }

        public Task<GraphQLResponse<GetUserResult>> GetUserAsync()
        {
            return Task.FromResult<GraphQLResponse<GetUserResult>>(null);
        }

        public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language)
        {
            return Task.FromResult<IReadOnlyCollection<CodigaSnippet>?>(null);
        }

        public Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language)
        {
            return Task.FromResult(-1L);
        }

        public Task<string> RecordRecipeUseAsync(long recipeId)
        {
            return Task.FromResult<string>(null);
        }

        public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, int howMany,
            int skip)
        {
            return Task.FromResult<IReadOnlyCollection<CodigaSnippet>?>(null);
        }

        public Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, bool onlyPrivate,
            bool onlySubscribed, int howMany, int skip)
        {
            return Task.FromResult<IReadOnlyCollection<CodigaSnippet>?>(null);
        }

        public Task<IReadOnlyCollection<RuleSetsForClient>?> GetRulesetsForClientAsync(
            IReadOnlyCollection<string> names)
        {
            return Task.FromResult(RulesetsForClientTestSupport.GetRulesetsForClient(names));
        }

        public Task<long> GetRulesetsLastUpdatedTimestampAsync(IReadOnlyCollection<string> names)
        {
            return Task.FromResult(RulesetsForClientTestSupport.GetRulesetsLastTimestamp(names));
        }
        
        public void Dispose()
        {
        }
    }
}
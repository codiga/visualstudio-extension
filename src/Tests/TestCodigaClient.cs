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

        public async Task<GraphQLResponse<GetUserResult>> GetUserAsync()
        {
            return null;
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language)
        {
            return null;
        }

        public async Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language)
        {
            return -1L;
        }

        public async Task<string> RecordRecipeUseAsync(long recipeId)
        {
            return null;
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, int howMany,
            int skip)
        {
            return null;
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, bool onlyPrivate,
            bool onlySubscribed, int howMany, int skip)
        {
            return null;
        }

        public async Task<IReadOnlyCollection<RuleSetsForClient>?> GetRulesetsForClientAsync(
            IReadOnlyCollection<string> names)
        {
            return RulesetsForClientTestSupport.GetRulesetsForClient(names);
        }

        public async Task<long> GetRulesetsLastUpdatedTimestampAsync(IReadOnlyCollection<string> names)
        {
            return RulesetsForClientTestSupport.GetRulesetsLastTimestamp(names);
        }
        
        public void Dispose()
        {
        }
    }
}
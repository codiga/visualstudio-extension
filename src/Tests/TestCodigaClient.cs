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
            return await Task.FromResult(new GraphQLResponse<GetUserResult>());
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientByShortcutAsync(string language)
        {
            return await Task.FromResult<>(null);
        }

        public async Task<long> GetRecipesForClientByShortcutLastTimestampAsync(string language)
        {
            return await Task.FromResult(-1L);
        }

        public async Task<string> RecordRecipeUseAsync(long recipeId)
        {
            return await Task.FromResult<string>("");
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, int howMany,
            int skip)
        {
            return await Task.FromResult<>(null);
        }

        public async Task<IReadOnlyCollection<CodigaSnippet>?> GetRecipesForClientSemanticAsync(string keywords,
            IReadOnlyCollection<string> languages, bool onlyPublic, bool onlyPrivate,
            bool onlySubscribed, int howMany, int skip)
        {
            return await Task.FromResult<>(null);
        }

        public async Task<IReadOnlyCollection<RuleSetsForClient>?> GetRulesetsForClientAsync(
            IReadOnlyCollection<string> names)
        {
            return await Task.FromResult(RulesetsForClientTestSupport.GetRulesetsForClient(names));
        }

        public async Task<long> GetRulesetsLastUpdatedTimestampAsync(IReadOnlyCollection<string> names)
        {
            return await Task.FromResult(RulesetsForClientTestSupport.GetRulesetsLastTimestamp(names));
        }

        public void Dispose()
        {
        }
    }
}
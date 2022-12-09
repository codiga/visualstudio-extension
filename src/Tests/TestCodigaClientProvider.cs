using Extension.Caching;
using GraphQLClient;

namespace Tests
{
    /// <summary>
    /// Codiga client provider implementation for testing.
    /// </summary>
    public class TestCodigaClientProvider : ICodigaClientProvider
    {
        public bool TryGetClient(out ICodigaClient client)
        {
            client = GetClient();
            return true;
        }

        public ICodigaClient GetClient()
        {
            return new TestCodigaClient();
        }
    }
}
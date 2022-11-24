using GraphQLClient;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	internal class QueryProviderTest
	{
		[Test]
		public void QueryProvider_should_load_query_strings()
		{
			// act
			var lastTimeStampQuery = QueryProvider.ShortcutLastTimestampQuery;
			var shortcutQuery = QueryProvider.ShortcutQuery;
			var recordAccessMutation = QueryProvider.RecordRecipeUseMutation;
			var semanticQuery = QueryProvider.SemanticQuery;
			var userQuery = QueryProvider.GetUserQuery;
			var rulesetsQuery = QueryProvider.GetRulesetsForClientQuery;
			var rulesetsTimestampQuery = QueryProvider.GetRulesetsLastUpdatedTimestampQuery;

			// assert
			Assert.NotNull(lastTimeStampQuery);
			Assert.IsNotEmpty(lastTimeStampQuery);

			Assert.NotNull(shortcutQuery);
			Assert.IsNotEmpty(shortcutQuery);

			Assert.NotNull(recordAccessMutation);
			Assert.IsNotEmpty(recordAccessMutation);

			Assert.NotNull(semanticQuery);
			Assert.IsNotEmpty(semanticQuery);

			Assert.NotNull(userQuery);
			Assert.IsNotEmpty(userQuery);
			
			Assert.NotNull(rulesetsQuery);
			Assert.IsNotEmpty(rulesetsQuery);
			
			Assert.NotNull(rulesetsTimestampQuery);
			Assert.IsNotEmpty(rulesetsTimestampQuery);
		}
	}
}

﻿using GraphQLClient;

namespace Tests
{
	[TestFixture]
	[Explicit("External API call")]
	internal class GraphQLTest
	{

		[Test]
		public async Task GetRecipesForClientByShortcutAsync_should_return_all_snippets()
		{
			// arrange
			var client = new CodigaClient();

			// act
			var snippets = await client.GetRecipesForClientByShortcutAsync("Csharp");

			// assert
			Assert.NotNull(snippets);
		}

		[Test]
		public async Task GetRecipesForClientByShortcutLastTimestamp_should_return_last_timestamp()
		{
			// arrange
			var client = new CodigaClient();

			// act
			var ts = await client.GetRecipesForClientByShortcutLastTimestampAsync("Csharp");

			// assert
			Assert.NotNull(ts);
		}

		[Test]
		public async Task RecordRecipeAccess_should_record_access()
		{
			// arrange
			var client = new CodigaClient();
			var snippets = await client.GetRecipesForClientByShortcutAsync("Csharp");

			// act
			await client.RecordRecipeAccessAsync(snippets.First().Id, "");

			// assert
		}
	}
}
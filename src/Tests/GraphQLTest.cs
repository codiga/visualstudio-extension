using GraphQLClient;
using NUnit.Framework;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Extension.SnippetFormats.CodigaLanguages;

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
			Assert.IsNotEmpty(snippets);
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
		public async Task RecordRecipeUse_should_record_access()
		{
			// arrange
			var client = new CodigaClient();

			// act
			var result = await client.RecordRecipeUseAsync(9260);

			// assert
			Assert.That(result, Is.EqualTo("ok"));
		}

		[Test]
		public async Task GetRecipesForClientSemantic_should_return_snippets()
		{
			// arrange
			var client = new CodigaClient();
			var languages = new ReadOnlyCollection<string>(new[] { "Csharp" });

			// act
			var result = await client.GetRecipesForClientSemanticAsync("add test", languages, true);

			// assert
			Assert.NotNull(result);
			Assert.IsNotEmpty(result);
		}
	}
}
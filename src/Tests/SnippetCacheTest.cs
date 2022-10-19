using Extension.Caching;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using GraphQLClient;
using System.Collections.ObjectModel;

namespace Tests
{
	[TestFixture]
	public class SnippetCacheTest
	{
		private ISnippetCache systemUnderTest;

		[SetUp]
		public void SetUp()
		{
			systemUnderTest = new SnippetCache();
		}

		[TearDown]
		public void TearDown()
		{
			systemUnderTest.StopPolling();
		}

		[Test]
		public void StartPolling_should_not_start_multiple_sessions_for_same_language()
		{
			// arrange
			var snippets = new ReadOnlyCollection<CodigaSnippet>(new[] { new CodigaSnippet() });

			var clientMock = new Mock<ICodigaClient>();
			clientMock.Setup(c => c.GetRecipesForClientByShortcutLastTimestampAsync(It.IsAny<string>()))
				.ReturnsAsync(0);
			clientMock.Setup(c => c.GetRecipesForClientByShortcutAsync(It.IsAny<string>()))
				.ReturnsAsync(snippets);

			var providerMock = new Mock<ICodigaClientProvider>();
			providerMock.Setup(p => p.GetClient()).Returns(clientMock.Object);

			//first polling session
			var started = systemUnderTest.StartPolling("Csharp", providerMock.Object);
			Assert.That(started, Is.True);

			// act
			started = systemUnderTest.StartPolling("Csharp", providerMock.Object);

			// arrange
			Assert.That(started, Is.False);
		}

		[Test]
		public void SnippetCache_should_setup_internal_structures()
		{
			// act
			var snippets = systemUnderTest.GetSnippets("Csharp");

			//assert
			Assert.That(snippets, Is.Empty);
		}

		[Test]
		[Explicit("External API call")]
		public async Task PollSnippetsFromServer_should_poll_snippets()
		{
			// arrange
			var token = new CancellationTokenSource();
			var cache = (SnippetCache)systemUnderTest;

			// act
			cache.PollSnippetsAsync(token.Token, "Csharp");

			// assert
			await Task.Delay(15000);

			var snippets = cache.GetSnippets("Csharp");
		}
	}
}
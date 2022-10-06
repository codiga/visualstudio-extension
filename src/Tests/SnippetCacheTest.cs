using Extension.Caching;

namespace Tests;

[TestFixture]
public class SnippetCacheTest
{
	[Test]
	public void SnippetCache_should_setup_api_client()
	{
		// act
		var cache = new SnippetCache();
	}

	[Test]
	public async Task PollSnippetsFromServer_should_poll_snippets()
	{
		// arrange
		var cache = new SnippetCache();
		var token = new CancellationTokenSource();

		// act
		cache.PollSnippetsAsync(token.Token);

		// assert
		await Task.Delay(15000);

		var snippets = cache.GetSnippets("Csharp");
	}
}
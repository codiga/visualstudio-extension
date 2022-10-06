using Extension.SnippetFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQLClient;
using CodigaSnippet = GraphQLClient.CodigaSnippet;

namespace Extension.Caching
{
	interface ISnippetCache
	{
		public IEnumerable<VisualStudioSnippet> GetSnippets(string language, ReadOnlyCollection<string> dependencies);
		public IEnumerable<VisualStudioSnippet> GetSnippets(string language);
		public IEnumerable<VisualStudioSnippet> GetSnippets();
	}

	[Export]
	public class SnippetCache : ISnippetCache
	{
		private readonly CodigaClient _client;
		private IDictionary<string, IReadOnlyCollection<CodigaSnippet>> _cachedSnippets;
		private CancellationTokenSource _pollTokenSource;

		private long? LastTimeStamp { get; set; }

		public SnippetCache()
		{
			_cachedSnippets = new Dictionary<string, IReadOnlyCollection<CodigaSnippet>>();
			_client = new CodigaClient();

		}

		public void StartPolling(string language)
		{
			_pollTokenSource = new CancellationTokenSource();
			PollSnippetsAsync(_pollTokenSource.Token);
		}

		public void StopPolling()
		{
			_pollTokenSource.Cancel();
		}

		public async Task PollSnippetsAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				// TODO add support for different languages
				var ts = await _client.GetRecipesForClientByShortcutLastTimestamp("Csharp");

				if (LastTimeStamp == null || ts > LastTimeStamp)
				{
					// TODO only add diff
					var snippets = await _client.GetRecipesForClientByShortcutAsync("Csharp");
					_cachedSnippets["Csharp"] = snippets;
					LastTimeStamp = ts;
				}
				LastTimeStamp ??= ts;

				var task = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

				try
				{
					await task;
				}
				catch (TaskCanceledException)
				{
					return;
				}
			}
		}

		public IEnumerable<VisualStudioSnippet> GetSnippets(string language, ReadOnlyCollection<string> dependencies)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<VisualStudioSnippet> GetSnippets(string language)
		{
			return _cachedSnippets[language].Select(SnippetParser.FromCodigaSnippet);
		}

		public IEnumerable<VisualStudioSnippet> GetSnippets()
		{
			var snippets = _cachedSnippets.Values
				.SelectMany(s => s)
				.Select(SnippetParser.FromCodigaSnippet);
			return snippets;
		}
	}
}

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
using EnvDTE;

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
		private IDictionary<string, PollingSession> _currentPollingSessions;

		public SnippetCache()
		{
			_cachedSnippets = new Dictionary<string, IReadOnlyCollection<CodigaSnippet>>();
			_currentPollingSessions = new Dictionary<string, PollingSession>();
			_client = new CodigaClient();

		}

		public void StartPolling(string language)
		{
			var tokenSource = new CancellationTokenSource();
			var session = new PollingSession
			{
				Source = tokenSource,
				LastTimeStamp = null
			};
			_currentPollingSessions.Add(language, session);
			PollSnippetsAsync(tokenSource.Token, language);
		}

		public void StopPolling()
		{
			foreach (var session in _currentPollingSessions.Values)
			{
				session.Source.Cancel();
			}
		}

		public async Task PollSnippetsAsync(CancellationToken cancellationToken, string language)
		{
			while (true)
			{
				var ts = await _client.GetRecipesForClientByShortcutLastTimestamp(language);
				var lastTs = _currentPollingSessions[language].LastTimeStamp;
				if (lastTs == null || ts > lastTs)
				{
					// TODO only add diff
					var snippets = await _client.GetRecipesForClientByShortcutAsync(language);
					_cachedSnippets[language] = snippets;
					_currentPollingSessions[language].LastTimeStamp = ts;
				}
				_currentPollingSessions[language].LastTimeStamp ??= ts;

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

	internal class PollingSession
	{
		public CancellationTokenSource Source { get; set; }

		public long? LastTimeStamp { get; set; }
	}
}

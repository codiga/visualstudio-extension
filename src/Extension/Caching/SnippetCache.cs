using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Extension.SnippetFormats;
using GraphQLClient;
using CodigaSnippet = GraphQLClient.CodigaSnippet;

namespace Extension.Caching
{
	interface ISnippetCache
	{
		public bool StartPolling(LanguageUtils.LanguageEnumeration language);
		public void StopPolling();
		public void StopPolling(LanguageUtils.LanguageEnumeration language);
		public void ReportActivity(LanguageUtils.LanguageEnumeration language);

		public IEnumerable<CodigaSnippet> GetSnippets(LanguageUtils.LanguageEnumeration language, ReadOnlyCollection<string> dependencies);
		public IEnumerable<CodigaSnippet> GetSnippets(LanguageUtils.LanguageEnumeration language);
	}

	[Export]
	public class SnippetCache : ISnippetCache
	{
		public const int PollIntervalInSeconds = 10;
		public const int IdleIntervalInMinutes = 10;

		private ICodigaClientProvider _clientProvider;
		private IDictionary<LanguageUtils.LanguageEnumeration, IReadOnlyCollection<CodigaSnippet>> _cachedSnippets;
		private IDictionary<LanguageUtils.LanguageEnumeration, PollingSession> _currentPollingSessions;

		public SnippetCache()
		{
			_clientProvider = new DefaultCodigaClientProvider();
			_cachedSnippets = new Dictionary<LanguageUtils.LanguageEnumeration, IReadOnlyCollection<CodigaSnippet>>();
			_currentPollingSessions = new Dictionary<LanguageUtils.LanguageEnumeration, PollingSession>();
		}

		public SnippetCache(ICodigaClientProvider provider)
		{
			_clientProvider = provider;
			_cachedSnippets = new Dictionary<LanguageUtils.LanguageEnumeration, IReadOnlyCollection<CodigaSnippet>>();
			_currentPollingSessions = new Dictionary<LanguageUtils.LanguageEnumeration, PollingSession>();
		}

		public bool StartPolling(LanguageUtils.LanguageEnumeration language)
		{
			if (_currentPollingSessions.TryGetValue(language, out var session) || language == LanguageUtils.LanguageEnumeration.Unknown)
			{
				return false;
			}

			var tokenSource = new CancellationTokenSource();
			session = new PollingSession
			{
				Language = language,
				Source = tokenSource,
				LastTimeStamp = null
			};

			session.IdleTimerElapsed += Session_IdleTimerElapsed;

			_currentPollingSessions.Add(language, session);
			PollSnippetsAsync(tokenSource.Token, language);

			return true;
		}

		public void StopPolling()
		{
			foreach (var session in _currentPollingSessions.Values)
			{
				session.Source.Cancel();
			}

			_currentPollingSessions.Clear();
		}

		public void StopPolling(LanguageUtils.LanguageEnumeration language)
		{
			if (_currentPollingSessions.TryGetValue(language, out var session))
			{
				session.Source.Cancel();
				_currentPollingSessions.Remove(language);
			}
		}

		public void ReportActivity(LanguageUtils.LanguageEnumeration language)
		{
			if (_currentPollingSessions.TryGetValue(language, out var session))
			{
				session.ResetTimer();
			}
			else
			{
				StartPolling(language);
			}
		}

		public IEnumerable<CodigaSnippet> GetSnippets(LanguageUtils.LanguageEnumeration language, ReadOnlyCollection<string> dependencies)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<CodigaSnippet> GetSnippets(LanguageUtils.LanguageEnumeration language)
		{
			if (!_cachedSnippets.TryGetValue(language, out var snippets))
			{
				snippets = new List<CodigaSnippet>();
			}

			return snippets;
		}

		internal async Task PollSnippetsAsync(CancellationToken cancellationToken, LanguageUtils.LanguageEnumeration language)
		{
			while (true)
			{
				if (!_currentPollingSessions.TryGetValue(language, out var session))
					return;

				var client = _clientProvider.GetClient();

				var ts = await client.GetRecipesForClientByShortcutLastTimestampAsync(language.GetName());
				var lastTs = session.LastTimeStamp;
				if (lastTs == null || ts > lastTs)
				{
					// TODO only add diff
					var snippets = await client.GetRecipesForClientByShortcutAsync(language.GetName());
					_cachedSnippets[language] = snippets;
					session.LastTimeStamp = ts;
				}
				session.LastTimeStamp ??= ts;

				var task = Task.Delay(TimeSpan.FromSeconds(PollIntervalInSeconds), cancellationToken);

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

		private void Session_IdleTimerElapsed(object sender, EventArgs e)
		{
			var session = (PollingSession)sender;
			if (!_currentPollingSessions.ContainsKey(session.Language))
				return;

			StopPolling(session.Language);
			session.IdleTimerElapsed -= Session_IdleTimerElapsed;
		}
	}

	/// <summary>
	/// Represents a running session for polling snippets from the Codiga API.
	/// </summary>
	internal class PollingSession
	{
		public LanguageUtils.LanguageEnumeration Language { get; set; }

		public CancellationTokenSource Source { get; set; }

		public long? LastTimeStamp { get; set; }

		private System.Timers.Timer IdleTimer { get; }

		public event EventHandler<EventArgs> IdleTimerElapsed;

		public PollingSession()
		{
			IdleTimer = new System.Timers.Timer(TimeSpan.FromMinutes(SnippetCache.IdleIntervalInMinutes).TotalMilliseconds);
			IdleTimer.AutoReset = true;
			IdleTimer.Elapsed += IdleTimer_Elapsed;
		}

		private void IdleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			IdleTimerElapsed?.Invoke(this, new EventArgs());
		}

		public void StartTimer()
		{
			IdleTimer.Start();
		}

		public void ResetTimer()
		{
			IdleTimer.Stop();
			IdleTimer.Start();
		}
	}
}

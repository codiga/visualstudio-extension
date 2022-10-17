﻿using Extension.SnippetFormats;
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
		public IEnumerable<CodigaSnippet> GetSnippets(string language, ReadOnlyCollection<string> dependencies);
		public IEnumerable<CodigaSnippet> GetSnippets(string language);
	}

	[Export]
	public class SnippetCache : ISnippetCache
	{
		public const int PollIntervalInSeconds = 10;
		public const int IdleIntervalInMinutes = 10;

		private CodigaClient _client;
		private IDictionary<string, IReadOnlyCollection<CodigaSnippet>> _cachedSnippets;
		private IDictionary<string, PollingSession> _currentPollingSessions;

		public SnippetCache()
		{
			_cachedSnippets = new Dictionary<string, IReadOnlyCollection<CodigaSnippet>>();
			_currentPollingSessions = new Dictionary<string, PollingSession>();
		}

		public void StartPolling(string language, CodigaClientProvider clientProvider)
		{
			_client = clientProvider.GetClient();
			StartPolling(language);
		}

		private void StartPolling(string language)
		{
			var tokenSource = new CancellationTokenSource();
			var session = new PollingSession
			{
				Language = language,
				Source = tokenSource,
				LastTimeStamp = null
			};

			session.IdleTimerElapsed += Session_IdleTimerElapsed; ;

			_currentPollingSessions.Add(language, session);
			PollSnippetsAsync(tokenSource.Token, language);
		}


		public void StopPolling()
		{
			foreach (var session in _currentPollingSessions.Values)
			{
				session.Source.Cancel();
			}

			_currentPollingSessions.Clear();
		}

		public void StopPolling(string language)
		{
			if (_currentPollingSessions.TryGetValue(language, out var session))
			{
				session.Source.Cancel();
				_currentPollingSessions.Remove(language);
			}
		}

		public void ReportActivity(string language)
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

		public async Task PollSnippetsAsync(CancellationToken cancellationToken, string language)
		{
			while (true)
			{
				if (!_currentPollingSessions.TryGetValue(language, out var session))
					return;

				var ts = await _client.GetRecipesForClientByShortcutLastTimestampAsync(language);
				var lastTs = session.LastTimeStamp;
				if (lastTs == null || ts > lastTs)
				{
					// TODO only add diff
					var snippets = await _client.GetRecipesForClientByShortcutAsync(language);
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

		public IEnumerable<CodigaSnippet> GetSnippets(string language, ReadOnlyCollection<string> dependencies)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<CodigaSnippet> GetSnippets(string language)
		{
			if(!_cachedSnippets.TryGetValue(language, out var snippets))
			{
				snippets = _client.GetRecipesForClientByShortcutAsync(language).GetAwaiter().GetResult();
			}

			return snippets;
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
		public string Language { get; set; }

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

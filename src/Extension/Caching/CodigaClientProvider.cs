using Extension.Settings;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

namespace Extension.Caching
{

	/// <summary>
	/// Provides a implementation independent way to get a CodigaClient. Useful for testing/mocking.
	/// </summary>
	public interface ICodigaClientProvider
	{
		public ICodigaClient GetClient();
	}

	/// <summary>
	/// Provides access to the default global singleton instance of the CodigaClient.
	/// </summary>
	public class DefaultCodigaClientProvider : ICodigaClientProvider
	{
		public ICodigaClient GetClient()
		{
			return GlobalCodigaClient.Instance;
		}
	}

	/// <summary>
	/// Provides singleton access to the Codiga GraphQL client.
	/// </summary>
	public class GlobalCodigaClient
	{
		private static CodigaClient _client { get; set; }

		public static ICodigaClient Instance
		{
			get
			{
				if (_client == null)
				{
					ThreadHelper.JoinableTaskFactory.Run(async () =>
					{
						await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					});

					var settings = EditorSettingsProvider.GetCurrentCodigaSettings();
					_client = new CodigaClient(settings.ApiToken, settings.Fingerprint);

					CodigaOptions.Saved += CodigaOptions_Saved;
				}

				return _client;
			}
		}

		private static void CodigaOptions_Saved(CodigaOptions obj)
		{
			Instance.SetApiToken(obj.ApiToken);
		}
	}
}

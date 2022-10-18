using Extension.Settings;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

namespace Extension.Caching
{
	[Export]
	public class CodigaClientProvider
	{
		private CodigaClient Client { get; set; }

		public CodigaClient GetClient()
		{
			if(Client == null)
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				});

				var settings = EditorSettingsProvider.GetCurrentCodigaSettings();
				Client = new CodigaClient(settings.ApiToken, settings.Fingerprint);

				CodigaOptions.Saved += CodigaOptions_Saved;
			}

			return Client;
		}

		private void CodigaOptions_Saved(CodigaOptions obj)
		{
			Client.SetApiToken(obj.ApiToken);
		}
	}
}

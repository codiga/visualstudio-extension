using Extension.Settings;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

namespace Extension.Caching
{
	public interface ICodigaClientProvider
	{
		public ICodigaClient GetClient();
	}

	[Export]
	public class CodigaClientProvider : ICodigaClientProvider
	{
		private CodigaClient Client { get; set; }

		public ICodigaClient GetClient()
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

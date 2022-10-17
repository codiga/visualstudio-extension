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

				var settings = CodigaOptions.Instance;
				Client = new CodigaClient(settings.ApiToken);
			}

			return Client;
		}
	}
}

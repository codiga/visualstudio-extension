using Extension.Settings;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

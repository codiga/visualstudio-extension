using System;
using Extension.Logging;

namespace Extension.Rosie
{
	/// <summary>
	/// Provides access to the default global singleton instance of the CodigaClient.
	/// </summary>
	public class RosieClientProvider
	{
		public static bool TryGetClient(out IRosieClient client)
		{
			try
			{
				client = GetClient();
			}
			catch (ArgumentException e)
			{
				client = null;
				ExtensionLogger.LogException(e);
				return false;
			}

			return true;
		}

		public static IRosieClient GetClient()
		{
			return GlobalRosieClient.Instance;
		}
	}

	/// <summary>
	/// Provides singleton access to the Rosie client.
	/// </summary>
	public class GlobalRosieClient
	{
		private static RosieClient _client { get; set; }

		public static IRosieClient Instance
		{
			get
			{
				if (_client == null)
				{
					try
					{
						_client = new RosieClient();
					}
					catch(Exception e)
					{
						ExtensionLogger.LogException(e);
					}
				}

				return _client;
			}
		}
	}
}

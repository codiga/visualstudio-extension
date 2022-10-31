using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ExtensionManager;
using Rollbar;
using System;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace Extension.Logging
{
	/// <summary>
	/// Used to log isses to Codiga rollbar. We need to catch exceptions manually as there is no global handler yet.
	/// See https://github.com/microsoft/VSExtensibility/issues/82
	/// </summary>
	public static class ExtensionLogger
	{
		private static IVsActivityLog _activityLog;

		static ExtensionLogger()
		{
			var environment = "production";

#if DEBUG
			environment = "development";
#endif

			var config = new RollbarInfrastructureConfig("f3faf24332054e00a2612c40a44f408d", environment);

#if DEBUG
			config.RollbarLoggerConfig.RollbarDeveloperOptions.RethrowExceptionsAfterReporting = true;
#endif

			config.RollbarLoggerConfig.RollbarPayloadAdditionOptions.CodeVersion = GetExtensionVersion().ToString();
			RollbarInfrastructure.Instance.Init(config);
		}

		public static ILogger LogException(Exception exception)
		{
			var parameters = new Dictionary<string, object>()
			{
				{"Source", exception.Source }
			};

			var logger = RollbarLocator.RollbarInstance.Error(exception, parameters);

			LogActivityError(exception);

			return logger;
		}

		public static ILogger LogWarning(string message)
		{
			var logger = RollbarLocator.RollbarInstance.Warning(message);
			return logger;
		}

		/// <summary>
		/// Tries to log the exception to VS ActivityLog
		/// </summary>
		/// <param name="exception"></param>
		private static void LogActivityError(Exception exception)
		{
			_activityLog = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				return await VS.GetServiceAsync<SVsActivityLog, IVsActivityLog>();
			});

			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				_activityLog.LogEntry((uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, exception.Source, exception.Message);
			}
			catch(ArgumentException e)
			{
				var parameters = new Dictionary<string, object>()
				{
					{"InitialException", exception }
				};
				RollbarLocator.RollbarInstance.Error(e, parameters);
			}
		}

		private static Version GetExtensionVersion()
		{
			var extensionManager = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				return await VS.GetServiceAsync<SVsExtensionManager, IVsExtensionManager>();
			});

			var codigaExtension = extensionManager.GetInstalledExtension("Codiga.7f415e9f-9649-4ced-bbb2-64044b3d0a72");
			
			return codigaExtension.Header.Version;
		}
	}
}

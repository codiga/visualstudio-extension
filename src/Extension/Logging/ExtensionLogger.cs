﻿using Community.VisualStudio.Toolkit;
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
			var config = new RollbarInfrastructureConfig("f3faf24332054e00a2612c40a44f408d");
			config.RollbarLoggerConfig.RollbarPayloadAdditionOptions.CodeVersion = GetExtensionVersion().ToString();
			RollbarInfrastructure.Instance.Init(config);
		}

		public static ILogger LogException(Exception exception)
		{
			var version = GetExtensionVersion();
			var parameters = new Dictionary<string, object>()
			{
				{"Version", version }
			};

			var logger = RollbarLocator.RollbarInstance.Error(exception, parameters);

			LogActivityError(exception);

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

			var codigaExtension = extensionManager.GetInstalledExtension("Codiga.2c544927-3588-41b4-9bfe-e80a0b99df80");
			
			return codigaExtension.Header.Version;
		}
	}
}
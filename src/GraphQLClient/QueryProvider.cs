using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GraphQLClient
{
	public static class QueryProvider
	{
		private const string Namespace = "GraphQLClient.Queries.";
		private const string ShortcutQueryFile = Namespace + "GetRecipesForClientByShortcut.graphql";
		private const string ShortcutLastTimestampQueryFile = Namespace + "GetRecipesForClientByShortcutLastTimestamp.graphql";
		private const string RecordRecipeUseMutationFile = Namespace + "RecordRecipeUse.graphql";
		private const string SemanticQueryFile = Namespace + "GetRecipesForClientSemantic.graphql";

		public static string ShortcutQuery { get; }
		public static string ShortcutLastTimestampQuery { get; }
		public static string SemanticQuery { get; }
		public static string RecordRecipeUseMutation { get; }

		static QueryProvider()
		{
			var assembly = Assembly.GetExecutingAssembly();

			using (Stream stream = assembly.GetManifestResourceStream(ShortcutQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				ShortcutQuery = reader.ReadToEnd();
			}

			using (Stream stream = assembly.GetManifestResourceStream(ShortcutLastTimestampQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				ShortcutLastTimestampQuery = reader.ReadToEnd();
			}

			using (Stream stream = assembly.GetManifestResourceStream(RecordRecipeUseMutationFile))
			{
				using StreamReader reader = new StreamReader(stream);
				RecordRecipeUseMutation = reader.ReadToEnd();
			}

			using (Stream stream = assembly.GetManifestResourceStream(SemanticQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				SemanticQuery = reader.ReadToEnd();
			}
		}
	}
}

using System.IO;
using System.Reflection;

namespace GraphQLClient
{
	/// <summary>
	/// Stores and provides the GraphQL queries for all Codiga features.
	/// <br/>
	/// The queries are loaded upon instantiation of this class.
	/// <br/>
	/// For proper initialization of this class, when adding a new query, make sure that the .graphql
	/// files are configured in the GraphQLClient.csproj file as well.
	/// </summary>
	public static class QueryProvider
	{
		private const string Namespace = "GraphQLClient.Queries.";
		private const string ShortcutQueryFile = Namespace + "GetRecipesForClientByShortcut.graphql";
		private const string ShortcutLastTimestampQueryFile = Namespace + "GetRecipesForClientByShortcutLastTimestamp.graphql";
		private const string RecordRecipeUseMutationFile = Namespace + "RecordRecipeUse.graphql";
		private const string SemanticQueryFile = Namespace + "GetRecipesForClientSemantic.graphql";
		private const string GetUserQueryFile = Namespace + "GetUser.graphql";
		private const string GetRulesetsForClientQueryFile = Namespace + "GetRulesetsForClient.graphql";
		private const string GetRulesetsLastUpdatedTimestampQueryFile = Namespace + "GetRulesetsForClientLastTimestamp.graphql";
		private const string RecordRuleFixMutationFile = Namespace + "RecordRuleFix.graphql";

		public static string ShortcutQuery { get; }
		public static string ShortcutLastTimestampQuery { get; }
		public static string SemanticQuery { get; }
		public static string RecordRecipeUseMutation { get; }
		public static string GetUserQuery { get; }
		public static string GetRulesetsForClientQuery { get; }
		public static string GetRulesetsLastUpdatedTimestampQuery { get; }
		public static string RecordRuleFixMutation { get; }

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

			using (Stream stream = assembly.GetManifestResourceStream(GetUserQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				GetUserQuery = reader.ReadToEnd();
			}
			
			using (Stream stream = assembly.GetManifestResourceStream(GetRulesetsForClientQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				GetRulesetsForClientQuery = reader.ReadToEnd();
			}
			
			using (Stream stream = assembly.GetManifestResourceStream(GetRulesetsLastUpdatedTimestampQueryFile))
			{
				using StreamReader reader = new StreamReader(stream);
				GetRulesetsLastUpdatedTimestampQuery = reader.ReadToEnd();
			}
			
			using (Stream stream = assembly.GetManifestResourceStream(RecordRuleFixMutationFile))
			{
				using StreamReader reader = new StreamReader(stream);
				RecordRuleFixMutation = reader.ReadToEnd();
			}
		}
	}
}

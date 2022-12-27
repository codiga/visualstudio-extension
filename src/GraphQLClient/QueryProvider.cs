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
		private const string RecordCreateCodigaYamlMutationFile = Namespace + "RecordCreateCodigaYaml.graphql";

		public static string ShortcutQuery { get; }
		public static string ShortcutLastTimestampQuery { get; }
		public static string SemanticQuery { get; }
		public static string RecordRecipeUseMutation { get; }
		public static string GetUserQuery { get; }
		public static string GetRulesetsForClientQuery { get; }
		public static string GetRulesetsLastUpdatedTimestampQuery { get; }
		public static string RecordRuleFixMutation { get; }
		public static string RecordCreateCodigaYamlMutation { get; }
		
		static QueryProvider()
		{
			var assembly = Assembly.GetExecutingAssembly();

			ShortcutQuery = ReadQuery(assembly, ShortcutQueryFile);
			ShortcutLastTimestampQuery = ReadQuery(assembly, ShortcutLastTimestampQueryFile);
			RecordRecipeUseMutation = ReadQuery(assembly, RecordRecipeUseMutationFile);
			SemanticQuery = ReadQuery(assembly, SemanticQueryFile);
			GetUserQuery = ReadQuery(assembly, GetUserQueryFile);
			GetRulesetsForClientQuery = ReadQuery(assembly, GetRulesetsForClientQueryFile);
			GetRulesetsLastUpdatedTimestampQuery = ReadQuery(assembly, GetRulesetsLastUpdatedTimestampQueryFile);
			RecordRuleFixMutation = ReadQuery(assembly, RecordRuleFixMutationFile);
			RecordCreateCodigaYamlMutation = ReadQuery(assembly, RecordCreateCodigaYamlMutationFile);
		}
		
		private static string ReadQuery(Assembly assembly, string queryFileName)
		{
			using (Stream stream = assembly.GetManifestResourceStream(queryFileName))
			{
				using StreamReader reader = new StreamReader(stream);
				return reader.ReadToEnd();
			}
		} 
	}
}

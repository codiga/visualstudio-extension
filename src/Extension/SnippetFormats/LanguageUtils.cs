using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.SnippetFormats
{
	public static class LanguageUtils
	{
		public enum LanguageEnumeration
		{
			Unknown,
			Coldfusion,
			Docker,
			Objectivec,
			Terraform,
			Json,
			Yaml,
			Typescript,
			Swift,
			Solidity,
			Sql,
			Shell,
			Scala,
			Rust,
			Ruby,
			Php,
			Python,
			Perl,
			Kotlin,
			Javascript,
			Java,
			Html,
			Haskell,
			Go,
			Dart,
			Csharp,
			Css,
			Cpp,
			C,
			Apex
		}

		public static string GetName(this LanguageEnumeration language)
		{
			var type = typeof(LanguageEnumeration);
			return Enum.GetName(type, language);
		}

		/// <summary>
		/// Parses the given file extension to LanguageEnumeration
		/// </summary>
		/// <param name="extension"></param>
		/// <returns></returns>
		public static LanguageEnumeration Parse(string extension)
		{
			return extension switch
			{
				".cs" => LanguageEnumeration.Csharp,
				".css" => LanguageEnumeration.Css,
				".html" => LanguageEnumeration.Html,
				".json" => LanguageEnumeration.Json,
				".py" => LanguageEnumeration.Python,
				".ts" => LanguageEnumeration.Typescript,
				".js" => LanguageEnumeration.Javascript,
				".c" => LanguageEnumeration.C,
				".cpp" => LanguageEnumeration.Cpp,
				".java" => LanguageEnumeration.Java,
				".rb" => LanguageEnumeration.Ruby,
				".rs" => LanguageEnumeration.Rust,
				".go" => LanguageEnumeration.Go,
				".php" => LanguageEnumeration.Php,
				".yml" => LanguageEnumeration.Yaml,
				".cfm" => LanguageEnumeration.Coldfusion,
				".dockerfile" => LanguageEnumeration.Docker,
				".m" => LanguageEnumeration.Objectivec,
				".tf" => LanguageEnumeration.Terraform,
				".swift" => LanguageEnumeration.Swift,
				".sol" => LanguageEnumeration.Solidity,
				".sql" => LanguageEnumeration.Sql,
				".sh" => LanguageEnumeration.Shell,
				".scala" => LanguageEnumeration.Scala,
				".pl" => LanguageEnumeration.Perl,
				".hs" => LanguageEnumeration.Haskell,
				".dart" => LanguageEnumeration.Dart,

				_ => LanguageEnumeration.Unknown
			};
		}

		public static string GetCommentSign(LanguageEnumeration language)
		{
			switch (language){

				case LanguageEnumeration.Javascript:
				case LanguageEnumeration.Typescript:
				case LanguageEnumeration.C:
				case LanguageEnumeration.Csharp:
				case LanguageEnumeration.Apex:
				case LanguageEnumeration.Cpp:
				case LanguageEnumeration.Scala:
				case LanguageEnumeration.Dart:
				case LanguageEnumeration.Go:
				case LanguageEnumeration.Objectivec:
				case LanguageEnumeration.Kotlin:
				case LanguageEnumeration.Java:
				case LanguageEnumeration.Swift:
				case LanguageEnumeration.Solidity:
				case LanguageEnumeration.Rust:
				
				default:
					return "//";

				case LanguageEnumeration.Python:
				case LanguageEnumeration.Shell:
				case LanguageEnumeration.Perl:
				case LanguageEnumeration.Yaml:
					return "#";

				case LanguageEnumeration.Coldfusion:
					return "<!---";

				case LanguageEnumeration.Haskell:
				case LanguageEnumeration.Sql:
					return "--";

				case LanguageEnumeration.Css:
					return "/*";
			}
		}
	}
}

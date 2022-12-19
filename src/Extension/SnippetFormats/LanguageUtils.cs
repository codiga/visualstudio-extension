using System;
using System.IO;

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
			Twig,
			Terraform,
			Json,
			Yaml,
			Typescript,
			Swift,
			Solidity,
			Sql,
			Shell,
			Scala,
			Scss,
			Sass,
			Rust,
			Ruby,
			Php,
			Python,
			Perl,
			Markdown,
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
		/// Parses the given file name's extension to LanguageEnumeration, or <see cref="LanguageEnumeration.Unknown"/>
		/// if the file extension is not supported.
		/// <br/>
		/// This is a convenience method for <c>LanguageEnumeration.Parse(Path.GetExtension(fileName))</c>.
		/// </summary>
		/// <param name="fileName">the file name whose extension is parsed</param>
		public static LanguageEnumeration ParseFromFileName(string fileName)
		{
			return fileName.ToLower().StartsWith("docker") ? LanguageEnumeration.Docker : Parse(Path.GetExtension(fileName));
		}

		/// <summary>
		/// Parses the given file extension to LanguageEnumeration, or <see cref="LanguageEnumeration.Unknown"/>
		/// if the file extension is not supported.
		/// </summary>
		/// <param name="extension">the file extension</param>
		public static LanguageEnumeration Parse(string extension)
		{
			return extension switch
			{
				".bash" => LanguageEnumeration.Shell,
				".c" => LanguageEnumeration.C,
				".cfc" => LanguageEnumeration.Coldfusion,
				".cfm" => LanguageEnumeration.Coldfusion,
				".cls" => LanguageEnumeration.Apex,
				".cpp" => LanguageEnumeration.Cpp,
				".cs" => LanguageEnumeration.Csharp,
				".css" => LanguageEnumeration.Css,
				".dart" => LanguageEnumeration.Dart,
				".dockerfile" => LanguageEnumeration.Docker,
				".go" => LanguageEnumeration.Go,
				".hs" => LanguageEnumeration.Haskell,
				".htm" => LanguageEnumeration.Html,
				".html" => LanguageEnumeration.Html,
				".html5" => LanguageEnumeration.Html,
				".ipynb" => LanguageEnumeration.Python,
				".java" => LanguageEnumeration.Java,
				".js" => LanguageEnumeration.Javascript,
				".json" => LanguageEnumeration.Json,
				".jsx" => LanguageEnumeration.Javascript,
				".kt" => LanguageEnumeration.Kotlin,
				".m" => LanguageEnumeration.Objectivec,
				".mm" => LanguageEnumeration.Objectivec,
				".M" => LanguageEnumeration.Objectivec,
				".md" => LanguageEnumeration.Markdown,
				".php" => LanguageEnumeration.Php,
				".php4" => LanguageEnumeration.Php,
				".php5" => LanguageEnumeration.Php,
				".pm" => LanguageEnumeration.Perl,
				".pl" => LanguageEnumeration.Perl,
				".py" => LanguageEnumeration.Python,
				".py3" => LanguageEnumeration.Python,
				".rb" => LanguageEnumeration.Ruby,
				".rhtml" => LanguageEnumeration.Ruby,
				".rs" => LanguageEnumeration.Rust,
				".sass" => LanguageEnumeration.Sass,
				".scala" => LanguageEnumeration.Scala,
				".scss" => LanguageEnumeration.Scss,
				".sh" => LanguageEnumeration.Shell,
				".sol" => LanguageEnumeration.Solidity,
				".sql" => LanguageEnumeration.Sql,
				".swift" => LanguageEnumeration.Swift,
				".tf" => LanguageEnumeration.Terraform,
				".ts" => LanguageEnumeration.Typescript,
				".tsx" => LanguageEnumeration.Typescript,
				".twig" => LanguageEnumeration.Twig,
				".yml" => LanguageEnumeration.Yaml,
				".yaml" => LanguageEnumeration.Yaml,
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
				case LanguageEnumeration.Sass:
				case LanguageEnumeration.Scss:
					return "//";

				case LanguageEnumeration.Python:
				case LanguageEnumeration.Shell:
				case LanguageEnumeration.Perl:
				case LanguageEnumeration.Yaml:
				case LanguageEnumeration.Terraform:
					return "#";

				case LanguageEnumeration.Html:
					return "<!--";
				
				case LanguageEnumeration.Coldfusion:
					return "<!---";

				case LanguageEnumeration.Haskell:
				case LanguageEnumeration.Sql:
					return "--";

				case LanguageEnumeration.Css:
					return "/*";
				
				case LanguageEnumeration.Twig:
					return "{#";
				
				default:
					return "//";
			}
		}
	}
}

using MSXML;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Numerics;
using System.Windows.Ink;
using System.Xml;
using System.Xml.Serialization;

namespace Extension.SnippetFormats
{
	/// <summary>
	/// Represents the XML snippet structure used by Viusal Studio for managing code snippets
	/// </summary>
	[XmlRoot(ElementName = "CodeSnippets", Namespace = "http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet")]
	public class VisualStudioSnippet
	{
		[XmlElement(ElementName = "CodeSnippet")]
		public CodeSnippet CodeSnippet { get; set; }
	}

	/// <summary>
	/// Specifies how Visual Studio inserts the code snippet.
	/// </summary>
	[XmlRoot(ElementName = "SnippetTypes")]
	public class SnippetTypes
	{
		/// <summary>
		/// The text value must be one of the following values:
		/// "SurroundsWith": allows the code snippet to be placed around a selected piece of code.
		/// "Expansion": allows the code snippet to be inserted at the cursor.
		/// "Refactoring": specifies that the code snippet is used during C# refactoring. Refactoring cannot be used in custom code snippets.
		/// </summary>
		[XmlElement(ElementName = "SnippetType")]
		public string SnippetType { get; set; }
	}

	[XmlRoot(ElementName = "Header")]
	public class Header
	{
		[XmlIgnore]
		public long Id { get; set; }

		[XmlIgnore]
		public bool IsPublic { get; set; }

		[XmlIgnore]
		public bool IsPrivate { get; set; }

		/// <summary>
		/// Required element. The friendly name of the code snippet. 
		/// There must be exactly one Title element in a Header element.
		/// </summary>
		[XmlElement(ElementName = "Title")]
		public string Title { get; set; }

		/// <summary>
		/// Specifies the shortcut text used to insert the snippet. 
		/// The text value of a Shortcut element can only contain alphanumeric characters, and underscores ( _ ).
		/// Underscore (_) is not supported characters in C++ snippet shortcuts.
		/// </summary>
		[XmlElement(ElementName = "Shortcut")]
		public string Shortcut { get; set; }

		/// <summary>
		/// Specifies descriptive information about the contents of an IntelliSense Code Snippet.
		/// </summary>
		[XmlElement(ElementName = "Description")]
		public string Description { get; set; }

		/// <summary>
		/// Specifies the name of the snippet author. 
		/// The Code Snippets Manager displays the name stored in the Author element of the code snippet.
		/// </summary>
		[XmlElement(ElementName = "Author")]
		public string Author { get; set; }

		[XmlElement(ElementName = "SnippetTypes")]
		public SnippetTypes SnippetTypes { get; set; }

		/// <summary>
		/// Groups individual Keyword elements.
		/// </summary>
		[XmlArrayItem("Keyword")]
		public List<Keyword> Keywords { get; set; }
	}

	[XmlRoot(ElementName = "Literal")]
	public class Literal
	{
		/// <summary>
		/// Required element. Specifies a unique identifier for the literal.
		/// There must be exactly one ID element in a Literal element.
		/// </summary>
		[XmlElement(ElementName = "ID")]
		public string ID { get; set; }

		/// <summary>
		/// Optional element. Describes the expected value and usage of the literal.
		/// There may be zero or one Tooltip elements in a Literal element.
		/// </summary>
		[XmlElement(ElementName = "ToolTip")]
		public string ToolTip { get; set; }

		/// <summary>
		/// Required element. Specifies the literal's default value when you insert the code snippet.
		/// There must be exactly one Default element in a Literal element.
		/// </summary>
		[XmlElement(ElementName = "Default")]
		public string Default { get; set; }
	}

	/// <summary>
	/// Specifies a custom keyword for the code snippet. 
	/// The code snippet keywords are used by Visual Studio
	/// and represent a standard way for online content providers to add custom keywords for searching or categorization.
	/// </summary>
	[XmlRoot(ElementName = "Keyword")]
	public class Keyword
	{
		[XmlText]
		public string Text { get; set; }
	}

	[XmlRoot(ElementName = "Reference")]
	public class Reference
	{
		/// <summary>
		/// Specifies the name of the assembly referenced by the code snippet.
		/// The text value of the Assembly element is either the friendly text name of the assembly,
		/// such as System.dll, or its strong name, such as System,Version=1.0.0.1,Culture=neutral,PublicKeyToken=9b35aa323c18d4fb1.
		/// </summary>
		[XmlElement(ElementName = "Assembly")]
		public string Assembly { get; set; }
	}

	[XmlRoot(ElementName = "Code")]
	public class Code
	{
		/// <summary>
		/// Required attribute that specifies the language of the code snippet
		/// </summary>
		[XmlAttribute(AttributeName = "Language")]
		public string Language { get; set; }

		/// <summary>
		/// Optional attribute that specifies the kind of code that the snippet contains.
		/// </summary>
		[XmlAttribute(AttributeName = "Kind")]
		public string Kind { get; set; }

		/// <summary>
		/// Optional attribute that specifies the delimiter used to describe literals and objects in the code.
		/// By default, the delimiter is "$".
		/// </summary>
		[XmlAttribute(AttributeName = "Delimiter")]
		public string Delimiter { get; set; }

		/// <summary>
		/// This text specifies the code, along with the literals and objects,
		/// that you can use when this code snippet is inserted into a code file.
		/// </summary>
		[XmlText]
		public XmlNode[] CDataCode { get; set; }

		/// <summary>
		/// Wrapper property for CDataCode to be able to work with regular strings.
		/// </summary>
		[XmlIgnore]
		public string CodeString
		{
			get
			{
				return CDataCode.First().Value;
			}

			set
			{
				CDataCode = new XmlNode[] { new XmlDocument().CreateCDataSection(value) };
			}
		}

		/// <summary>
		/// The raw initial code string.
		/// </summary>
		[XmlIgnore]
		public string RawCode { get; }

		public Code()
		{

		}

		public Code(string language, string codeString)
		{
			RawCode = codeString;
			CDataCode = new XmlNode[] { new XmlDocument().CreateCDataSection(codeString) };
			Language = language;
		}
	}

	[XmlRoot(ElementName = "Snippet")]
	public class Snippet
	{
		[XmlArrayItem("Literal")]
		public List<Literal> Declarations { get; set; }

		[XmlArrayItem("Reference")]
		public List<Reference> References { get; set; }

		[XmlElement(ElementName = "Code")]
		public Code Code { get; set; }
	}

	[XmlRoot(ElementName = "CodeSnippet")]
	public class CodeSnippet
	{
		[XmlElement(ElementName = "Header")]
		public Header Header { get; set; }

		[XmlElement(ElementName = "Snippet")]
		public Snippet Snippet { get; set; }

		[XmlAttribute(AttributeName = "Format")]
		public string Format { get; set; }
	}
}

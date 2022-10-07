using MSXML;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

	[XmlRoot(ElementName = "SnippetTypes")]
	public class SnippetTypes
	{
		[XmlElement(ElementName = "SnippetType")]
		public string SnippetType { get; set; }
	}

	[XmlRoot(ElementName = "Header")]
	public class Header
	{
		[XmlElement(ElementName = "Title")]
		public string Title { get; set; }

		[XmlElement(ElementName = "Shortcut")]
		public string Shortcut { get; set; }

		[XmlElement(ElementName = "Description")]
		public string Description { get; set; }

		[XmlElement(ElementName = "Author")]
		public string Author { get; set; }

		[XmlElement(ElementName = "SnippetTypes")]
		public SnippetTypes SnippetTypes { get; set; }
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

		public Code()
		{
			
		}

		public Code(string language, string codeString)
		{
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

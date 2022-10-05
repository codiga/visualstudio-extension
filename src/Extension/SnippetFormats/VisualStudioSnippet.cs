using MSXML;
using System;
using System.Collections.Generic;
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
		[XmlElement(ElementName = "ID")]
		public string ID { get; set; }

		[XmlElement(ElementName = "ToolTip")]
		public string ToolTip { get; set; }

		[XmlElement(ElementName = "Default")]
		public string Default { get; set; }
	}

	[XmlRoot(ElementName = "Reference")]
	public class Reference
	{
		[XmlElement(ElementName = "Assembly")]
		public string Assembly { get; set; }
	}

	[XmlRoot(ElementName = "Code")]
	public class Code
	{
		[XmlAttribute(AttributeName = "Language")]
		public string Language { get; set; }

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

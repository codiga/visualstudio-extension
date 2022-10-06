using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;
using static System.Net.Mime.MediaTypeNames;
using VSLangProj;

namespace Extension.AssistantCompletion
{
	public class SnippetInsertion
	{
		public static void InsertSnippet(IVsTextView vsTextView, IVsExpansionClient expClient, out IVsExpansionSession session)
		{
			vsTextView.GetBuffer(out var textLines);
			IVsExpansion bufferExpansion = (IVsExpansion)textLines;
			var snippet = GetSnippet();
			var position = new TextSpan();
			vsTextView.GetCaretPos(out var startLine, out var endColumn);

			position.iStartIndex = endColumn;
			position.iEndIndex = endColumn;
			position.iStartLine = startLine;
			position.iEndLine = startLine;

			bufferExpansion.InsertSpecificExpansion(
				pSnippet: snippet,
				tsInsertPos: position,
				pExpansionClient: expClient,
				guidLang: Guid.Empty,
				pszRelativePath: string.Empty,
				out session);
		}

		public static IXMLDOMNode GetSnippet()
		{
			var doc = new MSXML.DOMDocument();
			doc.loadXML(SnippetXml);
			var node = doc.documentElement.childNodes.nextNode();

			return node;
		}

		private static string SnippetXml => "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
			"<CodeSnippets  xmlns=\"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet\">" +
			"  <CodeSnippet Format=\"1.0.0\">" +
			"		<Header>" +
			"            <Title>Test replacement fields</Title>" +
			"            <Shortcut>test</Shortcut>" +
			"            <Description>Code snippet for testing replacement fields</Description>" +
			"            <Author>MSIT</Author>" +
			"            <SnippetTypes>" +
			"                <SnippetType>Expansion</SnippetType>" +
			"            </SnippetTypes>" +
			"        </Header>" +
			"        <Snippet>" +
			"            <Declarations>" +
			"                <Literal>" +
			"                  <ID>param1</ID>" +
			"                    <ToolTip>First field</ToolTip>" +
			"                    <Default>first</Default>" +
			"                </Literal>" +
			"                <Literal>" +
			"                    <ID>param2</ID>" +
			"                    <ToolTip>Second field</ToolTip>" +
			"                    <Default>second</Default>" +
			"                </Literal>" +
			"            </Declarations>" +
			"            <References>" +
			"               <Reference>" +
			"                   <Assembly>System.Windows.Forms.dll</Assembly>" +
			"               </Reference>" +
			"            </References>" +
			"            <Code Language=\"CSharp\">" +
			"                <![CDATA[MessageBox.Show(\"$param1$\");" +
			"     MessageBox.Show(\"$param2$\");]]>" +
			"            </Code>" +
			"        </Snippet>" +
			"    </CodeSnippet>" +
			"</CodeSnippets>";
	}
}

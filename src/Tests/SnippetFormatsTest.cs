using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Moq;
using MSXML;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
	internal class SnippetFormatsTest
	{
		[Test]
		[TestCase("&[USER_INPUT:0]", ExpectedResult = "$end$")]
		[TestCase("&[USER_INPUT:0] test &[USER_INPUT:1] test &[USER_INPUT:2]", ExpectedResult = "$end$ test $end$ test $end$")]
		[TestCase("&[USER_INPUT:0:default]", ExpectedResult = "&[USER_INPUT:0:default]")]
		[TestCase("&[USER_INPUT:0: ]", ExpectedResult = "$end$", Ignore = "Not sure how to handle yet")]
		[TestCase("&[USER_INPsUT:0:__ ::]", ExpectedResult = "&[USER_INPsUT:0:__ ::]")]
		public string ReplaceUserCaretPositions_should_create_end_variable(string input)
		{
			// arrange
			var builder = new StringBuilder(input);

			// act
			SnippetParser.ReplaceUserCaretPositions(builder);

			// assert
			return builder.ToString();
		}

		[Test]
		[TestCase("&[USER_INPUT:0:default]", 1, ExpectedResult = "$param0$")]
		[TestCase("&[USER_INPUT:3:CONSTANT_NAME]", 1, ExpectedResult = "$param3$")]
		[TestCase("&[USER_INPUT:0: ]", 1, ExpectedResult = "&[USER_INPUT:0: ]", Ignore ="Not sure how to handle yet")]
		[TestCase("&[USER_INPUT:0:default0] test &[USER_INPUT:1:default1] test &[USER_INPUT:2:default2]", 3, ExpectedResult = "$param0$ test $param1$ test $param2$")]
		[TestCase("&[USER_INPUT:0]", 0, ExpectedResult = "&[USER_INPUT:0]")]
		[TestCase("&[USER_INPUT:0:default0] test &[USER_INPUT:0:default1] test &[USER_INPUT:0:default2]", 1, ExpectedResult = "$param0$ test $param0$ test $param0$")]

		public string ReplaceUserVariables_should_replace_codiga_format_with_vs_fromat_and_add_literals(string input, int literalCount)
		{
			// arrange
			var builder = new StringBuilder(input);
			var vsSnippet = new VisualStudioSnippet
			{
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					Header = new Header
					{
						Title = "tbd",
						Author = "tbd",
						Description = "tdb",
						Shortcut = "test",
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" }
					},

					Snippet = new Snippet
					{
						Declarations = new List<Literal>()
					}
				}
			};

			// act
			SnippetParser.ReplaceUserVariables(builder, vsSnippet);

			// assert
			Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(literalCount).Items);
			return builder.ToString();
		}

		[Test]
		[TestCase("&[CODIGA_INDENT]", ExpectedResult = "\t")]
		[TestCase("&[CODIGA_INDENT]&[CODIGA_INDENT]", ExpectedResult = "\t\t")]
		public string ReplaceIndentation_should_replace_codiga_indention_with_tabs(string input)
		{
			// arrange
			var builder = new StringBuilder(input);

			// act
			SnippetParser.ReplaceIndentation(builder);

			// assert
			return builder.ToString();
		}

		[Test]
		public void FromVisualStudioSnippets_should_return_CompletionItems()
		{
			// arrange
			var snippets = new List<VisualStudioSnippet> { SnippetTestData.Snippet };
			var completionSourceMock = new Mock<IAsyncCompletionSource>();

			// act
			var completionItems = SnippetParser.FromVisualStudioSnippets(snippets, completionSourceMock.Object);

			// assert
			Assert.That(completionItems, Has.Exactly(1).Items);
			var item = completionItems.Single();
			Assert.That(item.DisplayText, Is.EqualTo(SnippetTestData.Snippet.CodeSnippet.Header.Shortcut));
			var node = item.Properties.GetProperty<IXMLDOMNode>(nameof(VisualStudioSnippet.CodeSnippet.Snippet.Code));
			Assert.NotNull(node);

			var expected = new MSXML.DOMDocument();
			expected.loadXML(SnippetTestData.Xml);
			var expectedSnippet = expected.documentElement.childNodes.nextNode();

			// TODO deserialize and check
		}

		[Test]
        public void FromCodigaSnippet_should_convert_to_VisualStudioSnippet_with_two_user_variables()
        {
            // arrange
            var snippet = new CodigaSnippet
            {
	            Shortcut = "nunittest",
	            Language = "Csharp",
	            Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"[Test]
                                        public void &[USER_INPUT:1:Test_method]()
                                        {
                                            // arrange
  
                                            // act
                                            &[USER_INPUT:2:act]
    
                                            // assert
                                        }"))
            };

            // act
            var vsSnippet = SnippetParser.FromCodigaSnippet(snippet);

            // assert
            Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(2).Items);
			var param1 = vsSnippet.CodeSnippet.Snippet.Declarations.First();
			var param2 = vsSnippet.CodeSnippet.Snippet.Declarations.Last();
			Assert.That(param1.ID, Is.EqualTo("param1"));
			Assert.That(param1.Default, Is.EqualTo("Test_method"));
			Assert.That(param2.ID, Is.EqualTo("param2"));
			Assert.That(param2.Default, Is.EqualTo("act"));

			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CDataCode.First().Value.Contains("$param1$"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CDataCode.First().Value.Contains("$param2$"));
        }

        [Test]
        public void VisualStudioSnippet_should_serialize_correctly_to_xml()
        {
			// arrange
			var vsSnippet = SnippetTestData.Snippet;

			var serializer = new System.Xml.Serialization.XmlSerializer(vsSnippet.GetType());
            var actualXml = "";

			// act
			using (var sw = new StringWriter())
			{
				using var xw = XmlWriter.Create(sw);
				serializer.Serialize(xw, vsSnippet);
				actualXml = sw.ToString(); 
			}

			// assert
			var actual = new MSXML.DOMDocument();
			actual.loadXML(actualXml);
			var actualSnippet = actual.documentElement.childNodes.nextNode();

			var expected = new MSXML.DOMDocument();
			expected.loadXML(SnippetTestData.Xml);
			var expectedSnippet = actual.documentElement.childNodes.nextNode();

			Assert.That(expectedSnippet.xml, Is.EqualTo(actualSnippet.xml));
		}

		private static class SnippetTestData
		{
			public static VisualStudioSnippet Snippet => new VisualStudioSnippet
			{
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					
					Header = new Header
					{
						Title = "Test replacement fields",
						Shortcut = "test",
						Description = "Code snippet for testing replacement fields",
						Author = "MSIT",
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" }
					},

					Snippet = new Snippet
					{
						Declarations = new List<Literal>
						{
							new Literal
							{
								ID = "param1",
								ToolTip = "First field",
								Default = "first"
							},
							new Literal
							{
								ID = "param2",
								ToolTip = "Second field",
								Default = "second"
							}
						},
						References = new List<Reference>
						{
							new Reference
							{
								Assembly = "System.Windows.Forms.dll"
							}
						},
						Code = new Code("CSharp", "MessageBox.Show(\"$param1$\");     MessageBox.Show(\"$param2$\");")
					}
				}
			};

			public static string Xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
												"<CodeSnippets>" +
												"  <CodeSnippet Format=\"1.0.0\" xmlns=\"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet\">" +
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
}

using Extension.Xml;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Xml;

namespace Tests
{
    [TestFixture]
	internal class SnippetUtilTest
	{

        [Test]
        public void FromCodigaSnippet_should_convert_to_VisualStudioSnippet()
        {
            // arrange
            var snippet = new CodigaSnippet("nunittest", @"[Test]
                                        public void &[USER_INPUT:1:Test]()
                                        {
                                            // arrange
  
                                            // act
                                            &[USER_INPUT:2:act]
    
                                            // assert
                                        }");

            // act
            var vsSnippet = SnippetUtil.FromCodigaSnippet(snippet);

            // assert
            Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(2).Items);
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
						Code = new Code
						{
							Language = "TestSnippets",
							Text = "<![CDATA[MessageBox.Show(\"$param1$\");     MessageBox.Show(\"$param2$\");]]>"
						}
					}
				}
			};

			public static string Xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
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
}

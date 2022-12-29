using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Extension;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Moq;
using NUnit.Framework;

namespace Tests
{
	/// <summary>
	/// Unit test for <see cref="SnippetParser"/>.
	/// </summary>
    [TestFixture]
	internal class SnippetFormatsTest
	{
		[Test]
		[TestCase("&[USER_INPUT:0]", ExpectedResult = "ßendß")]
		[TestCase("&[USER_INPUT:0] test &[USER_INPUT:1] test &[USER_INPUT:2]", ExpectedResult = "ßendß test ßendß test ßendß")]
		[TestCase("&[USER_INPUT:0:default]", ExpectedResult = "&[USER_INPUT:0:default]ßendß")]
		[TestCase("&[USER_INPUT:0: ]", ExpectedResult = "ßendß", Ignore = "Not sure how to handle yet")]
		[TestCase("&[USER_INPsUT:0:__ ::]", ExpectedResult = "&[USER_INPsUT:0:__ ::]ßendß")]
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
		[TestCase("&[USER_INPUT:0:default]", 1, ExpectedResult = "ßparam0ß")]
		[TestCase("&[USER_INPUT:3:CONSTANT_NAME]", 1, ExpectedResult = "ßparam3ß")]
		[TestCase("&[USER_INPUT:0: ]", 1, ExpectedResult = "&[USER_INPUT:0: ]", Ignore ="Not sure how to handle yet")]
		[TestCase("&[USER_INPUT:0:default0] test &[USER_INPUT:1:default1] test &[USER_INPUT:2:default2]", 3, ExpectedResult = "ßparam0ß test ßparam1ß test ßparam2ß")]
		[TestCase("&[USER_INPUT:0]", 0, ExpectedResult = "&[USER_INPUT:0]")]
		[TestCase("&[USER_INPUT:0:default0] test &[USER_INPUT:0:default1] test &[USER_INPUT:0:default2]", 1, ExpectedResult = "ßparam0ß test ßparam0ß test ßparam0ß")]
		[TestCase("console.log(`${&[USER_INPUT:1:value]}: &[USER_INPUT:1:name]`);", 1, ExpectedResult = "console.log(`${ßparam1ß}: ßparam1ß`);")]
		[TestCase("console.log(`${&[USER_INPUT:1:value1]}: &[USER_INPUT:1:name1], ${&[USER_INPUT:2:value2]}: &[USER_INPUT:2:name2], &[USER_INPUT:3]`);", 
			2, ExpectedResult = "console.log(`${ßparam1ß}: ßparam1ß, ${ßparam2ß}: ßparam2ß, &[USER_INPUT:3]`);")]
		public string ReplaceUserVariables_should_replace_codiga_format_with_vs_format_and_add_literals(string input, int literalCount)
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
		// even tabs
		[TestCase("&[CODIGA_INDENT]", 4, 4, false,  ExpectedResult = "\t")]
		[TestCase("&[CODIGA_INDENT]", 2, 2, false,  ExpectedResult = "\t")]
		[TestCase("&[CODIGA_INDENT]", 8, 8, false,  ExpectedResult = "\t")]
		[TestCase("&[CODIGA_INDENT]&[CODIGA_INDENT]", 4, 4, false, ExpectedResult = "\t\t")]
		// spaces
		[TestCase("&[CODIGA_INDENT]", 4, 4, true, ExpectedResult = "    ")]
		[TestCase("&[CODIGA_INDENT]", 2, 2, true, ExpectedResult = "  ")]
		[TestCase("&[CODIGA_INDENT]", 8, 8, true, ExpectedResult = "        ")]
		[TestCase("&[CODIGA_INDENT]&[CODIGA_INDENT]", 4, 4, true, ExpectedResult = "        ")]
		// mixed
		[TestCase("&[CODIGA_INDENT]", 2, 4, false, ExpectedResult = "  ")]
		[TestCase("&[CODIGA_INDENT]", 2, 2, false, ExpectedResult = "\t")]
		[TestCase("&[CODIGA_INDENT]", 6, 4, false, ExpectedResult = "\t  ")]
		[TestCase("&[CODIGA_INDENT]&[CODIGA_INDENT]", 6, 4, false, ExpectedResult = "\t\t\t")]
		public string ReplaceIndentation_should_replace_codiga_indention_with_tabs(string input, int indentSize, int tabSize, bool useSpace)
		{
			// arrange
			var builder = new StringBuilder(input);
			var settings = new IndentationSettings(indentSize, tabSize, useSpace);

			// act
			SnippetParser.ReplaceIndentation(builder, settings);

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
			var snippet = item.Properties.GetProperty<VisualStudioSnippet>(nameof(VisualStudioSnippet.CodeSnippet.Snippet));
			Assert.NotNull(snippet);
		}

		#region FromCodigaSnippet
		
		[Test]
        public void FromCodigaSnippet_should_convert_to_VisualStudioSnippet_with_two_user_variables()
        {
            // arrange
            var snippet = new CodigaSnippet
            {
				Id = 99,
	            Shortcut = "nunittest",
				Name = "NUnit Test",
				Description = "Creates NUnit Test",
				Language = "Csharp",
				Owner = new Owner { Id = 1, DisplayName = "The Owner"},
				Keywords = new string[] {"Add", "NUnit", "Test" },
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
            var vsSnippet = SnippetParser.FromCodigaSnippet(snippet, new IndentationSettings(4, 4, false));

			// assert
			Assert.That(vsSnippet.CodeSnippet.Header.Id, Is.EqualTo(99));
			Assert.That(vsSnippet.CodeSnippet.Header.Shortcut, Is.EqualTo("nunittest"));
			Assert.That(vsSnippet.CodeSnippet.Header.Title, Is.EqualTo("NUnit Test"));
			Assert.That(vsSnippet.CodeSnippet.Header.Description, Is.EqualTo("Creates NUnit Test"));
			Assert.That(vsSnippet.CodeSnippet.Header.Author, Is.EqualTo("The Owner"));

			Assert.That(vsSnippet.CodeSnippet.Header.Keywords, Has.Exactly(3).Items);
			var keyword1 = vsSnippet.CodeSnippet.Header.Keywords[0];
			var keyword2 = vsSnippet.CodeSnippet.Header.Keywords[1];
			var keyword3 = vsSnippet.CodeSnippet.Header.Keywords[2];

			Assert.That(keyword1.Text, Is.EqualTo("Add"));
			Assert.That(keyword2.Text, Is.EqualTo("NUnit"));
			Assert.That(keyword3.Text, Is.EqualTo("Test"));

			Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(2).Items);
			var param1 = vsSnippet.CodeSnippet.Snippet.Declarations.First();
			var param2 = vsSnippet.CodeSnippet.Snippet.Declarations.Last();
			Assert.That(param1.ID, Is.EqualTo("param1"));
			Assert.That(param1.Default, Is.EqualTo("Test_method"));
			Assert.That(param2.ID, Is.EqualTo("param2"));
			Assert.That(param2.Default, Is.EqualTo("act"));

			Assert.That(vsSnippet.CodeSnippet.Snippet.Code.Language, Is.EqualTo("Csharp"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam1ß"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam2ß"));
        }

		[Test]
		public void FromCodigaSnippet_should_handle_unknown_owner()
		{
			// arrange
			var snippet = new CodigaSnippet
			{
				Id = 99,
				Shortcut = "nunittest",
				Name = "NUnit Test",
				Description = "Creates NUnit Test",
				Language = "Csharp",
				Owner = null,
				Keywords = new string[] { "Add", "NUnit", "Test" },
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
			var vsSnippet = SnippetParser.FromCodigaSnippet(snippet, new IndentationSettings(4, 4, false));

			// assert
			Assert.That(vsSnippet.CodeSnippet.Header.Id, Is.EqualTo(99));
			Assert.That(vsSnippet.CodeSnippet.Header.Shortcut, Is.EqualTo("nunittest"));
			Assert.That(vsSnippet.CodeSnippet.Header.Title, Is.EqualTo("NUnit Test"));
			Assert.That(vsSnippet.CodeSnippet.Header.Description, Is.EqualTo("Creates NUnit Test"));
			Assert.That(vsSnippet.CodeSnippet.Header.Author, Is.Null);

			Assert.That(vsSnippet.CodeSnippet.Header.Keywords, Has.Exactly(3).Items);
			var keyword1 = vsSnippet.CodeSnippet.Header.Keywords[0];
			var keyword2 = vsSnippet.CodeSnippet.Header.Keywords[1];
			var keyword3 = vsSnippet.CodeSnippet.Header.Keywords[2];

			Assert.That(keyword1.Text, Is.EqualTo("Add"));
			Assert.That(keyword2.Text, Is.EqualTo("NUnit"));
			Assert.That(keyword3.Text, Is.EqualTo("Test"));

			Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(2).Items);
			var param1 = vsSnippet.CodeSnippet.Snippet.Declarations.First();
			var param2 = vsSnippet.CodeSnippet.Snippet.Declarations.Last();
			Assert.That(param1.ID, Is.EqualTo("param1"));
			Assert.That(param1.Default, Is.EqualTo("Test_method"));
			Assert.That(param2.ID, Is.EqualTo("param2"));
			Assert.That(param2.Default, Is.EqualTo("act"));

			Assert.That(vsSnippet.CodeSnippet.Snippet.Code.Language, Is.EqualTo("Csharp"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam1ß"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam2ß"));
		}

		[Test]
		public void FromCodigaSnippet_should_work_with_python_snippet()
		{

			// arrange
			var snippet = new CodigaSnippet
			{
				Id = 99,
				Shortcut = "csv.file.read",
				Name = "read csv file",
				Description = "Read a CSV file in Python",
				Language = "Python",
				Keywords = new string[] { "read", "csv", "file" },
				Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(@"
					import csv

					with open(&[USER_INPUT:1:file_name], mode ='r', encoding='utf-8') as file:
						# reading the CSV file
						csvFile = csv.DictReader(file)
						# displaying the contents of the CSV file
						for line in csvFile:
						&[USER_INPUT:0]"))
			};

			// act
			var vsSnippet = SnippetParser.FromCodigaSnippet(snippet, new IndentationSettings(4, 4, false));

			// assert
			Assert.That(vsSnippet.CodeSnippet.Header.Id, Is.EqualTo(99));
			Assert.That(vsSnippet.CodeSnippet.Header.Shortcut, Is.EqualTo("csv.file.read"));
			Assert.That(vsSnippet.CodeSnippet.Header.Title, Is.EqualTo("read csv file"));
			Assert.That(vsSnippet.CodeSnippet.Header.Description, Is.EqualTo("Read a CSV file in Python"));
			Assert.That(vsSnippet.CodeSnippet.Header.Author, Is.Null);

			Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(1).Items);
			var param1 = vsSnippet.CodeSnippet.Snippet.Declarations.First();

			Assert.That(param1.ID, Is.EqualTo("param1"));
			Assert.That(param1.Default, Is.EqualTo("file_name"));

			Assert.That(vsSnippet.CodeSnippet.Snippet.Code.Language, Is.EqualTo("Python"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam1ß"));
			Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßendß"));
		}

		[Test]
		public void FromCodigaSnippet_should_work_with_snippet_including_original_delimiter()
		{
			// arrange
			var snippet = new CodigaSnippet
			{
				Id = 89,
				Shortcut = "console.log.multiple",
				Name = "console log",
				Description = "Console Log Value/Name (Multiple)",
				Language = "JavaScript",
				Keywords = new [] { "console", "log", "multiple" },
				Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(
					"console.log(`${&[USER_INPUT:1:value1]}: &[USER_INPUT:1:name1], ${&[USER_INPUT:2:value2]}: &[USER_INPUT:2:name2], &[USER_INPUT:3]`);"))
			};

			// act
			var vsSnippet = SnippetParser.FromCodigaSnippet(snippet, new IndentationSettings(4, 4, false));

			// assert
			Assert.Multiple(() =>
			{
				Assert.That(vsSnippet.CodeSnippet.Header.Id, Is.EqualTo(89));
				Assert.That(vsSnippet.CodeSnippet.Header.Shortcut, Is.EqualTo("console.log.multiple"));
				Assert.That(vsSnippet.CodeSnippet.Header.Title, Is.EqualTo("console log"));
				Assert.That(vsSnippet.CodeSnippet.Header.Description, Is.EqualTo("Console Log Value/Name (Multiple)"));
				Assert.That(vsSnippet.CodeSnippet.Header.Author, Is.Null);
			});
			
			Assert.That(vsSnippet.CodeSnippet.Snippet.Declarations, Has.Exactly(2).Items);

			Assert.Multiple(() =>
			{
				var param1 = vsSnippet.CodeSnippet.Snippet.Declarations.First();
				Assert.That(param1.ID, Is.EqualTo("param1"));
				Assert.That(param1.Default, Is.EqualTo("value1"));
				
				var param2 = vsSnippet.CodeSnippet.Snippet.Declarations[1];
				Assert.That(param2.ID, Is.EqualTo("param2"));
				Assert.That(param2.Default, Is.EqualTo("value2"));

				Assert.That(vsSnippet.CodeSnippet.Snippet.Code.Language, Is.EqualTo("JavaScript"));
				Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam1ß"));
				Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßparam2ß"));
				Assert.True(vsSnippet.CodeSnippet.Snippet.Code.CodeString.Contains("ßendß"));				
			});
		}
		
		#endregion

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

		[Test]
		public void GetPreviewCode_should_replace_all_vs_literals()
		{
			// act
			var preview = SnippetParser.GetPreviewCode(SnippetTestData.Snippet);

			// assert
			Assert.That(preview, Is.EqualTo("MessageBox.Show(\"first\");     MessageBox.Show(\"second\");"));
		}
		
		[Test]
		public void GetPreviewCode_should_replace_all_vs_literals_including_delimiters()
		{
			// act
			var preview = SnippetParser.GetPreviewCode(SnippetTestData.SnippetWithOriginalDelimiters);

			// assert
			Assert.That(preview, Is.EqualTo("console.log(`${first}: first, ${second}: second, `);"));
		}

		[Test]
		[TestCase("using System;", ExpectedResult = "System.dll")]
		[TestCase("using System.Collections.Generic;", ExpectedResult = "System.Collections.Generic.dll")]
		[TestCase("using NUnit.Framework;", ExpectedResult = "NUnit.Framework.dll")]
		public string GetClrReference_should_parse_to_assembly_name(string usingStatement)
		{
			// arrange
			var collection = new ReadOnlyCollection<string>(new[] { usingStatement });

			// act
			var references = SnippetParser.GetClrReference(collection);

			// assert
			Assert.That(references.Count, Is.EqualTo(1));

			return references.Single().Assembly;
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
						References = new List<Reference> { new Reference { Assembly = "System.Windows.Forms.dll" } },
						Code = new Code("CSharp", "MessageBox.Show(\"ßparam1ß\");     MessageBox.Show(\"ßparam2ß\");", "ß")
					}
				}
			};
			
			public static VisualStudioSnippet SnippetWithOriginalDelimiters => new VisualStudioSnippet
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
						References = new List<Reference> { new Reference { Assembly = "System.Windows.Forms.dll" } },
						Code = new Code("JavaScript", "console.log(`${ßparam1ß}: ßparam1ß, ${ßparam2ß}: ßparam2ß, ßendß`);", "ß")
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
												"                <![CDATA[MessageBox.Show(\"ßparam1ß\");" +
												"     MessageBox.Show(\"ßparam2ß\");]]>" +
												"            </Code>" +
												"        </Snippet>" +
												"    </CodeSnippet>" +
												"</CodeSnippets>";
		}
	}
}

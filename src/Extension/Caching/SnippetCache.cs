using Extension.SnippetFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQLClient;

namespace Extension.Caching
{

	interface ISnippetCache
	{
		//public IEnumerable<VisualStudioSnippet> GetSnippets(LanguageEnumeration language);
		public IEnumerable<VisualStudioSnippet> GetSnippets(LanguageEnumeration language);
		public IEnumerable<VisualStudioSnippet> GetSnippets();

	}

    [Export]
    internal class SnippetCache
    {
        public List<VisualStudioSnippet> CodigaSnippets { get; } = new List<VisualStudioSnippet>
        {
            SnippetUtil.FromCodigaSnippet(new CodigaSnippet("nunittest",@"[Test]
                                        public void &[USER_INPUT:1:Test]()
                                        {
                                            // arrange
  
                                            // act
                                            &[USER_INPUT:2:act]
    
                                            // assert
                                        }")),

			SnippetUtil.FromCodigaSnippet(new CodigaSnippet("do",@"do
                                    {
                                        &[USER_INPUT:0]
                                    }
                                    while (&[USER_INPUT:1:true]);")),

			SnippetUtil.FromCodigaSnippet((new CodigaSnippet("if",@"if (&[USER_INPUT:1:true])
                                    {
                                        &[USER_INPUT:0]
                                    }")))
        };
    }
}

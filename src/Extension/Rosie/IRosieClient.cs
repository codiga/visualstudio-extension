using System.Collections.Generic;
using System.Threading.Tasks;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text;

namespace Extension.Rosie
{
    /// <summary>
    /// API for retrieving Rosie specific information from the Codiga API.
    /// </summary>
    public interface IRosieClient
    {
        /// <summary>
        /// Returns the annotations from the Codiga API based on the argument <c>textBuffer</c>'s content,
        /// based on which code annotation is applied in the provider text buffer.
        /// </summary>
        /// <param name="textBuffer">contains the file content to query Rosie information for</param>
        /// <returns>The list of violation information from Rosie</returns>
        Task<IList<RosieAnnotation>> GetAnnotations(ITextBuffer textBuffer);
    }
}

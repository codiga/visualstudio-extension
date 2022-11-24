using System.Collections.Generic;
using System.Threading.Tasks;
using Extension.Rosie.Model;

namespace Extension.Rosie
{
    /// <summary>
    /// Service for retrieving Rosie specific information from the Codiga API.
    /// </summary>
    public interface IRosieClient
    {
        /// <summary>
        /// Returns the annotations from the Codiga API based on the argument file, based on which code annotation
        /// will be applied in the currently selected and active editor.
        /// </summary>
        /// <param name="file">the file to query Rosie information for</param>
        /// <returns></returns>
        Task<IList<RosieAnnotation>> GetAnnotations(string file);
    }
}

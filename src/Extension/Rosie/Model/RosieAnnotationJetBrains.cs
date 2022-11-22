using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// An updated version of <see cref="RosieAnnotation"/>. It stores the start and end offsets based
    /// on the current Editor. This is used in <see cref="RosieAnnotator"/>}
    /// to provide the annotation information.
    /// </summary>
    /// <seealso cref="RosieAnnotation"/>
    public class RosieAnnotationJetBrains
    {
        public string RulesetName { get; }
        public string RuleName { get; }
        public string Message { get; }
        public string Severity { get; }
        public string Category { get; }
        public int Start { get; }
        public int End { get; }
        public IReadOnlyList<RosieViolationFix> Fixes { get; }
        
        public RosieAnnotationJetBrains(RosieAnnotation annotation, ITextBuffer textBuffer) {
            RulesetName = annotation.RulesetName;
            RuleName = annotation.RuleName;
            Message = annotation.Message;
            Severity = annotation.Severity;
            Category = annotation.Category;
            Start = annotation.Start.GetOffset(textBuffer);
            End = annotation.End.GetOffset(textBuffer);
            Fixes = new List<RosieViolationFix>(annotation.Fixes);
        }
    }
}
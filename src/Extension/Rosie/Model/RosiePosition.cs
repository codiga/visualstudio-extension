using Microsoft.VisualStudio.Text;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Represents a position in an editor by its line number and its column number within that line.
    /// </summary>
    public class RosiePosition
    {
        /// <summary>
        /// The line returned from Codiga is 1-based, while the VS ITextBuffer is 0-based.
        /// </summary>
        public int Line { get; set; }
        public int Col { get; set; }

        /// <summary>
        /// Returns the position offset within the Document of the argument Editor.
        /// </summary>
        /// <param name="textBuffer">the editor in which the offset is calculated</param>
        public int GetOffset(ITextBuffer textBuffer)
        {
            return textBuffer.CurrentSnapshot.GetLineFromLineNumber(AdjustOffset(Line)).Start.Position + AdjustOffset(Col);
        }

        /// <summary>
        /// Adjusts the column offset by -1 since the column index returned by Codiga is 1-based, while the IDE editor is 0-based.
        /// <br/>
        /// It doesn't adjust the offset if it is 0, so at the beginning of a line. 
        /// </summary>
        /// <param name="offset">the offset to adjust</param>
        private static int AdjustOffset(int offset)
        {
            return offset != 0 ? offset - 1 : offset;
        }
        
        public override bool Equals(object obj)
        {
            return obj is RosiePosition position &&
                   Line == position.Line &&
                   Col == position.Col;
        }

        public override int GetHashCode()
        {
            int hashCode = -611629200;
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Col.GetHashCode();
            return hashCode;
        }
    }
}
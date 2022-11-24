using Microsoft.VisualStudio.Text;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Represents a position in an editor by its line number and its column number within that line.
    /// </summary>
    public class RosiePosition
    {
        public int Line { get; set; }
        public int Col { get; set; }

        /// <summary>
        /// Returns the position offset within the Document of the argument Editor.
        /// </summary>
        /// <param name="textBuffer">the editor in which the offset is calculated</param>
        /// <returns></returns>
        public int GetOffset(ITextBuffer textBuffer)
        {
            return textBuffer.CurrentSnapshot.GetLineFromLineNumber(Line - 1).Start.Position + AdjustColumnOffset(Col);
        }

         /// <summary>
         /// Adjusts the column offset by -1 since the column index returned by Codiga is 1-based, while the IDE editor is 0-based.
         ///
         /// It doesn't adjust the offset if it is 0, so at the beginning of a line. 
         /// </summary>
         /// <param name="columnOffset">the offset to adjust</param>
         /// <returns></returns>
        private int AdjustColumnOffset(int columnOffset)
        {
            return columnOffset != 0 ? (columnOffset - 1) : columnOffset;
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
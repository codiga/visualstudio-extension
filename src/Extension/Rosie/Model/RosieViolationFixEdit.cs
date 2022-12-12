using System.Collections.Generic;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Represents a single quick fix edit in an Editor.
    /// </summary>
    public class RosieViolationFixEdit
    {
        /// <summary>
        /// The position of the edit from where the fix will begin.
        /// </summary>
        public RosiePosition? Start { get; set; }

        /// <summary>
        /// The position of the edit at where the fix will end.
        /// </summary>
        public RosiePosition? End { get; set; }

        /// <summary>
        /// Content for string insertion and replacement in the editor. Not used for removal edits.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The type of edit to apply. See constants in <see cref="RosieEditTypes"/>.
        /// </summary>
        public string EditType { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RosieViolationFixEdit edit &&
                   EqualityComparer<RosiePosition>.Default.Equals(Start, edit.Start) &&
                   EqualityComparer<RosiePosition>.Default.Equals(End, edit.End) &&
                   Content == edit.Content &&
                   EditType == edit.EditType;
        }

        public override int GetHashCode()
        {
            int hashCode = -2132825764;
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(Start);
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(End);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Content);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EditType);
            return hashCode;
        }
    }
}
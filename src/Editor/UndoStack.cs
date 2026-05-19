/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace NEdit.Editor
{
    /// <summary>
    /// Captures a document snapshot before and after an undoable edit.
    /// </summary>
    /// <param name="BeforeLines">The document lines before the edit.</param>
    /// <param name="BeforeCursor">The cursor position before the edit.</param>
    /// <param name="AfterLines">The document lines after the edit.</param>
    /// <param name="AfterCursor">The cursor position after the edit.</param>
    internal sealed record UndoRecord(string[] BeforeLines, Position BeforeCursor, string[] AfterLines, Position AfterCursor);

    /// <summary>
    /// Manages bounded undo and redo history for editor edits.
    /// </summary>
    internal sealed class UndoStack
    {
        private readonly List<UndoRecord> _undo = [];
        private readonly List<UndoRecord> _redo = [];
        private const int Limit = 200;

        /// <summary>
        /// Gets a value that indicates whether an undo record is available.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if an edit can be undone; otherwise, <see langword="false" />.
        /// </value>
        public bool CanUndo => _undo.Count > 0;

        /// <summary>
        /// Gets a value that indicates whether a redo record is available.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if an edit can be redone; otherwise, <see langword="false" />.
        /// </value>
        public bool CanRedo => _redo.Count > 0;

        /// <summary>
        /// Clears all undo and redo history.
        /// </summary>
        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        /// <summary>
        /// Adds an undo record and clears redo history.
        /// </summary>
        /// <param name="record">The undo record to add.</param>
        /// <returns>
        /// <see langword="true" /> if the record changed document state and was stored; otherwise, <see langword="false" />.
        /// </returns>
        public bool Push(UndoRecord record)
        {
            if (Same(record.BeforeLines, record.AfterLines) && record.BeforeCursor == record.AfterCursor)
            {
                return false;
            }

            _undo.Add(record);
            _redo.Clear();

            while (_undo.Count > Limit)
            {
                _undo.RemoveAt(0);
            }

            return true;
        }

        /// <summary>
        /// Removes and returns the latest undo record.
        /// </summary>
        /// <returns>
        /// The latest undo record, or <see langword="null" /> when no undo record is available.
        /// </returns>
        public UndoRecord? PopUndo()
        {
            if (_undo.Count == 0)
            {
                return null;
            }

            UndoRecord record = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            _redo.Add(record);
            return record;
        }

        /// <summary>
        /// Removes and returns the latest redo record.
        /// </summary>
        /// <returns>
        /// The latest redo record, or <see langword="null" /> when no redo record is available.
        /// </returns>
        public UndoRecord? PopRedo()
        {
            if (_redo.Count == 0)
            {
                return null;
            }

            UndoRecord record = _redo[^1];
            _redo.RemoveAt(_redo.Count - 1);
            _undo.Add(record);
            return record;
        }

        private static bool Same(string[] left, string[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

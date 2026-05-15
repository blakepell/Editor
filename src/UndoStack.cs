/*
 * @project  : ApexGate Editor
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

namespace Editor
{
    internal sealed record UndoRecord(string[] BeforeLines, Position BeforeCursor, string[] AfterLines, Position AfterCursor);

    internal sealed class UndoStack
    {
        private readonly List<UndoRecord> _undo = [];
        private readonly List<UndoRecord> _redo = [];
        private const int Limit = 200;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

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

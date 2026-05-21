namespace NEdit.Editor
{
    /// <summary>
    /// Stores the key combination, file path, and line number for an editor bookmark.
    /// </summary>
    public readonly record struct Bookmark(string KeyCombination, string FilePath, int LineNumber);
}

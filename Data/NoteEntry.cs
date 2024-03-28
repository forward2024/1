public class NoteEntry
{
    public Note Note { get; set; }
    public int Occurrences { get; set; }

    public NoteEntry(Note note, int occurrences)
    {
        Note = note;
        Occurrences = occurrences;
    }
}

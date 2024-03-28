public class NoteWeight
{
    public Note Note { get; set; }
    public int Weight { get; set; }

    public NoteWeight(Note note, int initialWeight)
    {
        Note = note;
        Weight = initialWeight;
    }
}

public class SearchService
{
    public List<Note> Search(string query)
    {
        var searchTerms = query.ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3)
            .Distinct();

        var notesWeights = new Dictionary<Guid, NoteWeight>();

        foreach (var term in searchTerms)
        {
            if (NoteManager.keywordsDictionary.ContainsKey(term))
            {
                foreach (var entry in NoteManager.keywordsDictionary[term])
                {
                    if (!notesWeights.ContainsKey(entry.Note.Id))
                    {
                        notesWeights[entry.Note.Id] = new NoteWeight(entry.Note, 0);
                    }
                    notesWeights[entry.Note.Id].Weight += entry.Occurrences;
                }
            }
        }

        var sortedNotes = notesWeights.Values
            .OrderByDescending(nw => nw.Weight)
            .Select(nw => nw.Note)
            .ToList();

        return sortedNotes;
    }
}
public class NoteManager
{
    private readonly IDbContextFactory<DBContext> context;
    public List<Note> Current { get; private set; } = new List<Note>();
    ConcurrentQueue<Note> Adds = new ConcurrentQueue<Note>();
    ConcurrentQueue<Note> Edits = new ConcurrentQueue<Note>();
    ConcurrentQueue<Guid> Removes = new ConcurrentQueue<Guid>();

    public NoteManager(IDbContextFactory<DBContext> context)
    {
        this.context = context;
        Task.Run(async () => await GetNotesAsync());
    }

    public void Add(Note note) => Adds.Enqueue(note);
    public void Edit(Note note) => Edits.Enqueue(note);
    public void Remove(Guid id) => Removes.Enqueue(id);

    public async Task GetNotesAsync()
    {
        using (var context = this.context.CreateDbContext())
        {
            Current = await context.Notes.OrderBy(x => x.DateCreated).ToListAsync();
            foreach (var note in Current)
            {
                AddNoteToDictionary(note);
            }
        }
    }

    public async Task RefreshDataAsync()
    {
        using (var dbContext = this.context.CreateDbContext())
        {
            await AddNotesAsync(dbContext);
            await EditNotesAsync(dbContext);
            await RemoveNotesAsync(dbContext);
        }
    }

    private async Task AddNotesAsync(DBContext dbContext)
    {
        using (var transaction = await dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Note note;
                while (Adds.TryDequeue(out note))
                {
                    await dbContext.Notes.AddAsync(note);
                    AddNoteToDictionary(note);
                }
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }
    }

    private async Task EditNotesAsync(DBContext dbContext)
    {
        using (var transaction = await dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Note note;
                while (Edits.TryDequeue(out note))
                {
                    var existingNote = await dbContext.Notes.FindAsync(note.Id);
                    if (existingNote != null)
                    {
                        RemoveNoteFromDictionary(existingNote);
                        dbContext.Entry(existingNote).CurrentValues.SetValues(note);
                        AddNoteToDictionary(note);
                    }
                }
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }
    }

    private async Task RemoveNotesAsync(DBContext dbContext)
    {
        using (var transaction = await dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Guid noteId;
                while (Removes.TryDequeue(out noteId))
                {
                    var existingNote = await dbContext.Notes.FindAsync(noteId);
                    if (existingNote != null)
                    {
                        dbContext.Notes.Remove(existingNote);
                        RemoveNoteFromDictionary(existingNote);
                    }
                }
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }
    }

    static public Dictionary<string, List<NoteEntry>> keywordsDictionary = new();

    public void AddNoteToDictionary(Note note)
    {
        var keywords = GetWords(note.Title ?? string.Empty)
                       .Concat(GetWords(note.Content ?? string.Empty))
                       .Where(word => word.Length >= 3)
                       .Distinct();

        foreach (var keyword in keywords)
        {
            int occurrences = CountKeywordOccurrences(note, keyword);

            if (!keywordsDictionary.ContainsKey(keyword))
            {
                keywordsDictionary[keyword] = new List<NoteEntry>();
            }

            var list = keywordsDictionary[keyword];
            var index = FindInsertionIndex(list, occurrences);
            list.Insert(index, new NoteEntry(note, occurrences));
        }
    }

    public IEnumerable<string> GetWords(string text) => text.ToLowerInvariant().Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

    private int CountKeywordOccurrences(Note note, string keyword)
    {
        var titleWords = GetWords(note.Title ?? string.Empty);
        var contentWords = GetWords(note.Content ?? string.Empty);

        return titleWords.Concat(contentWords).Count(word => string.Equals(word, keyword, StringComparison.OrdinalIgnoreCase));
    }

    private void RemoveNoteFromDictionary(Note note)
    {
        var keywords = GetWords(note.Title ?? string.Empty)
                       .Concat(GetWords(note.Content ?? string.Empty))
                       .Where(word => word.Length >= 3)
                       .Distinct();

        foreach (var keyword in keywords)
        {
            if (keywordsDictionary.ContainsKey(keyword))
            {
                var entries = keywordsDictionary[keyword];
                bool removed = entries.RemoveAll(e => e.Note.Id == note.Id) > 0;

                if (removed && !entries.Any())
                {
                    keywordsDictionary.Remove(keyword);
                }
            }
        }
    }

    public int FindInsertionIndex(List<NoteEntry> list, int targetOccurrences)
    {
        int left = 0;
        int right = list.Count;

        while (left < right)
        {
            int mid = left + (right - left) / 2;
            int currentOccurrences = list[mid].Occurrences;

            if (currentOccurrences > targetOccurrences)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }
}
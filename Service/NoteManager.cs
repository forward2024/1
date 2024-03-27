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
                        dbContext.Entry(existingNote).CurrentValues.SetValues(note);
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
}
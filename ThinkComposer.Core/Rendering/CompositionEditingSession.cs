namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed class CompositionEditingSession
{
    private readonly Stack<CompositionViewSnapshot> _undo = new();
    private readonly Stack<CompositionViewSnapshot> _redo = new();

    public CompositionEditingSession(CompositionViewSnapshot initialSnapshot)
    {
        CurrentSnapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
    }

    public CompositionViewSnapshot CurrentSnapshot { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public CompositionViewSnapshot Apply(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (ReferenceEquals(snapshot, CurrentSnapshot))
        {
            return CurrentSnapshot;
        }

        _undo.Push(CurrentSnapshot);
        _redo.Clear();
        CurrentSnapshot = snapshot;
        return CurrentSnapshot;
    }

    public CompositionViewSnapshot Undo()
    {
        if (!CanUndo)
        {
            return CurrentSnapshot;
        }

        _redo.Push(CurrentSnapshot);
        CurrentSnapshot = _undo.Pop();
        return CurrentSnapshot;
    }

    public CompositionViewSnapshot Redo()
    {
        if (!CanRedo)
        {
            return CurrentSnapshot;
        }

        _undo.Push(CurrentSnapshot);
        CurrentSnapshot = _redo.Pop();
        return CurrentSnapshot;
    }
}

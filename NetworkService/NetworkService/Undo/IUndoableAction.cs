namespace NetworkService.Undo
{
    
    public interface IUndoableAction
    {
        
        string Description { get; }

        
        void Undo();
    }
}

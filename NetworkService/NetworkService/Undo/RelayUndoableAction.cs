using System;

namespace NetworkService.Undo
{
   
    public class RelayUndoableAction : IUndoableAction
    {
        private readonly Action _undo;

        public string Description { get; }

        public RelayUndoableAction(string description, Action undo)
        {
            Description = description;
            _undo = undo;
        }

        public void Undo()
        {
            _undo?.Invoke();
        }
    }
}

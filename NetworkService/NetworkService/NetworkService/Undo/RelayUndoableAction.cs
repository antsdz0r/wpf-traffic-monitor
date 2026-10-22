using System;

namespace NetworkService.Undo
{
    /// <summary>
    /// Univerzalna implementacija IUndoableAction-a koja prima opis i delegat
    /// (Action) koji opisuje kako se akcija ponistava. Time izbegavamo pisanje
    /// posebne klase za svaku vrstu akcije, a kod ostaje jednostavan za odbranu.
    /// </summary>
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

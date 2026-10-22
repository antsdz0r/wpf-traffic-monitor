using System.Collections.ObjectModel;
using NetworkService.Model;
using NetworkService.Undo;

namespace NetworkService.Services
{
   
    public interface IShellContext
    {
        ObservableCollection<Entity> Entities { get; }
        ObservableCollection<EntityType> Types { get; }

        
        void AddUndoableAction(IUndoableAction action);

       
        int GetNextAvailableId();

        
        void RestartSimulator();

        
        void OnMeasurementReceived(Entity entity);
    }
}

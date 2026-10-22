using System.Collections.ObjectModel;
using NetworkService.Model;
using NetworkService.Undo;

namespace NetworkService.Services
{
    /// <summary>
    /// Ugovor (interfejs) preko kojeg pod-ViewModel-i (Entities, Display, Graph)
    /// pristupaju zajednickim podacima i funkcijama glavnog ViewModel-a (shell-a),
    /// bez direktne zavisnosti od njegove konkretne klase.
    /// </summary>
    public interface IShellContext
    {
        ObservableCollection<Entity> Entities { get; }
        ObservableCollection<EntityType> Types { get; }

        // Registruje akciju u globalnu istoriju (za Undo i History paletu).
        void AddUndoableAction(IUndoableAction action);

        // Vraca sledeci slobodan jedinstveni Id za novi entitet.
        int GetNextAvailableId();

        // Automatski restartuje MeteringSimulator nakon dodavanja/brisanja entiteta.
        void RestartSimulator();

        // Obavestava Display i Graph poglede da je stigla nova vrednost za entitet.
        void OnMeasurementReceived(Entity entity);
    }
}

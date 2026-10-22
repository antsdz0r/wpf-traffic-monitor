using System.Collections.ObjectModel;
using NetworkService.Helpers;

namespace NetworkService.Model
{
    /// <summary>
    /// Grupa entiteta jednog tipa za prikaz u TreeView kontroli
    /// (npr. "IA putevi", "IB putevi"). Sadrzi samo entitete koji NISU
    /// trenutno postavljeni na Drag&Drop mrezu.
    /// </summary>
    public class TypeGroup : BindableBase
    {
        public string Header { get; set; }
        public ObservableCollection<Entity> Items { get; } = new ObservableCollection<Entity>();

        public TypeGroup(string header)
        {
            Header = header;
        }
    }
}

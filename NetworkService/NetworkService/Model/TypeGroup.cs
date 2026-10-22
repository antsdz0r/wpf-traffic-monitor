using System.Collections.ObjectModel;
using NetworkService.Helpers;

namespace NetworkService.Model
{
    
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

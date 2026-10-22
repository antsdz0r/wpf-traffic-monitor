using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NetworkService.Helpers;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.Undo;

namespace NetworkService.ViewModel
{
    /// <summary>
    /// Network Entities View: tabelarni prikaz entiteta, dodavanje i brisanje,
    /// pretraga (P1) i filtriranje (CG4 - van/unutar opsega + dodatni filter po tipu/Id-u).
    /// Validacija polja je implementirana preko IDataErrorInfo (poruke ispod polja).
    /// </summary>
    public class NetworkEntitiesViewModel : BindableBase, IDataErrorInfo
    {
        private readonly IShellContext _shell;

        public IEnumerable<Entity> Entities => _shell.Entities;
        public IEnumerable<EntityType> Types => _shell.Types;

        // Filtrirani pogled nad zajednickom kolekcijom entiteta (za DataGrid).
        public ICollectionView EntitiesView { get; }

        // ---------------- FORMA ZA DODAVANJE ----------------
        private string _newIdText = string.Empty;
        public string NewIdText
        {
            get { return _newIdText; }
            set { SetProperty(ref _newIdText, value); AddCommand.RaiseCanExecuteChanged(); }
        }

        private string _newName = string.Empty;
        public string NewName
        {
            get { return _newName; }
            set { SetProperty(ref _newName, value); AddCommand.RaiseCanExecuteChanged(); }
        }

        private EntityType _newType;
        public EntityType NewType
        {
            get { return _newType; }
            set { SetProperty(ref _newType, value); AddCommand.RaiseCanExecuteChanged(); }
        }

        private Entity _selectedEntity;
        public Entity SelectedEntity
        {
            get { return _selectedEntity; }
            set { SetProperty(ref _selectedEntity, value); DeleteCommand.RaiseCanExecuteChanged(); }
        }

        // ---------------- PRETRAGA (P1) ----------------
        private bool _searchByName = true;
        public bool SearchByName
        {
            get { return _searchByName; }
            set { if (SetProperty(ref _searchByName, value)) EntitiesView.Refresh(); }
        }

        private bool _searchByType;
        public bool SearchByType
        {
            get { return _searchByType; }
            set { if (SetProperty(ref _searchByType, value)) EntitiesView.Refresh(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get { return _searchText; }
            set { if (SetProperty(ref _searchText, value)) EntitiesView.Refresh(); }
        }

        // ---------------- FILTER PO TIPU / ID-U (P2, prema wireframe-u) ----------------
        private EntityType _filterType;
        public EntityType FilterType
        {
            get { return _filterType; }
            set { SetProperty(ref _filterType, value); }
        }

        private bool _idLess;
        public bool IdLess { get { return _idLess; } set { SetProperty(ref _idLess, value); } }

        private bool _idGreater;
        public bool IdGreater { get { return _idGreater; } set { SetProperty(ref _idGreater, value); } }

        private bool _idEqual;
        public bool IdEqual { get { return _idEqual; } set { SetProperty(ref _idEqual, value); } }

        private string _idFilterValue = string.Empty;
        public string IdFilterValue
        {
            get { return _idFilterValue; }
            set { SetProperty(ref _idFilterValue, value); }
        }

        // ---------------- CG4 DODATNI FILTER: van / unutar opsega ----------------
        private bool _showOutOfRange;
        public bool ShowOutOfRange
        {
            get { return _showOutOfRange; }
            set { SetProperty(ref _showOutOfRange, value); }
        }

        private bool _showInRange;
        public bool ShowInRange
        {
            get { return _showInRange; }
            set { SetProperty(ref _showInRange, value); }
        }

        // Da li je filter trenutno primenjen (dugme Primeni). Pretraga radi "uzivo".
        private bool _filterApplied;

        // ---------------- KOMANDE ----------------
        public MyICommand AddCommand { get; }
        public MyICommand CancelAddCommand { get; }
        public MyICommand DeleteCommand { get; }
        public MyICommand ApplyFilterCommand { get; }
        public MyICommand ResetFilterCommand { get; }
        public MyICommand ResetSearchCommand { get; }

        // ---- Potvrda brisanja (custom overlay umesto MessageBox-a) ----
        private bool _isConfirmDeleteVisible;
        public bool IsConfirmDeleteVisible
        {
            get { return _isConfirmDeleteVisible; }
            set { SetProperty(ref _isConfirmDeleteVisible, value); }
        }

        private string _confirmDeleteMessage;
        public string ConfirmDeleteMessage
        {
            get { return _confirmDeleteMessage; }
            set { SetProperty(ref _confirmDeleteMessage, value); }
        }

        public MyICommand ConfirmDeleteCommand { get; }
        public MyICommand CancelDeleteCommand { get; }

        public NetworkEntitiesViewModel(IShellContext shell)
        {
            _shell = shell;

            // Sopstveni pogled (ListCollectionView) da filter na tabeli ne bi uticao
            // na podrazumevani pogled koji koriste druge kontrole (npr. ComboBox u grafu).
            EntitiesView = new ListCollectionView(_shell.Entities);
            EntitiesView.Filter = FilterPredicate;

            AddCommand = new MyICommand(AddEntity, CanAddEntity);
            CancelAddCommand = new MyICommand(ClearForm);
            DeleteCommand = new MyICommand(RequestDelete, () => SelectedEntity != null);
            ApplyFilterCommand = new MyICommand(ApplyFilter);
            ResetFilterCommand = new MyICommand(ResetFilter);
            ResetSearchCommand = new MyICommand(ResetSearch);

            ConfirmDeleteCommand = new MyICommand(ConfirmDelete);
            CancelDeleteCommand = new MyICommand(() => IsConfirmDeleteVisible = false);
        }

        // ===================== DODAVANJE =====================
        private bool CanAddEntity()
        {
            // Dugme Add je aktivno samo ako su sva polja validna.
            return string.IsNullOrEmpty(this[nameof(NewIdText)])
                && string.IsNullOrEmpty(this[nameof(NewName)])
                && string.IsNullOrEmpty(this[nameof(NewType)]);
        }

        private void AddEntity()
        {
            int id = int.Parse(NewIdText);
            var entity = new Entity(id, NewName.Trim(), NewType);
            _shell.Entities.Add(entity);

            // Undo: ponistavanje dodavanja = uklanjanje tog entiteta.
            _shell.AddUndoableAction(new RelayUndoableAction(
                "Dodat entitet: " + entity.Name + " (ID: " + entity.Id + ")",
                () =>
                {
                    if (_shell.Entities.Contains(entity))
                    {
                        _shell.Entities.Remove(entity);
                        _shell.RestartSimulator();
                    }
                }));

            NotificationService.Instance.Show(ToastType.Success, "Entitet dodat",
                "Uspesno dodat entitet \"" + entity.Name + "\" (ID: " + entity.Id + ", tip: " + entity.Type.Name + ").");

            ClearForm();
            _shell.RestartSimulator(); // automatski restart simulatora nakon dodavanja
        }

        private void ClearForm()
        {
            NewIdText = string.Empty;
            NewName = string.Empty;
            NewType = null;
        }

        // ===================== BRISANJE (sa potvrdom) =====================
        private void RequestDelete()
        {
            if (SelectedEntity == null)
            {
                return;
            }

            ConfirmDeleteMessage = "Da li ste sigurni da zelite da obrisete entitet \""
                + SelectedEntity.Name + "\" (ID: " + SelectedEntity.Id + ")?";
            IsConfirmDeleteVisible = true;
        }

        private void ConfirmDelete()
        {
            IsConfirmDeleteVisible = false;

            Entity entity = SelectedEntity;
            if (entity == null)
            {
                return;
            }

            int originalIndex = _shell.Entities.IndexOf(entity);
            _shell.Entities.Remove(entity);

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Obrisan entitet: " + entity.Name + " (ID: " + entity.Id + ")",
                () =>
                {
                    int idx = Math.Min(originalIndex, _shell.Entities.Count);
                    _shell.Entities.Insert(idx, entity);
                    _shell.RestartSimulator();
                }));

            NotificationService.Instance.Show(ToastType.Success, "Entitet obrisan",
                "Entitet \"" + entity.Name + "\" je uspesno obrisan iz sistema.");

            SelectedEntity = null;
            _shell.RestartSimulator(); // automatski restart simulatora nakon brisanja
        }

        // ===================== FILTRIRANJE / PRETRAGA =====================
        private void ApplyFilter()
        {
            _filterApplied = true;
            EntitiesView.Refresh();
            NotificationService.Instance.Show(ToastType.Info, "Filter", "Filter je primenjen.");
        }

        private void ResetFilter()
        {
            _filterApplied = false;
            FilterType = null;
            IdLess = IdGreater = IdEqual = false;
            IdFilterValue = string.Empty;
            ShowOutOfRange = false;
            ShowInRange = false;
            EntitiesView.Refresh();
        }

        private void ResetSearch()
        {
            SearchText = string.Empty;
            SearchByName = true;
            SearchByType = false;
            EntitiesView.Refresh();
        }

        // Jedinstveni predikat koji kombinuje pretragu (P1), filter po tipu/Id-u
        // i CG4 filter van/unutar opsega. Prikazuju se samo redovi koji zadovoljavaju
        // SVE aktivne kriterijume.
        private bool FilterPredicate(object obj)
        {
            if (!(obj is Entity e))
            {
                return false;
            }

            // 1) Pretraga (P1) - uzivo, po nazivu ili tipu.
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string text = SearchText.Trim().ToLowerInvariant();
                if (SearchByType)
                {
                    if (e.Type == null || !e.Type.Name.ToLowerInvariant().Contains(text))
                    {
                        return false;
                    }
                }
                else // podrazumevano po nazivu
                {
                    if (e.Name == null || !e.Name.ToLowerInvariant().Contains(text))
                    {
                        return false;
                    }
                }
            }

            // 2) i 3) primenjuju se tek kada korisnik klikne "Primeni".
            if (_filterApplied)
            {
                // Filter po tipu.
                if (FilterType != null && e.Type != FilterType)
                {
                    return false;
                }

                // Filter po Id-u (<, >, =).
                if (!string.IsNullOrWhiteSpace(IdFilterValue) && int.TryParse(IdFilterValue, out int idVal))
                {
                    if (IdLess && !(e.Id < idVal)) return false;
                    if (IdGreater && !(e.Id > idVal)) return false;
                    if (IdEqual && !(e.Id == idVal)) return false;
                }

                // CG4 - van / unutar opsega ocekivanih vrednosti.
                if (ShowOutOfRange ^ ShowInRange) // tacno jedna opcija cekirana
                {
                    if (ShowOutOfRange && (e.IsValid || !e.HasMeasurement)) return false;
                    if (ShowInRange && (!e.IsValid || !e.HasMeasurement)) return false;
                }
            }

            return true;
        }

        // ===================== VALIDACIJA (IDataErrorInfo) =====================
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(NewIdText):
                        if (string.IsNullOrWhiteSpace(NewIdText))
                            return "ID je obavezan.";
                        if (!int.TryParse(NewIdText, out int id))
                            return "ID mora biti ceo broj.";
                        if (id <= 0)
                            return "ID mora biti pozitivan broj.";
                        if (_shell.Entities.Any(x => x.Id == id))
                            return "Vec postoji entitet sa ovim ID-jem.";
                        return null;

                    case nameof(NewName):
                        if (string.IsNullOrWhiteSpace(NewName))
                            return "Naziv je obavezan.";
                        if (NewName.Trim().Length < 2)
                            return "Naziv mora imati bar 2 karaktera.";
                        return null;

                    case nameof(NewType):
                        if (NewType == null)
                            return "Izaberite tip puta (IA ili IB).";
                        return null;
                }

                return null;
            }
        }
    }
}

namespace NetworkService.Undo
{
    /// <summary>
    /// Interfejs za svaku akciju koja se moze ponistiti (Undo).
    /// CG4 zahteva ponistavanje efekta poslednje realizovane komande,
    /// kao i listanje svih izvrsenih akcija u History paleti.
    /// </summary>
    public interface IUndoableAction
    {
        // Tekst koji se prikazuje u History paleti (npr. "Dodat entitet: Put A1").
        string Description { get; }

        // Vraca sistem u stanje pre nego sto je akcija izvrsena.
        void Undo();
    }
}

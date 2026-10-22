# NetworkService — Simulator infrastrukturnog sistema (PZ2)

**Student:** Antonić Luka, PR 7/2023
**Kombinacija:** CG4 (Službenici) · T3 (Dnevni saobraćaj) · G2 (barovi po vremenu) · P1 (pretraga)

WPF MVVM aplikacija (NetworkService) za monitoring izmerenih vrednosti nad putevima.
Vrednosti merenja stižu od aplikacije `MeteringSimulator` preko TCP-a (port 25675).

---

## Kako pokrenuti

1. Otvori `NetworkService/NetworkService/NetworkService.sln` u Visual Studio-u (target .NET Framework 4.7.2).
2. Build → Rebuild Solution, pa Start (F5). NetworkService prvo mora da radi (on je TCP server).
3. Zatim otvori i pokreni `MeteringSimulator` (zaseban projekat u istom paketu): izaberi temu **T3**, klikni **Start**.
   - Simulator za T3 šalje vrednosti u opsegu ~3000–21000, pa ćeš videti i validna i nevalidna stanja.
4. Po dodavanju/brisanju entiteta NetworkService automatski restartuje simulator (vidi „Restart simulatora" niže).

> Ako automatski restart ne nађe simulator, podesi apsolutnu putanju do `MeteringSimulator.exe`
> u `App.config` → `appSettings` → ključ `SimulatorPath`.

---

## Tema T3 — modelovanje entiteta

Entitet je **put** sa atributima `Id` (jedinstven ceo broj), `Name`, `Type`.
Tip može biti **IA** (validno do 15000) ili **IB** (validno do 7000); sve iznad je nevalidno.
Tip je posebna klasa (`EntityType`) sa imenom i putanjom do unapred definisane slike (`/Images/type_IA.png`, `type_IB.png`).

---

## Arhitektura (MVVM)

```
Model/        Entity, EntityType, DisplaySlot, TypeGroup, Connection, MeasurementRecord, TypeShare
ViewModel/    MainWindowViewModel (shell), NetworkEntitiesViewModel, NetworkDisplayViewModel, MeasurementGraphViewModel
Views/        NetworkEntitiesView, NetworkDisplayView, MeasurementGraphView (UserControl-i)
Helpers/      BindableBase (INotifyPropertyChanged), MyICommand (ICommand)
Services/     Logger (Log.txt), NotificationService + ToastNotification, SimulatorService, IShellContext
Undo/         IUndoableAction, RelayUndoableAction
Converters/   boje/slike po validnosti, vidljivost, tip toasta...
```

- Navigacija se radi preko `ContentControl` + `DataTemplate`-a (VM → View) u `App.xaml`.
- Pod-ViewModel-i se kreiraju **jednom** u shell-u i čuvaju, pa se stanje (npr. raspored na mreži) ne gubi pri navigaciji.

---

## Mapiranje zahteva na rešenje

**Log (.txt):** svaka pristigla vrednost se upisuje u `Log.txt` (vreme, ID+naziv entiteta, vrednost) — `Services/Logger.cs`, poziva se iz `MainWindowViewModel.ProcessMeasurement`.

**Network Entities View:**
- Dodavanje preko forme (ID, Naziv, Tip) + brisanje preko dugmeta „Obriši selektovano".
- **Validacija po polju** preko `IDataErrorInfo` — poruka se ispisuje ispod konkretnog polja (bez MessageBox-a).
- **Potvrda brisanja** — sopstveni overlay (ne MessageBox).
- **Pretraga (P1):** dva RadioButton-a (Naziv/Tip) + TextBox, radi uživo.
- **CG4 dodatni filter:** „Van opsega / Unutar opsega" — kombinuje se sa pretragom i filterom po tipu/ID-u; svi kriterijumi se primenjuju zajedno (`FilterPredicate`).
- Poništavanje pretrage/filtera vraća prikaz svih elemenata.
- Posle dodavanja/brisanja → automatski restart simulatora.

**Network Display View:**
- 12 polja (Drag&Drop mreža) + TreeView sa entitetima grupisanim po tipu (samo oni koji nisu na mreži).
- Prevlačenje iz TreeView-a na polje; premeštanje polje→polje; dvoklik vraća entitet u TreeView.
- Uz sliku se prikazuju ID i poslednja vrednost; nevalidno stanje → crvena ivica + alarm ikonica.
- **Povezivanje linijama** (toggle „Poveži", pa klik na dva entiteta); linije prate entitete; brisanje entiteta ili uklanjanje sa mreže briše i njegove linije; nije moguće duplirati liniju.
- „Rasporedi sve" automatski popunjava slobodna polja.

**Measurement Graph View:**
- **G2** grafikon (barovi po vremenu, **obe ose označene**), poslednjih 5 merenja za entitet izabran u ComboBox-u; crta se programski na `Canvas`-u (bez gotovih Chart kontrola), osvežava se u realnom vremenu. Validno=zeleno, nevalidno=crveno, isprekidana linija = granica validnosti.
- **CG4 dodatni grafikon:** pravougaonik podeljen po procentu zastupljenosti tipova (IA/IB).

**CG4 — Undo i History:**
- Svaka akcija (dodavanje, brisanje, postavljanje/premeštanje/uklanjanje na mreži, povezivanje, raspoređivanje) registruje se kao `IUndoableAction` u globalnoj istoriji.
- **Undo** poništava poslednju akciju; **Undo sve** poništava sve odjednom (dostupno i na Entities i na Display).
- **History paleta** (desno) lista sve izvršene akcije.

**Opšte:**
- Toast notifikacije za uspešne akcije (bez MessageBox-a) — tip, naslov, sadržaj.
- ToolTip-ovi i promene kursora; prečice: `Ctrl+1/2/3` navigacija, `Ctrl+Z` Undo, `Ctrl+Shift+Z` Undo sve.
- Konzistentna paleta boja, ikonice i fontovi; kod na engleskom, UI na srpskom.
- Učitana 4 entiteta sa validnim podacima (Put A1/A2 — IA, Put B1/B2 — IB).

---

## TCP protokol (zadržan iz starter projekta)

- Simulator pita `"Need object count"` → NetworkService vraća **ukupan** broj entiteta (`Entities.Count`).
- Simulator šalje `"Entitet_<index>:<vrednost>"` → NetworkService ažurira `Entities[index].Value`, upisuje u Log i osvežava poglede.

## Restart simulatora

`SimulatorService` ubije postojeće `MeteringSimulator` procese i pokrene novu instancu sa
putanje iz `App.config` (`SimulatorPath`) ili sa uobičajenih relativnih lokacija pored `.exe`-a.

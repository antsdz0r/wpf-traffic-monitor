# NetworkService — Simulator infrastrukturnog sistema (PZ2)

**Student:** Antonić Luka, PR 7/2023
**Kombinacija:** CG4 (Službenici) · T3 (Dnevni saobraćaj) · G2 (barovi po vremenu) · P1 (pretraga)

WPF MVVM aplikacija (NetworkService) za monitoring izmerenih vrednosti nad putevima.
Vrednosti merenja stižu od aplikacije `MeteringSimulator` preko TCP-a (localhost, port 25675).

---

## Kako pokrenuti

1. Otvori `NetworkService.sln` u Visual Studio-u (target .NET Framework 4.7.2).
2. Build → Rebuild Solution, pa Start (F5). **NetworkService mora prvi da se pokrene** (on je TCP server).
3. Pokreni `MeteringSimulator` (iz tvog originalnog starter projekta — nepromenjen je):
   izaberi temu **T3**, klikni **Start**. Za T3 simulator šalje vrednosti ~3000–21000,
   pa ćeš videti i validna i nevalidna stanja.
4. Po dodavanju/brisanju entiteta NetworkService automatski restartuje simulator.
   Ako ga ne nađe automatski, upiši punu putanju do `MeteringSimulator.exe`
   u `App.config` → `appSettings` → ključ `SimulatorPath`.

> Napomena: NetworkService i MeteringSimulator komuniciraju isključivo preko TCP-a
> (localhost:25675), pa ne moraju biti u istom solution-u. Slobodno ih dodaj u isti
> solution ako želiš (Add → Existing Project).

---

## Slike tipova i statusa

Slike tipova (IA/IB) i statusne ikonice (validno/opasnost) su definisane kao
**vektorski crteži (DrawingImage)** u `App.xaml` — nema eksternih `.png` fajlova,
pa ništa ne može da „fali". Tip se preslikava u sliku preko `TypeNameToImageConverter`.

---

## Tema T3 — modelovanje entiteta

Entitet je **put** sa atributima `Id` (jedinstven ceo broj), `Name`, `Type`.
Tip može biti **IA** (validno do 15000) ili **IB** (validno do 7000); sve iznad je nevalidno.

---

## Arhitektura (MVVM)

```
Model/        Entity, EntityType, DisplaySlot, TypeGroup, Connection, MeasurementRecord, TypeShare
ViewModel/    MainWindowViewModel (shell), NetworkEntitiesViewModel, NetworkDisplayViewModel, MeasurementGraphViewModel
Views/        NetworkEntitiesView, NetworkDisplayView, MeasurementGraphView (UserControl-i)
Helpers/      BindableBase (INotifyPropertyChanged), MyICommand (ICommand)
Services/     Logger (Log.txt), NotificationService + ToastNotification, SimulatorService, IShellContext
Undo/         IUndoableAction, RelayUndoableAction
Converters/   boje/slike po validnosti, vidljivost, tip toasta, slika tipa
```

- Navigacija: `ContentControl` + `DataTemplate` (VM → View) u `App.xaml`.
- Pod-ViewModel-i se kreiraju **jednom** i čuvaju, pa se stanje (raspored na mreži) ne gubi pri navigaciji.

---

## Mapiranje zahteva na rešenje

**Log (.txt):** svaka pristigla vrednost se upisuje u `Log.txt` (vreme, ID+naziv, vrednost) — `Services/Logger.cs`, poziv iz `MainWindowViewModel.ProcessMeasurement`.

**Network Entities View:** dodavanje (forma ID/Naziv/Tip) + brisanje; **validacija po polju** (`IDataErrorInfo`, poruka ispod polja, bez MessageBox-a); **potvrda brisanja** kroz sopstveni overlay; **pretraga P1** (RadioButton Naziv/Tip + TextBox); **CG4 filter** van/unutar opsega koji se kombinuje sa pretragom i filterom po tipu/ID-u; poništavanje filtera/pretrage; auto-restart simulatora posle izmena.

**Network Display View:** 12 polja (Drag&Drop) + TreeView po tipu; prevlačenje iz TreeView-a, premeštanje polje→polje, dvoklik vraća u TreeView; ID + poslednja vrednost uz sliku; nevalidno = crvena ivica + alarm ikonica; **povezivanje linijama** (toggle „Poveži"); linije prate entitet, brišu se sa entitetom/uklanjanjem; nema dupliranja; „Rasporedi sve"; stanje se čuva pri navigaciji.

**Measurement Graph View:** **G2** barovi po vremenu (obe ose označene), poslednjih 5 merenja za izabrani entitet, crtano programski na Canvas-u, real-time; validno=zeleno, nevalidno=crveno, isprekidana linija = granica; **CG4** grafikon raspodele tipova (procenti).

**CG4 — Undo i History:** svaka akcija je `IUndoableAction` u globalnoj istoriji; **Undo** (poslednja) i **Undo sve** (na Entities i Display); **History paleta** lista sve akcije.

**Opšte:** Toast notifikacije (bez MessageBox-a); ToolTip-ovi i promene kursora; prečice `Ctrl+1/2/3`, `Ctrl+Z`, `Ctrl+Shift+Z`; konzistentna paleta/ikone/fontovi; kod na engleskom, UI na srpskom; 4 seed entiteta (Put A1/A2 — IA, Put B1/B2 — IB).

---

## TCP protokol (zadržan iz starter projekta)

- `"Need object count"` → NetworkService vraća **ukupan** broj entiteta (`Entities.Count`).
- `"Entitet_<index>:<vrednost>"` → ažurira `Entities[index].Value`, upis u Log, osvežavanje pogleda.

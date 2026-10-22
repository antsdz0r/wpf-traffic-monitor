# RoadPulse

A WPF dashboard for monitoring road traffic in real time. A simulator streams traffic counts over TCP. RoadPulse lets you manage the monitored roads, arrange them on a drag-and-drop network map, flags readings that exceed safe limits, and charts recent measurements live.

Built in C# with MVVM on .NET Framework 4.7.2 as the second practical assignment (PZ2) for the Human–Computer Interaction course (Interakcija čovek–računar).

---

## What's in the repo

| Project | Role |
|---|---|
| **NetworkService** | The main app: a TCP server on `localhost:25675` that receives measurements and shows them in three views |
| **MeteringSimulator** | A small WPF tool that sends random traffic values for each monitored entity |

## Domain

Each monitored entity is a **road** with an `Id`, a `Name` and a `Type`:

| Type | Valid range |
|---|---|
| **IA** | 0 – 15 000 vehicles |
| **IB** | 0 – 7 000 vehicles |

Any reading outside its type's range is shown as invalid. The app starts with four sample roads: Put A1 and Put A2 (IA), and Put B1 and Put B2 (IB).

## Features

### Network Entities
- Add roads through a form with per-field validation. Errors appear under each field (`IDataErrorInfo`), with no pop-up dialogs.
- Delete roads, with an in-app confirmation overlay.
- Search by name or type.
- Filter by type, by ID, and to show only in-range or only out-of-range roads. Search and filters can be combined.
- The simulator restarts automatically after the list changes, so it always knows how many entities to send data for.

### Network Display
- A 12-slot grid plus a TreeView of roads grouped by type.
- Drag roads from the tree onto slots and between slots. Double-click a slot to send the road back to the tree.
- Each slot shows the road's icon, ID and latest value. Invalid values get a red border and an alarm icon.
- Connect mode draws lines between roads. Lines follow their roads, can't be duplicated, and disappear when a road is removed.
- "Arrange all" fills the grid automatically. The layout is kept when you switch views.

### Measurement Graph
- A bar chart of the last five measurements for the selected road, drawn on a `Canvas` and updated live.
- Valid bars are green and invalid bars are red. A dashed line marks the type's limit.
- A chart showing what percentage of roads belong to each type.

### Undo and history
- Every action (add, delete, place, move or connect) is recorded as an `IUndoableAction`.
- **Undo** reverts the last action and **Undo all** reverts every action.
- A **History** panel lists all actions taken in the session.

### General
- Toast notifications instead of message boxes.
- Tooltips and context-appropriate cursors throughout.
- Keyboard shortcuts:

| Shortcut | Action |
|---|---|
| `Ctrl+1` / `Ctrl+2` / `Ctrl+3` | Switch to Entities / Display / Graph |
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` | Undo all |

- Every received measurement is appended to `Log.txt` with the time, the entity and the value.

## Architecture

```
NetworkService/
├── Model/        Entity, EntityType, DisplaySlot, Connection, MeasurementRecord, TypeGroup, TypeShare
├── ViewModel/    MainWindowViewModel (shell + TCP listener), NetworkEntitiesViewModel,
│                 NetworkDisplayViewModel, MeasurementGraphViewModel
├── Views/        NetworkEntitiesView, NetworkDisplayView, MeasurementGraphView
├── Helpers/      BindableBase (INotifyPropertyChanged), MyICommand (ICommand)
├── Services/     Logger, NotificationService + ToastNotification, SimulatorService
├── Undo/         IUndoableAction, RelayUndoableAction
└── Converters/   validity colors and icons, type images, visibility
```

Navigation uses a `ContentControl` with view-model-to-view `DataTemplate`s in `App.xaml`. The child view models are created once and reused, so their state survives navigation.

### Simulator protocol

| Simulator sends | NetworkService replies / does |
|---|---|
| `Need object count` | Replies with the current number of entities |
| `Entitet_<index>:<value>` | Updates that entity's value, logs it, and refreshes all views |

## Running it

**Requirements:** Windows and Visual Studio with .NET Framework 4.7.2 targeting support.

1. Open `NetworkService/NetworkService/NetworkService.sln`. It includes both projects.
2. Start **NetworkService** first, since it is the TCP server.
3. Start **MeteringSimulator**, choose theme **T3**, and click **Start**. T3 sends values of roughly 3 000–21 000, so you'll see both valid and invalid readings.

NetworkService tries to find and restart `MeteringSimulator.exe` by itself. If it can't, set the full path in `App.config` under the `SimulatorPath` key.

## Assignment variant

PR 7/2023 · CG4 (clerical workers) · T3 (daily traffic) · G2 (time-based bar chart) · P1 (search)

The UI text is in Serbian. Code and identifiers are in English.

# PhuCapCom follow-up

## P1 — complete the remaining coordinator split

bUnit component tests now protect the DevExpress selection callback and export
grid data binding. The reload lifecycle, selection callback, and selection-based
lock command lifecycle have moved into `Reload`, `Filters`, and `Commands`
partials without changing callback contracts.

Move the remaining period/filter, recalculation, row-refresh, and presentation
methods from `PhuCapCom.razor.cs` only together with focused regression tests
for each interaction.

## Resolved P2 — shared monthly-work popup

The popup source now lives under `Components/Shared/MonthlyWork`. Its established
public name and namespace remain unchanged while the five existing consumers are
stabilized.

using System.Globalization;

namespace FEFWRNGSolver.UI;

internal sealed class SeedCellMenu : ContextMenuStrip
{
    public SeedCellMenu(DataGridView grid, WindowText text, Action<uint> adoptSeed)
    {
        grid.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var cell = grid[e.ColumnIndex, e.RowIndex];
                if (cell.ContextMenuStrip == this)
                    grid.CurrentCell = cell;
            }
        };
        var adopt = Items.Add("", null, (_, _) =>
        {
            if (grid.CurrentCell?.FormattedValue is string text &&
                uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint seed))
                adoptSeed(seed);
        });
        text.Bind(() => adopt.Text = text.Catalog["SYS_ADOPT_SEED"]);
    }
}

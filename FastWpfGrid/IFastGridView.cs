using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public interface IFastGridView
    {
        /// <summary>
        /// invalidates whole grid
        /// </summary>
        void InvalidateAll();

        /// <summary>
        /// invalidates given cell
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        void InvalidateCell(int row, int column);

        /// <summary>
        /// invalidates given row header
        /// </summary>
        /// <param name="row"></param>
        void InvalidateRowHeader(int row);

        /// <summary>
        /// invalidates given row (all cells including header)
        /// </summary>
        /// <param name="row"></param>
        void InvalidateRow(int row);

        /// <summary>
        /// invalidates given column header
        /// </summary>
        /// <param name="column"></param>
        void InvalidateColumnHeader(int column);

        /// <summary>
        /// invalidates given column (all cells including header)
        /// </summary>
        /// <param name="column"></param>
        void InvalidateColumn(int column);

        /// <summary>
        /// invalidates grid header (top-left header cell)
        /// </summary>
        void InvalidateGridHeader();

        /// <summary>
        /// forces grid to refresh all data
        /// </summary>
        void NotifyRefresh();

        /// <summary>
        /// notifies grid about new rows added to the end
        /// </summary>
        void NotifyAddedRows();

        /// <summary>
        /// notifies grid, that result of GetHiddenColumns() or GetFrozenColumns() is changed
        /// </summary>
        void NotifyColumnArrangeChanged();

        /// <summary>
        /// notifies grid, that result of GetHiddenRows() or GetFrozenRows() is changed
        /// </summary>
        void NotifyRowArrangeChanged();

        /// <summary>
        /// set/get whether grid is transposed
        /// </summary>
        bool IsTransposed { get; set; }

        /// <summary>
        /// gets whether flexible rows (real rows) are curently used
        /// </summary>
        bool FlexibleRows { get; }

        /// <summary>
        /// gets summary of active rows
        /// </summary>
        /// <returns></returns>
        ActiveSeries GetActiveRows();

        /// <summary>
        /// gets summary of active columns
        /// </summary>
        /// <returns></returns>
        ActiveSeries GetActiveColumns();
    }
}

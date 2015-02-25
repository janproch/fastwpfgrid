using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    partial class FastGridControl
    {
        private class InvalidationContext : IDisposable
        {
            private FastGridControl _grid;
            internal InvalidationContext(FastGridControl grid)
            {
                _grid = grid;
                _grid.EnterInvalidation();
            }

            public void Dispose()
            {
                _grid.LeaveInvalidation();
            }
        }

        private int _invalidationCount;

        private void LeaveInvalidation()
        {
            _invalidationCount--;
            if (_invalidationCount == 0)
            {
                if (_isInvalidated)
                {
                    RenderGrid();
                }
            }
        }

        private void EnterInvalidation()
        {
            _invalidationCount++;
        }

        private InvalidationContext CreateInvalidationContext()
        {
            return new InvalidationContext(this);
        }
    }
}

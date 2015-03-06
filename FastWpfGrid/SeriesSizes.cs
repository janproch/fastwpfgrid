using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWpfGrid
{
    public class SeriesSizeItem
    {
        public int Index;
        public int Size;
        public int Position;

        public int EndPosition
        {
            get { return Position + Size; }
        }
    }

    public class SeriesSizes
    {
        private List<SeriesSizeItem> _items = new List<SeriesSizeItem>();
        private Dictionary<int, SeriesSizeItem> _itemByIndex = new Dictionary<int, SeriesSizeItem>();
        private Dictionary<int, int> _sizeOverrides = new Dictionary<int, int>();
        private List<int> _positions = new List<int>();
        private List<int> _indexes = new List<int>();

        public int Count;
        public int DefaultSize;

        public void Clear()
        {
            _items.Clear();
            _itemByIndex.Clear();
            _sizeOverrides.Clear();
            _positions.Clear();
            _indexes.Clear();
        }

        public void PutSizeOverride(int index, int size)
        {
            if (!_sizeOverrides.ContainsKey(index)) _sizeOverrides[index] = size;
            if (size > _sizeOverrides[index]) _sizeOverrides[index] = size;
        }

        public void BuildIndex()
        {
            _items.Clear();
            _itemByIndex.Clear();

            _indexes = _sizeOverrides.Keys.ToList();
            _indexes.Sort();

            int lastIndex = -1;
            int lastEndPosition = 0;

            foreach (int index in _indexes)
            {
                int size = _sizeOverrides[index];
                var item = new SeriesSizeItem
                    {
                        Index = index,
                        Size = size,
                        Position = lastEndPosition + (index - lastIndex - 1)*DefaultSize,
                    };
                _items.Add(item);
                _itemByIndex[index] = item;
                lastIndex = index;
                lastEndPosition = item.EndPosition;
            }

            _positions = _items.Select(x => x.Position).ToList();
        }

        public int GetIndexOnPosition(int position)
        {
            int itemOrder = _positions.BinarySearch(position);
            if (itemOrder >= 0) return itemOrder;
            itemOrder = ~itemOrder; // bitwise complement - index is next larger index
            if (itemOrder == 0) return position/DefaultSize;
            if (position <= _items[itemOrder - 1].EndPosition) return _items[itemOrder - 1].Index;
            return (position - _items[itemOrder - 1].Position)/DefaultSize + _items[itemOrder - 1].Index;
        }

        public int GetSizeSum(int start, int end)
        {
            int order1 = _indexes.BinarySearch(start);
            int order2 = _indexes.BinarySearch(end);

            int count = end - start;


            if (order1 < 0) order1 = ~order1;
            if (order2 < 0) order2 = ~order2;

            int result = 0;

            for (int i = order1; i <= order2; i++)
            {
                if (i < 0) continue;
                if (i >= _items.Count) continue;
                var item = _items[i];
                if (item.Index < start) continue;
                if (item.Index >= end) continue;

                result += item.Size;
                count--;
            }

            result += count*DefaultSize;
            return result;
        }

        public int GetSize(int index)
        {
            if (_sizeOverrides.ContainsKey(index)) return _sizeOverrides[index];
            return DefaultSize;
        }

        public int GetScroll(int source, int target)
        {
            if (source < target)
            {
                return -Enumerable.Range(source, target - source).Select(GetSize).Sum();
            }
            else
            {
                return Enumerable.Range(target, source - target).Select(GetSize).Sum();
            }
        }

        public int GetTotalSizeSum()
        {
            return _sizeOverrides.Values.Sum() + (Count - _sizeOverrides.Count)*DefaultSize;
        }

        public int GetPosition(int index)
        {
            int order = _indexes.BinarySearch(index);
            if (order >= 0) return _items[order].Position;
            order = ~order;
            order--;
            if (order < 0) return index*DefaultSize;
            return _items[order].EndPosition + (index - _items[order].Index - 1)*DefaultSize;
        }

        public int GetVisibleCount(int firstVisibleIndex, int viewportSize)
        {
            int res = 0;
            int index = firstVisibleIndex;
            int count = 0;
            while (res < viewportSize && index <= Count)
            {
                res += GetSize(index);
                index++;
                count++;
            }
            return count;
        }

        public void InvalidateAfterScroll(int oldFirstVisible, int newFirstVisible, Action<int> invalidate, int viewportSize)
        {
            //int oldFirstVisible = FirstVisibleColumn;
            //FirstVisibleColumn = column;
            //int visibleCols = VisibleColumnCount;

            if (newFirstVisible > oldFirstVisible)
            {
                int oldVisibleCount = GetVisibleCount(oldFirstVisible, viewportSize);
                int newVisibleCount = GetVisibleCount(newFirstVisible, viewportSize);

                for (int i = oldFirstVisible + oldVisibleCount - 1; i <= newFirstVisible + newVisibleCount; i++)
                {
                    invalidate(i);
                }
            }
            else
            {
                for (int i = newFirstVisible; i <= oldFirstVisible; i++)
                {
                    invalidate(i);
                }
            }
        }

        public bool IsWholeInView(int firstVisibleIndex, int index, int viewportSize)
        {
            int res = 0;
            int testedIndex = firstVisibleIndex;
            while (res < viewportSize && testedIndex < Count)
            {
                res += GetSize(testedIndex);
                if (testedIndex == index) return res <= viewportSize;
                testedIndex++;
            }
            return false;
        }

        public int ScrollInView(int firstVisibleIndex, int index, int viewportSize)
        {
            if (IsWholeInView(firstVisibleIndex, index, viewportSize))
            {
                return firstVisibleIndex;
            }

            if (index < firstVisibleIndex)
            {
                return index;
            }

            // scroll to the end
            int res = 0;
            int testedIndex = index;
            while (res < viewportSize && testedIndex >= 0)
            {
                int size = GetSize(testedIndex);
                if (res + size > viewportSize) return testedIndex + 1;
                testedIndex--;
                res += size;
            }

            if (res >= viewportSize && testedIndex < index) return testedIndex + 1;
            return firstVisibleIndex;
            //if (testedIndex < index) return testedIndex + 1;
            //return index;
        }

        public void Resize(int index, int newSize)
        {
            if (index < 0) return;
            // can be done more effectively
            _sizeOverrides[index] = newSize;
            BuildIndex();
        }
    }
}

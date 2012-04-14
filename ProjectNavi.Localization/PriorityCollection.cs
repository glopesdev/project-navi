using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;

namespace ProjectNavi.Localization
{
    internal class PriorityCollection<TPriority, TItem>
        : IEnumerable<TItem>
        , ICollection
    {
        private class Entry
        {
            public Entry Parent;
            public Entry Child;
            public Entry Right;
            public Entry Left;
            public int Rank;
            public bool Marked;
            public TPriority Priority;
            public TItem Value;
        }

        private const int DefaultBlockSize = 100;
        private int blockSize = DefaultBlockSize;
        private readonly IComparer<TPriority> comparer = null;
        private List<Entry> trees = null;
        private Dictionary<TItem, Entry> entries = null;
        private int heapValue = 0;

        public PriorityCollection()
            : this(DefaultBlockSize, Comparer<TPriority>.Default)
        {
        }

        public PriorityCollection(int blockSize)
            : this(blockSize, Comparer<TPriority>.Default)
        {
        }

        public PriorityCollection(IComparer<TPriority> comparer)
            : this(DefaultBlockSize, comparer)
        {
        }

        public PriorityCollection(int blockSize, IComparer<TPriority> comparer)
        {
            if (blockSize < 0)
            {
                throw new ArgumentOutOfRangeException("comparer");
            }
            if (blockSize > 0)
            {
                this.Initialize(blockSize);
            }
            this.comparer = comparer;
            Initialize(blockSize);
        }

        private void Initialize(int blockSize)
        {
            this.blockSize = blockSize;

            this.trees = new List<Entry>(blockSize);
            // allocate a new block
            for (int i = 0; i < blockSize; ++i)
                this.trees.Add(null);

            this.entries = new Dictionary<TItem, Entry>();

            // The value of the heap helps to keep track of the maximum rank while
            // entries are inserted or deleted.
            this.heapValue = 0;
        }

        private void Meld(Entry entryTreeList)
        {
            Entry entryFirst, entryNext, entryCurrent, entryNewRoot, entryTemp, entryTemp2, entryLeftChild, entryRightChild;
            int rank;

            // We meld each tree in the circularly linked list back into the root level
            // of the heap.  Each entry in the linked list is the root entry of a tree.
            // The circularly linked list uses the sibling pointers of entries.  This
            // makes melding of the child entries from a DeleteMin operation simple.
            entryCurrent = entryFirst = entryTreeList;

            do
            {
                // Keep a pointer to the next entry and remove sibling and parent links
                // from the current entry.  entryCurrent refers to the current entry.
                entryNext = entryCurrent.Right;
                entryCurrent.Right = entryCurrent.Left = entryCurrent;
                entryCurrent.Parent = null;

                // We merge the current entry, entryCurrent, by inserting it into the
                // root level of the heap.
                entryNewRoot = entryCurrent;
                rank = entryCurrent.Rank;

                // This loop inserts the new root into the heap, possibly restructuring
                // the heap to ensure that only one tree for each degree exists.
                do
                {
                    // Check if there is already a tree of degree rank in the heap.
                    // If there is then we need to link it with entryNewRoot so it will be
                    // reinserted into a new place in the heap.
                    if (rank >= this.trees.Count)
                        entryTemp = null;
                    else
                        entryTemp = this.trees[rank];
                    if (entryTemp != null)
                    {

                        // entryTemp will be linked to entryNewRoot and relocated so we no
                        // longer will have a tree of degree rank.
                        this.trees[rank] = null;
                        this.heapValue -= (1 << rank);

                        // Swap entryTemp and entryNewRoot if necessary so that entryNewRoot always
                        // points to the root entry which has the smaller priority of the
                        // two.
                        if (this.comparer.Compare(entryTemp.Priority, entryNewRoot.Priority) < 0)
                        {
                            entryTemp2 = entryNewRoot;
                            entryNewRoot = entryTemp;
                            entryTemp = entryTemp2;
                        }

                        // Link entryTemp with entryNewRoot, making sure that sibling references
                        // get updated if rank is greater than 0.  Also, increase rank for
                        // the entryNext pass through the loop since the rank of new has
                        // increased.
                        if (rank++ > 0)
                        {
                            entryRightChild = entryNewRoot.Child;
                            entryLeftChild = entryRightChild.Left;
                            entryTemp.Left = entryLeftChild;
                            entryTemp.Right = entryRightChild;
                            entryLeftChild.Right = entryRightChild.Left = entryTemp;
                        }
                        entryNewRoot.Child = entryTemp;
                        entryNewRoot.Rank = rank;
                        entryTemp.Parent = entryNewRoot;
                        entryTemp.Marked = false;
                    }
                    // Otherwise if there is not a tree of degree rank in the heap we
                    // allow entryNewRoot, which possibly carries moved trees in the heap,
                    // to be a tree of degree rank in the heap.
                    else
                    {
                        // check if trees can contain this new tree
                        if (rank >= this.trees.Count)
                        {
                            // allocate a new block
                            for (int i = 0; i < blockSize; ++i)
                                this.trees.Add(null);
                        }

                        this.trees[rank] = entryNewRoot;
                        this.heapValue += (1 << rank);

                        // NOTE:  Because entryNewRoot is now a root we ensure it is
                        //        marked.
                        entryNewRoot.Marked = true;
                    }

                    // Note that entryTemp will be null if and only if there was not a tree
                    // of degree rank.
                }
                while (entryTemp != null);

                entryCurrent = entryNext;

            }
            while (entryCurrent != entryFirst);
        }

        /// <summary>
        /// Determines whether an element is in the FibonacciHeap.
        /// </summary>
        /// <param name="item">The Object to locate in the FibonacciHeap. The element to locate can be a null reference (Nothing in Visual Basic).</param>
        /// <returns>true if item is found in the heap; otherwise, false.</returns>
        public bool Contains(TItem value)
        {
            // the values are used as keys of the entries hash table
            return this.entries.ContainsKey(value);
        }

        public void Enqueue(TItem item, TPriority priority)
        {
            // create a new Fibonacci heap entry
            Entry entry = new Entry();
            entry.Parent = null;
            entry.Child = null;
            entry.Right = entry;
            entry.Left = entry;
            entry.Rank = 0;
            entry.Value = item;
            entry.Priority = priority;
            entry.Marked = false;

            // Maintain a reference to the new entry. 
            try
            {
                this.entries.Add(item, entry);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("The value already exists in the FibonacciHeap", "value");
            }

            // Meld the new entry into the heap. 
            Meld(entry);
        }

        public TItem Dequeue(out TPriority itemPriority)
        {
            Entry entryMin, entryChild, entryNext;
            TPriority priority, key2;
            int rank, heapValue;
            TItem value;

            // First we determine the maximum rank in the heap. 
            heapValue = this.heapValue;
            rank = -1;
            while (heapValue != 0)
            {
                heapValue = heapValue >> 1;
                rank++;
            };

            // Now determine which root entry is the minimum. 
            entryMin = this.trees[rank];
            priority = entryMin.Priority;
            while (rank > 0)
            {
                rank--;
                entryNext = this.trees[rank];
                if (entryNext != null)
                {
                    key2 = entryNext.Priority;
                    if (this.comparer.Compare(priority, key2) > 0)
                    {
                        priority = key2;
                        entryMin = entryNext;
                    }
                }
            }

            // Remove the minimum entry from the heap but keep a pointer to it. 
            rank = entryMin.Rank;
            this.trees[rank] = null;
            this.heapValue -= (1 << rank);

            entryChild = entryMin.Child;
            if (entryChild != null)
                Meld(entryChild);

            // Record the vertex no of the old minimum entry before deleting it. 
            value = entryMin.Value;
            itemPriority = entryMin.Priority;
            this.entries.Remove(value);

            return value;
        }

        /// <summary>
        /// Removes all of the objects in the heap.
        /// </summary>
        public void Clear()
        {
            Initialize(this.blockSize);
        }

        #region IEnumerable<TItem> Members

        public IEnumerator<TItem> GetEnumerator()
        {
            return entries.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return entries.Keys.GetEnumerator();
        }

        #endregion

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            TItem[] typedArray = (TItem[])array;
            entries.Keys.CopyTo(typedArray, index);
        }

        public int Count
        {
            get
            {
                return this.entries.Count;
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return null; }
        }

        #endregion
    }
}

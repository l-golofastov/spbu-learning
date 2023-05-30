namespace ExamSystem.ConcurrentCollections;

internal class StripedHashSet<T>
{
    /// <summary>
    /// A set
    /// </summary>
    private List<T>[] _table;
    
    /// <summary>
    /// Set size
    /// </summary>
    private int _setSize;

    /// <summary>
    /// An array of strings(threads) to lock
    /// </summary>
    private readonly ReaderWriterLockSlim[] _locks;


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="capacity">Capacity of a set</param>

    public StripedHashSet(int capacity)
    {
        _setSize = 0;
        _table = new List<T>[capacity]; //initialize the set
        _locks = new ReaderWriterLockSlim[capacity]; // initialize a thread locker for the set
        for (int i = 0; i < capacity; i++) // fill the set and 
        {
            _table[i] = new List<T>();
            _locks[i] = new ReaderWriterLockSlim();
        }
    }

    /// <summary>
    /// Flag to know if the set needs to be resized
    /// </summary>
    private bool PolicyDemandsResize => _setSize / _table.Length > 4;

    /// <summary>
    /// Function that makes a set two times bigger
    /// </summary>
    private void Resize()
    {
        int oldCapacity = _table.Length;

        foreach (var l in _locks) // lock all threads
        {
            l.EnterWriteLock();
        }

        try
        {
            if (oldCapacity != _table.Length) // some data could change, so we check if the capacity remained the same
            {
                return;
            }
            int newCapacity = 2 * oldCapacity;
            List<T>[] oldTable = _table;
            _table = new List<T>[newCapacity];
            for (int i = 0; i < newCapacity; i++)
                _table[i] = new List<T>();
            foreach (List<T> bucket in oldTable) // set x to a set choosing the index as follows
            {
                foreach (T x in bucket)
                {
                    _table[x.GetHashCode() % _table.Length].Add(x);
                }
            }
        }
        finally
        {
            foreach (var l in _locks) // unlock the threads
            {
                l.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Checks if x contains in a set
    /// </summary>
    /// <param name="x">Student</param>
    /// <returns></returns>
    public bool Contains(T x)
    {
        _locks[x.GetHashCode() % _locks.Length].EnterReadLock(); // lock the thread which contains x
        try
        {
            int myBucket = x.GetHashCode() % _table.Length; // get the thread id which contains x
            return _table[myBucket].Contains(x);
        }
        finally
        {
            _locks[x.GetHashCode() % _locks.Length].ExitReadLock(); // unlock the thread which contais x
        }
    }

    /// <summary>
    /// Add an x to a set.
    /// Returns true if a student is added to a set.
    /// Returns false if the given student already exists.
    /// </summary>
    /// <param name="x">Student</param>
    /// <returns></returns>
    public bool Add(T x)
    {
        bool result = false;
        _locks[x.GetHashCode() % _locks.Length].EnterWriteLock();
        try
        {
            int myBucket = x.GetHashCode() % _table.Length;
            if (!_table[myBucket].Contains(x))
            {
                _table[myBucket].Add(x);
                result = true;
                _setSize++;
            }
        }
        finally
        {
            _locks[x.GetHashCode() % _locks.Length].ExitWriteLock();
        }
        if (PolicyDemandsResize)
            Resize();
        return result;
    }

    /// <summary>
    /// Removes a student from a set.
    /// Returns true if a student was removed from a set.
    /// Returns false otherwise.
    /// </summary>
    /// <param name="x">Student</param>
    /// <returns></returns>
    public bool Remove(T x)
    {
        _locks[x.GetHashCode() % _locks.Length].EnterWriteLock();
        try
        {
            int myBucket = x.GetHashCode() % _table.Length;
            bool result = _table[myBucket].Remove(x);
            _setSize = result ? _setSize - 1 : _setSize;
            return result;
        }
        finally
        {
            _locks[x.GetHashCode() % _locks.Length].ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns the size to a set
    /// </summary>
    /// <returns></returns>
    public int Count() => _setSize;
}
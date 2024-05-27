using System.Collections.Concurrent;

namespace EmmettLRU;

/// <summary>
/// LRU cache discards the least recently used (read or written) item when the Capacity is reached.
/// </summary>
public class LeastRecentlyUsedCache<K, V> : IConcurrentBoundedCache<K, V> where K : IEquatable<K>
{
    private int _currentSize = 0;
    public int CurrentSize
    {
        get
        {
            lock (_lock)
            {
                return _currentSize;
            }
        }
    }

    public int Capacity { get; }
    
    // Dictionary provides O(1) reads and writes. ConcurrentDictionary is unnecessary as we use a manual lock to keep
    // the dictionary and linked list in sync, precluding race conditions when accessing the dictionary. When benchmarked,
    // a ConcurrentDictionary incurs a 120% overhead on writes
    private readonly Dictionary<K, LinkedListNode<KeyValuePair<K, V>>> _underlyingDictionary;
    
    // Linked list allows FIFO tracking of KVPs. We use the keys on to help remove the associated entry from the underlying
    // dictionary when evicting, and the KVP object when promoting a read pair from the dictionary to the front of the
    // list
    private readonly LinkedList<KeyValuePair<K, V>> _underlyingLinkedList = new ();
    
    // We do not differentiate between reads and writes in our lock, as reads are mutating the underlying data structure
    private readonly object _lock = new ();

    public LeastRecentlyUsedCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), $"Invalide size of '{capacity}' provided. Size must be >= 0.");

        Capacity = capacity;
        _underlyingDictionary = new Dictionary<K, LinkedListNode<KeyValuePair<K, V>>>(capacity);
    }

    /// <summary>
    /// Puts the key value pair into the cache in O(1) time complexity. If the cache is at capacity, we evict the last
    /// read item.
    /// </summary>
    public void Put(K key, V value)
    {
        lock (_lock)
        {
            if (CurrentSize == Capacity)
            {
                var lastKvp = _underlyingLinkedList.Last!.Value;
                _underlyingLinkedList.RemoveLast();
                _underlyingDictionary.Remove(lastKvp.Key, out _);
                _currentSize--;
            }
            
            _underlyingLinkedList.AddFirst(new KeyValuePair<K, V>(key, value));
            _underlyingDictionary.Add(key, _underlyingLinkedList.First!);
            _currentSize++;
        }
    }
    
    /// <summary>
    /// Gets the associated value from the cache. If the key is not in the cache, `false` is returned and value is default
    /// </summary>
    public bool TryGet(K key, out V value)
    {
        lock (_lock)
        {
            var result = _underlyingDictionary.TryGetValue(key, out var kvpNode);

            if (!result)
            {
                value = default!;
                return false;
            }

            _underlyingLinkedList.Remove(kvpNode!);
            _underlyingLinkedList.AddFirst(kvpNode!);
            value = kvpNode!.ValueRef.Value;
            return result;
        }
    }
}
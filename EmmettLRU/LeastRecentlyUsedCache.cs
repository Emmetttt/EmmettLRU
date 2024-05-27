namespace EmmettLRU;

/// <summary>
/// LRU cache discards the least recently used item when the Capacity is reached.
///
/// The LRU cache offers O(1) reads and writes.
/// </summary>
public class LeastRecentlyUsedCache<K, V> : IBoundedCache<K, V> where K : IEquatable<K>
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
    
    // Dictionary provides O(1) reads. ConcurrentDictionary unnecessary as we use a lock to keep the dictionary
    // and linked list in sync anyway
    private readonly Dictionary<K, LinkedListNode<KeyValuePair<K, V>>> _underlyingDictionary;
    
    // Linked list allows FILO or FIFO tracking of keys
    private readonly LinkedList<KeyValuePair<K, V>> _underlyingLinkedList = new LinkedList<KeyValuePair<K, V>>();
    
    private readonly object _lock = new ();


    public LeastRecentlyUsedCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), $"Invalide size of '{capacity}' provided. Size must be >= 0.");

        Capacity = capacity;
        _underlyingDictionary = new Dictionary<K, LinkedListNode<KeyValuePair<K, V>>>(capacity);
    }

    /// <summary>
    /// Puts the key value pair into the cache in O(1). If the cache is at capacity, we  
    /// </summary>
    public void Put(K key, V value)
    {
        lock (_lock)
        {
            if (CurrentSize == Capacity)
            {
                var lastKvp = _underlyingLinkedList.Last();
                _underlyingLinkedList.RemoveLast();
                _underlyingDictionary.Remove(lastKvp.Key, out _);
                _currentSize--;
            }
            
            _underlyingLinkedList.AddFirst(new KeyValuePair<K, V>(key, value));
            _underlyingDictionary.Add(key, _underlyingLinkedList.First!);
            _currentSize++;
        }
    }
    
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
            value = kvpNode.ValueRef.Value;
            return result;
        }
    }
}
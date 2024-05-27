namespace EmmettLRU;

/// <summary>
/// Represents a generic cache of a specified capacity where items of type V are keyed by type V
/// </summary>
public interface IConcurrentBoundedCache<K, V> where K : notnull, IEquatable<K>
{
    /// <summary>
    /// Gets the value that is associated with the specified key. Returns false and default(V) if the value is not in
    /// the cache. Reads are thread-safe.
    /// </summary>
    public bool TryGet(K key, out V value);

    /// <summary>
    /// Puts the Key Value pair into the cache. Throws if key is null, and will put in a thread-safe manner.
    /// </summary>
    public void Put(K key, V value);

    /// <summary>
    /// Retrieves the current size of the cache
    /// </summary>
    public int CurrentSize { get; }
    
    /// <summary>
    /// Retrieves the maximum capacity of the cache
    /// </summary>
    public int Capacity { get; }
}
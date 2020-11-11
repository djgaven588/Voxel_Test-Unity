using System.Collections.Generic;
using System.Linq;

public class ItemTypeManager<T> where T : class, new()
{
    protected readonly Dictionary<string, T> _data = new Dictionary<string, T>();

    public virtual void Init()
    {

    }

    public virtual T GetEntryOrDefault(string name)
    {
        if (_data.TryGetValue(name, out T value))
        {
            return value;
        }

        return null;
    }

    public void AddEntries(string[] names, T[] entries)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            _data.Add(names[i], entries[i]);
        }
    }

    public T[] GetAll()
    {
        return _data.Values.ToArray();
    }
}

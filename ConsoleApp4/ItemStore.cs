namespace ConsoleApp4;

public class ItemStore
{
    private readonly Item[] items = Enumerable.Range(0, 5000).Select(i => new Item { Id = i, Name = $"Item {i}" }).ToArray();

    public async Task<Item[]> GetItems(int offset, int count)
    {
        await Task.Delay(10000);
        return items.Skip(offset).Take(count).ToArray();
    }
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}
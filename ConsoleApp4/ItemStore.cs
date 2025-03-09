namespace ConsoleApp4;

public class ItemStore
{
    private readonly Item[] items = Enumerable.Range(0, 500).Select(i => new Item { Id = i, Name = $"Item {i}" }).ToArray();

    public async Task<ItemsResponse> GetItems(int offset, int count)
    {
        await Task.Delay(2000);
        return new ItemsResponse
        {
            Items = items.Skip(offset).Take(count).ToArray(),
            PageStart = offset,
            HasMore = (offset + count < items.Length),
        };
    }
}

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ItemsResponse
{
    public Item[] Items { get; set; }
    public int PageStart { get; set; }
    public bool HasMore { get; set; }
    public int PageEnd => PageStart + Items.Length - 1;
}
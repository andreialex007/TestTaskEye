namespace FileSorter.Core;

public static class PriorityQueueExtensions
{
    public static PriorityQueue<TElement, TPriority> ToPriorityQueue<TElement, TPriority>(
        this IEnumerable<(TElement Element, TPriority Priority)> source)
    {
        return new PriorityQueue<TElement, TPriority>(source);
    }
}

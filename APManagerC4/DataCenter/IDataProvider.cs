namespace APManagerC4
{
    public interface IDataProvider<T>
    {
        IEnumerable<T> Retrieve(Predicate<T> predicate);
    }
}

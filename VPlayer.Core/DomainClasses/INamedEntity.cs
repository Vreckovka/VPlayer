namespace VPlayer.Core.DomainClasses
{
    public interface INamedEntity : IEntity
    {
        string Name { get; set; }
    }

    public interface IEntity
    {
        int Id { get; set; }
    }
}

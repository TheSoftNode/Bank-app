namespace Entities_Dtos.Interface;

public interface IEntity
{
}

public interface IEntity<TId> : IEntity
{
    TId Id { get; set; }
}

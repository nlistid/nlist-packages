using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Abstractions.Data;

/// <summary>
/// Stores projections: the read models a projector builds, one per stream.
/// Derived data, not a source of truth. Querying across models is store-specific and not part of this
/// interface: each service reads the store directly.
/// </summary>
public interface IProjectionRepository
{
    /// <summary>The stream's read model, or a null value if nothing has been projected yet.</summary>
    Task<IResult<T?>> GetAsync<T>(
        Guid aggregateId,
        CancellationToken cancellationToken)
        where T : IEntity;

    /// <summary>
    /// Saves the read model if the stored one is still at <paramref name="expectedVersion"/>
    /// (0 when none is stored). If another writer has saved since, nothing is written
    /// and the result is a conflict failure. On success the value is the model's version.
    /// </summary>
    Task<IResult<long>> SaveAsync<T>(
        T model,
        long expectedVersion,
        CancellationToken cancellationToken)
        where T : IEntity;
}

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    partial class CachedRepository<T, TKey>
    {
        protected class ConstraintViolationHandler
        {
            private readonly Dictionary<T, T> _toReplace = new();
            private readonly Dictionary<T, T> _toUpdate = new();
            private readonly ICachedRepository<T> _repository;
            private readonly ILogger _logger;
            private readonly RepositoryOperation _operation;

            public IEnumerable<T> ToUpdate => _toUpdate.Values;
            public IEnumerable<T> ToDelete => _toReplace.Values;
            public ConstraintViolationHandler(ICachedRepository<T> repository, ILogger logger, RepositoryOperation operation)
            {
                _repository = repository;
                _logger = logger;
                _operation = operation;
            }

#nullable enable
            public (bool continueValidation, bool ignoreViolation) Handle(ConstraintViolation<T> violation, ConflictResolutionAction? userAction)
#nullable disable
            {
                var conflictResolution = violation.Constraint.ConflictResolution; // default resolution

                // Custom conflict resolution
                if (userAction != null)
                {
                    if (userAction is UpdateConflicResolutionAction<T> updateAction)
                    {
                        _logger.LogInformation("[ConstraintViolationHandler] Choosing advanced conflic resolution 'Update'.");

                        // Set the entity to update for this entity
                        _toUpdate[violation.NewEntity] = updateAction.EntityToUpdate;

                        // not include, continue
                        return (continueValidation: true, ignoreViolation: false);
                    }
                    else if (userAction.Resolution != null)
                    {
                        conflictResolution = userAction.Resolution.Value;
                    }
                }

                _logger.LogInformation("[ConstraintViolationHandler] Choosing conflic resolution {method}.", conflictResolution);

                switch (conflictResolution)
                {
                    case ConflictResolution.Fail:
                        // If resolution is fail: Dont check the other entities or constraints -> fail the entire transaction
                        throw new ConstraintViolationException(
                            "Unique constraint violation",
                            violation.Constraint.PropertyName,
                            violation.NewEntity,
                            violation.ExistingEntity);
                    case ConflictResolution.Ignore:

                        // Revoke update entity, because Ignore > Update
                        if (_toUpdate.Remove(violation.NewEntity))
                        {
                            _logger?.LogDebug("[ConstraintViolationHandler] Revoked update of entity.");
                        }

                        // Revoke replace entity, because Ignore > Replace
                        if (_toReplace.Remove(violation.NewEntity))
                        {
                            _logger?.LogDebug("[ConstraintViolationHandler] Revoked replace of entity.");
                        }

                        // not include, not continue
                        return (continueValidation: false, ignoreViolation: false);
                    case ConflictResolution.Replace:
                        
                        // On upserts we dont need to perform an extra replace
                        if (_operation is not RepositoryOperation.Upsert)
                        {
                            _toReplace[violation.NewEntity] = violation.ExistingEntity;

                            return (continueValidation: true, ignoreViolation: false);
                        }

                        // include, continue
                        return (continueValidation: true, ignoreViolation: true);
                    default:
                        return (continueValidation: true, ignoreViolation: false);
                }
            }
            public void Reset()
            {
                _toReplace.Clear();
                _toUpdate.Clear();
            }

            public void PerformReplaces()
            {
                var keysToReplace = _toReplace.Values
                    .Select(x => _repository.GetKey(x))
                    .Distinct()
                    .ToHashSet();

                if (!keysToReplace.Any())
                {
                    _logger?.LogDebug("[ConstraintViolationHandler] No entities to be replaced.");
                    return;
                }

                _logger?.LogDebug("[ConstraintViolationHandler] Upserting {n} entities to be replaced.", keysToReplace.Count);

                _repository.SetMany(_toReplace.Values, onConflict: v => ConflictResolutionAction.Fail());
            }

            public void PerformUpdates()
            {
                if (!_toUpdate.Any())
                {
                    _logger?.LogDebug("[ConstraintViolationHandler] No entities to be updated.");
                    return;
                }

                _logger?.LogDebug("[ConstraintViolationHandler] Updating {n} entities.", _toUpdate.Count);

                _repository.UpdateMany(_toUpdate.Values, onConflict: v => ConflictResolutionAction.Fail());
            }
        }
    }
}

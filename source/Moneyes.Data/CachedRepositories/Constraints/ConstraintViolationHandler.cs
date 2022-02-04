using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    partial class CachedRepository<T>
    {
        protected class ConstraintViolationHandler
        {
            private readonly List<T> _toDelete = new();
            private readonly List<T> _toUpdate = new();
            private readonly ICachedRepository<T> _repository;
            private readonly ILogger _logger;

            public IReadOnlyList<T> ToUpdate => _toUpdate;
            public IReadOnlyList<T> ToDelete => _toDelete;
            public ConstraintViolationHandler(ICachedRepository<T> repository, ILogger logger)
            {
                _repository = repository;
                _logger = logger;
            }

            public (bool continueValidation, bool ignore) Handle(ConstraintViolation<T> violation, ConflictResolutionAction? userAction)
            {
                var conflictResolution = violation.Constraint.ConflictResolution; // default resolution

                // Custom conflict resolution
                if (userAction != null)
                {
                    if (userAction is UpdateConflicResolutionAction<T> updateAction)
                    {
                        _logger.LogInformation("Choosing advanced conflic resolution 'Update'.");

                        _toUpdate.Add(updateAction.EntityToUpdate);

                        // not include, continue
                        return (continueValidation: true, ignore: false);
                    }
                    else if (userAction.Resolution != null)
                    {
                        conflictResolution = userAction.Resolution.Value;
                    }
                }

                _logger.LogInformation("Choosing conflic resolution {method}.", conflictResolution);

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
                        // not include, not continue
                        return (continueValidation: false, ignore: false);
                    case ConflictResolution.Replace:
                        _toDelete.Add(violation.ExistingEntity);

                        // include, continue
                        return (continueValidation: true, ignore: true);
                    default:
                        return (continueValidation: true, ignore: false);
                }
            }
            public void Reset()
            {
                _toDelete.Clear();
                _toUpdate.Clear();
            }

            public void PerformDeletes()
            {
                var keysToDelete = _toDelete.Select(x => _repository.GetKey(x)).ToHashSet();

                if (!keysToDelete.Any())
                {
                    return;
                }

                _repository.DeleteMany(x => keysToDelete.Contains(_repository.GetKey(x)));
            }

            public void PerformUpdates()
            {
                if (!_toUpdate.Any())
                {
                    return;
                }

                _repository.Update(_toUpdate);
            }
        }
    }
}

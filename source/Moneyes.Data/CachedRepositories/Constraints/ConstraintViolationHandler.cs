using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Moneyes.Data
{
    partial class CachedRepository<T, TKey>
    {
        protected internal class ConstraintViolationHandler
        {
            private readonly Dictionary<T, ResolutionMap> _tempResolutionMaps = new();
            private ResolutionMap _resolutionMap = new();

            private readonly ICachedRepository<T, TKey> _repository;
            private readonly ILogger _logger;

            public IEnumerable<T> ToUpdate => _resolutionMap.ToUpdate.Values;
            public IReadOnlySet<TKey> ToReplace => _resolutionMap.ToReplace;

            public ConstraintViolationHandler(ICachedRepository<T, TKey> repository, ILogger logger)
            {
                _repository = repository;
                _logger = logger;
            }

#nullable enable
            public (bool continueValidation, bool ignoreViolation) HandleViolation(ConstraintViolation<T> violation, ConflictResolutionAction? userAction)
#nullable disable
            {
                var conflictResolution = violation.Constraint.ConflictResolution; // default resolution
                ResolutionMap tempResolutionMap;

                // Custom conflict resolution
                if (userAction != null)
                {
                    if (userAction is UpdateConflicResolutionAction<T> updateAction)
                    {
                        _logger.LogInformation("[ConstraintViolationHandler] Choosing advanced conflic resolution 'Update'.");

                        var entityToUpdate = updateAction.UpdateFactory(violation.ExistingEntity, violation.NewEntity);
                        var updateKey = _repository.GetKey(entityToUpdate);

                        // Set the entity to update for this entity

                         tempResolutionMap = GetTempResolutionMap(violation.NewEntity);

                        if (_resolutionMap.ToReplace.Contains(updateKey) ||
                            tempResolutionMap.ToReplace.Contains(updateKey))
                        {
                            _logger.LogInformation("[ConstraintViolationHandler] Updated entity is replaced -> Ignoring violation");

                            return (continueValidation: true, ignoreViolation: true);
                        }

                        tempResolutionMap.ToUpdate[updateKey] = entityToUpdate;


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

                        // Revoke entities, because Ignore > Update, Replace                       

                        if (_tempResolutionMaps.Remove(violation.NewEntity, out tempResolutionMap))
                        {
                            _logger?.LogDebug("[ConstraintViolationHandler] Revoked action of {n} entities.",
                                tempResolutionMap.ToReplace.Count + tempResolutionMap.ToUpdate.Count);
                        }

                        // not include, not continue
                        return (continueValidation: false, ignoreViolation: false);
                    case ConflictResolution.Replace:
                        var replaceKey = _repository.GetKey(violation.ExistingEntity);

                        tempResolutionMap = GetTempResolutionMap(violation.NewEntity);

                        // Revoke update

                        if (tempResolutionMap.ToUpdate.Remove(replaceKey))
                        {
                            _logger?.LogDebug("[ConstraintViolationHandler] Revoked update of {key}.", replaceKey);
                        }

                        tempResolutionMap.ToReplace.Add(replaceKey);

                        // include, continue
                        return (continueValidation: true, ignoreViolation: true);
                    default:
                        return (continueValidation: true, ignoreViolation: false);
                }
            }

            /// <summary>
            /// Gets the temp resolution map for the given entity. <br></br>
            /// Will create a new map if no map exists.
            /// </summary>
            /// <returns></returns>
            private ResolutionMap GetTempResolutionMap(T entity)
            {
                if (!_tempResolutionMaps.TryGetValue(entity, out var map))
                {
                    _tempResolutionMaps.Add(entity, map = new());
                }

                return map;
            }

            /// <summary>
            /// This method should be called when the given entity finished validation. <br></br>
            /// All temporary replace / update actions will be applied.
            /// </summary>
            /// <param name="entity">The entity that finished validating.</param>
            public void OnEntityFinishedValidating(T entity)
            {
                if (_tempResolutionMaps.Remove(entity, out var tempMap))
                {
                    foreach (var replace in tempMap.ToReplace)
                    {
                        _resolutionMap.ToReplace.Add(replace);
                    }

                    foreach (var kvp in tempMap.ToUpdate)
                    {
                        _resolutionMap.ToUpdate.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            public void Reset()
            {
                _tempResolutionMaps.Clear();
                _resolutionMap = new();
            }

            public void PerformDeletes()
            {
                if (_resolutionMap.ToReplace.Any())
                {
                    _repository.DeleteMany(_resolutionMap.ToReplace);
                }
            }

            public void PerformUpdates()
            {
                if (_resolutionMap.ToUpdate.Any())
                {
                    _repository.UpdateMany(_resolutionMap.ToUpdate.Values);
                }
            }

            private class ResolutionMap
            {
                public HashSet<TKey> ToReplace { get; } = new();
                public Dictionary<TKey, T> ToUpdate { get; } = new();
            }
        }
    }
}

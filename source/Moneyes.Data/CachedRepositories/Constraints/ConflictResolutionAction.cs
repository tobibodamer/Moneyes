using System;

namespace Moneyes.Data
{
    /// <summary>
    /// Describes a complex conflic resolution action.
    /// </summary>
    public class ConflictResolutionAction
    {
        /// <summary>
        /// The simple conflic resolution, or <see langword="null"/> if this instance is a more complex resolution action.
        /// </summary>
        public ConflictResolution? Resolution { get; }
        protected ConflictResolutionAction()
        {
            Resolution = null;
        }
        public ConflictResolutionAction(ConflictResolution conflictResolution)
        {
            Resolution = conflictResolution;
        }

        /// <summary>
        /// Creates an empty conflict resolution action, meaning the default resolution will be used.
        /// </summary>
        /// <returns></returns>
        public static ConflictResolutionAction Default()
        {
            return new();
        }

        /// <summary>
        /// Creates a <see cref="ConflictResolution.Ignore"/> action.
        /// </summary>
        /// <returns></returns>
        public static ConflictResolutionAction Ignore()
        {
            return new(ConflictResolution.Ignore);
        }

        /// <summary>
        /// Creates a <see cref="ConflictResolution.Fail"/> action.
        /// </summary>
        /// <returns></returns>
        public static ConflictResolutionAction Fail()
        {
            return new(ConflictResolution.Fail);
        }

        /// <summary>
        /// Creates a <see cref="ConflictResolution.Replace"/> action.
        /// </summary>
        /// <returns></returns>
        public static ConflictResolutionAction Replace()
        {
            return new(ConflictResolution.Replace);
        }

        /// <summary>
        /// Creates an update conflic resolution action, with a given <paramref name="entityToUpdate"/> 
        /// that updates a existing entity to resolve the conflict.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityToUpdate"></param>
        /// <returns></returns>
        public static ConflictResolutionAction Update<T>(Func<T, T, T> updateAction)
        {
            return new UpdateConflicResolutionAction<T>(updateAction);
        }

        /// <summary>
        /// Create a simple <see cref="ConflictResolutionAction"/> from a <see cref="ConflictResolution"/>.
        /// </summary>
        /// <param name="conflictResolution"></param>
        public static implicit operator ConflictResolutionAction(ConflictResolution conflictResolution) =>
            new(conflictResolution);

    }
    internal class UpdateConflicResolutionAction<T> : ConflictResolutionAction
    {
        public Func<T, T, T> UpdateFactory { get; }

        public UpdateConflicResolutionAction(Func<T, T, T> updateAction)
        {
            UpdateFactory = updateAction;
        }
    }
}

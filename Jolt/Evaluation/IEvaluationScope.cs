using Jolt.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jolt.Evaluation
{
    /// <summary>
    /// Represents the scope of closures and range variables available to evaluate.
    /// </summary>
    public interface IEvaluationScope
    {
        /// <summary>
        /// Gets a series of currently available source document closures in closest to farthest order.
        /// </summary>
        IEnumerable<IJsonToken> AvailableClosures { get; }
                
        /// <summary>
        /// Determines whether a variable name is currently in scope.
        /// </summary>
        /// <param name="variableName">The name of the variable to look for.</param>
        /// <returns>True if the variable is present, false otherwise.</returns>
        bool ContainsVariable(string variableName, bool onlyCheckTopLayer = false);

        /// <summary>
        /// Tries to get a currently available variable by name.
        /// </summary>
        /// <param name="variableName">The name of the variable to look for.</param>
        /// <param name="variable">The instance of the variable if present, default otherwise.</param>
        /// <returns>True if the variable is present, false otherwise.</returns>
        bool TryGetVariable(string variableName, out RangeVariable? variable);

        /// <summary>
        /// Includes a source document token as a closure available within the current scope.
        /// </summary>
        /// <param name="token">The source document token to include.</param>
        /// <returns>An instance of <see cref="IEvaluationScope"/> that also contains the closure token.</returns>
        IEvaluationScope CreateClosureOver(IJsonToken token);

        /// <summary>
        /// Adds or updates a variable by name with the provided value.
        /// </summary>
        /// <param name="variable">The variable to add or update.</param>
        /// <param name="forceApplyToCurrentLayer">Forces the variable to be included in the current scope layer if new when true, creates a new scope layer otherwise.</param>
        /// <returns></returns>
        IEvaluationScope AddOrUpdateVariable(RangeVariable variable, bool forceApplyToCurrentLayer = false);

        /// <summary>
        /// Removes the most recent closure.
        /// </summary>
        /// <returns>An instance of <see cref="IEvaluationScope"/> with the most recent closure token removed.</returns>
        IEvaluationScope RemoveCurrentClosure();

        /// <summary>
        /// Removes the most recently added layer of variables. If variables were added to the current layer with the 'force apply' option, 
        /// this action will remove them as well.
        /// </summary>
        /// <returns>An instance of <see cref="IEvaluationScope"/> with the most recent layer of variables removed.</returns>
        IEvaluationScope RemoveCurrentVariablesLayer();
    }
}

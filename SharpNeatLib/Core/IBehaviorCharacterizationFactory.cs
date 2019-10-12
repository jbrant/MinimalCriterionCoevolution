#region

#endregion

namespace SharpNeat.Core
{
    /// <summary>
    ///     Interface for behavior characterization factory.  Behavior characterization factories construct new behavior
    ///     characterizations.
    /// </summary>
    public interface IBehaviorCharacterizationFactory
    {
        /// <summary>
        ///     Generates a behavior characterization of the appropriate type.
        /// </summary>
        /// <returns>The constructed behavior characterization.</returns>
        IBehaviorCharacterization CreateBehaviorCharacterization();
    }
}
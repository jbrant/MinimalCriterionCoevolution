#region

using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a factory for creating new end-point behavior characterization instances.
    /// </summary>
    public class EndPointBehaviorCharacterizationFactory : IBehaviorCharacterizationFactory
    {
        /// <summary>
        ///     Constructs a new end-point behavior characterization.
        /// </summary>
        /// <returns>Constructed end-point behavior characterization.</returns>
        public IBehaviorCharacterization CreateBehaviorCharacterization()
        {
            return new EndPointBehaviorCharacterization();
        }
    }
}
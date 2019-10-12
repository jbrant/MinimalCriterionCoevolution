#region

using SharpNeat.Core;

#endregion

namespace SharpNeat.Behaviors
{
    /// <summary>
    ///     Defines a factory for creating new trajectory behavior characterization instances.
    /// </summary>
    public class TrajectoryBehaviorCharacterizationFactory : IBehaviorCharacterizationFactory
    {
        /// <summary>
        ///     Constructs a new trajectory behavior characterization.
        /// </summary>
        /// <returns>Constructed trajectory behavior characterization.</returns>
        public IBehaviorCharacterization CreateBehaviorCharacterization()
        {
            return new TrajectoryBehaviorCharacterization();
        }
    }
}
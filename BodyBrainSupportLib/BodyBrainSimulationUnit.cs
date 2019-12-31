using System.Collections.Generic;
using SharpNeat.Utility;

namespace BodyBrainSupportLib
{
    /// <summary>
    ///     Encapsulates a point-in-time instance of the simulation state during a body/brain trial.
    /// </summary>
    public struct BodyBrainSimulationTimestepUnit
    {
        /// <summary>
        ///     The current simulation timestep.
        /// </summary>
        public int Timestep { get; }

        /// <summary>
        ///     The current simulation wall/clock time.
        /// </summary>
        public double Time { get; }

        /// <summary>
        ///     The position vector of the voxel body (as measured from the center-of-mass).
        /// </summary>
        public Point3DDouble Position { get; }

        /// <summary>
        ///     The euclidean distance the voxel has traveled (measured from the center-of-mass).
        /// </summary>
        public double Distance { get; }

        /// <summary>
        ///     The number of voxels in contact with the environment floor.
        /// </summary>
        public int VoxelsTouchingFloor { get; }

        /// <summary>
        ///     The velocity of the voxel with highest velocity.
        /// </summary>
        public double MaxVoxelVelocity { get; }

        /// <summary>
        ///     The displacement of the voxel with highest displacement.
        /// </summary>
        public double MaxVoxelDisplacement { get; }

        /// <summary>
        ///     The displacement vector of the voxel body (as measured from the center-of-mass).
        /// </summary>
        public Point3DDouble Displacement { get; }

        /// <summary>
        ///     BodyBrainSimulationTimestepUnit constructor.
        /// </summary>
        /// <param name="timestep">The current simulation timestep.</param>
        /// <param name="time">The current simulation wall/clock time.</param>
        /// <param name="xPosition">The X-component of the voxel body position vector.</param>
        /// <param name="yPosition">The Y-component of the voxel body position vector.</param>
        /// <param name="zPosition">The Z-component of the voxel body position vector.</param>
        /// <param name="distance">The euclidean distance the voxel has traveled (measured from the center-of-mass).</param>
        /// <param name="voxelsTouchingFloor">The number of voxels in contact with the environment floor.</param>
        /// <param name="maxVoxelVelocity">The velocity of the voxel with highest velocity.</param>
        /// <param name="maxVoxelDisplacement">The displacement of the voxel with highest displacement.</param>
        /// <param name="xDisplacement">The X-component of the voxel body displacement vector.</param>
        /// <param name="yDisplacement">The Y-component of the voxel body displacement vector.</param>
        /// <param name="zDisplacement">The Z-component of the voxel body displacement vector.</param>
        public BodyBrainSimulationTimestepUnit(int timestep, double time, double xPosition,
            double yPosition, double zPosition, double distance, int voxelsTouchingFloor, double maxVoxelVelocity,
            double maxVoxelDisplacement, double xDisplacement, double yDisplacement, double zDisplacement)
        {
            Timestep = timestep;
            Time = time;
            Position = new Point3DDouble(xPosition, yPosition, zPosition);
            Distance = distance;
            VoxelsTouchingFloor = voxelsTouchingFloor;
            MaxVoxelVelocity = maxVoxelVelocity;
            MaxVoxelDisplacement = maxVoxelDisplacement;
            Displacement = new Point3DDouble(xDisplacement, yDisplacement, zDisplacement);
        }
    }

    /// <summary>
    ///     Encapsulates the changes in simulation state for the given body/brain combination throughout a trial.
    /// </summary>
    public class BodyBrainSimulationUnit
    {
        /// <summary>
        ///     The BodyBrainSimulationUnit constructor.
        /// </summary>
        /// <param name="brainId">The voxel brain genome ID.</param>
        /// <param name="bodyId">The voxel body genome ID.</param>
        public BodyBrainSimulationUnit(uint brainId, uint bodyId)
        {
            BrainId = brainId;
            BodyId = bodyId;
            BodyBrainSimulationTimestepUnits = new List<BodyBrainSimulationTimestepUnit>();
        }

        /// <summary>
        ///     The voxel brain genome ID.
        /// </summary>
        public uint BrainId { get; }

        /// <summary>
        ///     The voxel body genome ID.
        /// </summary>
        public uint BodyId { get; }

        /// <summary>
        ///     The body/simulation state at each timestep interval.
        /// </summary>
        public IList<BodyBrainSimulationTimestepUnit> BodyBrainSimulationTimestepUnits { get; }

        /// <summary>
        ///     Adds body/simulation state information for the given timestep.
        /// </summary>
        /// <param name="timestep">The current simulation timestep.</param>
        /// <param name="time">The current simulation wall/clock time.</param>
        /// <param name="xPosition">The X-component of the voxel body position vector.</param>
        /// <param name="yPosition">The Y-component of the voxel body position vector.</param>
        /// <param name="zPosition">The Z-component of the voxel body position vector.</param>
        /// <param name="distance">The euclidean distance the voxel has traveled (measured from the center-of-mass).</param>
        /// <param name="voxelsTouchingFloor">The number of voxels in contact with the environment floor.</param>
        /// <param name="maxVoxelVelocity">The velocity of the voxel with highest velocity.</param>
        /// <param name="maxVoxelDisplacement">The displacement of the voxel with highest displacement.</param>
        /// <param name="xDisplacement">The X-component of the voxel body displacement vector.</param>
        /// <param name="yDisplacement">The Y-component of the voxel body displacement vector.</param>
        /// <param name="zDisplacement">The Z-component of the voxel body displacement vector.</param>
        public void AddTimestepInfo(int timestep, double time, double xPosition,
            double yPosition, double zPosition, double distance, int voxelsTouchingFloor, double maxVoxelVelocity,
            double maxVoxelDisplacement, double xDisplacement, double yDisplacement, double zDisplacement)
        {
            BodyBrainSimulationTimestepUnits.Add(new BodyBrainSimulationTimestepUnit(timestep, time, xPosition,
                yPosition, zPosition, distance, voxelsTouchingFloor, maxVoxelVelocity, maxVoxelDisplacement,
                xDisplacement, yDisplacement, zDisplacement));
        }
    }
}
#region

using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    /// <summary>
    ///     Specifies the orientation of the solution path when it intersects the juncture point.
    /// </summary>
    public enum IntersectionOrientation
    {
        /// <summary>
        ///     Indicates a horizontal solution path inbound to the juncture point.
        /// </summary>
        Horizontal,

        /// <summary>
        ///     Indicates a vertical solution path inbound to the juncture point.
        /// </summary>
        Vertical
    }

    /// <summary>
    ///     Specifies the cardinal directions in which a juncture point can be shifted during a mutation and its associated
    ///     ordinal.
    /// </summary>
    public enum PointShift
    {
        /// <summary>
        ///     Indicates a left shift.
        /// </summary>
        Left = 1,

        /// <summary>
        ///     Indicates a right shift.
        /// </summary>
        Right,

        /// <summary>
        ///     Indicates an upward shift.
        /// </summary>
        Up,

        /// <summary>
        ///     Indicates a downward shift.
        /// </summary>
        Down
    }

    /// <summary>
    ///     The path gene encapsulates juncture points on a solution path and the orientation of the solution path inbound to
    ///     that point.
    /// </summary>
    public class PathGene
    {
        #region Public methods

        /// <summary>
        ///     Creates a copy of the current gene.
        /// </summary>
        /// <returns></returns>
        public PathGene CreateCopy()
        {
            return new PathGene(this);
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor which accepts a juncture point and intersection orientation and creates a new path gene.
        /// </summary>
        /// <param name="innovationId">
        ///     The unique "innovation" identifier for this gene (analogous to the innovation IDs on NEAT
        ///     connection genes).
        /// </param>
        /// <param name="juncturePoint">The point at which two paths intersect.</param>
        /// <param name="intersectionOrientation">The orientation (horizontal or vertical) of the incoming path segment.</param>
        public PathGene(uint innovationId, Point2DInt juncturePoint, IntersectionOrientation intersectionOrientation)
        {
            InnovationId = innovationId;
            JuncturePoint = juncturePoint;
            Orientation = intersectionOrientation;
        }

        /// <summary>
        ///     Copy constructor for duplicating path gene.
        /// </summary>
        /// <param name="copyFrom">The path gene to deep copy.</param>
        public PathGene(PathGene copyFrom)
        {
            InnovationId = copyFrom.InnovationId;
            JuncturePoint = copyFrom.JuncturePoint;
            Orientation = copyFrom.Orientation;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     The unique "innovation" identifier for this gene.
        /// </summary>
        public uint InnovationId { get; }

        /// <summary>
        ///     The point at which two path segments perpendicularly intersect.
        /// </summary>
        public Point2DInt JuncturePoint { get; set; }

        /// <summary>
        ///     Orientation for incoming path segment.
        /// </summary>
        public IntersectionOrientation Orientation { get; }

        #endregion Properties
    }
}
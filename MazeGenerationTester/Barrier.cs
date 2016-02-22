#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

#endregion

namespace MazeGenerationTester
{
    public class Barrier
    {
        private static readonly Dictionary<short, BarrierDirectionComponent> _directionMap = new Dictionary
            <short, BarrierDirectionComponent>
        {
            {(short) BarrierDirection.West, new BarrierDirectionComponent(180)},
            {(short) BarrierDirection.NorthWest, new BarrierDirectionComponent(135)},
            {(short) BarrierDirection.North, new BarrierDirectionComponent(90)},
            {(short) BarrierDirection.NorthEast, new BarrierDirectionComponent(45)},
            {(short) BarrierDirection.East, new BarrierDirectionComponent(0)},
            {(short) BarrierDirection.SouthEast, new BarrierDirectionComponent(315)},
            {(short) BarrierDirection.South, new BarrierDirectionComponent(270)},
            {(short) BarrierDirection.SouthWest, new BarrierDirectionComponent(225)}
        };

        private readonly bool _isPenetrating;
        private short _direction;
        public Point startPoint, endPoint;

        public Barrier(int first, int second, int mazeSideLength)
        {
            // Unpack barrier length, direction, and penetration status
            _isPenetrating = Convert.ToBoolean((first & 0x1));
            short direction = Convert.ToInt16((first & 0x0F)/2);
            int length = ((first & 0x7FFFFFFF)/16)%mazeSideLength;

            // Unpack barrier starting point
            startPoint = new Point(second%mazeSideLength, (second/mazeSideLength)%mazeSideLength);

            // Calculate the second point
            endPoint = CalculateEndPoint(startPoint, length, direction);

            // Don't allow ending location to exceed maze boundaries
            if (endPoint.X < 0)
                endPoint.X = 0;
            if (endPoint.X >= mazeSideLength)
                endPoint.X = mazeSideLength - 1;
            if (endPoint.Y < 0)
                endPoint.Y = 0;
            if (endPoint.Y >= mazeSideLength)
                endPoint.Y = mazeSideLength - 1;
        }

        public void UpdateEndPoint(List<Barrier> otherBarriers)
        {
            // If this barrier shouldn't penetrate, reduce its endpoint (if applicable)
            if (_isPenetrating == false)
            {
                foreach (Barrier otherBarrier in otherBarriers)
                {
                    // Only update end point if the "other barrier" is not the current barrier
                    if (otherBarrier != this)
                    {
                        // Calculate the determinant's denominator
                        double denominator = (startPoint.X - endPoint.X)*
                                             (otherBarrier.startPoint.Y - otherBarrier.endPoint.Y) -
                                             (startPoint.Y - endPoint.Y)*
                                             (otherBarrier.startPoint.X - otherBarrier.endPoint.X);

                        if (denominator != 0)
                        {
                            // Calculate the determinants
                            double xDeterminant = ((startPoint.X*endPoint.Y - startPoint.Y*endPoint.X)*
                                                   (otherBarrier.startPoint.X - otherBarrier.endPoint.X) -
                                                   (startPoint.X - endPoint.X)*
                                                   (otherBarrier.startPoint.X*otherBarrier.endPoint.Y -
                                                    otherBarrier.startPoint.Y*otherBarrier.endPoint.X))/denominator;
                            double yDeterminant = ((startPoint.X*endPoint.Y - startPoint.Y*endPoint.X)*
                                                   (otherBarrier.startPoint.Y - otherBarrier.endPoint.Y) -
                                                   (startPoint.Y - endPoint.Y)*
                                                   (otherBarrier.startPoint.X*otherBarrier.endPoint.Y -
                                                    otherBarrier.startPoint.Y*otherBarrier.endPoint.X))/denominator;

                            // Ensure that the intersection point actually lies within both line segments
                            if (xDeterminant >= Math.Min(startPoint.X, endPoint.X) &&
                                xDeterminant <= Math.Max(startPoint.X, endPoint.X) &&
                                xDeterminant >= Math.Min(otherBarrier.startPoint.X, otherBarrier.endPoint.X) &&
                                xDeterminant <= Math.Max(otherBarrier.startPoint.X, otherBarrier.endPoint.X) &&
                                yDeterminant >= Math.Min(startPoint.Y, endPoint.Y) &&
                                yDeterminant <= Math.Max(startPoint.X, startPoint.Y) &&
                                yDeterminant >= Math.Min(otherBarrier.startPoint.Y, otherBarrier.endPoint.Y) &&
                                yDeterminant <= Math.Max(otherBarrier.startPoint.Y, otherBarrier.endPoint.Y))
                            {
                                endPoint = new Point((int) xDeterminant, (int) yDeterminant);
                            }
                        }
                    }
                }
            }
        }

        private static Point CalculateEndPoint(Point startPoint, int length, short direction)
        {
            Point endPoint = new Point();

            endPoint.X = (int) (startPoint.X + length*_directionMap[direction].HorizontalDirectionComponent);
            endPoint.Y = (int) (startPoint.Y + length*_directionMap[direction].VerticalDirectionComponent);

            return endPoint;
        }

        private struct BarrierDirectionComponent
        {
            public BarrierDirectionComponent(int angle)
            {
                HorizontalDirectionComponent = Math.Cos((Math.PI/180)*angle);
                VerticalDirectionComponent = Math.Sin((Math.PI/180)*angle);
            }

            public double HorizontalDirectionComponent { get; }
            public double VerticalDirectionComponent { get; }
        }

        private enum BarrierDirection
        {
            West,
            NorthWest,
            North,
            NorthEast,
            East,
            SouthEast,
            South,
            SouthWest
        }
    }
}
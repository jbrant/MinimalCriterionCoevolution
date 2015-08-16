/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */
using SharpNeat.Utility;

namespace SharpNeat.Domains.BoxesVisualDiscrimination
{
    /// <summary>
    /// Represents test cases for the Boxes visual discrimination task. The test case field is fixed at a resolution of 11x11
    /// the visual field of the agents being evaluated on teh task can have a variable visual field resolution - the visual 
    /// field pixels sample the 11x11 pixels in the test field.
    /// </summary>
    public class TestCaseField
    {
        /// <summary>Resolution of the test field pixel grid.</summary>
        public const int TestFieldResolution = 11;
        const int CoordBoundIdx = TestFieldResolution - 1;
        const int TestFieldPixelCount = TestFieldResolution * TestFieldResolution;

        IntPoint _smallBoxTopLeft;
        IntPoint _largeBoxTopLeft;

        FastRandom _rng;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TestCaseField()
        {
            _rng = new FastRandom();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// (Re)initialize with a fresh test case.
        /// Returns the target point (center of large box).
        /// </summary>
        public IntPoint InitTestCase(int largeBoxRelativePos)
        {
            // Get small and large box center positions.
            IntPoint[] boxPosArr = GenerateRandomTestCase(largeBoxRelativePos);
            _smallBoxTopLeft = boxPosArr[0];
            _largeBoxTopLeft = boxPosArr[1];
            _largeBoxTopLeft.X--;
            _largeBoxTopLeft.Y--;
            return boxPosArr[1];
        }

        /// <summary>
        /// Gets the value of the pixel at a position in the 'real/sensor' coordinate space (continuous x and y, -1 to 1).
        /// </summary>
        public double GetPixel(double x, double y)
        {
            // Quantize real position to test field pixel coords.
            int pixelX = (int)(((x + 1.0) * TestFieldResolution) / 2.0);
            int pixelY = (int)(((y + 1.0) * TestFieldResolution) / 2.0);

            // Test for intersection with small box pixel.
            if(_smallBoxTopLeft.X == pixelX && _smallBoxTopLeft.Y == pixelY) {
                return 1.0;
            }

            // Test for intersection with large box pixel.
            int deltaX = pixelX - _largeBoxTopLeft.X;
            int deltaY = pixelY - _largeBoxTopLeft.Y;
            return (deltaX > -1 && deltaX < 3 && deltaY > -1 && deltaY < 3) ? 1.0 : 0.0;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the coordinate of the small box (the small box occupies a single pixel).
        /// </summary>
        public IntPoint SmallBoxTopLeft
        {
            get { return _smallBoxTopLeft; }
            set { _smallBoxTopLeft = value; }
        }

        /// <summary>
        /// Gets the coordinate of the large box's top left pixel.
        /// </summary>
        public IntPoint LargeBoxTopLeft
        {
            get { return _largeBoxTopLeft; }
            set { _largeBoxTopLeft = value; }
        }

        #endregion

        #region Private Methods

        private IntPoint[] GenerateRandomTestCase(int largeBoxRelativePos)
        {
            // Randomly select a position for the small box (the box is a single pixel in size).
            IntPoint smallBoxPos = new IntPoint(_rng.Next(TestFieldResolution), _rng.Next(TestFieldResolution));
            
            // Large box center is 5 pixels to the right, down or diagonally from the small box.
            IntPoint largeBoxPos = smallBoxPos;
            switch(largeBoxRelativePos)
            {
                case 0: // Right
                    largeBoxPos.X += 5;
                    break;
                case 1: // Down
                    largeBoxPos.Y += 5;
                    break;
                case 2: // Diagonal
                    // Two alternate position get us to exactly 5 pixels distant from the small box.
                    if(_rng.NextBool())
                    {
                        largeBoxPos.X += 3;
                        largeBoxPos.Y += 4;
                    }
                    else
                    {
                        largeBoxPos.X += 4;
                        largeBoxPos.Y += 3;
                    }
                    break;
            }

            // Handle cases where the large box is outside the visual field or overlapping the edge.
            if(largeBoxPos.X > CoordBoundIdx) 
            {   // Wrap around.
                largeBoxPos.X -= TestFieldResolution;

                if(0 == largeBoxPos.X)
                {   // Move box fully into the visual field.
                    largeBoxPos.X++;
                }
            }
            else if(CoordBoundIdx == largeBoxPos.X)
            {   // Move box fully into the visual field.
                largeBoxPos.X--;
            }
            else if(0 == largeBoxPos.X)
            {   // Move box fully into the visual field.
                largeBoxPos.X++;
            }


            if(largeBoxPos.Y > CoordBoundIdx) 
            {   // Wrap around.
                largeBoxPos.Y -= TestFieldResolution;

                if(0 == largeBoxPos.Y)
                {   // Move box fully into the visual field.
                    largeBoxPos.Y++;
                }
            }
            else if(CoordBoundIdx == largeBoxPos.Y)
            {   // Move box fully into the visual field.
                largeBoxPos.Y--;
            }
            else if(0 == largeBoxPos.Y)
            {   // Move box fully into the visual field.
                largeBoxPos.Y++;
            }
            return new IntPoint[] {smallBoxPos, largeBoxPos};
        }

        #endregion
    }
}

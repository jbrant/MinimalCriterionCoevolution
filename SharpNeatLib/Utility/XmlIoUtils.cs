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

#region

using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Genomes.Substrate;

#endregion

namespace SharpNeat.Utility
{
    /// <summary>
    ///     Static helper methods for XML IO.
    /// </summary>
    public static class XmlIoUtils
    {
        #region Higher-level, type-agnostic genome serialization

        /// <summary>
        ///     Serializes a NEAT or maze genome into a string.
        /// </summary>
        /// <param name="genome">The genome object to serialize.</param>
        /// <returns>The serialized genome string.</returns>
        public static string GetGenomeXml(object genome)
        {
            // Create a new string writer into which to write the genome XML
            var genomeStringWriter = new StringWriter();

            switch (genome)
            {
                // Serialize NeatGenome
                case NeatGenome neatGenome:
                {
                    using (var genomeTextWriter = new XmlTextWriter(genomeStringWriter))
                    {
                        NeatGenomeXmlIO.WriteComplete(genomeTextWriter, neatGenome,
                            neatGenome.ActivationFnLibrary.GetFunctionList().Count > 1);
                    }

                    break;
                }
                // Serialize NeatSubstrateGenome
                case NeatSubstrateGenome neatSubstrateGenome:
                {
                    using (var genomeTextWriter = new XmlTextWriter(genomeStringWriter))
                    {
                        NeatSubstrateGenomeXmlIO.WriteComplete(genomeTextWriter, neatSubstrateGenome,
                            neatSubstrateGenome.ActivationFnLibrary.GetFunctionList().Count > 1);
                    }

                    break;
                }
                // Serialize MazeGenome
                case MazeGenome mazeGenome:
                {
                    using (var genomeTextWriter = new XmlTextWriter(genomeStringWriter))
                    {
                        MazeGenomeXmlIO.WriteComplete(genomeTextWriter, mazeGenome);
                    }

                    break;
                }
            }

            // Convert to a string representation and return
            return genomeStringWriter.ToString();
        }

        #endregion

        #region Public Static Methods [Low-level XML Parsing]

        /// <summary>
        ///     Read from the XmlReader until we encounter an element. If the name doesn't match
        ///     elemName then throw an exception, else return normally.
        /// </summary>
        public static void MoveToElement(XmlReader xr, bool skipCurrent, string elemName)
        {
            var localName = MoveToElement(xr, skipCurrent);
            if (localName != elemName)
            {
                // No element or unexpected element.
                throw new InvalidDataException(string.Format("Expected element [{0}], encountered [{1}]", elemName,
                    localName));
            }
        }

        /// <summary>
        ///     Read from the XmlReader until we encounter an element.
        ///     Return the Local name of the element or null if no element was found before
        ///     the end of the input.
        /// </summary>
        public static string MoveToElement(XmlReader xr, bool skipCurrent)
        {
            // Optionally skip the current node.
            if (skipCurrent)
            {
                if (!xr.Read())
                {
                    // EOF.
                    return null;
                }
            }

            // Keep reading until we encounter an element.
            do
            {
                if (XmlNodeType.Element == xr.NodeType)
                {
                    return xr.LocalName;
                }
            } while (xr.Read());

            // No element encountered.
            return null;
        }

        /// <summary>
        ///     Read the named attribute and parse its string value as a boolean.
        /// </summary>
        public static bool ReadAttributeAsBool(XmlReader xr, string attrName)
        {
            var valStr = xr.GetAttribute(attrName);
            return bool.Parse(valStr);
        }

        /// <summary>
        ///     Read the named attribute and parse its string value as an integer.
        /// </summary>
        public static int ReadAttributeAsInt(XmlReader xr, string attrName)
        {
            var valStr = xr.GetAttribute(attrName);
            return int.Parse(valStr, NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///     Read the named attribute and parse its string value as a uint.
        /// </summary>
        public static uint ReadAttributeAsUInt(XmlReader xr, string attrName)
        {
            var valStr = xr.GetAttribute(attrName);
            return uint.Parse(valStr, NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///     Read the named attribute and parse its string value as a double.
        /// </summary>
        public static double ReadAttributeAsDouble(XmlReader xr, string attrName)
        {
            var valStr = xr.GetAttribute(attrName);
            return double.Parse(valStr, NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///     Read the named attribute and parse its string value as an array of doubles.
        /// </summary>
        public static double[] ReadAttributeAsDoubleArray(XmlReader xr, string attrName)
        {
            var valStr = xr.GetAttribute(attrName);
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            // Parse comma separated values.
            var strArr = valStr.Split(',');
            var dblArr = new double[strArr.Length];
            for (var i = 0; i < strArr.Length; i++)
            {
                dblArr[i] = double.Parse(strArr[i], NumberFormatInfo.InvariantInfo);
            }

            return dblArr;
        }

        /// <summary>
        ///     Read the named attribute as a string (no parsing is involved here).
        /// </summary>
        /// <param name="xr">XML reader reference.</param>
        /// <param name="attrName">Attribute name.</param>
        /// <returns>String value of attribute.</returns>
        public static string ReadAttributeAsString(XmlReader xr, string attrName)
        {
            return xr.GetAttribute(attrName);
        }

        /// <summary>
        ///     Writes a double array as a comma separated list of values.
        /// </summary>
        public static void WriteAttributeString(XmlWriter xw, string attrName, double[] arr)
        {
            if (null == arr || arr.Length == 0)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(arr[0].ToString("R", NumberFormatInfo.InvariantInfo));
            for (var i = 1; i < arr.Length; i++)
            {
                sb.Append(',');
                sb.Append(arr[i].ToString("R", NumberFormatInfo.InvariantInfo));
            }

            xw.WriteAttributeString(attrName, sb.ToString());
        }

        #endregion
    }
}
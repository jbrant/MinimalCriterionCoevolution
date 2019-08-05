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

using System;
using System.Xml;

#endregion

namespace MCC_Domains.Utils
{
    /// <summary>
    ///     Static helper methods for reading value from XML configuration data in DOM form.
    /// </summary>
    public static class XmlUtils
    {
        /// <summary>
        ///     Parse the inner text of element with the given name as an integer. If element is missing or parsing fails then
        ///     throws an ArgumentException.
        /// </summary>
        public static int GetValueAsInt(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsInt(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val.Value;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as an unsigned integer. If element is missing or parsing fails
        ///     then throws an ArgumentException.
        /// </summary>
        public static uint GetValueAsUInt(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsUInt(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val.Value;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as an integer. If element is missing or parsing fails then
        ///     returns null.
        /// </summary>
        public static int? TryGetValueAsInt(XmlElement xmlParent, string elemName)
        {
            var xmlElem = xmlParent.SelectSingleNode(elemName) as XmlElement;
            if (null == xmlElem)
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            if (int.TryParse(valStr, out var result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as an unsigned integer. If element is missing or parsing fails
        ///     then returns null.
        /// </summary>
        public static uint? TryGetValueAsUInt(XmlElement xmlParent, string elemName)
        {
            var xmlElem = xmlParent.SelectSingleNode(elemName) as XmlElement;
            if (null == xmlElem)
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            if (uint.TryParse(valStr, out var result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as an unsigned long. If element is missing or parsing fails
        ///     then
        ///     throws an ArgumentException.
        /// </summary>
        public static ulong GetValueAsULong(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsULong(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val.Value;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as an unsigned long. If element is missing or parsing fails
        ///     then
        ///     returns null.
        /// </summary>
        public static ulong? TryGetValueAsULong(XmlElement xmlParent, string elemName)
        {
            var xmlElem = xmlParent.SelectSingleNode(elemName) as XmlElement;
            if (null == xmlElem)
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            if (ulong.TryParse(valStr, out var result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as a double. If element is missing or parsing fails then
        ///     throws an ArgumentException.
        /// </summary>
        public static double GetValueAsDouble(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsDouble(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val.Value;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as a double. If element is missing or parsing fails then
        ///     returns null.
        /// </summary>
        public static double? TryGetValueAsDouble(XmlElement xmlParent, string elemName)
        {
            var xmlElem = xmlParent.SelectSingleNode(elemName) as XmlElement;
            if (null == xmlElem)
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            if (double.TryParse(valStr, out var result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as a boolean. If element is missing or parsing fails then
        ///     throws an ArgumentException.
        /// </summary>
        public static bool GetValueAsBool(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsBool(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val.Value;
        }

        /// <summary>
        ///     Parse the inner text of element with the given name as a boolean. If element is missing or parsing fails then
        ///     returns null.
        /// </summary>
        public static bool? TryGetValueAsBool(XmlElement xmlParent, string elemName)
        {
            var xmlElem = xmlParent.SelectSingleNode(elemName) as XmlElement;
            if (null == xmlElem)
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            if (string.IsNullOrEmpty(valStr))
            {
                return null;
            }

            if (bool.TryParse(valStr, out var result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        ///     Read the inner text of element with the given name. If element is missing then throws an ArgumentException.
        /// </summary>
        public static string GetValueAsString(XmlElement xmlParent, string elemName)
        {
            var val = TryGetValueAsString(xmlParent, elemName);
            if (null == val)
            {
                throw new ArgumentException($"Missing [{elemName}] configuration setting.");
            }
            return val;
        }

        /// <summary>
        ///     Read the inner text of element with the given name. If element is missing then returns null.
        /// </summary>
        public static string TryGetValueAsString(XmlElement xmlParent, string elemName)
        {
            if (!(xmlParent.SelectSingleNode(elemName) is XmlElement xmlElem))
            {
                return null;
            }

            var valStr = xmlElem.InnerText;
            
            return string.IsNullOrEmpty(valStr) ? null : valStr;
        }
    }
}
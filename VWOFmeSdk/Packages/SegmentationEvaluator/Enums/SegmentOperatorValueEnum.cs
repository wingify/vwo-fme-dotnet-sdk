#pragma warning disable 1587
/**
 * Copyright 2024-2025 Wingify Software Pvt. Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#pragma warning disable 1587

using System;

namespace VWOFmeSdk.Packages.SegmentationEvaluator.Enums
{
    public enum SegmentOperatorValueEnum
    {
        AND,
        NOT,
        OR,
        CUSTOM_VARIABLE,
        USER,
        COUNTRY,
        REGION,
        CITY,
        OPERATING_SYSTEM,
        DEVICE_TYPE,
        BROWSER_AGENT,
        UA,
        DEVICE,
        FEATURE_ID,
        IP,
        BROWSER_VERSION,
        OS_VERSION
    }

    public static class SegmentOperatorValueEnumExtensions
    {
        /// <summary>
        /// Returns the string value of the enum
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static string GetValue(this SegmentOperatorValueEnum operand)
        {
            switch (operand)
            {
                case SegmentOperatorValueEnum.AND:
                    return "and";
                case SegmentOperatorValueEnum.NOT:
                    return "not";
                case SegmentOperatorValueEnum.OR:
                    return "or";
                case SegmentOperatorValueEnum.CUSTOM_VARIABLE:
                    return "custom_variable";
                case SegmentOperatorValueEnum.USER:
                    return "user";
                case SegmentOperatorValueEnum.COUNTRY:
                    return "country";
                case SegmentOperatorValueEnum.REGION:
                    return "region";
                case SegmentOperatorValueEnum.CITY:
                    return "city";
                case SegmentOperatorValueEnum.OPERATING_SYSTEM:
                    return "os";
                case SegmentOperatorValueEnum.DEVICE_TYPE:
                    return "device_type";
                case SegmentOperatorValueEnum.BROWSER_AGENT:
                    return "browser_string";
                case SegmentOperatorValueEnum.UA:
                    return "ua";
                case SegmentOperatorValueEnum.DEVICE:
                    return "device";
                case SegmentOperatorValueEnum.FEATURE_ID:
                    return "featureId";
                case SegmentOperatorValueEnum.IP:
                    return "ip_address";
                case SegmentOperatorValueEnum.BROWSER_VERSION:
                    return "browser_version";
                case SegmentOperatorValueEnum.OS_VERSION:
                    return "os_version";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operand), operand, null);
            }
        }

        public static SegmentOperatorValueEnum FromValue(string value)
        {
            switch (value)
            {
                case "and":
                    return SegmentOperatorValueEnum.AND;
                case "not":
                    return SegmentOperatorValueEnum.NOT;
                case "or":
                    return SegmentOperatorValueEnum.OR;
                case "custom_variable":
                    return SegmentOperatorValueEnum.CUSTOM_VARIABLE;
                case "user":
                    return SegmentOperatorValueEnum.USER;
                case "country":
                    return SegmentOperatorValueEnum.COUNTRY;
                case "region":
                    return SegmentOperatorValueEnum.REGION;
                case "city":
                    return SegmentOperatorValueEnum.CITY;
                case "os":
                    return SegmentOperatorValueEnum.OPERATING_SYSTEM;
                case "device_type":
                    return SegmentOperatorValueEnum.DEVICE_TYPE;
                case "browser_string":
                    return SegmentOperatorValueEnum.BROWSER_AGENT;
                case "ua":
                    return SegmentOperatorValueEnum.UA;
                case "device":
                    return SegmentOperatorValueEnum.DEVICE;
                case "featureId":
                    return SegmentOperatorValueEnum.FEATURE_ID;
                case "ip_address":
                    return SegmentOperatorValueEnum.IP;
                case "browser_version":
                    return SegmentOperatorValueEnum.BROWSER_VERSION;
                case "os_version":
                    return SegmentOperatorValueEnum.OS_VERSION;
                default:
                    throw new ArgumentException("No enum constant with value " + value);
            }
        }
    }
}

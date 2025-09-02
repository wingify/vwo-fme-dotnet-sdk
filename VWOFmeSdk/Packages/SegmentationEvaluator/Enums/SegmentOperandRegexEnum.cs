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
    public enum SegmentOperandRegexEnum
    {
        LOWER,
        LOWER_MATCH,
        WILDCARD,
        WILDCARD_MATCH,
        REGEX,
        REGEX_MATCH,
        STARTING_STAR,
        ENDING_STAR,
        GREATER_THAN_MATCH,
        GREATER_THAN_EQUAL_TO_MATCH,
        LESS_THAN_MATCH,
        LESS_THAN_EQUAL_TO_MATCH
    }

    public static class SegmentOperandRegexEnumExtensions
    {
        /// <summary>
        /// Get regex for the operand
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static string GetRegex(this SegmentOperandRegexEnum operand)
        {
            switch (operand)
            {
                case SegmentOperandRegexEnum.LOWER:
                    return "^lower";
                case SegmentOperandRegexEnum.LOWER_MATCH:
                    return "^lower\\((.*)\\)";
                case SegmentOperandRegexEnum.WILDCARD:
                    return "^wildcard";
                case SegmentOperandRegexEnum.WILDCARD_MATCH:
                    return "^wildcard\\((.*)\\)";
                case SegmentOperandRegexEnum.REGEX:
                    return "^regex";
                case SegmentOperandRegexEnum.REGEX_MATCH:
                    return "^regex\\((.*)\\)";
                case SegmentOperandRegexEnum.STARTING_STAR:
                    return "^\\*";
                case SegmentOperandRegexEnum.ENDING_STAR:
                    return "\\*$";
                case SegmentOperandRegexEnum.GREATER_THAN_MATCH:
                    return "^gt\\(([\\d.]+)\\)";
                case SegmentOperandRegexEnum.GREATER_THAN_EQUAL_TO_MATCH:
                    return "^gte\\(([\\d.]+)\\)";
                case SegmentOperandRegexEnum.LESS_THAN_MATCH:
                    return "^lt\\(([\\d.]+)\\)";
                case SegmentOperandRegexEnum.LESS_THAN_EQUAL_TO_MATCH:
                    return "^lte\\(([\\d.]+)\\)";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operand), operand, null);
            }
        }
    }
}

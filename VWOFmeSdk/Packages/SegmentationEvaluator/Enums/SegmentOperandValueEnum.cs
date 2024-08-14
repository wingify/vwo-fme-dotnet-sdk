#pragma warning disable 1587
/**
 * Copyright 2024 Wingify Software Pvt. Ltd.
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
    public enum SegmentOperandValueEnum
    {
        LOWER_VALUE = 1,
        STARTING_ENDING_STAR_VALUE = 2,
        STARTING_STAR_VALUE = 3,
        ENDING_STAR_VALUE = 4,
        REGEX_VALUE = 5,
        EQUAL_VALUE = 6,
        GREATER_THAN_VALUE = 7,
        GREATER_THAN_EQUAL_TO_VALUE = 8,
        LESS_THAN_VALUE = 9,
        LESS_THAN_EQUAL_TO_VALUE = 10
    }

    public static class SegmentOperandValueEnumExtensions
    {
        /// <summary>
        /// Get value for the operand
        /// </summary>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static int GetValue(this SegmentOperandValueEnum operand)
        {
            switch (operand)
            {
                case SegmentOperandValueEnum.LOWER_VALUE:
                    return 1;
                case SegmentOperandValueEnum.STARTING_ENDING_STAR_VALUE:
                    return 2;
                case SegmentOperandValueEnum.STARTING_STAR_VALUE:
                    return 3;
                case SegmentOperandValueEnum.ENDING_STAR_VALUE:
                    return 4;
                case SegmentOperandValueEnum.REGEX_VALUE:
                    return 5;
                case SegmentOperandValueEnum.EQUAL_VALUE:
                    return 6;
                case SegmentOperandValueEnum.GREATER_THAN_VALUE:
                    return 7;
                case SegmentOperandValueEnum.GREATER_THAN_EQUAL_TO_VALUE:
                    return 8;
                case SegmentOperandValueEnum.LESS_THAN_VALUE:
                    return 9;
                case SegmentOperandValueEnum.LESS_THAN_EQUAL_TO_VALUE:
                    return 10;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operand), operand, null);
            }
        }
    }
}

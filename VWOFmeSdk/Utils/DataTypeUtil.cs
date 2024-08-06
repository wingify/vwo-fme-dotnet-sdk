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
#pragma warning restore 1587

using System;

/// <summary>
/// This class contains utility methods to check the data type of a variable.
/// </summary>
namespace VWOFmeSdk.Utils
{
    public static class DataTypeUtil
    {
        public static bool IsObject(object val)
        {
            return val != null && !(val is Array) && !(val is Delegate) && !(val is string) && !(val is DateTime);
        }

        public static bool IsArray(object val)
        {
            return val is Array;
        }

        public static bool IsNull(object val)
        {
            return val == null;
        }

        public static bool IsUndefined(object val)
        {
            return val == null;
        }

        public static bool IsDefined(object val)
        {
            return val != null;
        }

        public static bool IsNumber(object val)
        {
            return val is double || val is float || val is int || val is long || val is decimal;
        }

        public static bool IsInteger(object val)
        {
            return val is int;
        }

        public static bool IsString(object val)
        {
            return val is string;
        }

        public static bool IsBoolean(object val)
        {
            return val is bool;
        }

        public static bool IsNaN(object val)
        {
            return val is double && double.IsNaN((double)val);
        }

        public static bool IsDate(object val)
        {
            return val is DateTime;
        }

        public static bool IsFunction(object val)
        {
            return val is Delegate;
        }

        public static string GetType(object val)
        {
            if (IsObject(val))
            {
                return "Object";
            }
            if (IsArray(val))
            {
                return "Array";
            }
            if (IsNull(val))
            {
                return "Null";
            }
            if (IsUndefined(val))
            {
                return "Undefined";
            }
            if (IsNaN(val))
            {
                return "NaN";
            }
            if (IsNumber(val))
            {
                return "Number";
            }
            if (IsString(val))
            {
                return "String";
            }
            if (IsBoolean(val))
            {
                return "Boolean";
            }
            if (IsDate(val))
            {
                return "Date";
            }
            if (IsFunction(val))
            {
                return "Function";
            }
            if (IsInteger(val))
            {
                return "Integer";
            }
            return "Unknown Type";
        }
    }
}

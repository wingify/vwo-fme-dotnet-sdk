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
#pragma warning restore 1587

using System;
using VWOFmeSdk.Interfaces.Batching;

namespace VWOFmeSdk.Models
{
    public class BatchEventData
    {
        public int EventsPerRequest { get; set; } = 100; // Default value
        public int RequestTimeInterval { get; set; } = 600; // Default value (in seconds)
        public IFlushInterface FlushCallback { get; set; }
    }
}
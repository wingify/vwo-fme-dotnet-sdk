/**
 * Copyright 2024-2026 Wingify Software Pvt. Ltd.
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

using WingifyFmeSdk.Packages.NetworkLayer.Client;
using Xunit;

namespace VWOFlagTesting.Tests
{
    public class NetworkClientContentTypeTests
    {
        // The settings endpoint responds with "application/json", while data-residency
        // (region-prefixed) settings endpoints respond with "application/javascript" for the same
        // JSON body. Both must be accepted so background settings polling succeeds.
        // See https://github.com/wingify/vwo-fme-dotnet-sdk/issues/8
        [Theory]
        [InlineData("application/json")]
        [InlineData("application/json; charset=UTF-8")]
        [InlineData("application/javascript")]
        [InlineData("application/javascript; charset=UTF-8")]
        public void IsJsonCompatibleContentType_AcceptsJsonAndJavascript(string contentType)
        {
            Assert.True(NetworkClient.IsJsonCompatibleContentType(contentType));
        }

        [Theory]
        [InlineData("text/html")]
        [InlineData("text/plain")]
        [InlineData("application/xml")]
        [InlineData("")]
        [InlineData(null)]
        public void IsJsonCompatibleContentType_RejectsNonJsonAndNull(string contentType)
        {
            Assert.False(NetworkClient.IsJsonCompatibleContentType(contentType));
        }
    }
}

﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using FluentAssertions;

namespace chocolatey.tests
{
    public static class MockLoggerExtensions
    {
        public static void ShouldHaveWarningContaining(this MockLogger logger, string expectedSubstring)
        {
            logger.Messages.Should().ContainKey(LogLevel.Warn.ToString())
                .WhoseValue.Should().Contain(w => w.Contains(expectedSubstring));
        }
    }
}

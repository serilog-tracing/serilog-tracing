// Copyright © SerilogTracing Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SerilogTracing.Expressions;

/// <summary>
/// Provides some simple default formatters that incorporate properties added by SerilogTracing.
/// </summary>
public static class Formatters
{
    /// <summary>
    /// Produces a text format that includes span timings.
    /// </summary>
    /// <param name="theme">Optional template theme to apply, useful only for ANSI console output.</param>
    /// <returns>The formatter.</returns>
    public static ITextFormatter CreateConsoleTextFormatter(TemplateTheme? theme = null)
    {
        return new ExpressionTemplate(
            "[{@t:HH:mm:ss} {@l:u3}] " +
            "{#if IsRootSpan()}\u2514\u2500 {#else if IsSpan()}\u251c {#else if @sp is not null}\u2502 {#else}\u250A {#end}" +
            "{@m}" +
            "{#if IsSpan()} ({Milliseconds(Elapsed()):0.###} ms){#end}" +
            "\n" +
            "{@x}",
            theme: theme,
            nameResolver: new TracingNameResolver());
    }
}
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

using System.Text.RegularExpressions;

namespace SerilogTracing.Instrumentation.SqlClient;

static class CommandTextTokenizer
{
    static readonly Regex Pattern = new("\\b(select|insert|update|delete|exec)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
    public static string? FindFirstOperation(string sql)
    {
        var m = Pattern.Match(sql);
        return m.Success ? m.Groups[0].Value.ToUpperInvariant() : null;
    }
}
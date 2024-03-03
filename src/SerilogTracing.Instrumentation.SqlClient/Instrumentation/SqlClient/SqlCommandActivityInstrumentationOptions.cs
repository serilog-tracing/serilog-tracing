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

using Microsoft.Data.SqlClient;

namespace SerilogTracing.Instrumentation.SqlClient;

/// <summary>
/// Configuration for <see cref="SqlCommand"/> instrumentation.
/// </summary>
public sealed class SqlCommandActivityInstrumentationOptions
{
    /// <summary>
    /// Include the command text (i.e. the stored procedure name, SQL statement, or table name) in spans. The risk
    /// of exposing sensitive data, and bloating span size, means that the default value is <c langword="false"/>.
    /// </summary>
    public bool IncludeCommandText { get; set; } = false;

    /// <summary>
    /// Attempt to infer the operation (<c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>EXEC</c>)
    /// by inspecting the command text. The inferred value may be incorrect in some cases as only very limited command
    /// parsing is performed. The default is <c langword="true"/>.
    /// </summary>
    public bool InferOperation { get; set; } = true;
}

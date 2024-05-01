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

using System.Data;
using Microsoft.Data.SqlClient;

namespace SerilogTracing.Instrumentation.SqlClient;

static class SqlCommandInspector
{
    public static string GetOperation(SqlCommand command, bool inferOperationFromCommandText)
    {
        if (command.CommandType == CommandType.StoredProcedure)
            return "EXEC";

        if (command.CommandType == CommandType.TableDirect)
            return "DIRECT";

        return inferOperationFromCommandText ?
            CommandTextTokenizer.FindFirstOperation(command.CommandText) ?? "BATCH" :
            "BATCH";
    }
}

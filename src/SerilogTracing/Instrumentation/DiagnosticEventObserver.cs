﻿// Copyright © SerilogTracing Contributors
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

using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticEventObserver : IObserver<KeyValuePair<string, object?>>
{
    readonly IActivityInstrumentor _instrumentor;

    internal DiagnosticEventObserver(IActivityInstrumentor instrumentor)
    {
        _instrumentor = instrumentor;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Value == null || Activity.Current == null) return;

        OnNext(Activity.Current, value.Key, value.Value);
    }

    internal void OnNext(Activity activity, string eventName, object eventValue)
    {
        if (!ActivityInstrumentation.IsDataSuppressed(activity))
        {
            _instrumentor.InstrumentActivity(activity, eventName, eventValue);
        }
    }
}
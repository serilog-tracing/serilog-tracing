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

namespace SerilogTracing.Instrumentation;

// This alternative implementation to DiagnosticEventObserver avoids an extra downcast or flow control in that
// type, for instrumentors that require raw events.
sealed class DirectDiagnosticEventObserver: IObserver<KeyValuePair<string, object?>>
{
    readonly IObserver<KeyValuePair<string, object?>> _inner;
    readonly IInstrumentationEventObserver _observer;

    internal DirectDiagnosticEventObserver(IObserver<KeyValuePair<string, object?>> inner, IInstrumentationEventObserver observer)
    {
        _inner = inner;
        _observer = observer;
    }

    public void OnCompleted() => _inner.OnCompleted();

    public void OnError(Exception error) => _inner.OnError(error);

    public void OnNext(KeyValuePair<string, object?> value)
    {
        _observer.OnNext(value.Key, value.Value);
        _inner.OnNext(value);
    }
}

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

using System.Diagnostics;

namespace SerilogTracing.Instrumentation;

sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>, IDisposable
{
    internal DiagnosticListenerObserver(IReadOnlyList<IActivityInstrumentor> instrumentors)
    {
        _instrumentors = instrumentors;
    }

    readonly IReadOnlyList<IActivityInstrumentor> _instrumentors;
    readonly List<IDisposable?> _subscriptions = [];

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(DiagnosticListener value)
    {
        foreach (var instrumentor in _instrumentors)
        {
            if (!instrumentor.ShouldSubscribeTo(value.Name)) continue;

            IObserver<KeyValuePair<string, object?>> observer = new DiagnosticEventObserver(instrumentor);
            if (instrumentor is IInstrumentationEventObserver direct)
            {
                observer = new DirectDiagnosticEventObserver(observer, direct);
            }

            _subscriptions.Add(value.Subscribe(observer));
        }
    }

    public void Dispose()
    {
        var failedDisposes = new List<Exception>();

        foreach (var subscription in _subscriptions)
        {
            try
            {
                subscription?.Dispose();
            }
            catch (Exception e)
            {
                failedDisposes.Add(e);
            }
        }

        if (failedDisposes.Count > 0)
        {
            throw new AggregateException(failedDisposes);
        }
    }
}

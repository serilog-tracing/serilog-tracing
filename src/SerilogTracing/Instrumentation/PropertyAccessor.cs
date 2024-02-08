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

using System.Collections.Concurrent;
using System.Reflection;

namespace SerilogTracing.Instrumentation;

/// <summary>
/// Helper type for dynamically retrieving properties from anonymously-typed diagnostic event args.
/// </summary>
/// <param name="propertyName">The property name to retrieve.</param>
/// <typeparam name="T">Type type of the property value.</typeparam>
/// <remarks>Not much optimization done here, yet, but some opportunities.</remarks>
public class PropertyAccessor<T>(string propertyName)
    where T : notnull
{
    // It's possible that the receiver type may vary even during a single execution of the
    // program. Keeping a per-type mapping here avoids this thwarting our attempts to
    // optimize, but the dictionary type will need to be carefully chosen.
    const int MaxCachedAccessors = 5;
    readonly ConcurrentDictionary<Type, Func<object, (bool, T?)>> _accessors = [];

    /// <summary>
    /// Get the value of the property.
    /// </summary>
    /// <param name="receiver">The object to retrieve the property from.</param>
    /// <param name="value">The value, if the property could be retrieved.</param>
    /// <returns>True if the property exists on the receiver. Note that the property
    /// value may still be <c langword="null"/> in that case.</returns>
    public bool TryGetValue(object receiver, out T? value)
    {
        var accessor = _accessors.GetOrAdd(receiver.GetType(), receiverType =>
        {
            var property = receiverType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);

            if (property == null)
            {
                return _ => (false, default);
            }

            return r =>
            {
                // Not present.
                var pv = property.GetValue(r);
                return pv switch
                {
                    // Present, non-null.
                    T t => (true, t),
                    // Present, but null.
                    null => (true, default),
                    // The value is not of the expected type; possibly need to self-log here.
                    _ => (false, default)
                };
            };
        });

        if (_accessors.Count > MaxCachedAccessors)
        {
            // No great solution to this using `ConcurrentDictionary`; we'll repopulate on the next
            // access.
            _accessors.Clear();
        }

        var (accessed, exists) = accessor(receiver);
        value = exists;
        return accessed;
    }
}

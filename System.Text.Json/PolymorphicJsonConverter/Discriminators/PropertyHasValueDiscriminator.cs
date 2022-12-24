namespace Rosser.Json.Discriminators;

using System;
using System.Text.Json;

public record PropertyHasValueDiscriminator : IDiscriminatorOptions
{
    private readonly string propertyName;
    private readonly object? propertyValue;

    public PropertyHasValueDiscriminator(string propertyName, object? propertyValue)
    {
        this.propertyName = propertyName;
        this.propertyValue = propertyValue;
    }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        string propertyName = propertyNamingPolicy?.ConvertName(this.propertyName) ?? this.propertyName;

        return new(element =>
        {
            return element.TryGetProperty(propertyName, out JsonElement prop) && IsMatch(prop, this.propertyValue);
        });

        static bool IsMatch(JsonElement element, object? v)
        {
            return v switch
            {
                null => element.ValueKind == JsonValueKind.Null,
                string value => IsStringMatch(element, value),
                int value => IsInt32Match(element, value),
                double value => IsDoubleMatch(element, value),
                float value => IsSingleMatch(element, value),
                bool value => IsBoolMatch(element, value),
                byte value => IsByteMatch(element, value),
                Array values => IsArrayMatch(element, values),
                _ => false,
            };
        }

        static bool IsByteMatch(JsonElement element, byte value)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetByte(out byte b) && b == value;
        }

        static bool IsBoolMatch(JsonElement element, bool value)
        {
            return (element.ValueKind == JsonValueKind.True && value) || (element.ValueKind == JsonValueKind.False && !value);
        }

        static bool IsSingleMatch(JsonElement element, float value)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out float f) && f == value;
        }

        static bool IsDoubleMatch(JsonElement element, double value)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double d) && d == value;
        }

        static bool IsInt32Match(JsonElement element, int value)
        {
            return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int i) && i == value;
        }

        static bool IsStringMatch(JsonElement element, string value)
        {
            return element.ValueKind == JsonValueKind.String && element.GetString() == value;
        }

        static bool IsArrayMatch(JsonElement element, Array array)
        {
            JsonElement[] items = element.EnumerateArray().ToArray();

            if (items.Length == 0)
            {
                return array.Length == 0;
            }
            else if (items.Length != array.Length)
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (!IsMatch(items[i], array.GetValue(i)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
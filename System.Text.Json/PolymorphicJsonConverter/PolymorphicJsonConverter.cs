namespace PRosser.Json
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal class PolymorphicJsonConverter<T> : JsonConverter<T>
        where T : class
    {
        private readonly PolymorphicJsonConverterAbstractTypeOptions options;

        public PolymorphicJsonConverter(PolymorphicJsonConverterAbstractTypeOptions options)
        {
            this.options = options;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return !this.TryGetConcreteType(reader, out Type? concreteType) ||
                concreteType is null
                ? throw new JsonException($"No matching type discriminator configured. Could not determine concrete type.")
                : JsonSerializer.Deserialize(ref reader, concreteType, options) as T;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }

        private static bool IsMatch(Utf8JsonReader reader, PolymorphicJsonConverterConcreteTypeOptions options)
        {
            return JsonElement.TryParseValue(ref reader, out JsonElement? element) &&
                element is not null &&
                options.Predicate(element.Value);
        }

        private bool TryGetConcreteType(Utf8JsonReader reader, out Type? concreteType)
        {
            concreteType = null;
            foreach (PolymorphicJsonConverterConcreteTypeOptions concreteOptions in this.options.ConcreteTypeOptions)
            {
                if (IsMatch(reader, concreteOptions))
                {
#if DEBUG
                    if (concreteType is not null)
                    {
                        throw new InvalidOperationException($"More than one concrete type matched. This indicates a code defect in the configuration of the {nameof(PolymorphicJsonConverterAbstractTypeOptions)} used to initialize the {nameof(PolymorphicJsonConverter<T>)}");
                    }
#endif
                    concreteType = concreteOptions.Type;

#if RELEASE
                   break;
#endif
                }
            }

            return concreteType is not null;
        }
    }
}

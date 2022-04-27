namespace PRosser.Json
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// A polymorphic abstract class converter that uses a type discriminator property to select the appropriate
    /// concrete type.
    /// </summary>
    public class PolymorphicJsonConverterFactory : JsonConverterFactory, ICollection<PolymorphicJsonConverterAbstractTypeOptions>
    {
        private readonly IDictionary<Type, PolymorphicJsonConverterAbstractTypeOptions> optionsMap;

        /// <summary>
        /// Initializes a new instance of <see cref="PolymorphicJsonConverterFactory"/> with the specified options.
        /// </summary>
        /// <param name="options">Options that configure the types to convert.</param>
        public PolymorphicJsonConverterFactory(params PolymorphicJsonConverterAbstractTypeOptions[] options)
        {
            this.optionsMap = options.ToDictionary(x => x.BaseType);
        }

        /// <inheritdoc/>
        public int Count => this.Options.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => this.Options.IsReadOnly;

        /// <summary>Gets the configured options for the factory.</summary>
        public ICollection<PolymorphicJsonConverterAbstractTypeOptions> Options => this.optionsMap.Values;

        /// <inheritdoc/>
        public void Add(PolymorphicJsonConverterAbstractTypeOptions item)
        {
            this.optionsMap.Add(item.BaseType, item);
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            return this.optionsMap.ContainsKey(typeToConvert);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.optionsMap.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(PolymorphicJsonConverterAbstractTypeOptions item)
        {
            return this.Options.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(PolymorphicJsonConverterAbstractTypeOptions[] array, int arrayIndex)
        {
            this.Options.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter? converter = null;

            if (this.optionsMap.TryGetValue(typeToConvert, out PolymorphicJsonConverterAbstractTypeOptions? converterOptions))
            {
                converterOptions.Initialize(options.PropertyNamingPolicy);
                converter = Activator.CreateInstance(typeof(PolymorphicJsonConverter<>).MakeGenericType(typeToConvert), converterOptions) as JsonConverter;
            }

            return converter;
        }

        /// <inheritdoc/>
        public IEnumerator<PolymorphicJsonConverterAbstractTypeOptions> GetEnumerator()
        {
            return this.Options.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.Options).GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Remove(PolymorphicJsonConverterAbstractTypeOptions item)
        {
            Type? key = this.optionsMap.Where(kv => kv.Value == item).Select(kv => kv.Key).FirstOrDefault();

            return key is not null && this.optionsMap.Remove(key);
        }
    }
}
namespace PRosser.Json
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    /// <summary>Options to configure an abstract type for polymorphic JSON deserialization.</summary>
    public class JsonPolymorphicConverterAbstractTypeOptions : ICollection<JsonPolymorphicConverterConcreteTypeOptions>
    {
        /// <summary>Initializes a new instance of <see cref="JsonPolymorphicConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <remarks>
        /// Must add at least one <see cref="JsonPolymorphicConverterConcreteTypeOptions"/> or deserialization will fail
        /// at runtime.
        /// </remarks>
        public JsonPolymorphicConverterAbstractTypeOptions(Type baseType)
        {
            this.BaseType = baseType;
            this.ConcreteTypeOptions = new List<JsonPolymorphicConverterConcreteTypeOptions>();
        }

        /// <summary>Initializes a new instance of <see cref="JsonPolymorphicConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <param name="concreteTypeOptions">Options for each concrete type to be converted.</param>
        /// <exception cref="ArgumentException">No concrete type options were provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// One of the concrete type options' Type properties was not assignable to the base type.
        /// </exception>
        public JsonPolymorphicConverterAbstractTypeOptions(Type baseType, params JsonPolymorphicConverterConcreteTypeOptions[] concreteTypeOptions)
            : this(baseType, (IEnumerable<JsonPolymorphicConverterConcreteTypeOptions>)concreteTypeOptions)
        {
        }

        /// <summary>Initializes a new instance of <see cref="JsonPolymorphicConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <param name="concreteTypeOptions">Options for each concrete type to be converted.</param>
        /// <exception cref="ArgumentException">No concrete type options were provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// One of the concrete type options' Type properties was not assignable to the base type.
        /// </exception>
        public JsonPolymorphicConverterAbstractTypeOptions(Type baseType, IEnumerable<JsonPolymorphicConverterConcreteTypeOptions> concreteTypeOptions)
        {
            IList<JsonPolymorphicConverterConcreteTypeOptions> optionsArray = concreteTypeOptions as IList<JsonPolymorphicConverterConcreteTypeOptions>
                ?? concreteTypeOptions.ToList();

            if (optionsArray.Count == 0)
            {
                throw new ArgumentException("Must provide one or more concrete types", nameof(concreteTypeOptions));
            }

            Type[] unassignable = optionsArray
                .Where(o => !baseType.IsAssignableFrom(o.Type))
                .Select(o => o.Type)
                .ToArray();
            if (unassignable.Length > 0)
            {
                string names = string.Join(", ", unassignable.Select(x => x.Name));
                throw new ArgumentOutOfRangeException($"All types in {nameof(concreteTypeOptions)} must be assignable to {baseType.Name}. The following types were not valid: {names}");
            }

            this.BaseType = baseType;
            this.ConcreteTypeOptions = optionsArray;
        }

        /// <summary>Base type that can be converted.</summary>
        public Type BaseType { get; }

        /// <summary>Options for concrete types that can be converted.</summary>
        public IList<JsonPolymorphicConverterConcreteTypeOptions> ConcreteTypeOptions { get; }

        /// <inheritdoc/>
        public int Count => this.ConcreteTypeOptions.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => this.ConcreteTypeOptions.IsReadOnly;

        /// <inheritdoc/>
        public void Add(JsonPolymorphicConverterConcreteTypeOptions item)
        {
            this.ConcreteTypeOptions.Add(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.ConcreteTypeOptions.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(JsonPolymorphicConverterConcreteTypeOptions item)
        {
            return this.ConcreteTypeOptions.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(JsonPolymorphicConverterConcreteTypeOptions[] array, int arrayIndex)
        {
            this.ConcreteTypeOptions.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<JsonPolymorphicConverterConcreteTypeOptions> GetEnumerator()
        {
            return this.ConcreteTypeOptions.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.ConcreteTypeOptions).GetEnumerator();
        }

        public void Initialize(JsonNamingPolicy? propertyNamingPolicy)
        {
            foreach (JsonPolymorphicConverterConcreteTypeOptions cto in this.ConcreteTypeOptions)
            {
                cto.Initialize(propertyNamingPolicy);
            }
        }

        /// <inheritdoc/>
        public bool Remove(JsonPolymorphicConverterConcreteTypeOptions item)
        {
            return this.ConcreteTypeOptions.Remove(item);
        }
    }
}
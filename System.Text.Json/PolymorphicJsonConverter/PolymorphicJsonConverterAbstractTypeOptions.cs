namespace PRosser.Json
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    /// <summary>Options to configure an abstract type for polymorphic JSON deserialization.</summary>
    public class PolymorphicJsonConverterAbstractTypeOptions : ICollection<PolymorphicJsonConverterConcreteTypeOptions>
    {
        /// <summary>Initializes a new instance of <see cref="PolymorphicJsonConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <remarks>
        /// Must add at least one <see cref="PolymorphicJsonConverterConcreteTypeOptions"/> or deserialization will fail
        /// at runtime.
        /// </remarks>
        public PolymorphicJsonConverterAbstractTypeOptions(Type baseType)
        {
            this.BaseType = baseType;
            this.ConcreteTypeOptions = new List<PolymorphicJsonConverterConcreteTypeOptions>();
        }

        /// <summary>Initializes a new instance of <see cref="PolymorphicJsonConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <param name="concreteTypeOptions">Options for each concrete type to be converted.</param>
        /// <exception cref="ArgumentException">No concrete type options were provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// One of the concrete type options' Type properties was not assignable to the base type.
        /// </exception>
        public PolymorphicJsonConverterAbstractTypeOptions(Type baseType, params PolymorphicJsonConverterConcreteTypeOptions[] concreteTypeOptions)
            : this(baseType, (IEnumerable<PolymorphicJsonConverterConcreteTypeOptions>)concreteTypeOptions)
        {
        }

        /// <summary>Initializes a new instance of <see cref="PolymorphicJsonConverterAbstractTypeOptions"/>.</summary>
        /// <param name="baseType">Base type that can be converted.</param>
        /// <param name="concreteTypeOptions">Options for each concrete type to be converted.</param>
        /// <exception cref="ArgumentException">No concrete type options were provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// One of the concrete type options' Type properties was not assignable to the base type.
        /// </exception>
        public PolymorphicJsonConverterAbstractTypeOptions(Type baseType, IEnumerable<PolymorphicJsonConverterConcreteTypeOptions> concreteTypeOptions)
        {
            IList<PolymorphicJsonConverterConcreteTypeOptions> optionsArray = concreteTypeOptions as IList<PolymorphicJsonConverterConcreteTypeOptions>
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
        public IList<PolymorphicJsonConverterConcreteTypeOptions> ConcreteTypeOptions { get; }

        /// <inheritdoc/>
        public int Count => this.ConcreteTypeOptions.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => this.ConcreteTypeOptions.IsReadOnly;

        /// <inheritdoc/>
        public void Add(PolymorphicJsonConverterConcreteTypeOptions item)
        {
            this.ConcreteTypeOptions.Add(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.ConcreteTypeOptions.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(PolymorphicJsonConverterConcreteTypeOptions item)
        {
            return this.ConcreteTypeOptions.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(PolymorphicJsonConverterConcreteTypeOptions[] array, int arrayIndex)
        {
            this.ConcreteTypeOptions.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<PolymorphicJsonConverterConcreteTypeOptions> GetEnumerator()
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
            foreach (PolymorphicJsonConverterConcreteTypeOptions cto in this.ConcreteTypeOptions)
            {
                cto.Initialize(propertyNamingPolicy);
            }
        }

        /// <inheritdoc/>
        public bool Remove(PolymorphicJsonConverterConcreteTypeOptions item)
        {
            return this.ConcreteTypeOptions.Remove(item);
        }
    }
}
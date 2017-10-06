using System;

using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Elements.Identities;
using NuClear.Querying.Metadata.Elements;
using NuClear.Querying.Metadata.Features;

namespace NuClear.Querying.Metadata.Builders
{
    public sealed class EntityRelationElementBuilder : MetadataElementBuilder<EntityRelationElementBuilder, EntityRelationElement>
    {
        private string _name;
        private EntityRelationCardinality? _cardinality;
        private EntityElement _targetEntityElement;
        private bool _containsTarget;

        internal Uri TargetEntityReference
        {
            get
            {
                if (_targetEntityElement != null)
                {
                    return _targetEntityElement.Identity.Id;
                }

                if (TargetEntityElementConfig != null)
                {
                    return TargetEntityElementConfig.EntityId;
                }

                throw new InvalidOperationException("The reference is not set.");
            }
        }

        internal EntityElementBuilder TargetEntityElementConfig { get; private set; }

        public EntityRelationElementBuilder Name(string name)
        {
            _name = name;
            return this;
        }

        public EntityRelationElementBuilder DirectTo(EntityElement entityElement)
        {
            _targetEntityElement = entityElement;
            TargetEntityElementConfig = null;
            return this;
        }

        public EntityRelationElementBuilder DirectTo(EntityElementBuilder entityElementBuilder)
        {
            _targetEntityElement = null;
            TargetEntityElementConfig = entityElementBuilder;
            return this;
        }

        public EntityRelationElementBuilder AsOneOptionally()
        {
            _cardinality = EntityRelationCardinality.OptionalOne;
            return this;
        }

        public EntityRelationElementBuilder AsOne()
        {
            _cardinality = EntityRelationCardinality.One;
            return this;
        }

        public EntityRelationElementBuilder AsMany()
        {
            _cardinality = EntityRelationCardinality.Many;
            return this;
        }

        public EntityRelationElementBuilder AsContainment()
        {
            _containsTarget = true;
            return this;
        }

        protected override EntityRelationElement BuildInternal()
        {
            if (string.IsNullOrEmpty(_name))
            {
                throw new InvalidOperationException("The relation name was not specified.");
            }

            if (!_cardinality.HasValue)
            {
                throw new InvalidOperationException("The relation cardinality was not specified.");
            }

            if (_targetEntityElement == null && TargetEntityElementConfig == null)
            {
                throw new InvalidOperationException("The relation target was not specified.");
            }

            AddFeatures(new EntityRelationCardinalityFeature(_cardinality.Value, _targetEntityElement ?? TargetEntityElementConfig));

            if (_containsTarget)
            {
                AddFeatures(new EntityRelationContainmentFeature());
            }

            return new EntityRelationElement(_name.AsUri().AsIdentity(), Features);
        }
    }
}
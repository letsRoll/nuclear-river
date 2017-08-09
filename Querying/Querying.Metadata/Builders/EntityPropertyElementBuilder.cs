using System;

using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Elements.Identities;
using NuClear.Querying.Metadata.Elements;
using NuClear.Querying.Metadata.Features;

namespace NuClear.Querying.Metadata.Builders
{
    public sealed class EntityPropertyElementBuilder : MetadataElementBuilder<EntityPropertyElementBuilder, EntityPropertyElement>
    {
        private string _name;
        private bool _isNullable;

        public IStructuralModelTypeElement TypeElement { get; private set; }

        public Uri TypeReference { get; private set; }

        public EntityPropertyElementBuilder Name(string name)
        {
            _name = name;
            return this;
        }

        public EntityPropertyElementBuilder OfType(ElementaryTypeKind elementaryTypeKind)
        {
            TypeElement = PrimitiveTypeElement.OfKind(elementaryTypeKind);
            TypeReference = TypeElement.Identity.Id;
            return this;
        }

        public EntityPropertyElementBuilder OfType<T>(T typeElement) where T : IStructuralModelTypeElement
        {
            TypeElement = typeElement;
            TypeReference = TypeElement.Identity.Id;
            return this;
        }

        public EntityPropertyElementBuilder Nullable()
        {
            _isNullable = true;
            return this;
        }

        protected override EntityPropertyElement BuildInternal()
        {
            if (string.IsNullOrEmpty(_name))
            {
                throw new InvalidOperationException("The property name was not specified.");
            }

            if (TypeElement == null)
            {
                throw new InvalidOperationException("The property type was not specified");
            }

            if (_isNullable)
            {
                AddFeatures(new EntityPropertyNullableFeature(true));
            }

            return new EntityPropertyElement(_name.AsUri().AsIdentity(), TypeElement, Features);
        }
    }
}
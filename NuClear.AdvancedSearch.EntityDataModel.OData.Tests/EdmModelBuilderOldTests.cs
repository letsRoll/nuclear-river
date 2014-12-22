﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;

using Moq;

using NuClear.AdvancedSearch.EntityDataModel.OData.Building;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NuClear.AdvancedSearch.EntityDataModel.OData.Tests
{
    //[TestFixture]
    internal class EdmModelBuilderOldTests
    {
//        private Mock<IEdmModelSource> _modelSource;
//
//        [SetUp]
//        public void Setup()
//        {
//            _modelSource = new Mock<IEdmModelSource>();
//            _modelBuilder = EdmModelBuilder.Build(_modelSource.Object);
//        }
//
//        [Test, Ignore]
//        public void ShouldBuildValidModel()
//        {
//            var model = _modelBuilder.Build();
//            Debug.WriteLine(model.Dump());
//
//            Assert.NotNull(model);
//            Assert.That(model, Model.IsValid);
//        }
//
//        [Test]
//        public void ShouldBuildModel()
//        {
//            _modelSource.Setup(x => x.Namespace).Returns("CustomerIntelligence");
//            _modelSource.Setup(x => x.Entities).Returns(
//                new[]
//                    {
//                        Entity("Book", 
//                            //Key("Id"),
//                            Property("Id", EdmPrimitiveTypeKind.Int64), 
//                            Property("Title", EdmPrimitiveTypeKind.String)
//                            ),
//                    }
//                );
//
//            var model = _modelBuilder.Build();
//            Debug.WriteLine(model.Dump());
//
//            Assert.NotNull(model);
//            Assert.That(model, Model.IsValid);
//        }
//
//        private static EdmEntityType Entity(string name, params EdmEntityPropertyInfo[] properties)
//        {
//            return new EdmEntityType(name, properties, null);
//        }
//
//        private static EdmEntityPropertyInfo Property(string name, EdmPrimitiveTypeKind typeKind)
//        {
//            return new EdmEntityPropertyInfo(name, new EdmTypeReference(new EdmPrimitiveType(typeKind)));
//        }

        #region Model constraints

        private static class Model
        {
            public static Constraint IsValid
            {
                get
                {
                    return new ModelValidationConstraint();
                }
            }

            private class ModelValidationConstraint : Constraint
            {
                private const int MaxErrorsToDisplay = 5;
                private IEnumerable<EdmError> _errors;

                public override bool Matches(object actual)
                {
                    var model = actual as IEdmModel;
                    if (model == null)
                    {
                        throw new ArgumentException("The specified actual value is not a model.", "actual");
                    }

                    return model.Validate(out _errors);
                }

                public override void WriteDescriptionTo(MessageWriter writer)
                {
                    writer.WriteCollectionElements(_errors, 0, MaxErrorsToDisplay);
                }
            }
        }

        #endregion
    }
}

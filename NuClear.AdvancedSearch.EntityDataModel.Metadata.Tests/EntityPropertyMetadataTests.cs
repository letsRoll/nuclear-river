﻿using System.Collections;

using NuClear.AdvancedSearch.EntityDataModel.Metadata;

using NUnit.Framework;

namespace NuClear.EntityDataModel.Tests
{
    [TestFixture]
    internal class EntityPropertyMetadataTests : BaseMetadataFixture
    {
        public override IEnumerable Provider
        {
            get
            {
                yield return Case(EntityPropertyElement.Config.Name("Property"))
                    .Returns("{'Identity':{'Id':'Property'},'Features':[]}")
                    .SetName("ShouldDeclareProperty");
                yield return Case(EntityPropertyElement.Config.Name("Property").OfType(EntityPropertyType.Int64))
                    .Returns("{'Identity':{'Id':'Property'},'Features':[{'PropertyType':'Int64'}]}")
                    .SetName("ShouldDeclareTypedProperty");
                yield return Case(EntityPropertyElement.Config.Name("Property").Nullable())
                    .Returns("{'Identity':{'Id':'Property'},'Features':[{'IsNullable':true}]}")
                    .SetName("ShouldDeclareNullableProperty");
                yield return Case(EntityPropertyElement.Config.Name("Property")
                                                       .UsingEnum("Gender")
                                                       .WithMember("Female", 1)
                                                       .WithMember("Male", 2))
                    .Returns("{'Identity':{'Id':'Property'},'Features':[{'Name':'Gender','UnderlyingType':'Int32','Members':{'Female':1,'Male':2},'PropertyType':'Enum'}]}")
                    .SetName("ShouldDeclareEnumProperty");
            }
        }
    }
}

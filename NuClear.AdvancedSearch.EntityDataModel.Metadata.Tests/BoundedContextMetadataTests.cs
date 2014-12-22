﻿using System;
using System.Diagnostics;

using NuClear.AdvancedSearch.EntityDataModel.Metadata;
using NuClear.Metamodeling.Elements;
using NuClear.Metamodeling.Elements.Identities;
using NuClear.Metamodeling.Processors;
using NuClear.Metamodeling.Provider;

using NUnit.Framework;

namespace NuClear.EntityDataModel.Tests
{
    internal class BoundedContextMetadataTests
    {
        private MetadataProvider _provider;

        [SetUp]
        public void Setup()
        {
            var sources = new[] { new AdvancedSearchMetadataSource() };
            var processors = new IMetadataProcessor[] { };

            _provider = new MetadataProvider(sources, processors);
        }

        [Test]
        public void ShouldReturnMetadataByIdentity()
        {
            MetadataSet metadataSet;
            
            Assert.IsTrue(_provider.TryGetMetadata<AdvancedSearchIdentity>(out metadataSet));
            Assert.IsNotNull(metadataSet);
        }

        [Test]
        public void ShouldReturnMetadataByContextId()
        {
            BoundedContextElement contextElement;

            var id = IdBuilder.For<AdvancedSearchIdentity>("CustomerIntelligence");
            Assert.IsTrue(_provider.TryGetMetadata(id, out contextElement));
            Assert.IsNotNull(contextElement);
        }

        [TestCase("CustomerIntelligence/Firm", "Identity,Elements,Features", Explicit = true, Description = "Used to get dump of hierarchy")]
        [TestCase("CustomerIntelligence", "Identity", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence'}}")]
        [TestCase("CustomerIntelligence/Firm", "Identity", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm'}}")]
        [TestCase("CustomerIntelligence/Firm/Id", "Identity", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Id'}}")]
        [TestCase("CustomerIntelligence/Firm/Id", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Id'},'Features':[{'IsNullable':false},{'PropertyType':'Int64'}]}")]
        [TestCase("CustomerIntelligence/Firm/Categories", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Categories'},'Features':[{'Cardinality':'Many'}]}")]
        [TestCase("CustomerIntelligence/Firm/Categories/Category/Id", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Categories/Category/Id'},'Features':[{'IsNullable':false},{'PropertyType':'Int64'}]}")]
        [TestCase("CustomerIntelligence/Firm/Categories/Category/Name", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Categories/Category/Name'},'Features':[{'PropertyType':'String'}]}")]
        [TestCase("CustomerIntelligence/Firm/Categories/Category/CategoryGroup", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Categories/Category/CategoryGroup'},'Features':[{'PropertyType':'Byte'}]}")]
        [TestCase("CustomerIntelligence/Firm/Client", "Identity,Features", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Client'},'Features':[{'Cardinality':'OptionalOne'}]}")]
        [TestCase("CustomerIntelligence/Firm/Client/Client/Id", "Identity", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Client/Client/Id'}}")]
        [TestCase("CustomerIntelligence/Firm/Client/Client/CategoryGroup", "Identity", Result = "{'Identity':{'Id':'erm://metadata/AdvancedSearch/CustomerIntelligence/Firm/Client/Client/CategoryGroup'}}")]
        [TestCase("CustomerIntelligence/Firm/Client/Client/Contacts/Contact/Role", "Features", Result = "{'Features':[{'Name':'ContactRole','UnderlyingType':'Int32','Members':{'Employee':200000,'InfluenceDecisions':200001,'MakingDecisions':200002},'PropertyType':'Enum'}]}")]
        public string ShouldReturnMetadata(string path, string csPropertyNames)
        {
            IMetadataElement element;

            var id = IdBuilder.For<AdvancedSearchIdentity>(path.Split('/'));
            Assert.IsTrue(_provider.TryGetMetadata(id, out element));
            Assert.IsNotNull(element);

            var propertyNames = (csPropertyNames ?? "").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteLine(element.ToJson(true, propertyNames));
            return element.ToJson(propertyNames).Replace("\"", "'");
        }
    }
}

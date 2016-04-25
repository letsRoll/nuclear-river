﻿using NuClear.CustomerIntelligence.Storage.Model.CI;
using NuClear.CustomerIntelligence.Storage.Model.Facts;
using NuClear.DataTest.Metamodel.Dsl;

using Project = NuClear.CustomerIntelligence.Storage.Model.CI.Project;

namespace NuClear.CustomerIntelligence.Replication.StateInitialization.Tests
{
    public sealed partial class TestCaseMetadataSource
    {
        // ReSharper disable once UnusedMember.Local
        private static ArrangeMetadataElement ProjectAggregate
            => ArrangeMetadataElement.Config
                                     .Name(nameof(ProjectAggregate))
                                     .CustomerIntelligence(
                                                           new Project { Id = 1, Name = "ProjectOne" },
                                                           new ProjectCategory { ProjectId = 1, CategoryId = 1, Name = "Category 1 level 1", Level = 1, ParentId = null },
                                                           new ProjectCategory { ProjectId = 1, CategoryId = 2, Name = "Category 2 level 2", Level = 2, ParentId = 1 },
                                                           new ProjectCategory { ProjectId = 1, CategoryId = 3, Name = "Category 3 level 3", Level = 3, ParentId = 2 })
                                     .Fact(
                                           new Storage.Model.Facts.Project { Id = 1, Name = "ProjectOne", OrganizationUnitId = 1 },
                                           new Category { Id = 1, Level = 1, Name = "Category 1 level 1", ParentId = null },
                                           new Category { Id = 2, Level = 2, Name = "Category 2 level 2", ParentId = 1 },
                                           new Category { Id = 3, Level = 3, Name = "Category 3 level 3", ParentId = 2 },
                                           new CategoryOrganizationUnit { Id = 1, CategoryId = 1, OrganizationUnitId = 1 },
                                           new CategoryOrganizationUnit { Id = 2, CategoryId = 2, OrganizationUnitId = 1 },
                                           new CategoryOrganizationUnit { Id = 3, CategoryId = 3, OrganizationUnitId = 1 });
    }
}

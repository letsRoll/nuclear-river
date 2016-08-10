-- create schema
if not exists (select * from sys.schemas where name = 'ErmFacts') exec('create schema ErmFacts')

-- drop views with schemabindings
if object_id('ErmFacts.ViewClient', 'view') is not null drop view ErmFacts.ViewClient

-- drop tables
if object_id('ErmFacts.Account') is not null drop table ErmFacts.Account
if object_id('ErmFacts.Activity') is not null drop table ErmFacts.Activity
if object_id('ErmFacts.BranchOfficeOrganizationUnit') is not null drop table ErmFacts.BranchOfficeOrganizationUnit
if object_id('ErmFacts.Category') is not null drop table ErmFacts.Category
if object_id('ErmFacts.CategoryGroup') is not null drop table ErmFacts.CategoryGroup
if object_id('ErmFacts.CategoryFirmAddress') is not null drop table ErmFacts.CategoryFirmAddress
if object_id('ErmFacts.CategoryOrganizationUnit') is not null drop table ErmFacts.CategoryOrganizationUnit
if object_id('ErmFacts.Client') is not null drop table ErmFacts.Client
if object_id('ErmFacts.Contact') is not null drop table ErmFacts.Contact
if object_id('ErmFacts.Firm') is not null drop table ErmFacts.Firm
if object_id('ErmFacts.FirmAddress') is not null drop table ErmFacts.FirmAddress
if object_id('ErmFacts.FirmContact') is not null drop table ErmFacts.FirmContact
if object_id('ErmFacts.Lead') is not null drop table ErmFacts.Lead
if object_id('ErmFacts.LegalPerson') is not null drop table ErmFacts.LegalPerson
if object_id('ErmFacts.Order') is not null drop table ErmFacts.[Order]
if object_id('ErmFacts.Project') is not null drop table ErmFacts.Project
if object_id('ErmFacts.Territory') is not null drop table ErmFacts.Territory
if object_id('ErmFacts.SalesModelCategoryRestriction') is not null drop table ErmFacts.SalesModelCategoryRestriction
go


-- Account
create table ErmFacts.Account(
	Id bigint not null
    , Balance decimal(19,4) not null
    , BranchOfficeOrganizationUnitId bigint not null
    , LegalPersonId bigint not null
    , constraint PK_Accounts primary key (Id)
)
go

-- Activity
create table ErmFacts.Activity(
	Id bigint not null
    , ModifiedOn datetimeoffset(2) not null
	, FirmId bigint null
	, ClientId bigint null
    , constraint PK_Activities primary key (Id)
)
go
create nonclustered index IX_Activity_ClientId
on ErmFacts.Activity (ClientId)
include (ModifiedOn)
go
create nonclustered index IX_Activity_FirmId
on ErmFacts.Activity (FirmId)
include (ModifiedOn)
go

-- BranchOfficeOrganizationUnit
create table ErmFacts.BranchOfficeOrganizationUnit(
	Id bigint not null
    , OrganizationUnitId bigint not null
    , constraint PK_BranchOfficeOrganizationUnits primary key (Id)
)
go

-- Category
create table ErmFacts.Category(
	Id bigint not null
    , Name nvarchar(256) not null
    , [Level] int not null
    , ParentId bigint null
    , constraint PK_Categories primary key (Id)
)
go

-- CategoryGroup
create table ErmFacts.CategoryGroup(
	Id bigint not null
    , Name nvarchar(256) not null
    , Rate float not null
    , constraint PK_CategoryGroups primary key (Id)
)
go

-- CategoryFirmAddress
create table ErmFacts.CategoryFirmAddress(
	Id bigint not null
    , CategoryId bigint not null
    , FirmAddressId bigint not null
    , constraint PK_CategoryFirmAddresses primary key (Id)
)
go
create nonclustered index IX_CategoryFirmAddress_FirmAddressId
on ErmFacts.CategoryFirmAddress (FirmAddressId)
include (CategoryId)
go
create nonclustered index IX_CategoryFirmAddress_CategoryId
on ErmFacts.CategoryFirmAddress (CategoryId)
include (FirmAddressId)
go

-- CategoryOrganizationUnit
create table ErmFacts.CategoryOrganizationUnit(
	Id bigint not null
	, CategoryId bigint not null
    , CategoryGroupId bigint not null
    , OrganizationUnitId bigint not null
    , constraint PK_CategoryOrganizationUnits primary key (Id)
)
go
create nonclustered index IX_CategoryOrganizationUnit_CategoryId_OrganizationUnitId
on ErmFacts.CategoryOrganizationUnit (CategoryId, OrganizationUnitId)
include (CategoryGroupId)
go

-- Client
create table ErmFacts.Client(
	Id bigint not null
    , Name nvarchar(256) not null
    , LastDisqualifiedOn datetimeoffset(2) null
    , HasPhone bit not null constraint DF_Clients_HasPhone default 0
    , HasWebsite bit not null constraint DF_Clients_HasWebsite default 0
    , constraint PK_Clients primary key (Id)
)
go

-- Contact
create table ErmFacts.Contact(
	Id bigint not null
	, [Role] int not null
    , HasPhone bit not null constraint DF_Contacts_HasPhone default 0
    , HasWebsite bit not null constraint DF_Contacts_HasWebsite default 0
    , ClientId bigint not null
    , constraint PK_Contacts primary key (Id)
)
go
create nonclustered index IX_Contact_HasPhone_HasWebsite
on ErmFacts.Contact (HasPhone, HasWebsite)
go

-- Firm
create table ErmFacts.Firm(
	Id bigint not null
    , Name nvarchar(256) not null
    , CreatedOn datetimeoffset(2) not null
    , LastDisqualifiedOn datetimeoffset(2) null
    , ClientId bigint null
    , OrganizationUnitId bigint not null
    , OwnerId bigint not null
    , constraint PK_Firms primary key (Id)
)
go
create nonclustered index IX_Firm_ClientId_OrganizationUnitId
on ErmFacts.Firm (ClientId, OrganizationUnitId)
include (Id)
go

-- FirmAddress
create table ErmFacts.FirmAddress(
	Id bigint not null
    , FirmId bigint not null
	, TerritoryId bigint null
    , constraint PK_FirmAddresses primary key (Id)
)
go
create nonclustered index IX_FirmAddress_FirmId
on ErmFacts.FirmAddress (FirmId)
include (Id)
go

-- FirmContact
create table ErmFacts.FirmContact(
	Id bigint not null
    , HasPhone bit not null constraint DF_FirmContacts_HasPhone default 0
    , HasWebsite bit not null constraint DF_FirmContacts_HasWebsite default 0
    , FirmAddressId bigint not null
    , constraint PK_FirmContacts primary key (Id)
)
go
create nonclustered index IX_FirmContact_HasPhone_FirmAddressId
on ErmFacts.FirmContact (HasPhone, FirmAddressId)
go
create nonclustered index IX_FirmContact_HasWebsite_FirmAddressId
on ErmFacts.FirmContact (HasWebsite,FirmAddressId)
go

-- Lead
create table ErmFacts.Lead(
	Id bigint not null
    , FirmId bigint not null
    , IsInQueue bigint not null
    , Type int not null
    , constraint PK_Leads primary key (Id)
)
go

-- LegalPerson
create table ErmFacts.LegalPerson(
	Id bigint not null
    , ClientId bigint not null
    , constraint PK_LegalPersons primary key (Id)
)
go

-- Order
create table ErmFacts.[Order](
	Id bigint not null
    , EndDistributionDateFact datetimeoffset(2) not null
    , FirmId bigint null
    , constraint PK_Orders primary key (Id)
)
go

-- Project
create table ErmFacts.Project(
	Id bigint not null
    , Name nvarchar(256) not null
    , OrganizationUnitId bigint not null
    , constraint PK_Projects primary key (Id)
)
go

-- Territory
create table ErmFacts.Territory(
	Id bigint not null
    , Name nvarchar(256) not null
    , OrganizationUnitId bigint not null
    , constraint PK_Territories primary key (Id)
)
go

-- SalesModelCategoryRestriction
create table ErmFacts.SalesModelCategoryRestriction(
    Id bigint not null
    , CategoryId bigint not null
    , ProjectId  bigint not null
    , SalesModel int not null
    , constraint PK_SalesModelCategoryRestrictions primary key (Id)
)
go

-- ViewClient, indexed view for query optimization
create view ErmFacts.ViewClient
with schemabinding
as
select 
	Firm.ClientId,
	CategoryFirmAddress.FirmAddressId,
	CategoryOrganizationUnit.CategoryId,
	CategoryOrganizationUnit.CategoryGroupId,
	CategoryGroup.Rate
from ErmFacts.Firm
	inner join ErmFacts.FirmAddress on Firm.Id = FirmAddress.FirmId
	inner join ErmFacts.CategoryFirmAddress on FirmAddress.Id = CategoryFirmAddress.FirmAddressId
	inner join ErmFacts.CategoryOrganizationUnit on CategoryFirmAddress.CategoryId = CategoryOrganizationUnit.CategoryId AND Firm.OrganizationUnitId = CategoryOrganizationUnit.OrganizationUnitId
	inner join ErmFacts.CategoryGroup on CategoryOrganizationUnit.CategoryGroupId = CategoryGroup.Id
where Firm.ClientId is not null
go
create unique clustered index PK_ViewClient
    on ErmFacts.ViewClient (FirmAddressId, CategoryId)
go
create nonclustered index IX_ViewClient_ClientId_CategoryGroupId_Rate
	on ErmFacts.ViewClient (ClientId, Rate)
	include (CategoryGroupId)
go
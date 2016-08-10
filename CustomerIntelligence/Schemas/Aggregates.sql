-- create schema
if not exists (select * from sys.schemas where name = 'Aggregates') exec('create schema Aggregates')

-- drop tables
if object_id('Aggregates.CategoryGroup') is not null drop table Aggregates.CategoryGroup
if object_id('Aggregates.Project') is not null drop table Aggregates.Project
if object_id('Aggregates.ProjectCategory') is not null drop table Aggregates.ProjectCategory
if object_id('Aggregates.Territory') is not null drop table Aggregates.Territory
if object_id('Aggregates.Firm') is not null drop table Aggregates.Firm
if object_id('Aggregates.FirmBalance') is not null drop table Aggregates.FirmBalance
if object_id('Aggregates.FirmLead') is not null drop table Aggregates.FirmLead
if object_id('Aggregates.FirmForecast') is not null drop table Aggregates.FirmForecast
if object_id('Aggregates.FirmActivity') is not null drop table Aggregates.FirmActivity
if object_id('Aggregates.Client') is not null drop table Aggregates.Client
if object_id('Aggregates.Contact') is not null drop table Aggregates.Contact
if object_id('Aggregates.ClientContact') is not null drop table Aggregates.ClientContact
if object_id('Aggregates.FirmCategory') is not null drop table Aggregates.FirmCategory -- удалить после релиза
if object_id('BitFacts.FirmCategory', 'view') is not null drop view BitFacts.FirmCategory -- удалить после релиза
if object_id('Aggregates.FirmCategory1') is not null drop table Aggregates.FirmCategory1
if object_id('Aggregates.FirmCategory2') is not null drop table Aggregates.FirmCategory2
if object_id('Aggregates.FirmCategory3') is not null drop table Aggregates.FirmCategory3
if object_id('Aggregates.FirmTerritory') is not null drop table Aggregates.FirmTerritory
if object_id('Aggregates.ProjectCategoryStatistics') is not null drop table Aggregates.ProjectCategoryStatistics
if object_id('Aggregates.ProjectStatistics') is not null drop table Aggregates.ProjectStatistics
if object_id('Aggregates.FirmView', 'view') is not null drop view Aggregates.FirmView
go


-- CategoryGroup
create table Aggregates.CategoryGroup(
	Id bigint not null
    , Name nvarchar(256) not null
    , Rate float not null
    , constraint PK_CategoryGroups primary key (Id)
)
go

-- Project
create table Aggregates.Project(
	Id bigint not null
    , Name nvarchar(256) not null
    , constraint PK_Projects primary key (Id)
)
go

-- ProjectCategory
create table Aggregates.ProjectCategory(
	ProjectId bigint not null
    , CategoryId bigint not null
    , Name nvarchar(256) not null
    , [Level] int not null
    , ParentId bigint null
	, SalesModel int not null
    , constraint PK_ProjectCategories primary key (ProjectId, CategoryId)
)
go

-- Territory
create table Aggregates.Territory(
	Id bigint not null
    , Name nvarchar(256) not null
    , ProjectId bigint not null
    , constraint PK_Territories primary key (Id)
)
go

-- Firm
create table Aggregates.Firm(
	Id bigint not null
    , Name nvarchar(256) not null
    , CreatedOn datetimeoffset(2) not null
    , LastDisqualifiedOn datetimeoffset(2) null
	, LastDistributedOn datetimeoffset(2) null
    , HasPhone bit not null constraint DF_Firms_HasPhone default 0
    , HasWebsite bit not null constraint DF_Firms_HasWebsite default 0
    , AddressCount int not null constraint DF_Firms_AddressCount default 0
    , CategoryGroupId bigint not null
    , ClientId bigint null
    , ProjectId bigint not null
    , OwnerId bigint not null
    , constraint PK_Firms primary key (Id)
)
go

-- FirmActivity
create table Aggregates.FirmActivity(
	FirmId bigint not null
    , LastActivityOn datetimeoffset(2) null
    , constraint PK_FirmActivities primary key (FirmId)
)
go

-- FirmBalance
create table Aggregates.FirmBalance(
    FirmId bigint not null
    , AccountId bigint not null
	, ProjectId bigint not null
    , Balance decimal(19,4) not null
    , constraint PK_FirmBalances primary key (FirmId, AccountId)
)
go

-- FirmCategory1
create table Aggregates.FirmCategory1(
	FirmId bigint not null
	, CategoryId bigint not null
    , constraint PK_FirmCategory1 primary key (FirmId, CategoryId)
)
go

-- FirmCategory2
create table Aggregates.FirmCategory2(
	FirmId bigint not null
	, CategoryId bigint not null
    , constraint PK_FirmCategory2 primary key (FirmId, CategoryId)
)
go

-- ProjectStatistics
create table Aggregates.ProjectStatistics(
    Id bigint not null
    , constraint PK_ProjectStatistics primary key (Id)
)
go

-- FirmForecast
create table Aggregates.ProjectCategoryStatistics(
    ProjectId bigint not null
    , CategoryId bigint not null
    , constraint PK_ProjectCategoryStatistics primary key (ProjectId, CategoryId)
)
go

-- FirmCategory3
create table Aggregates.FirmCategory3(
    ProjectId bigint not null
    , FirmId bigint not null
    , CategoryId bigint not null
    , Name nvarchar(256) not null
    , Hits int not null
    , Shows int not null
    , FirmCount int not null
    , AdvertisersShare float not null
    , ForecastClick int null
    , ForecastAmount decimal(19,4) null
    , constraint PK_FirmCategory3 primary key (FirmId, CategoryId)
)
go

-- FirmForecast
create table Aggregates.FirmForecast(
    ProjectId bigint not null
    , FirmId bigint not null
    , ForecastClick int not null
    , ForecastAmount decimal(19,4) not null
    , constraint PK_FirmForecast primary key (FirmId)
)
go

-- FirmLead
create table Aggregates.FirmLead(
	FirmId bigint not null
    , LeadId bigint not null
    , IsInQueue bit not null
    , Type int not null
    , constraint PK_FirmLeads primary key (FirmId, LeadId)
)
go

-- FirmTerritory
create table Aggregates.FirmTerritory(
	FirmId bigint not null
    , FirmAddressId bigint not null
	, TerritoryId bigint null
    , constraint PK_FirmTerritory primary key (FirmId, FirmAddressId)
)
go

-- Client
create table Aggregates.Client(
	Id bigint not null
    , Name nvarchar(256) not null
    , CategoryGroupId bigint not null
    , constraint PK_Clients primary key (Id)
)
go

-- ClientContact
create table Aggregates.ClientContact(
    ClientId bigint not null
    , ContactId bigint not null
    , [Role] int not null
    , constraint PK_ClientContact primary key (ClientId, ContactId)
)
go


-- FirmView
create view Aggregates.FirmView
as
select Firm.*, FirmActivity.LastActivityOn, FirmForecast.ForecastClick, FirmForecast.ForecastAmount
from Aggregates.Firm
	inner join Aggregates.FirmActivity on FirmActivity.FirmId = Firm.Id
	left join Aggregates.FirmForecast on FirmForecast.FirmId = Firm.Id
go


-- Идексы для клиента
create nonclustered index IX_Quering_1
on Aggregates.Firm (ProjectId,OwnerId,CreatedOn)
include (Id)
go
create nonclustered index IX_Quering_2
on Aggregates.Firm (ProjectId,OwnerId,LastDistributedOn)
include (Id)
go
create nonclustered index IX_Quering_3
on Aggregates.Firm (ProjectId,OwnerId,LastDisqualifiedOn)
include (Id)
go
create nonclustered index IX_Quering_4
on Aggregates.Firm (LastDistributedOn,ProjectId,OwnerId)
include (Id,Name,LastDisqualifiedOn,ClientId)
go
create nonclustered index IX_Quering_5
on Aggregates.Firm (ProjectId,OwnerId)
include (Id,Name,LastDisqualifiedOn,ClientId)
go
create nonclustered index IX_Quering_6
on Aggregates.Firm (HasWebsite,ProjectId,OwnerId)
include (Id)
go
create nonclustered index IX_Quering_7
on Aggregates.Firm (HasWebsite,ProjectId,OwnerId)
include (Id,Name,LastDisqualifiedOn,ClientId)
go
create nonclustered index IX_Quering_8
on Aggregates.Firm (ProjectId,OwnerId,AddressCount)
include (Id)
go
create nonclustered index IX_Quering_9
on Aggregates.Firm (ProjectId,OwnerId,AddressCount)
include (Id,Name,LastDisqualifiedOn,ClientId)
go
create nonclustered index IX_Quering_10
on Aggregates.FirmBalance (ProjectId,Balance)
include (FirmId)
go
create nonclustered index IX_Quering_11
on Aggregates.FirmActivity(LastActivityOn)
include (FirmId)
go
create nonclustered index IX_Quering_12
on Aggregates.ClientContact (Role)
include (ClientId)
go

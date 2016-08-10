-- create schema
if not exists (select * from sys.schemas where name = 'BitFacts') exec('create schema BitFacts')

-- drop tables
if object_id('BitFacts.ProjectCategoryStatistics') is not null drop table BitFacts.ProjectCategoryStatistics
if object_id('BitFacts.FirmCategoryStatistics') is not null drop table BitFacts.FirmCategoryStatistics
if object_id('BitFacts.FirmForecast') is not null drop table BitFacts.FirmForecast
if object_id('BitFacts.FirmCategoryForecast') is not null drop table BitFacts.FirmCategoryForecast
go

-- ProjectCategoryStatistics
create table BitFacts.ProjectCategoryStatistics(
    ProjectId bigint not null,
    CategoryId bigint not null,
    AdvertisersCount bigint not null, 
    constraint PK_ProjectCategoryStatistics primary key (ProjectId, CategoryId)
)
go

-- FirmCategoryStatistics
create table BitFacts.FirmCategoryStatistics(
    ProjectId bigint not null,
    FirmId bigint not null,
    CategoryId bigint not null,
    Hits int not null,
    Shows int not null,
    constraint PK_FirmCategoryStatistics primary key (FirmId, CategoryId)
)
go

-- FirmForecast
create table BitFacts.FirmForecast(
    ProjectId bigint not null,
    FirmId bigint not null,
    ForecastClick int not null,
    ForecastAmount decimal(19,4) not null,
    constraint PK_FirmForecast primary key (FirmId)
)
go

-- FirmCategoryForecast
create table BitFacts.FirmCategoryForecast(
    ProjectId bigint not null,
    FirmId bigint not null,
    CategoryId bigint not null,
    ForecastClick int not null,
    ForecastAmount decimal(19,4) not null,
    constraint PK_FirmCategoryForecast primary key (FirmId, CategoryId)
)
go

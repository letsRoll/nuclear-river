using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Query;

using NuClear.AdvancedSearch.Web.OData.DataAccess;

namespace NuClear.AdvancedSearch.Web.OData.Controllers
{
    public abstract class GenericODataController<TEntity> : ODataController where TEntity : class
    {
        private readonly IFinder _finder;

        protected GenericODataController(IFinder finder)
        {
            _finder = finder;
        }

        [DynamicEnableQuery]
        public IHttpActionResult Get(ODataQueryOptions<TEntity> queryOptions)
        {
            var entities = _finder.FindAll<TEntity>();

            return Ok(entities);
        }

        // TODO: get by id
    }
}
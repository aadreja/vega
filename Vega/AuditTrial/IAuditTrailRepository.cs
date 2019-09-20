using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vega
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    public interface IAuditTrailRepository<Entity>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="operation"></param>
        /// <param name="audit"></param>
        /// <returns></returns>
        bool Add(Entity entity, RecordOperationEnum operation, IAuditTrail audit);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="recordVersionNo"></param>
        /// <param name="updatedBy"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        bool Add(object recordId, int? recordVersionNo, object updatedBy, RecordOperationEnum operation);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IEnumerable<Entity> ReadAll(object id);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        List<IAuditTrail> ReadAllAuditTrail(object id);
        //IEnumerable<T> ReadAll<T>(string tableName, object id) where T : new();
    }
}

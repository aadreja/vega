using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vega
{
    /// <summary>
    /// Interface to implement AuditTrail CRUD actions
    /// </summary>
    /// <typeparam name="Entity">Entity Type which needs AuditTrail</typeparam>
    public interface IAuditTrailRepository<Entity>
    {
        /// <summary>
        /// Add record to AuditTrail. Insert or Update action on a Table
        /// </summary>
        /// <param name="entity">Entity which was modified</param>
        /// <param name="operation">Insert or Update operation</param>
        /// <param name="audit">Audit Trail object of type IAuditTrail</param>
        /// <returns>true if success, False if fail</returns>
        bool Add(Entity entity, RecordOperationEnum operation, IAuditTrail audit);

        /// <summary>
        /// Add record to AuditTrail. Delete or Recover action on a Table
        /// </summary>
        /// <param name="recordId">RecordId which was modified</param>
        /// <param name="recordVersionNo">Record Version o</param>
        /// <param name="updatedBy">Modified By</param>
        /// <param name="operation">Delete or Recover operation</param>
        /// <returns>true if success, False if fail</returns>
        bool Add(object recordId, int? recordVersionNo, object updatedBy, RecordOperationEnum operation);

        /// <summary>
        /// Read and Return chain of Record from insert to update till now for a given Record of an Entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns>List of Entity with modification</returns>
        IEnumerable<Entity> ReadAll(object id);

        /// <summary>
        /// Read and Return chain of AuditTrial with column, old and new values for a given Record
        /// </summary>
        /// <param name="id"></param>
        /// <returns>List of AuditTrail with column, old and new values</returns>
        List<IAuditTrail> ReadAllAuditTrail(object id);
    }
}

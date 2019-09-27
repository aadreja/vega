/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;

namespace Vega
{
    [Table(IsNoDefaultFields = false)]
    internal class EntityDefault : EntityBase
    {
        //just to parse default values
    }

    /// <summary>
    /// Entity parent class. All entities must be inherited from this abstract class
    /// </summary>
    [Serializable]
    public abstract class EntityBase
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public EntityBase()
        {

        }

        #region fields

        private int? versionNo = 1;
        private int? pastVersionNo;

        #endregion

        #region properties

        [IgnoreColumn(true)]
        internal object KeyId
        {
            get
            {
                return EntityCache.Get(GetType()).PkColumn.GetAction(this);
            }
            set
            {
                EntityCache.Get(GetType()).PkColumn.SetAction(this, value);
            }
        }

        /// <summary>
        /// Gets or Set CreatedBy Property
        /// </summary>
        [Column(Title = "Created By")]
        public virtual object CreatedBy { get; set; }

        /// <summary>
        /// Gets or Set CreatedByName Property
        /// </summary>
        [IgnoreColumn(true)]
        [Column(Title = "Created By")]
        public virtual string CreatedByName { get; set; }

        /// <summary>
        /// Gets or Set CreatedOn Property
        /// </summary>
        [Column(Title = "Created On")]
        public virtual DateTime? CreatedOn { get; set; }

        /// <summary>
        /// Gets or Set UpdatedBy Property
        /// </summary>
        [Column(Title = "Updated By")]
        public virtual object UpdatedBy { get; set; }

        /// <summary>
        /// Gets or Set UpdatedByName Property
        /// </summary>
        [IgnoreColumn(true)]
        [Column(Title = "Updated By")]
        public virtual string UpdatedByName { get; set; }

        /// <summary>
        /// Gets or Set UpdatedOn Property
        /// </summary>
        [Column(Title = "Updated On")]
        public virtual DateTime? UpdatedOn { get; set; }

        /// <summary>
        /// Gets or Set VersionNo Property
        /// </summary>
        [Column(Title = "Version")]
        public virtual int? VersionNo
        {
            get { return versionNo; }
            set
            {
                pastVersionNo = versionNo;
                versionNo = value;
            }
        }

        /// <summary>
        /// Gets or Set IsActive Property
        /// </summary>
        /// TODO: to be discussed
        [Column(Title = "Is Active")]
        public virtual bool? IsActive { get; set; }

        /// <summary>
        /// Gets operation type
        /// </summary>
        [IgnoreColumn(true)]
        public string Operation
        {
            get
            {
                if (!(IsActive ?? false)) return "In Active";
                else if (VersionNo == 0 || VersionNo == 1) return "Add";
                else if (VersionNo > 1) return "Update";
                else return "Unknown";
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Created clone of current object
        /// </summary>
        /// <returns>Clonned object</returns>
        public virtual EntityBase ShallowCopy()
        {
            return (EntityBase)MemberwiseClone();
        }

        /// <summary>
        /// Reverts version no. Can be used when Insert, Update, Delete operation fails
        /// </summary>
        public virtual void RevertVersionNo()
        {
            if (pastVersionNo > 0)
            {
                VersionNo = pastVersionNo;
            }
        }

        /// <summary>
        /// Is PrimaryKey property is empty
        /// </summary>
        /// <returns></returns>
        public bool IsKeyIdEmpty()
        {
            return IsKeyFieldEmpty(KeyId, "Primary Key");
        }

        internal bool IsCreatedByEmpty()
        {
            return IsKeyFieldEmpty(CreatedBy, "Created By");
        }

        internal bool IsUpdatedByEmpty()
        {
            return IsKeyFieldEmpty(UpdatedBy, "Updated By");
        }

        internal static bool IsKeyFieldEmpty(object id, string fieldName)
        {
            if (id is null)
                return true;
            else if (id.IsNumber())
                return Equals(id, Convert.ChangeType(0, id.GetType()));
            else if (id is Guid)
                return Equals(id, Guid.Empty);
            else
                throw new Exception(id.GetType().Name + " data type not supported for " + fieldName);
        }

        #endregion
    }
}
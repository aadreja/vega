/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Vega
{
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

        private int versionNo = 1;
        private int pastVersionNo;

        #endregion

        #region properties

        [IgnoreColumn(true)]
        internal object KeyId
        {
            get
            {
                return EntityCache.Get(GetType()).PrimaryKeyColumn.GetAction(this);
            }
            set
            {
                EntityCache.Get(GetType()).PrimaryKeyColumn.SetAction(this, value);
            }
        }

        /// <summary>
        /// Gets or Set CreatedBy Property
        /// </summary>
        [Column(Name = Config.CREATEDBY_COLUMNNAME, Title = "Created By")]
        public Int32 CreatedBy { get; set; }

        /// <summary>
        /// Gets or Set CreatedByName Property
        /// </summary>
        [IgnoreColumn(true)]
        [Column(Name = Config.CREATEDBYNAME_COLUMNNAME, Title = "Created By")]
        public string CreatedByName { get; set; }

        /// <summary>
        /// Gets or Set CreatedOn Property
        /// </summary>
        [Column(Name = Config.CREATEDON_COLUMNNAME, Title = "Created On")]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or Set UpdatedBy Property
        /// </summary>
        [Column(Name = Config.UPDATEDBY_COLUMNNAME, Title = "Updated By")]
        public Int32 UpdatedBy { get; set; }

        /// <summary>
        /// Gets or Set UpdatedByName Property
        /// </summary>
        [IgnoreColumn(true)]
        [Column(Name = Config.UPDATEDBYNAME_COLUMNNAME, Title = "Updated By")]
        public string UpdatedByName { get; set; }

        /// <summary>
        /// Gets or Set UpdatedOn Property
        /// </summary>
        [Column(Name = Config.UPDATEDON_COLUMNNAME, Title = "Updated On")]
        public DateTime UpdatedOn { get; set; }

        /// <summary>
        /// Gets or Set VersionNo Property
        /// </summary>
        [Column(Name = Config.VERSIONNO_COLUMNNAME, Title = "Version")]
        public int VersionNo
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
        [Column(Name = Config.ISACTIVE_COLUMNNAME, Title = "Is Active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets operation type
        /// </summary>
        [IgnoreColumn(true)]
        public string Operation
        {
            get
            {
                if (!IsActive) return "In Active";
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
            var id = KeyId;

            if (id is null)
                return true;
            else if (id.IsNumber())
            {
                if (Equals(id, Convert.ChangeType(0, id.GetType()))) return true;
                else return false;
            }
            else if (id is Guid)
            {
                if (Equals(id, Guid.Empty)) return true;
                else return false;
            }
            else
                throw new Exception(id.GetType().Name + " data type not supported for Primary Key");
        }

        #endregion
    }
}
/*
 Description: Vega - Fastest ORM with enterprise features
 Author: Ritesh Sutaria
 Date: 9-Dec-2017
 Home Page: https://github.com/aadreja/vega
            http://www.vegaorm.com
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vega
{
    /// <summary>
    /// To hold session info like CurrentUserId
    /// value must be initialized at the application start once
    /// as needed one may add other session properties
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Session() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="currentUserId"></param>
        public Session(int currentUserId)
        {
            CurrentUserId = currentUserId;
        }

        /// <summary>
        /// Current User Id - used by framework for Insert, Update and Delete operations
        /// Must set at application start
        /// </summary>
        public Int32 CurrentUserId { get; set; }

    }
}

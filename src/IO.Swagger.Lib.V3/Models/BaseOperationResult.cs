/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Repository Service Specification
 *
 * The Full Profile of the Asset Administration Shell Repository Service Specification as part of Specification of the Asset Administration Shell: Part 2. Publisher: Industrial Digital Twin Association (IDTA) April 2023
 *
 * OpenAPI spec version: V3.0_SSP-001
 * Contact: info@idtwin.org
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace IO.Swagger.Models
{
    /// <summary>
    /// 
    /// </summary>
    [ DataContract ]
    public partial class BaseOperationResult : Result, IEquatable<BaseOperationResult>
    {
        /// <summary>
        /// Gets or Sets ExecutionState
        /// </summary>

        [ DataMember(Name = "executionState") ]
        public ExecutionState ExecutionState { get; set; }

        /// <summary>
        /// Gets or Sets Success
        /// </summary>

        [ DataMember(Name = "success") ]
        public bool? Success { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BaseOperationResult {\n");
            sb.Append("  ExecutionState: ").Append(ExecutionState).Append("\n");
            sb.Append("  Success: ").Append(Success).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public new string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((BaseOperationResult) obj);
        }

        /// <summary>
        /// Returns true if BaseOperationResult instances are equal
        /// </summary>
        /// <param name="other">Instance of BaseOperationResult to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(BaseOperationResult? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    ExecutionState == other.ExecutionState ||
                    (ExecutionState != null &&
                     ExecutionState.Equals(other.ExecutionState))
                ) &&
                (
                    Success == other.Success ||
                    (Success != null &&
                     Success.Equals(other.Success))
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                if (ExecutionState != null)
                    hashCode = (hashCode * 59) + ExecutionState.GetHashCode();
                if (Success != null)
                    hashCode = (hashCode * 59) + Success.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(BaseOperationResult left, BaseOperationResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BaseOperationResult left, BaseOperationResult right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
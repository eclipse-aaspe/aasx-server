/*
 * DotAAS Part 2 | HTTP/REST | Asset Administration Shell Registry Service Specification
 *
 * The Full Profile of the Asset Administration Shell Registry Service Specification as part of the [Specification of the Asset Administration Shell: Part 2](http://industrialdigitaltwin.org/en/content-hub).   Publisher: Industrial Digital Twin Association (IDTA) 2023
 *
 * OpenAPI spec version: V3.0.1_SSP-001
 * Contact: info@idtwin.org
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace IO.Swagger.Registry.Lib.V3.Models
{
    /// <summary>
    /// 
    /// </summary>
    [ DataContract ]
    public partial class Endpoint : IEquatable<Endpoint>
    {
        /// <summary>
        /// Gets or Sets _Interface
        /// </summary>
        [ Required ]
        [ MaxLength(128) ]
        [ DataMember(Name = "interface") ]
        public string? Interface { get; set; }

        /// <summary>
        /// Gets or Sets ProtocolInformation
        /// </summary>
        [ Required ]
        [ DataMember(Name = "protocolInformation") ]
        public ProtocolInformation? ProtocolInformation { get; set; }

        public Endpoint(string? @interface = null, ProtocolInformation? protocolInformation = null)
        {
            Interface = @interface;
            ProtocolInformation = protocolInformation;
        }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Endpoint {\n");
            sb.Append("  _Interface: ").Append(Interface).Append("\n");
            sb.Append("  ProtocolInformation: ").Append(ProtocolInformation).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public string ToJson()
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
            return obj.GetType() == GetType() && Equals((Endpoint) obj);
        }

        /// <summary>
        /// Returns true if Endpoint instances are equal
        /// </summary>
        /// <param name="other">Instance of Endpoint to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Endpoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return
                (
                    Interface == other.Interface ||
                    Interface != null &&
                    Interface.Equals(other.Interface)
                ) &&
                (
                    ProtocolInformation == other.ProtocolInformation ||
                    ProtocolInformation != null &&
                    ProtocolInformation.Equals(other.ProtocolInformation)
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
                if (Interface != null)
                    hashCode = hashCode * 59 + Interface.GetHashCode();
                if (ProtocolInformation != null)
                    hashCode = hashCode * 59 + ProtocolInformation.GetHashCode();
                return hashCode;
            }
        }

        #region Operators

#pragma warning disable 1591

        public static bool operator ==(Endpoint left, Endpoint right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Endpoint left, Endpoint right)
        {
            return !Equals(left, right);
        }

#pragma warning restore 1591

        #endregion Operators
    }
}
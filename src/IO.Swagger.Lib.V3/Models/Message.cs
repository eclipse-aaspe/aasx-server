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
    [DataContract]
    public partial class Message : IEquatable<Message>
    { 
        /// <summary>
        /// Gets or Sets Code
        /// </summary>

        [StringLength(32, MinimumLength=1)]
        [DataMember(Name="code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or Sets CorrelationId
        /// </summary>

        [StringLength(128, MinimumLength=1)]
        [DataMember(Name="correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or Sets MessageType
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum MessageTypeEnum
        {
            /// <summary>
            /// Enum UndefinedEnum for Undefined
            /// </summary>
            [EnumMember(Value = "Undefined")]
            UndefinedEnum = 0,
            /// <summary>
            /// Enum InfoEnum for Info
            /// </summary>
            [EnumMember(Value = "Info")]
            InfoEnum = 1,
            /// <summary>
            /// Enum WarningEnum for Warning
            /// </summary>
            [EnumMember(Value = "Warning")]
            WarningEnum = 2,
            /// <summary>
            /// Enum ErrorEnum for Error
            /// </summary>
            [EnumMember(Value = "Error")]
            ErrorEnum = 3,
            /// <summary>
            /// Enum ExceptionEnum for Exception
            /// </summary>
            [EnumMember(Value = "Exception")]
            ExceptionEnum = 4        }

        /// <summary>
        /// Gets or Sets MessageType
        /// </summary>

        [DataMember(Name="messageType")]
        public MessageTypeEnum? MessageType { get; set; }

        /// <summary>
        /// Gets or Sets Text
        /// </summary>

        [DataMember(Name="text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or Sets Timestamp
        /// </summary>
        [RegularExpression("/^-?(([1-9][0-9][0-9][0-9]+)|(0[0-9][0-9][0-9]))-((0[1-9])|(1[0-2]))-((0[1-9])|([12][0-9])|(3[01]))T(((([01][0-9])|(2[0-3])):[0-5][0-9]:([0-5][0-9])(\\.[0-9]+)?)|24:00:00(\\.0+)?)(Z|\\+00:00|-00:00)$/")]
        [DataMember(Name="timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class Message {\n");
            sb.Append("  Code: ").Append(Code).Append("\n");
            sb.Append("  CorrelationId: ").Append(CorrelationId).Append("\n");
            sb.Append("  MessageType: ").Append(MessageType).Append("\n");
            sb.Append("  Text: ").Append(Text).Append("\n");
            sb.Append("  Timestamp: ").Append(Timestamp).Append("\n");
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
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Message)obj);
        }

        /// <summary>
        /// Returns true if Message instances are equal
        /// </summary>
        /// <param name="other">Instance of Message to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(Message other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return 
                (
                    Code == other.Code ||
                    Code != null &&
                    Code.Equals(other.Code)
                ) && 
                (
                    CorrelationId == other.CorrelationId ||
                    CorrelationId != null &&
                    CorrelationId.Equals(other.CorrelationId)
                ) && 
                (
                    MessageType == other.MessageType ||
                    MessageType != null &&
                    MessageType.Equals(other.MessageType)
                ) && 
                (
                    Text == other.Text ||
                    Text != null &&
                    Text.Equals(other.Text)
                ) && 
                (
                    Timestamp == other.Timestamp ||
                    Timestamp != null &&
                    Timestamp.Equals(other.Timestamp)
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
                    if (Code != null)
                    hashCode = hashCode * 59 + Code.GetHashCode();
                    if (CorrelationId != null)
                    hashCode = hashCode * 59 + CorrelationId.GetHashCode();
                    if (MessageType != null)
                    hashCode = hashCode * 59 + MessageType.GetHashCode();
                    if (Text != null)
                    hashCode = hashCode * 59 + Text.GetHashCode();
                    if (Timestamp != null)
                    hashCode = hashCode * 59 + Timestamp.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(Message left, Message right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Message left, Message right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}
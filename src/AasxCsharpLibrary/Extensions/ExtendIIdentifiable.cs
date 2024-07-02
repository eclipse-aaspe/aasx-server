﻿using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendIIdentifiable
    {
        #region List of Identifiers

        public static string ToStringExtended(this List<IIdentifiable> identifiables, string delimiter = ",")
        {
            return string.Join(delimiter, identifiables.Select((x) => x.Id));
        }

        #endregion
        public static Reference? GetReference(this IIdentifiable identifiable)
        {
            var key = new Key(ExtensionsUtil.GetKeyType(identifiable), identifiable.Id);
            // TODO (jtikekar, 2023-09-04): if model or Global reference?
            var outputReference = new Reference(ReferenceTypes.ModelReference, new List<IKey>() { key });

            return outputReference;
        }
    }
}

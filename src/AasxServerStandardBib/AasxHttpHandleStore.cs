﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdminShellNS;
using Newtonsoft.Json;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). 
The Grapevine REST server framework is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    /// <summary>
    /// Describes a handle to a Identification or Reference to be used in HTTP REST APIs
    /// </summary>
    public abstract class AasxHttpHandle
    {
        [JsonProperty(PropertyName = "key")]
        public string Key;
        [JsonIgnore]
        public DateTime ExpiresInternal;
        [JsonProperty(PropertyName = "expires")]
        public string Expires; // http-date, see https://stackoverflow.com/questions/21120882/the-date-time-format-used-in-http-headers
    }

    /// <summary>
    /// Describes a handle to a Identification to be used in HTTP REST APIs
    /// </summary>
    public class AasxHttpHandleIdentification : AasxHttpHandle
    {
        private static int counter = 1;

        public AdminShell.Identification identification = null;

        public AasxHttpHandleIdentification(AdminShell.Identification src, string keyPreset = null)
        {
            if (keyPreset == null)
                this.Key = $"@ID{counter++:00000000}";
            else
                this.Key = keyPreset;
            this.ExpiresInternal = DateTime.UtcNow.AddMinutes(60);
            this.Expires = this.ExpiresInternal.ToString("R");
            this.identification = new AdminShell.Identification(src);
        }
    }
}

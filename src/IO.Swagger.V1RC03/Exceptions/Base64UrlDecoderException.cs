﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Exceptions
{
    public class Base64UrlDecoderException : Exception
    {
        public Base64UrlDecoderException(string fieldName) : base($"Unable decode {fieldName}. Incorrect Base64Url encoded string.")
        {

        }
    }
}

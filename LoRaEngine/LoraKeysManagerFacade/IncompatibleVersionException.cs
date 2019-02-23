// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;
    using System.Runtime.Serialization;

    public class IncompatibleVersionException : Exception
    {
        public IncompatibleVersionException()
        {
        }

        public IncompatibleVersionException(string message)
            : base(message)
        {
        }

        public IncompatibleVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected IncompatibleVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

// <copyright file="SessionAbortedException.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using Properties;

    /// <summary>
    /// Represent an exception which causes a multicast session to be aborted.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class SessionAbortedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAbortedException"/> class.
        /// </summary>
        public SessionAbortedException()
            : this(Resources.SessionAborted)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAbortedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SessionAbortedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAbortedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public SessionAbortedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

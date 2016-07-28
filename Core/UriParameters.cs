// <copyright file="UriParameters.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Cryptography;
    using Properties;

    /// <summary>
    /// Represent a class which can read multicast parameters from a <see cref="Uri"/>
    /// </summary>
    /// <remarks>In addition to using a valid URI path, the client and server's <see cref="IEncoder"/> parameters and TTL settings must match.
    /// <example>This example shows how to specify a normal multicast session, with a file named "test.cab".
    /// <code>mc://test.corp.us/test.cab</code>
    /// </example>
    /// <example>This example shows how to specify a multicast session with TLS and a custom port, with a folder named "images".
    /// <code>mcs://test.corp.us:8080/images</code>
    /// </example>
    /// </remarks>
    public class UriParameters
    {
        /// <summary>
        /// The normal scheme, which is mc
        /// </summary>
        public const string NormalScheme = "mc";

        /// <summary>
        /// The TLS scheme, which is mcs
        /// </summary>
        public const string TlsScheme = "mcs";

        /// <summary>
        /// The default port, which is 789
        /// </summary>
        public const int DefaultPort = 789;

        /// <summary>
        /// Initializes a new instance of the <see cref="UriParameters"/> class.
        /// </summary>
        public UriParameters()
        {
            this.UseTls = false;
            this.Hostname = "localhost";
            this.Port = DefaultPort;
            this.Path = "+";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UriParameters"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public UriParameters(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (uri.Scheme == TlsScheme)
            {
                this.UseTls = true;
            }
            else if (uri.Scheme != NormalScheme)
            {
                throw new ArgumentException(Resources.InvalidUriScheme);
            }

            this.Hostname = uri.DnsSafeHost;
            this.Port = uri.Port;
            if (this.Port <= 0)
            {
                this.Port = DefaultPort;
            }

            this.Path = uri.AbsolutePath;
        }

        /// <summary>
        /// Gets or sets the hostname.
        /// </summary>
        /// <value>
        /// The hostname.
        /// </value>
        public string Hostname
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether using TLS in the session.
        /// </summary>
        /// <value>
        ///   <c>true</c> if using TLS; otherwise, <c>false</c>.
        /// </value>
        public bool UseTls
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        /// <remarks>For a client, the path is the location relative to the server root of the file or directory being requested.
        /// For a server, the path is the root path from where files will be located. Therefor, the path in a client request is implicitly located
        /// under the server path.
        /// </remarks>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Converts this object to a <see cref="Uri"/>.
        /// </summary>
        /// <returns>A URI object.</returns>
        public Uri ToUri()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((this.UseTls ? TlsScheme : NormalScheme) + "://");
            sb.Append(this.Hostname ?? string.Empty);
            if (this.Port != DefaultPort)
            {
                sb.Append(":" + this.Port);
            }

            if (!string.IsNullOrEmpty(this.Path))
            {
                string[] pathParts = this.Path.Split('/', '\\');
                foreach (string part in pathParts)
                {
                    sb.Append("/" + Uri.EscapeDataString(part));
                }
            }

            return new Uri(sb.ToString());
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.ToUri().ToString();
        }
    }
}

// <copyright file="ITcpSocketClientExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.Cryptography
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.Cryptography;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Tls;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Security;
    using PCLStorage;
    using Properties;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;
    using asn1x509 = Org.BouncyCastle.Asn1.X509;
    using iopem = Org.BouncyCastle.Utilities.IO.Pem;

    internal static class ITcpSocketClientExtensions
    {
        internal static TlsServerProtocol AcceptTls(this ITcpSocketClient serverSocket, byte[] psk)
        {
            Contract.Requires(psk != null);
            SecureRandom sr = new SecureRandom();
            TlsServerProtocol serverProtocol = new TlsServerProtocol(serverSocket.ReadStream, serverSocket.WriteStream, sr);
            serverProtocol.Accept(new TcpPskTlsServer(psk));
            return serverProtocol;
        }

        // See https://github.com/onovotny/BouncyCastle-PCL/blob/b7e5032ad477410b05df569ea742f82cd04246a1/crypto/test/src/crypto/tls/test/MockPskTlsServer.cs
        internal class TcpPskTlsServer : PskTlsServer
        {
            private ILog log = LogManager.GetLogger<TcpPskTlsServer>();

            internal TcpPskTlsServer(byte[] psk)
                : base(new MyIdentityManager(psk))
            {
            }

            protected override ProtocolVersion MaximumVersion
            {
                get { return ProtocolVersion.TLSv12; }
            }

            protected override ProtocolVersion MinimumVersion
            {
                get { return ProtocolVersion.TLSv12; }
            }

            public override void NotifyAlertRaised(byte alertLevel, byte alertDescription, string message, Exception cause)
            {
                using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
                {
                    sw.WriteLine("TLS-PSK server raised alert: " + AlertLevel.GetText(alertLevel) + ", " + AlertDescription.GetText(alertDescription));
                    if (message != null)
                    {
                        sw.WriteLine("> " + message);
                    }

                    if (cause != null)
                    {
                        sw.WriteLine(cause);
                    }

                    this.LogAlert(alertLevel, sw);
                }
            }

            public override void NotifyAlertReceived(byte alertLevel, byte alertDescription)
            {
                using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
                {
                    sw.WriteLine("TLS-PSK server received alert: " + AlertLevel.GetText(alertLevel) + ", " + AlertDescription.GetText(alertDescription));
                    this.LogAlert(alertLevel, sw);
                }
            }

            public override void NotifyHandshakeComplete()
            {
                base.NotifyHandshakeComplete();
                byte[] pskIdentity = this.mContext.SecurityParameters.PskIdentity;
                if (pskIdentity != null)
                {
                    string name = Convert.ToBase64String(pskIdentity);
                    this.log.Debug("TLS-PSK server completed handshake for PSK identity: " + name);
                }
            }

            public override ProtocolVersion GetServerVersion()
            {
                ProtocolVersion serverVersion = base.GetServerVersion();
                this.log.Debug("TLS-PSK server negotiated " + serverVersion);
                return serverVersion;
            }

            protected override int[] GetCipherSuites()
            {
                return new int[]
                {
                    CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA
                };
            }

            protected override TlsEncryptionCredentials GetRsaEncryptionCredentials()
            {
                return null;
            }

            private void LogAlert(byte alertLevel, StringWriter sw)
            {
                if (alertLevel == AlertLevel.fatal)
                {
                    this.log.Error(sw.ToString());
                }
                else
                {
                    this.log.Warn(sw.ToString());
                }
            }

            internal class MyIdentityManager : TlsPskIdentityManager
            {
                private byte[] psk;

                internal MyIdentityManager(byte[] psk)
                {
                    this.psk = psk;
                }

                public virtual byte[] GetHint()
                {
                    return Encoding.UTF8.GetBytes("hint");
                }

                public virtual byte[] GetPsk(byte[] identity)
                {
                    if (identity != null)
                    {
                        string name = Encoding.UTF8.GetString(identity, 0, identity.Length);
                        if (name.Equals("client"))
                        {
                            return this.psk;
                        }
                    }

                    return null;
                }
            }
        }
    }
}

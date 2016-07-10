// <copyright file="ITcpSocketClientExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.Cryptography;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Digests;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Crypto.Signers;
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
        internal static TlsClientProtocol ConnectTls(this ITcpSocketClient client, TcpPskTlsClient tlsClient)
        {
            Contract.Requires(tlsClient != null);
            SecureRandom sr = new SecureRandom();
            TlsClientProtocol clientProtocol = new TlsClientProtocol(client.ReadStream, client.WriteStream, sr);
            clientProtocol.Connect(tlsClient);
            return clientProtocol;
        }

        // See https://github.com/onovotny/BouncyCastle-PCL/blob/b7e5032ad477410b05df569ea742f82cd04246a1/crypto/test/src/crypto/tls/test/MockPskTlsClient.cs
        internal class TcpPskTlsClient : PskTlsClient
        {
            private ILog log = LogManager.GetLogger<TcpPskTlsClient>();
            private TlsSession session;

            internal TcpPskTlsClient(TlsSession session, byte[] psk)
                : base(new BasicTlsPskIdentity(Encoding.UTF8.GetBytes("client"), psk))
            {
                this.session = session;
            }

            public override ProtocolVersion MinimumVersion
            {
                get
                {
                    return ProtocolVersion.TLSv12;
                }
            }

            public override TlsSession GetSessionToResume()
            {
                return this.session;
            }

            public override void NotifyAlertRaised(byte alertLevel, byte alertDescription, string message, Exception cause)
            {
                using (StringWriter sw = new StringWriter())
                {
                    sw.WriteLine("TLS-PSK client raised alert: " + AlertLevel.GetText(alertLevel) + ", " + AlertDescription.GetText(alertDescription));
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
                using (StringWriter sw = new StringWriter())
                {
                    sw.WriteLine("TLS-PSK client received alert: " + AlertLevel.GetText(alertLevel) + ", " + AlertDescription.GetText(alertDescription));
                    this.LogAlert(alertLevel, sw);
                }
            }

            public override void NotifyHandshakeComplete()
            {
                base.NotifyHandshakeComplete();
                TlsSession newSession = this.mContext.ResumableSession;
                if (newSession != null)
                {
                    byte[] newSessionID = newSession.SessionID;
                    string base64 = Convert.ToBase64String(newSessionID);
                    if (this.session != null && this.session.SessionID.SequenceEqual(newSessionID))
                    {
                        this.log.Debug("Resumed session: " + base64);
                    }
                    else
                    {
                        this.log.Debug("Established session: " + base64);
                    }

                    this.session = newSession;
                }
            }

            public override int[] GetCipherSuites()
            {
                return new int[]
                {
                    CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384,
                    CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA
                };
            }

            public override IDictionary GetClientExtensions()
            {
                IDictionary clientExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(base.GetClientExtensions());
                TlsExtensionsUtilities.AddEncryptThenMacExtension(clientExtensions);
                return clientExtensions;
            }

            public override void NotifyServerVersion(ProtocolVersion serverVersion)
            {
                base.NotifyServerVersion(serverVersion);
                this.log.Debug("TLS-PSK client negotiated " + serverVersion);
            }

            public override TlsAuthentication GetAuthentication()
            {
                return new MyTlsAuthentication(this.mContext);
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

            internal class MyTlsAuthentication : ServerOnlyTlsAuthentication
            {
                private readonly TlsContext context;
                private ILog log = LogManager.GetLogger<MyTlsAuthentication>();

                internal MyTlsAuthentication(TlsContext context)
                {
                    this.context = context;
                }

                public override void NotifyServerCertificate(Certificate serverCertificate)
                {
                    asn1x509.X509CertificateStructure[] chain = serverCertificate.GetCertificateList();
                    this.log.Debug("TLS-PSK client received server certificate chain of length " + chain.Length);
                    for (int i = 0; i != chain.Length; i++)
                    {
                        asn1x509.X509CertificateStructure entry = chain[i];

                        // TODO Create fingerprint based on certificate signature algorithm digest
                        this.log.Debug("    fingerprint:SHA-256 " + entry.Signature + " (" + entry.Subject + ")");
                    }
                }
            }
        }
    }
}

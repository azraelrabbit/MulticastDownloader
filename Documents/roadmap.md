Summary
=======

This document is the roadmap for getting the current version of the
Multicast Downloader to V1. Note that there will be allusions to a prior
version of a multicast downloader here. Where this comes from (and why)
is immaterial to this document, but it important to know that this is
deliberate.

Current Downloader
------------------

![](roadmap_media/media/image1.emf)

Figure Multicast Session Diagram

To summarize, the current multicast downloader is presented as a block
diagram in Figure 1. The TCP channel in green is opened to initiate a
Session Join Request and receive a list of files to be copied, and again
to leave the session once the files have been downloaded. The UDP
channel in yellow is used to send periodic client updates once a session
has been initiated. The UDP channel in orange is used to send a file
payload as a IPV6 multicast packet. Figures 2 and 3 further outline how
the server and client transmit and receive files.

![](roadmap_media/media/image2.emf)

Figure Multicast client

![](roadmap_media/media/image3.emf)

Figure Multicast Server

Jumbo Packets
-------------

One of the limitations in the current system is that the packet size is
assumed to be 1500. Some networks can be configured to support larger
packet sizes. The new downloader should support arbitrarily sized
packets, &gt;= 1500 bytes. It should also be able to encode multiple
chunks of file data into a single packet, to reduce overhead.

IGMP Multicast
--------------

The current downloader is limited to link-local networks. IGMP multicast
would allow a file transfer to occur over more diverse network
topologies.

V4 and V6 IP
------------

The current downloader is restricted to link-local V6 IP networks.
Supporting V4, in addition to V6, would allow for it to be used over
more diverse network topologies.

Overhead Encoding Size
----------------------

The current downloader transmits overhead data using .NET binary
encoding. Using a variable-length encoding scheme, such as Protocol
Buffers, should decrease the amount of overhead data we need to transmit
for a single session dramatically.

Throughput Calculation
----------------------

The current downloader calculates total throughput by determining which
client is receiving the minimum rate of packets per second. Different
applications may necessitate different methods of calculating
throughput. For example, if it is not necessary for all clients to
receive the same file in the same interval, using the maximum reception
rate instead of the minimum could have a beneficial effect on
performance.

Logging
-------

Diagnosing problems with a multicast download can be complicated. The
new downloader should use an extensible, well-known logging library.

Encryption
----------

The current downloader will not attempt to authenticate session join
requests, nor can it prevent unauthorized clients from viewing multicast
data. The new downloader should support encrypted session join requests
and file transfers via a Passphrase or Certificiate-based credential.

Different Codebases
-------------------

The current downloader has a C++ library for the client, and a C\#
library for the server. Making both libraries .NET portable class
libraries will bring several benefits, including interoperability with
UWP and Xamarin, as well as the introduction of Cmdlets to encapsulate
client/server downloader functionality.

UDP Session Status
------------------

Some session status, particularly session join/session leave status, is
sent over TCP/IP. This is more reliable, but could potentially impact
performance when many clients are trying to connect to the same server.
Transmitting all session status over UDP should have a beneficial effect
on this.

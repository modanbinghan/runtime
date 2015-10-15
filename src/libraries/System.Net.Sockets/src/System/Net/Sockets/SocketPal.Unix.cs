// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Sockets
{
    internal static partial class SocketPal
    {
        // The API that uses this information is not supported on *nix, and will throw
        // PlatformNotSupportedException instead.
        public const int ProtocolInformationSize = 0;

        public static SocketError GetLastSocketError()
        {
            return GetSocketErrorForErrorCode(Interop.Sys.GetLastError());
        }

        public static SocketError GetSocketErrorForErrorCode(Interop.Error errorCode)
        {
            // TODO: audit these using winsock.h
            switch (errorCode)
            {
                case (Interop.Error)0:
                    return SocketError.Success;

                case Interop.Error.EINTR:
                    return SocketError.Interrupted;

                case Interop.Error.EACCES:
                    return SocketError.AccessDenied;

                case Interop.Error.EFAULT:
                    return SocketError.Fault;

                case Interop.Error.EINVAL:
                    return SocketError.InvalidArgument;

                case Interop.Error.EMFILE:
                case Interop.Error.ENFILE:
                    return SocketError.TooManyOpenSockets;

                case Interop.Error.EAGAIN:
                    return SocketError.WouldBlock;

                case Interop.Error.EINPROGRESS:
                    return SocketError.InProgress;

                case Interop.Error.EALREADY:
                    return SocketError.AlreadyInProgress;

                case Interop.Error.ENOTSOCK:
                    return SocketError.NotSocket;

                case Interop.Error.EDESTADDRREQ:
                    return SocketError.DestinationAddressRequired;

                case Interop.Error.EMSGSIZE:
                    return SocketError.MessageSize;

                case Interop.Error.EPROTOTYPE:
                    return SocketError.ProtocolType;

                case Interop.Error.ENOPROTOOPT:
                    return SocketError.ProtocolOption;

                case Interop.Error.EPROTONOSUPPORT:
                    return SocketError.ProtocolNotSupported;

                // SocketError.SocketNotSupported
                // SocketError.OperationNotSupported
                // SocketError.ProtocolFamilyNotSupported

                case Interop.Error.EAFNOSUPPORT:
                    return SocketError.AddressFamilyNotSupported;

                case Interop.Error.EADDRINUSE:
                    return SocketError.AddressAlreadyInUse;

                case Interop.Error.EADDRNOTAVAIL:
                    return SocketError.AddressNotAvailable;

                case Interop.Error.ENETDOWN:
                    return SocketError.NetworkDown;

                case Interop.Error.ENETUNREACH:
                    return SocketError.NetworkUnreachable;

                case Interop.Error.ENETRESET:
                    return SocketError.NetworkReset;

                case Interop.Error.ECONNABORTED:
                    return SocketError.ConnectionAborted;

                case Interop.Error.ECONNRESET:
                    return SocketError.ConnectionReset;

                // SocketError.NoBufferSpaceAvailable

                case Interop.Error.EISCONN:
                    return SocketError.IsConnected;

                case Interop.Error.ENOTCONN:
                    return SocketError.NotConnected;

                // SocketError.Shutdown

                case Interop.Error.ETIMEDOUT:
                    return SocketError.TimedOut;

                case Interop.Error.ECONNREFUSED:
                    return SocketError.ConnectionRefused;

                // SocketError.HostDown

                case Interop.Error.EHOSTUNREACH:
                    return SocketError.HostUnreachable;

                // SocketError.ProcessLimit

                // Extended Windows Sockets error constant definitions
                // SocketError.SystemNotReady
                // SocketError.VersionNotSupported
                // SocketError.NotInitialized
                // SocketError.Disconnecting
                // SocketError.TypeNotFound
                // SocketError.HostNotFound
                // SocketError.TryAgain
                // SocketError.NoRecovery
                // SocketError.NoData

                // OS dependent errors
                // SocketError.IOPending
                // SocketError.OperationAborted

                default:
                    return SocketError.SocketError;
            }
        }

        public static int GetPlatformAddressFamily(AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
                case AddressFamily.Unspecified:
                    return Interop.libc.AF_UNSPEC;

                case AddressFamily.Unix:
                    return Interop.libc.AF_UNIX;

                case AddressFamily.InterNetwork:
                    return Interop.libc.AF_INET;

                case AddressFamily.InterNetworkV6:
                    return Interop.libc.AF_INET6;

                default:
                    return (int)addressFamily;
            }
        }

        public static int GetPlatformSocketType(SocketType socketType)
        {
            switch (socketType)
            {
                case SocketType.Stream:
                    return Interop.libc.SOCK_STREAM;

                case SocketType.Dgram:
                    return Interop.libc.SOCK_DGRAM;

                case SocketType.Raw:
                    return Interop.libc.SOCK_RAW;

                case SocketType.Rdm:
                    return Interop.libc.SOCK_RDM;

                case SocketType.Seqpacket:
                    return Interop.libc.SOCK_SEQPACKET;

                default:
                    return (int)socketType;
            }
        }

        public static int GetPlatformSocketFlags(SocketFlags socketFlags)
        {
            const SocketFlags StandardFlagsMask = 
                SocketFlags.ControlDataTruncated |
                SocketFlags.DontRoute |
                SocketFlags.OutOfBand |
                SocketFlags.Peek |
                SocketFlags.Truncated;

            if ((int)(socketFlags & StandardFlagsMask) != 0)
            {
                // TODO: how to handle this?
                return (int)socketFlags;
            }

            return
                ((socketFlags & SocketFlags.ControlDataTruncated) == 0 ? 0 : Interop.libc.MSG_CTRUNC) |
                ((socketFlags & SocketFlags.DontRoute) == 0 ? 0 : Interop.libc.MSG_DONTROUTE) |
                ((socketFlags & SocketFlags.OutOfBand) == 0 ? 0 : Interop.libc.MSG_OOB) |
                ((socketFlags & SocketFlags.Peek) == 0 ? 0 : Interop.libc.MSG_PEEK) |
                ((socketFlags & SocketFlags.Truncated) == 0 ? 0 : Interop.libc.MSG_TRUNC);
        }

        public static SocketFlags GetSocketFlags(int platformSocketFlags)
        {
            const int StandardFlagsMask = 
                Interop.libc.MSG_CTRUNC |
                Interop.libc.MSG_DONTROUTE |
                Interop.libc.MSG_OOB |
                Interop.libc.MSG_PEEK |
                Interop.libc.MSG_TRUNC;

            if ((platformSocketFlags & StandardFlagsMask) != 0)
            {
                // TODO: how to handle this?
                return (SocketFlags)platformSocketFlags;
            }

            return
                ((platformSocketFlags & Interop.libc.MSG_CTRUNC) == 0 ? 0 : SocketFlags.ControlDataTruncated) |
                ((platformSocketFlags & Interop.libc.MSG_DONTROUTE) == 0 ? 0 : SocketFlags.DontRoute) |
                ((platformSocketFlags & Interop.libc.MSG_OOB) == 0 ? 0 : SocketFlags.OutOfBand) |
                ((platformSocketFlags & Interop.libc.MSG_PEEK) == 0 ? 0 : SocketFlags.Peek) |
                ((platformSocketFlags & Interop.libc.MSG_TRUNC) == 0 ? 0 : SocketFlags.Truncated);
        }

        private static bool GetPlatformOptionInfo(SocketOptionLevel optionLevel, SocketOptionName optionName, out int optLevel, out int optName)
        {
            // TODO: determine what option level honors these option names
            // - SocketOptionName.BsdUrgent
            // - case SocketOptionName.Expedited

            // TODO: decide how to handle option names that have no corresponding name on *nix
            switch (optionLevel)
            {
                case SocketOptionLevel.Socket:
                    optLevel = Interop.libc.SOL_SOCKET;
                    switch (optionName)
                    {
                        case SocketOptionName.Debug:
                            optName = Interop.libc.SO_DEBUG;
                            break;

                        case SocketOptionName.AcceptConnection:
                            optName = Interop.libc.SO_ACCEPTCONN;
                            break;

                        case SocketOptionName.ReuseAddress:
                            optName = Interop.libc.SO_REUSEADDR;
                            break;

                        case SocketOptionName.KeepAlive:
                            optName = Interop.libc.SO_KEEPALIVE;
                            break;

                        case SocketOptionName.DontRoute:
                            optName = Interop.libc.SO_DONTROUTE;
                            break;

                        case SocketOptionName.Broadcast:
                            optName = Interop.libc.SO_BROADCAST;
                            break;

                        // SocketOptionName.UseLoopback:

                        case SocketOptionName.Linger:
                            optName = Interop.libc.SO_LINGER;
                            break;

                        case SocketOptionName.OutOfBandInline:
                            optName = Interop.libc.SO_OOBINLINE;
                            break;

                        // case SocketOptionName.DontLinger
                        // case SocketOptionName.ExclusiveAddressUse

                        case SocketOptionName.SendBuffer:
                            optName = Interop.libc.SO_SNDBUF;
                            break;

                        case SocketOptionName.ReceiveBuffer:
                            optName = Interop.libc.SO_RCVBUF;
                            break;

                        case SocketOptionName.SendLowWater:
                            optName = Interop.libc.SO_SNDLOWAT;
                            break;

                        case SocketOptionName.ReceiveLowWater:
                            optName = Interop.libc.SO_RCVLOWAT;
                            break;

                        case SocketOptionName.SendTimeout:
                            optName = Interop.libc.SO_SNDTIMEO;
                            break;

                        case SocketOptionName.ReceiveTimeout:
                            optName = Interop.libc.SO_RCVTIMEO;
                            break;

                        case SocketOptionName.Error:
                            optName = Interop.libc.SO_ERROR;
                            break;

                        case SocketOptionName.Type:
                            optName = Interop.libc.SO_TYPE;
                            break;

                        // case SocketOptionName.MaxConnections
                        // case SocketOptionName.UpdateAcceptContext:
                        // case SocketOptionName.UpdateConnectContext:

                        default:
                            optName = (int)optionName;
                            return false;
                    }
                    return true;

                case SocketOptionLevel.Tcp:
                    optLevel = Interop.libc.IPPROTO_TCP;
                    switch (optionName)
                    {
                        case SocketOptionName.NoDelay:
                            optName = Interop.libc.TCP_NODELAY;
                            break;

                        default:
                            optName = (int)optionName;
                            return false;
                    }
                    return true;

                case SocketOptionLevel.Udp:
                    optLevel = Interop.libc.IPPROTO_UDP;

                    // case SocketOptionName.NoChecksum:
                    // case SocketOptionName.ChecksumCoverage:

                    optName = (int)optionName;
                    return false;

                case SocketOptionLevel.IP:
                    optLevel = Interop.libc.IPPROTO_IP;
                    switch (optionName)
                    {
                        case SocketOptionName.IPOptions:
                            optName = Interop.libc.IP_OPTIONS;
                            break;

                        case SocketOptionName.HeaderIncluded:
                            optName = Interop.libc.IP_HDRINCL;
                            break;

                        case SocketOptionName.TypeOfService:
                            optName = Interop.libc.IP_TOS;
                            break;

                        case SocketOptionName.IpTimeToLive:
                            optName = Interop.libc.IP_TTL;
                            break;

                        case SocketOptionName.MulticastInterface:
                            optName = Interop.libc.IP_MULTICAST_IF;
                            break;

                        case SocketOptionName.MulticastTimeToLive:
                            optName = Interop.libc.IP_MULTICAST_TTL;
                            break;

                        case SocketOptionName.MulticastLoopback:
                            optName = Interop.libc.IP_MULTICAST_LOOP;
                            break;

                        case SocketOptionName.AddMembership:
                            optName = Interop.libc.IP_ADD_MEMBERSHIP;
                            break;

                        case SocketOptionName.DropMembership:
                            optName = Interop.libc.IP_DROP_MEMBERSHIP;
                            break;

                        // case SocketOptionName.DontFragment

                        case SocketOptionName.AddSourceMembership:
                            optName = Interop.libc.IP_ADD_SOURCE_MEMBERSHIP;
                            break;

                        case SocketOptionName.DropSourceMembership:
                            optName = Interop.libc.IP_DROP_SOURCE_MEMBERSHIP;
                            break;

                        case SocketOptionName.BlockSource:
                            optName = Interop.libc.IP_BLOCK_SOURCE;
                            break;

                        case SocketOptionName.UnblockSource:
                            optName = Interop.libc.IP_UNBLOCK_SOURCE;
                            break;

                        case SocketOptionName.PacketInformation:
                            optName = Interop.libc.IP_PKTINFO;
                            break;

                        default:
                            optName = (int)optionName;
                            return false;
                    }
                    return true;

                case SocketOptionLevel.IPv6:
                    optLevel = Interop.libc.IPPROTO_IPV6;
                    switch (optionName)
                    {
                        // case SocketOptionName.HopLimit:

                        // case SocketOption.IPProtectionLevel:

                        case SocketOptionName.IPv6Only:
                            optName = Interop.libc.IPV6_V6ONLY;
                            break;

                        case SocketOptionName.PacketInformation:
                            optName = Interop.libc.IPV6_RECVPKTINFO;
                            break;

                        default:
                            optName = (int)optionName;
                            return false;
                    }
                    return true;

                default:
                    // TODO: rethink this
                    optLevel = (int)optionLevel;
                    optName = (int)optionName;
                    return false;
            }
        }

        private static unsafe IPPacketInformation GetIPPacketInformation(Interop.Sys.MessageHeader* messageHeader, bool isIPv4, bool isIPv6)
        {
            if (!isIPv4 && !isIPv6)
            {
                return default(IPPacketInformation);
            }

            Interop.Sys.IPPacketInformation nativePacketInfo;
            if (!Interop.Sys.TryGetIPPacketInformation(messageHeader, isIPv4, &nativePacketInfo))
            {
                return default(IPPacketInformation);
            }

            return new IPPacketInformation(nativePacketInfo.Address.GetIPAddress(), nativePacketInfo.InterfaceIndex);
        }

        public static SafeCloseSocket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            SafeCloseSocket handle = SafeCloseSocket.CreateSocket(addressFamily, socketType, protocolType);
            if (handle.IsInvalid)
            {
                // TODO: fix the exception here
                throw new SocketException((int)GetLastSocketError());
            }
            return handle;
        }

        public static unsafe SafeCloseSocket CreateSocket(SocketInformation socketInformation, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType)
        {
            throw new PlatformNotSupportedException();
        }

        private static unsafe int Receive(int fd, int flags, int available, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, out int receivedFlags, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null || socketAddressLen == 0);

            var pinnedSocketAddress = default(GCHandle);
            byte* sockAddr = null;
            int sockAddrLen = 0;

            long received;
            try
            {
                if (socketAddress != null)
                {
                    pinnedSocketAddress = GCHandle.Alloc(socketAddress, GCHandleType.Pinned);
                    sockAddr = (byte*)pinnedSocketAddress.AddrOfPinnedObject();
                    sockAddrLen = socketAddressLen;
                }

                fixed (byte* b = buffer)
                {
                    var iov = new Interop.Sys.IOVector {
                        Base = &b[offset],
                        Count = (UIntPtr)count
                    };

                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = &iov,
                        IOVectorCount = 1
                    };

                    errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                    receivedFlags = messageHeader.Flags;
                    sockAddrLen = messageHeader.SocketAddressLen;
                }
            }
            finally
            {
                if (pinnedSocketAddress.IsAllocated)
                {
                    pinnedSocketAddress.Free();
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        private static unsafe int Send(int fd, int flags, byte[] buffer, ref int offset, ref int count, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
        {
            var pinnedSocketAddress = default(GCHandle);
            byte* sockAddr = null;
            int sockAddrLen = 0;

            int sent;
            try
            {
                if (socketAddress != null)
                {
                    pinnedSocketAddress = GCHandle.Alloc(socketAddress, GCHandleType.Pinned);
                    sockAddr = (byte*)pinnedSocketAddress.AddrOfPinnedObject();
                    sockAddrLen = socketAddressLen;
                }

                fixed (byte* b = buffer)
                {
                    var iov = new Interop.Sys.IOVector {
                        Base = &b[offset],
                        Count = (UIntPtr)count
                    };

                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = &iov,
                        IOVectorCount = 1
                    };

                    long bytesSent;
                    errno = Interop.Sys.SendMessage(fd, &messageHeader, flags, &bytesSent);

                    sent = checked((int)bytesSent);
                }
            }
            finally
            {
                if (pinnedSocketAddress.IsAllocated)
                {
                    pinnedSocketAddress.Free();
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            
            offset += sent;
            count -= sent;
            return sent;
        }

        private static unsafe int Send(int fd, int flags, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, byte[] socketAddress, int socketAddressLen, out Interop.Error errno)
        {
            // Pin buffers and set up iovecs.
            int startIndex = bufferIndex, startOffset = offset;

            var pinnedSocketAddress = default(GCHandle);
            byte* sockAddr = null;
            int sockAddrLen = 0;

            int maxBuffers = buffers.Count - startIndex;
            var handles = new GCHandle[maxBuffers];
            var iovecs = new Interop.Sys.IOVector[maxBuffers];

            int sent;
            int toSend = 0, iovCount = maxBuffers;
            try
            {
                for (int i = 0; i < maxBuffers; i++, startOffset = 0)
                {
                    ArraySegment<byte> buffer = buffers[startIndex + i];
                    Debug.Assert(buffer.Offset + startOffset < buffer.Array.Length);

                    handles[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    iovecs[i].Base = &((byte*)handles[i].AddrOfPinnedObject())[buffer.Offset + startOffset];

                    toSend += (buffer.Count - startOffset);
                    iovecs[i].Count = (UIntPtr)(buffer.Count - startOffset);
                }

                if (socketAddress != null)
                {
                    pinnedSocketAddress = GCHandle.Alloc(socketAddress, GCHandleType.Pinned);
                    sockAddr = (byte*)pinnedSocketAddress.AddrOfPinnedObject();
                    sockAddrLen = socketAddressLen;
                }

                // Make the call
                fixed (Interop.Sys.IOVector* iov = iovecs)
                {
                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = iov,
                        IOVectorCount = iovCount
                    };

                    long bytesSent;
                    errno = Interop.Sys.SendMessage(fd, &messageHeader, flags, &bytesSent);

                    sent = checked((int)bytesSent);
                }
            }
            finally
            {
                // Free GC handles.
                for (int i = 0; i < iovCount; i++)
                {
                    if (handles[i].IsAllocated)
                    {
                        handles[i].Free();
                    }
                }

                if (pinnedSocketAddress.IsAllocated)
                {
                    pinnedSocketAddress.Free();
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            // Update position.
            int endIndex = bufferIndex, endOffset = offset, unconsumed = sent;
            for (; endIndex < buffers.Count && unconsumed > 0; endIndex++, endOffset = 0)
            {
                int space = buffers[endIndex].Count - endOffset;
                if (space > unconsumed)
                {
                    endOffset += unconsumed;
                    break;
                }
                unconsumed -= space;
            }

            bufferIndex = endIndex;
            offset = endOffset;

            return sent;
        }

        private static unsafe int Receive(int fd, int flags, int available, IList<ArraySegment<byte>> buffers, byte[] socketAddress, ref int socketAddressLen, out int receivedFlags, out Interop.Error errno)
        {
            // Pin buffers and set up iovecs.
            int maxBuffers = buffers.Count;
            var handles = new GCHandle[maxBuffers];
            var iovecs = new Interop.Sys.IOVector[maxBuffers];

            var pinnedSocketAddress = default(GCHandle);
            byte* sockAddr = null;
            int sockAddrLen = 0;

            long received = 0;
            int toReceive = 0, iovCount = maxBuffers;
            try
            {
                for (int i = 0; i < maxBuffers; i++)
                {
                    ArraySegment<byte> buffer = buffers[i];
                    handles[i] = GCHandle.Alloc(buffer.Array, GCHandleType.Pinned);
                    iovecs[i].Base = &((byte*)handles[i].AddrOfPinnedObject())[buffer.Offset];

                    int space = buffer.Count;
                    toReceive += space;
                    if (toReceive >= available)
                    {
                        iovecs[i].Count = (UIntPtr)(space - (toReceive - available));
                        toReceive = available;
                        iovCount = i + 1;
                        break;
                    }

                    iovecs[i].Count = (UIntPtr)space;
                }

                if (socketAddress != null)
                {
                    pinnedSocketAddress = GCHandle.Alloc(socketAddress, GCHandleType.Pinned);
                    sockAddr = (byte*)pinnedSocketAddress.AddrOfPinnedObject();
                    sockAddrLen = socketAddressLen;
                }

                // Make the call.
                fixed (Interop.Sys.IOVector* iov = iovecs)
                {
                    var messageHeader = new Interop.Sys.MessageHeader {
                        SocketAddress = sockAddr,
                        SocketAddressLen = sockAddrLen,
                        IOVectors = iov,
                        IOVectorCount = iovCount
                    };

                    errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                    receivedFlags = messageHeader.Flags;
                    sockAddrLen = messageHeader.SocketAddressLen;
                }
            }
            finally
            {
                // Free GC handles.
                for (int i = 0; i < iovCount; i++)
                {
                    if (handles[i].IsAllocated)
                    {
                        handles[i].Free();
                    }
                }

                if (pinnedSocketAddress.IsAllocated)
                {
                    pinnedSocketAddress.Free();
                }
            }

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        private static unsafe int ReceiveMessageFrom(int fd, int flags, int available, byte[] buffer, int offset, int count, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out int receivedFlags, out IPPacketInformation ipPacketInformation, out Interop.Error errno)
        {
            Debug.Assert(socketAddress != null);

            int cmsgBufferLen = Interop.Sys.GetControlMessageBufferSize(isIPv4, isIPv6);
            var cmsgBuffer = stackalloc byte[cmsgBufferLen];

            int sockAddrLen = socketAddressLen;

            Interop.Sys.MessageHeader messageHeader;

            long received;
            fixed (byte* rawSocketAddress = socketAddress)
            fixed (byte* b = buffer)
            {
                var sockAddr = (byte*)rawSocketAddress;

                var iov = new Interop.Sys.IOVector {
                    Base = &b[offset],
                    Count = (UIntPtr)count
                };

                messageHeader = new Interop.Sys.MessageHeader {
                    SocketAddress = sockAddr,
                    SocketAddressLen = sockAddrLen,
                    IOVectors = &iov,
                    IOVectorCount = 1,
                    ControlBuffer = cmsgBuffer,
                    ControlBufferLen = cmsgBufferLen
                };

                errno = Interop.Sys.ReceiveMessage(fd, &messageHeader, flags, &received);
                receivedFlags = messageHeader.Flags;
                sockAddrLen = messageHeader.SocketAddressLen;
            }

            ipPacketInformation = GetIPPacketInformation(&messageHeader, isIPv4, isIPv6);

            if (errno != Interop.Error.SUCCESS)
            {
                return -1;
            }

            socketAddressLen = sockAddrLen;
            return checked((int)received);
        }

        public static unsafe bool TryCompleteAccept(int fileDescriptor, byte[] socketAddress, ref int socketAddressLen, out int acceptedFd, out SocketError errorCode)
        {
            int fd;
            Interop.Error errno;
            int sockAddrLen = socketAddressLen;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                errno = Interop.Sys.Accept(fileDescriptor, rawSocketAddress, &sockAddrLen, &fd);
            }

            if (errno == Interop.Error.SUCCESS)
            {
                Debug.Assert(fd != -1);

                // If the accept completed successfully, ensure that the accepted socket is non-blocking.
                int err = Interop.Sys.Fcntl.SetIsNonBlocking(fd, 1);
                if (err == 0)
                {
                    socketAddressLen = sockAddrLen;
                    errorCode = SocketError.Success;
                    acceptedFd = fd;
                }
                else
                {
                    errorCode = GetLastSocketError();
                    acceptedFd = -1;
                    Interop.Sys.Close(fd);
                }

                return true;
            }

            acceptedFd = -1;
            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryStartConnect(int fileDescriptor, byte[] socketAddress, int socketAddressLen, out SocketError errorCode)
        {
            Debug.Assert(socketAddress != null);
            Debug.Assert(socketAddressLen > 0);

            Interop.Error err;
            fixed (byte* rawSocketAddress = socketAddress)
            {
                err = Interop.Sys.Connect(fileDescriptor, rawSocketAddress, socketAddressLen);
            }

            if (err == Interop.Error.SUCCESS)
            {
                errorCode = SocketError.Success;
                return true;
            }

            if (err != Interop.Error.EINPROGRESS)
            {
                errorCode = GetSocketErrorForErrorCode(err);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        // This method is used by systems that may need to reset some socket state before
        // reusing it for another connect attempt (e.g. Linux).
        static unsafe partial void PrimeForNextConnectAttempt(int fileDescriptor, int socketAddressLen);

        public static unsafe bool TryCompleteConnect(int fileDescriptor, int socketAddressLen, out SocketError errorCode)
        {
            int socketErrno;
			var optLen = (uint)sizeof(int);
            int err = Interop.libc.getsockopt(fileDescriptor, Interop.libc.SOL_SOCKET, Interop.libc.SO_ERROR, &socketErrno, &optLen);

            if (err != 0)
            {
                Debug.Assert(Interop.Sys.GetLastError() == Interop.Error.EBADF);
                errorCode = SocketError.SocketError;
                return true;
            }
			Debug.Assert(optLen == (uint)sizeof(int));

            Interop.Error socketError = Interop.Sys.ConvertErrorPlatformToPal(socketErrno);
            if (socketError == Interop.Error.SUCCESS)
            {
                errorCode = SocketError.Success;
                return true;
            }
            else if (socketError == Interop.Error.EINPROGRESS)
            {
                errorCode = SocketError.Success;
                return false;
            }

            errorCode = GetSocketErrorForErrorCode(socketError);
            PrimeForNextConnectAttempt(fileDescriptor, socketAddressLen);
            return true;
        }

        public static bool TryCompleteReceiveFrom(int fileDescriptor, byte[] buffer, int offset, int count, int flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out int receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(fileDescriptor, buffer, null, offset, count, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static bool TryCompleteReceiveFrom(int fileDescriptor, IList<ArraySegment<byte>> buffers, int flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out int receivedFlags, out SocketError errorCode)
        {
            return TryCompleteReceiveFrom(fileDescriptor, null, buffers, 0, 0, flags, socketAddress, ref socketAddressLen, out bytesReceived, out receivedFlags, out errorCode);
        }

        public static unsafe bool TryCompleteReceiveFrom(int fileDescriptor, byte[] buffer, IList<ArraySegment<byte>> buffers, int offset, int count, int flags, byte[] socketAddress, ref int socketAddressLen, out int bytesReceived, out int receivedFlags, out SocketError errorCode)
        {
            int available;
            int err = Interop.libc.ioctl(fileDescriptor, (UIntPtr)Interop.libc.FIONREAD, &available);
            if (err == -1)
            {
                bytesReceived = 0;
                receivedFlags = 0;
                errorCode = GetLastSocketError();
                return true;
            }
            if (available == 0)
            {
                // Always request at least one byte.
                available = 1;
            }

            int received;
            Interop.Error errno;
            if (buffer != null)
            {
                received = Receive(fileDescriptor, flags, available, buffer, offset, count, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
            }
            else
            {
                received = Receive(fileDescriptor, flags, available, buffers, socketAddress, ref socketAddressLen, out receivedFlags, out errno);
            }

            if (received != -1)
            {
                bytesReceived = received;
                errorCode = SocketError.Success;
                return true;
            }

            bytesReceived = 0;

            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static unsafe bool TryCompleteReceiveMessageFrom(int fileDescriptor, byte[] buffer, int offset, int count, int flags, byte[] socketAddress, ref int socketAddressLen, bool isIPv4, bool isIPv6, out int bytesReceived, out int receivedFlags, out IPPacketInformation ipPacketInformation, out SocketError errorCode)
        {
            int available;
            int err = Interop.libc.ioctl(fileDescriptor, (UIntPtr)Interop.libc.FIONREAD, &available);
            if (err == -1)
            {
                bytesReceived = 0;
                receivedFlags = 0;
                ipPacketInformation = default(IPPacketInformation);
                errorCode = GetLastSocketError();
                return true;
            }
            if (available == 0)
            {
                // Always request at least one byte.
                available = 1;
            }

            Interop.Error errno;
            int received = ReceiveMessageFrom(fileDescriptor, flags, available, buffer, offset, count, socketAddress, ref socketAddressLen, isIPv4, isIPv6, out receivedFlags, out ipPacketInformation, out errno);

            if (received != -1)
            {
                bytesReceived = received;
                errorCode = SocketError.Success;
                return true;
            }

            bytesReceived = 0;

            if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
            {
                errorCode = GetSocketErrorForErrorCode(errno);
                return true;
            }

            errorCode = SocketError.Success;
            return false;
        }

        public static bool TryCompleteSendTo(int fileDescriptor, byte[] buffer, ref int offset, ref int count, int flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int bufferIndex = 0;
            return TryCompleteSendTo(fileDescriptor, buffer, null, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(int fileDescriptor, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, int flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            int count = 0;
            return TryCompleteSendTo(fileDescriptor, null, buffers, ref bufferIndex, ref offset, ref count, flags, socketAddress, socketAddressLen, ref bytesSent, out errorCode);
        }

        public static bool TryCompleteSendTo(int fileDescriptor, byte[] buffer, IList<ArraySegment<byte>> buffers, ref int bufferIndex, ref int offset, ref int count, int flags, byte[] socketAddress, int socketAddressLen, ref int bytesSent, out SocketError errorCode)
        {
            for (;;)
            {
                int sent;
                Interop.Error errno;
                if (buffer != null)
                {
                    sent = Send(fileDescriptor, flags, buffer, ref offset, ref count, socketAddress, socketAddressLen, out errno);
                }
                else
                {
                    sent = Send(fileDescriptor, flags, buffers, ref bufferIndex, ref offset, socketAddress, socketAddressLen, out errno);
                }

                if (sent == -1)
                {
                    if (errno != Interop.Error.EAGAIN && errno != Interop.Error.EWOULDBLOCK)
                    {
                        errorCode = GetSocketErrorForErrorCode(errno);
                        return true;
                    }

                    errorCode = SocketError.Success;
                    return false;
                }

                bytesSent += sent;

                bool isComplete = sent == 0 ||
                    (buffer != null && count == 0) ||
                    (buffers != null && bufferIndex == buffers.Count);
                if (isComplete)
                {
                    errorCode = SocketError.Success;
                    return true;
                }
            }
        }

        public static SocketError SetBlocking(SafeCloseSocket handle, bool shouldBlock, out bool willBlock)
        {
            // NOTE: since we need to emulate blocking I/O on *nix (!), this does NOT change the blocking
            //       mode of the socket. Instead, it toggles a bit on the handle to indicate whether or not
            //       the PAL methods with blocking semantics should retry in the case of an operation that
            //       cannot be completed synchronously.
            handle.IsNonBlocking = !shouldBlock;
            willBlock = shouldBlock;
            return SocketError.Success;
        }

        public static unsafe SocketError GetSockName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            Interop.Error err;
            int addrLen = nameLen;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.GetSockName(handle.FileDescriptor, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetAvailable(SafeCloseSocket handle, out int available)
        {
            int value = 0;
            int err = Interop.libc.ioctl(handle.FileDescriptor, (UIntPtr)Interop.libc.FIONREAD, &value);
            available = value;

            return err == -1 ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetPeerName(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            Interop.Error err;
            int addrLen = nameLen;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.GetPeerName(handle.FileDescriptor, rawBuffer, &addrLen);
            }

            nameLen = addrLen;
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError Bind(SafeCloseSocket handle, byte[] buffer, int nameLen)
        {
            Interop.Error err;
            fixed (byte* rawBuffer = buffer)
            {
                err = Interop.Sys.Bind(handle.FileDescriptor, rawBuffer, nameLen);
            }

            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static SocketError Listen(SafeCloseSocket handle, int backlog)
        {
            Interop.Error err = Interop.Sys.Listen(handle.FileDescriptor, backlog);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static SafeCloseSocket Accept(SafeCloseSocket handle, byte[] buffer, ref int nameLen)
        {
            return SafeCloseSocket.Accept(handle, buffer, ref nameLen);
        }

        public static SocketError Connect(SafeCloseSocket handle, byte[] socketAddress, int socketAddressLen)
        {
            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Connect(socketAddress, socketAddressLen, -1);
            }

            SocketError errorCode;
            bool completed = TryStartConnect(handle.FileDescriptor, socketAddress, socketAddressLen, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Disconnect(Socket socket, SafeCloseSocket handle, bool reuseSocket)
        {
            throw new PlatformNotSupportedException();
        }

        public static SocketError Send(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out int bytesTransferred)
        {
            var bufferList = buffers;
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Send(bufferList, platformFlags, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            int bufferIndex = 0;
            int offset = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, bufferList, ref bufferIndex, ref offset, platformFlags, null, 0, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Send(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Send(buffer, offset, count, platformFlags, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, buffer, ref offset, ref count, platformFlags, null, 0, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError SendTo(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, byte[] socketAddress, int socketAddressLen, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.SendTo(buffer, offset, count, platformFlags, socketAddress, socketAddressLen, handle.SendTimeout, out bytesTransferred);
            }

            bytesTransferred = 0;
            SocketError errorCode;
            bool completed = TryCompleteSendTo(handle.FileDescriptor, buffer, ref offset, ref count, platformFlags, socketAddress, socketAddressLen, ref bytesTransferred, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Receive(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, ref SocketFlags socketFlags, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            SocketError errorCode;
            if (!handle.IsNonBlocking)
            {
                errorCode = handle.AsyncContext.Receive(buffers, ref platformFlags, handle.ReceiveTimeout, out bytesTransferred);
            }
            else
            {
                int socketAddressLen = 0;
                if (!TryCompleteReceiveFrom(handle.FileDescriptor, buffers, platformFlags, null, ref socketAddressLen, out bytesTransferred, out platformFlags, out errorCode))
                {
                    errorCode = SocketError.WouldBlock;
                }
            }

            socketFlags = GetSocketFlags(platformFlags);
            return errorCode;
        }

        public static SocketError Receive(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.Receive(buffer, offset, count, ref platformFlags, handle.ReceiveTimeout, out bytesTransferred);
            }

            int socketAddressLen = 0;
            SocketError errorCode;
            bool completed = TryCompleteReceiveFrom(handle.FileDescriptor, buffer, offset, count, platformFlags, null, ref socketAddressLen, out bytesTransferred, out platformFlags, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError ReceiveMessageFrom(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, ref SocketFlags socketFlags, Internals.SocketAddress socketAddress, out Internals.SocketAddress receiveAddress, out IPPacketInformation ipPacketInformation, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);
            byte[] socketAddressBuffer = socketAddress.Buffer;
            int socketAddressLen = socketAddress.Size;

            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(socket.AddressFamily, socketAddress, out isIPv4, out isIPv6);

            SocketError errorCode;
            if (!handle.IsNonBlocking)
            {
                errorCode = handle.AsyncContext.ReceiveMessageFrom(buffer, offset, count, ref platformFlags, socketAddressBuffer, ref socketAddressLen, isIPv4, isIPv6, handle.ReceiveTimeout, out ipPacketInformation, out bytesTransferred);
            }
            else
            {
                if (!TryCompleteReceiveMessageFrom(handle.FileDescriptor, buffer, offset, count, platformFlags, socketAddressBuffer, ref socketAddressLen, isIPv4, isIPv6, out bytesTransferred, out platformFlags, out ipPacketInformation, out errorCode))
                {
                    errorCode = SocketError.WouldBlock;
                }
            }

            socketAddress.InternalSize = socketAddressLen;
            receiveAddress = socketAddress;
            socketFlags = GetSocketFlags(platformFlags);
            return errorCode;
        }

        public static SocketError ReceiveFrom(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, byte[] socketAddress, ref int socketAddressLen, out int bytesTransferred)
        {
            int platformFlags = GetPlatformSocketFlags(socketFlags);

            if (!handle.IsNonBlocking)
            {
                return handle.AsyncContext.ReceiveFrom(buffer, offset, count, ref platformFlags, socketAddress, ref socketAddressLen, handle.ReceiveTimeout, out bytesTransferred);
            }

            SocketError errorCode;
            bool completed = TryCompleteReceiveFrom(handle.FileDescriptor, buffer, offset, count, platformFlags, socketAddress, ref socketAddressLen, out bytesTransferred, out platformFlags, out errorCode);
            return completed ? errorCode : SocketError.WouldBlock;
        }

        public static SocketError Ioctl(SafeCloseSocket handle, int ioControlCode, byte[] optionInValue, byte[] optionOutValue, out int optionLength)
        {
            // TODO: can this be supported in some reasonable fashion?
            throw new PlatformNotSupportedException();
        }

        public static SocketError IoctlInternal(SafeCloseSocket handle, IOControlCode ioControlCode, IntPtr optionInValue, int inValueLength, IntPtr optionOutValue, int outValueLength, out int optionLength)
        {
            // TODO: can this be supported in some reasonable fashion?
            throw new PlatformNotSupportedException();
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Socket)
            {
                if (optionName == SocketOptionName.ReceiveTimeout)
                {
                    handle.ReceiveTimeout = optionValue == 0 ? -1 : optionValue;
                    return SocketError.Success;
                }
                else if (optionName == SocketOptionName.SendTimeout)
                {
                    handle.SendTimeout = optionValue == 0 ? -1 : optionValue;
                    return SocketError.Success;
                }
            }

            int optLevel, optName;
            GetPlatformOptionInfo(optionLevel, optionName, out optLevel, out optName);

            int err = Interop.libc.setsockopt(handle.FileDescriptor, optLevel, optName, &optionValue, sizeof(int));

            return err == -1 ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            int optLevel, optName;
            GetPlatformOptionInfo(optionLevel, optionName, out optLevel, out optName);

            int err;
            if (optionValue == null || optionValue.Length == 0)
            {
                err = Interop.libc.setsockopt(handle.FileDescriptor, optLevel, optName, null, 0);
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.libc.setsockopt(handle.FileDescriptor, optLevel, optName, pinnedValue, (uint)optionValue.Length);
                }
            }

            return err == -1 ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError SetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            var opt = new Interop.Sys.IPv4MulticastOption {
                MulticastAddress = unchecked((uint)optionValue.Group.GetAddress()),
                LocalAddress = unchecked((uint)optionValue.LocalAddress.GetAddress()),
                InterfaceIndex = optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv4MulticastOption(handle.FileDescriptor, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, IPv6MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            var opt = new Interop.Sys.IPv6MulticastOption {
                Address = optionValue.Group.GetNativeIPAddress(),
                InterfaceIndex = (int)optionValue.InterfaceIndex
            };

            Interop.Error err = Interop.Sys.SetIPv6MulticastOption(handle.FileDescriptor, optName, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError SetLingerOption(SafeCloseSocket handle, LingerOption optionValue)
        {
            var opt = new Interop.Sys.LingerOption {
                OnOff = optionValue.Enabled ? 1 : 0,
                Seconds = optionValue.LingerTime
            };

            Interop.Error err = Interop.Sys.SetLingerOption(handle.FileDescriptor, &opt);
            return err == Interop.Error.SUCCESS ? SocketError.Success : GetSocketErrorForErrorCode(err);
        }

        public static unsafe SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, out int optionValue)
        {
            if (optionLevel == SocketOptionLevel.Socket)
            {
                if (optionName == SocketOptionName.ReceiveTimeout)
                {
                    optionValue = handle.ReceiveTimeout == -1 ? 0 : handle.ReceiveTimeout;
                    return SocketError.Success;
                }
                else if (optionName == SocketOptionName.SendTimeout)
                {
                    optionValue = handle.SendTimeout == -1 ? 0 : handle.SendTimeout;
                    return SocketError.Success;
                }
            }

            int optLevel, optName;
            GetPlatformOptionInfo(optionLevel, optionName, out optLevel, out optName);

            int value = 0;
            var optLen = (uint)sizeof(int);
            int err = Interop.libc.getsockopt(handle.FileDescriptor, optLevel, optName, &value, &optLen);
            optionValue = (int)value;

            return err == -1 ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetSockOpt(SafeCloseSocket handle, SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue, ref int optionLength)
        {
            int optLevel, optName;
            GetPlatformOptionInfo(optionLevel, optionName, out optLevel, out optName);

            uint optLen = (uint)optionLength;

            int err;
            if (optionValue == null || optionValue.Length == 0)
            {
                optLen = 0;
                err = Interop.libc.getsockopt(handle.FileDescriptor, optLevel, optName, null, &optLen);
            }
            else
            {
                fixed (byte* pinnedValue = optionValue)
                {
                    err = Interop.libc.getsockopt(handle.FileDescriptor, optLevel, optName, pinnedValue, &optLen);
                }
            }

            optionLength = (int)optLen;
            return err == -1 ? GetLastSocketError() : SocketError.Success;
        }

        public static unsafe SocketError GetMulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv4MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv4MulticastOption(handle.FileDescriptor, optName, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(MulticastOption);
                return GetSocketErrorForErrorCode(err);
            }

            var multicastAddress = new IPAddress((long)opt.MulticastAddress);
            var localAddress = new IPAddress((long)opt.LocalAddress);
            optionValue = new MulticastOption(multicastAddress, localAddress) {
                InterfaceIndex = opt.InterfaceIndex
            };

            return SocketError.Success;
        }

        public static unsafe SocketError GetIPv6MulticastOption(SafeCloseSocket handle, SocketOptionName optionName, out IPv6MulticastOption optionValue)
        {
            Debug.Assert(optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership);

            Interop.Sys.MulticastOption optName = optionName == SocketOptionName.AddMembership ?
                Interop.Sys.MulticastOption.MULTICAST_ADD :
                Interop.Sys.MulticastOption.MULTICAST_DROP;

            Interop.Sys.IPv6MulticastOption opt;
            Interop.Error err = Interop.Sys.GetIPv6MulticastOption(handle.FileDescriptor, optName, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(IPv6MulticastOption);
                return GetSocketErrorForErrorCode(err);
            }

            optionValue = new IPv6MulticastOption(opt.Address.GetIPAddress(), opt.InterfaceIndex);
            return SocketError.Success;
        }

        public static unsafe SocketError GetLingerOption(SafeCloseSocket handle, out LingerOption optionValue)
        {
            var opt = new Interop.Sys.LingerOption();
            Interop.Error err = Interop.Sys.GetLingerOption(handle.FileDescriptor, &opt);
            if (err != Interop.Error.SUCCESS)
            {
                optionValue = default(LingerOption);
                return GetSocketErrorForErrorCode(err);
            }

            optionValue = new LingerOption(opt.OnOff != 0, opt.Seconds);
            return SocketError.Success;
        }

        public static unsafe SocketError Poll(SafeCloseSocket handle, int microseconds, SelectMode mode, out bool status)
        {
            var fdset = new Interop.libc.fd_set();
            fdset.Set(handle.FileDescriptor);

            // TODO: this should probably be 0 if readfds, writefds, and errorfds are all null
            int nfds = handle.FileDescriptor + 1;
            Interop.libc.fd_set* readfds = mode == SelectMode.SelectRead ? &fdset : null;
            Interop.libc.fd_set* writefds = mode == SelectMode.SelectWrite ? &fdset : null;
            Interop.libc.fd_set* errorfds = mode == SelectMode.SelectError ? &fdset : null;

            int socketCount = 0;
            if (microseconds != -1)
            {
                var tv = new Interop.libc.timeval(microseconds);
                socketCount = Interop.libc.select(nfds, readfds, writefds, errorfds, &tv);
            }
            else
            {
                socketCount = Interop.libc.select(nfds, readfds, writefds, errorfds, null);
            }

            if (socketCount == -1)
            {
                status = false;
                return SocketError.SocketError; // TODO: should this be SCH.GetLastSocketError()?
            }

            status = fdset.IsSet(handle.FileDescriptor);
            return (SocketError)socketCount;
        }

        public static unsafe SocketError Select(IList checkRead, IList checkWrite, IList checkError, int microseconds)
        {
            var readSet = new Interop.libc.fd_set();
            int maxReadFd = Socket.FillFdSetFromSocketList(ref readSet, checkRead);

            var writeSet = new Interop.libc.fd_set();
            int maxWriteFd = Socket.FillFdSetFromSocketList(ref writeSet, checkWrite);

            var errorSet = new Interop.libc.fd_set();
            int maxErrorFd = Socket.FillFdSetFromSocketList(ref errorSet, checkError);

            int nfds = 0;
            Interop.libc.fd_set* readfds = null;
            Interop.libc.fd_set* writefds = null;
            Interop.libc.fd_set* errorfds = null;

            if (maxReadFd != 0)
            {
                readfds = &readSet;
                nfds = maxReadFd;
            }

            if (maxWriteFd != 0)
            {
                writefds = &writeSet;
                if (maxWriteFd > nfds)
                {
                    nfds = maxWriteFd;
                }
            }

            if (maxErrorFd != 0)
            {
                errorfds = &errorSet;
                if (maxErrorFd > nfds)
                {
                    nfds = maxErrorFd;
                }
            }

            int socketCount;
            if (microseconds != -1)
            {
                var tv = new Interop.libc.timeval(microseconds);
                socketCount = Interop.libc.select(nfds, readfds, writefds, errorfds, &tv);
            }
            else
            {
                socketCount = Interop.libc.select(nfds, readfds, writefds, errorfds, null);
            }

            GlobalLog.Print("Socket::Select() Interop.libc.select returns socketCount:" + socketCount);

            if (socketCount == -1)
            {
                return SocketError.SocketError; // TODO: should this be SCH.GetLastSocketError()?
            }

            Socket.FilterSocketListUsingFdSet(ref readSet, checkRead);
            Socket.FilterSocketListUsingFdSet(ref writeSet, checkWrite);
            Socket.FilterSocketListUsingFdSet(ref errorSet, checkError);

            return (SocketError)socketCount;
        }

        public static SocketError Shutdown(SafeCloseSocket handle, bool isConnected, bool isDisconnected, SocketShutdown how)
        {
            Interop.Error err = Interop.Sys.Shutdown(handle.FileDescriptor, how);
            if (err == Interop.Error.SUCCESS)
            {
                return SocketError.Success;
            }

            // If shutdown returns ENOTCONN and we think that this socket has ever been connected,
            // ignore the error. This can happen for TCP connections if the underlying connection
            // has reached the CLOSE state. Ignoring the error matches Winsock behavior.
            if (err == Interop.Error.ENOTCONN && (isConnected || isDisconnected))
            {
                return SocketError.Success;
            }

            return GetSocketErrorForErrorCode(err);
        }

        public static SocketError ConnectAsync(Socket socket, SafeCloseSocket handle, byte[] socketAddress, int socketAddressLen, ConnectOverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ConnectAsync(socketAddress, socketAddressLen, asyncResult.CompletionCallback);
        }

        public static SocketError DisconnectAsync(Socket socket, SafeCloseSocket handle, bool reuseSocket, DisconnectOverlappedAsyncResult asyncResult)
        {
            throw new PlatformNotSupportedException();
        }

        public static SocketError SendAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.SendAsync(buffer, offset, count, GetPlatformSocketFlags(socketFlags), asyncResult.CompletionCallback);
        }

        public static SocketError SendAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.SendAsync(buffers, GetPlatformSocketFlags(socketFlags), asyncResult.CompletionCallback);
        }

        public static SocketError SendToAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            return handle.AsyncContext.SendToAsync(buffer, offset, count, GetPlatformSocketFlags(socketFlags), socketAddress.Buffer, socketAddress.Size, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ReceiveAsync(buffer, offset, count, GetPlatformSocketFlags(socketFlags), asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveAsync(SafeCloseSocket handle, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, OverlappedAsyncResult asyncResult)
        {
            return handle.AsyncContext.ReceiveAsync(buffers, GetPlatformSocketFlags(socketFlags), asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveFromAsync(SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, OverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            return handle.AsyncContext.ReceiveFromAsync(buffer, offset, count, GetPlatformSocketFlags(socketFlags), socketAddress.Buffer, socketAddress.InternalSize, asyncResult.CompletionCallback);
        }

        public static SocketError ReceiveMessageFromAsync(Socket socket, SafeCloseSocket handle, byte[] buffer, int offset, int count, SocketFlags socketFlags, Internals.SocketAddress socketAddress, ReceiveMessageOverlappedAsyncResult asyncResult)
        {
            asyncResult.SocketAddress = socketAddress;

            bool isIPv4, isIPv6;
            Socket.GetIPProtocolInformation(((Socket)asyncResult.AsyncObject).AddressFamily, socketAddress, out isIPv4, out isIPv6);

            return handle.AsyncContext.ReceiveMessageFromAsync(buffer, offset, count, GetPlatformSocketFlags(socketFlags), socketAddress.Buffer, socketAddress.InternalSize, isIPv4, isIPv6, asyncResult.CompletionCallback);
        }

        public static SocketError AcceptAsync(Socket socket, SafeCloseSocket handle, SafeCloseSocket acceptHandle, int receiveSize, int socketAddressSize, AcceptOverlappedAsyncResult asyncResult)
        {
            Debug.Assert(acceptHandle == null);

            byte[] socketAddressBuffer = new byte[socketAddressSize];

            return handle.AsyncContext.AcceptAsync(socketAddressBuffer, socketAddressSize, asyncResult.CompletionCallback);
        }
    }
}

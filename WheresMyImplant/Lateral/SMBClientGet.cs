﻿using System;
using System.IO;
using System.Linq;

namespace WheresMyImplant
{
    sealed class SMBClientGet : SMBClient
    {
        private Int64 streamSize = 4096;
        private Int64 allocationSize = 8192;
        private Byte[] bFile;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean CreateRequestGet(String folder)
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x05, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2CreateRequest createRequest = new SMB2CreateRequest();
            if (!String.IsNullOrEmpty(folder))
                createRequest.SetFileName(folder);
            createRequest.SetExtraInfo(1, 0);
            createRequest.SetCreateOptions(new Byte[] { 0x00, 0x00, 0x20, 0x00 });
            createRequest.SetAccessMask(new Byte[] { 0x89, 0x00, 0x12, 0x00 });
            createRequest.SetShareAccess(new Byte[] { 0x05, 0x00, 0x00, 0x00 });
            Byte[] bData = createRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            Boolean result = GetStatus(recieve.Skip(12).Take(4).ToArray());
            if (result)
            {
                guidFileHandle = recieve.Skip(0x0084).Take(16).ToArray();
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean FindRequestGet()
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x0e, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);
            header.SetChainOffset(new Byte[] { 0x68, 0x00, 0x00, 0x00 });

            SMB2FindFileRequestFile findFileRequestFile = new SMB2FindFileRequestFile();
            findFileRequestFile.SetInfoLevel(new Byte[] { 0x25 });
            findFileRequestFile.SetFileID(guidFileHandle);
            findFileRequestFile.SetPadding(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            Byte[] bData = findFileRequestFile.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x0e, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header.SetFlags(new Byte[] { 0x04, 0x00, 0x00, 0x00 });

            SMB2FindFileRequestFile findFileRequestFile2 = new SMB2FindFileRequestFile();
            findFileRequestFile2.SetInfoLevel(new Byte[] { 0x25 });
            findFileRequestFile2.SetFileID(guidFileHandle);
            findFileRequestFile2.SetPadding(new Byte[] { 0x80, 0x00, 0x00, 0x00 });
            Byte[] bData2 = findFileRequestFile2.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader2 = header2.GetHeader();

            ////////////////////////////////////////////////////////////////////////////////
            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Combine combine = new Combine();
            combine.Extend(bHeader);
            combine.Extend(bData);
            combine.Extend(bHeader2);
            combine.Extend(bData2);

            Byte[] bSend = Misc.Combine(bSessionService, combine.Retrieve());
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadRequestGet()
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x08, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);
            header.SetChainOffset(new Byte[] { 0x00, 0x00, 0x00, 0x00 });

            SMB2ReadRequest readRequest = new SMB2ReadRequest();
            readRequest.SetGuidHandleFile(guidFileHandle);

            readRequest.SetLength(BitConverter.GetBytes((Int32)streamSize));
            Byte[] bData = readRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            ////////////////////////////////////////////////////////////////////////////////
            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
            {
                bFile = recieve.Skip(0x8a - 0x36).Take((Int32)streamSize).ToArray();
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean InfoRequestGet1()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x10, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2GetInfo getInfo = new SMB2GetInfo();
            getInfo.SetClass(new Byte[] { 0x01 });
            getInfo.SetInfoLevel(new Byte[] { 0x07 });
            getInfo.SetMaxResponseSize(new Byte[] { 0x00, 0x10, 0x00, 0x00 });
            getInfo.SetGetInfoInputOffset(new Byte[] { 0x68, 0x00 });
            getInfo.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData = getInfo.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            
            Byte[] bHeader = header.GetHeader();

            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x10, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header2.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x04 });

            SMB2GetInfo getInfo2 = new SMB2GetInfo();
            getInfo2.SetClass(new Byte[] { 0x01 });
            getInfo2.SetInfoLevel(new Byte[] { 0x16 });
            getInfo2.SetMaxResponseSize(new Byte[] { 0x00, 0x10, 0x00, 0x00 });
            getInfo2.SetGetInfoInputOffset(new Byte[] { 0x68, 0x00 });
            getInfo2.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData2 = getInfo2.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData2);
            }
            Byte[] bHeader2 = header2.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Combine combine = new Combine();
            combine.Extend(bHeader);
            combine.Extend(bData);
            combine.Extend(bHeader2);
            combine.Extend(bData2);

            Byte[] bSend = Misc.Combine(bSessionService, combine.Retrieve());
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
            {
                Byte[] size = recieve.Skip(0xda - 0x36).Take(8).ToArray();
                streamSize = BitConverter.ToInt64(size, 0);
                Byte[] size2 = recieve.Skip(0xe2 - 0x36).Take(8).ToArray();
                allocationSize = BitConverter.ToInt64(size2, 0);
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean InfoRequestGet2()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x10, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2GetInfo getInfo = new SMB2GetInfo();
            getInfo.SetClass(new Byte[] { 0x02 });
            getInfo.SetInfoLevel(new Byte[] { 0x01 });
            getInfo.SetMaxResponseSize(new Byte[] { 0x58, 0x00, 0x00, 0x00 });
            getInfo.SetGetInfoInputOffset(new Byte[] { 00, 00 });
            getInfo.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData = getInfo.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x10, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header2.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x04 });

            SMB2GetInfo getInfo2 = new SMB2GetInfo();
            getInfo2.SetClass(new Byte[] { 0x02 });
            getInfo2.SetInfoLevel(new Byte[] { 0x05 });
            getInfo2.SetMaxResponseSize(new Byte[] { 0x50, 0x00, 0x00, 0x00 });
            getInfo2.SetGetInfoInputOffset(new Byte[] { 00, 00 });
            getInfo2.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData2 = getInfo2.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData2);
            }
            Byte[] bHeader2 = header2.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Combine combine = new Combine();
            combine.Extend(bHeader);
            combine.Extend(bData);
            combine.Extend(bHeader2);
            combine.Extend(bData2);

            Byte[] bSend = Misc.Combine(bSessionService, combine.Retrieve());
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void WriteFile(String destination)
        {
            using (FileStream fileStream = File.Create(Path.GetFullPath(destination), (Int32)allocationSize, FileOptions.None))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    writer.Write(bFile);
                }
            }
        }
    }
}

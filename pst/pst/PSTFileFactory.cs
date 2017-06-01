﻿using pst.encodables.ndb;
using pst.encodables.ndb.btree;
using pst.impl.converters;
using pst.impl.decoders.ltp.hn;
using pst.impl.decoders.ltp.tc;
using pst.impl.decoders.ndb;
using pst.impl.io;
using pst.impl.ltp.tc;
using pst.utilities;
using System.Collections.Generic;
using System.IO;

namespace pst
{
    public partial class PSTFile
    {
        public static PSTFile Open(Stream stream)
        {
            var streamReader =
                new StreamBasedBlockReader(stream);

            var nodeBTree = new Dictionary<NID, LNBTEntry>();

            var blockBTree = new Dictionary<BID, LBBTEntry>();

            var header =
                PSTServiceFactory
                .CreateHeaderDecoder()
                .Decode(streamReader.Read(BREF.OfValue(BID.OfValue(0), IB.Zero), 546));

            foreach (var entry in PSTServiceFactory
                                  .CreateNodeBTreeLeafKeysEnumerator()
                                  .Enumerate(streamReader, header.Root.NBTRootPage))
            {
                nodeBTree.Add(entry.NodeId, entry);
            }

            foreach (var entry in PSTServiceFactory
                                  .CreateBlockBTreeLeafKeysEnumerator()
                                  .Enumerate(streamReader, header.Root.BBTRootPage))
            {
                blockBTree.Add(entry.BlockReference.BlockId, entry);
            }

            return
                new PSTFile(
                    new MessageStore(
                        Globals.NID_MESSAGE_STORE,
                        PSTServiceFactory.CreatePCBasedPropertyReader(),
                        new DictionaryBasedMapper<NID, LNBTEntry>(nodeBTree),
                        new DictionaryBasedMapper<BID, LBBTEntry>(blockBTree),
                        new LBBTEntryBlockReaderAdapter(streamReader)),
                    new Folder(
                        Globals.NID_ROOT_FOLDER,
                        new RowIndexReader<NID>(
                            new DataRecordToTCROWIDConverter(
                                new NIDDecoder()),
                            PSTServiceFactory.CreateBTreeOnHeapReader(
                                new NIDDecoder()),
                            PSTServiceFactory.CreateHeapOnNodeReader(),
                            new TCINFODecoder(
                                new HIDDecoder(),
                                new TCOLDESCDecoder())),
                        PSTServiceFactory.CreateRowMatrixReader(
                            new NIDDecoder()),
                        PSTServiceFactory.CreatePCBasedPropertyReader(),
                        new DictionaryBasedMapper<NID, LNBTEntry>(nodeBTree),
                        new DictionaryBasedMapper<BID, LBBTEntry>(blockBTree),
                        new LBBTEntryBlockReaderAdapter(streamReader)));
        }
    }
}
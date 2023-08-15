﻿using NebulaAPI;

namespace Dustbin.NebulaSupport
{
    namespace Packet
    {
        public class SyncPlanetData
        {
            public byte[] Data { get; set; }

            public SyncPlanetData()
            {
            }

            public SyncPlanetData(byte[] data)
            {
                Data = data;
            }
        }

        public class ToggleEvent
        {
            public int PlanetId { get; set; }
            public int StorageId { get; set; }
            public bool Enable { get; set; }

            public ToggleEvent()
            {
            }

            public ToggleEvent(int planetId, int storageId, bool enable)
            {
                PlanetId = planetId;
                StorageId = storageId;
                Enable = enable;
            }
        }

        [RegisterPacketProcessor]
        internal class SyncPlanetDataProcessor : BasePacketProcessor<SyncPlanetData>
        {
            public override void ProcessPacket(SyncPlanetData packet, INebulaConnection conn)
            {
                Dustbin.ImportData(packet.Data);
            }
        }

        [RegisterPacketProcessor]
        internal class ToggleEventProcessor : BasePacketProcessor<ToggleEvent>
        {
            public override void ProcessPacket(ToggleEvent packet, INebulaConnection conn)
            {
                var factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
                if (factory == null) return;
                var storageId = packet.StorageId;
                switch (storageId)
                {
                    case 0:
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(new SyncPlanetData(Dustbin.ExportData(factory)));
                        return;
                    case < 0:
                    {
                        var tankPool = factory.factoryStorage.tankPool;
                        TankPatch.IsDustbinField.SetValue(tankPool[-storageId], packet.Enable);
                        return;
                    }
                    default:
                    {
                        var storagePool = factory.factoryStorage.storagePool;
                        StoragePatch.IsDustbinField.SetValue(storagePool[storageId], packet.Enable);
                        return;
                    }
                }
            }
       }
    }
}
using System;
using System.IO;
using NIF.NiObjects.Enums;

namespace NIF.NiObjects
{
    public class BhkEntity : BhkWorldObject
    {
        public HkResponseType ResponseType { get; private set; }

        public ushort ProcessContactCallbackDelay { get; private set; }
        
        private BhkEntity(int shapeReference, SkyrimLayer layer, byte collisionFilterFlags, ushort group) : base(
            shapeReference, layer, collisionFilterFlags, group)
        {
        }

        protected BhkEntity(int shapeReference, SkyrimLayer layer, byte collisionFilterFlags, ushort group, HkResponseType responseType, ushort processContactCallbackDelay) : base(shapeReference, layer, collisionFilterFlags, group)
        {
            ResponseType = responseType;
            ProcessContactCallbackDelay = processContactCallbackDelay;
        }

        protected new static BhkEntity Parse(BinaryReader nifReader, string ownerObjectName, Header header)
        {
            var ancestor = BhkWorldObject.Parse(nifReader, ownerObjectName, header);
            var bhkEntity = new BhkEntity(ancestor.ShapeReference, ancestor.Layer, ancestor.CollisionFilterFlags, ancestor.Group);
            
            var responseType = nifReader.ReadByte();
            bhkEntity.ResponseType = (HkResponseType)responseType;
            
            nifReader.BaseStream.Seek(1, SeekOrigin.Current);

            bhkEntity.ProcessContactCallbackDelay = nifReader.ReadUInt16();

            return bhkEntity;
        }
    }
}
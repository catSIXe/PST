﻿using pst.core;
using pst.encodables.messaging;
using pst.interfaces;
using pst.interfaces.ltp;
using pst.interfaces.ndb;
using pst.utilities;
using System;
using System.Text;

namespace pst.impl.messaging
{
    class PropertyNameToIdMap : IPropertyNameToIdMap
    {
        private readonly IDecoder<NAMEID> nameIdDecoder;
        private readonly IPropertyContextBasedPropertyReader propertyContextBasedPropertyReader;

        public PropertyNameToIdMap(
            IDecoder<NAMEID> nameIdDecoder,
            IPropertyContextBasedPropertyReader propertyContextBasedPropertyReader)
        {
            this.nameIdDecoder = nameIdDecoder;
            this.propertyContextBasedPropertyReader = propertyContextBasedPropertyReader;
        }

        public Maybe<PropertyId> GetPropertyId(Guid propertySet, int numericalId)
        {
            var entryStream =
                propertyContextBasedPropertyReader.ReadProperty(
                    NodePath.OfValue(Globals.NID_NAME_TO_ID_MAP),
                    MAPIProperties.PidTagNameidStreamEntry);

            if (entryStream.HasNoValue)
            {
                return Maybe<PropertyId>.NoValue();
            }

            var entriesCount = entryStream.Value.Value.Length / 8;

            for (var i = 0; i < entriesCount; i++)
            {
                var entry = nameIdDecoder.Decode(entryStream.Value.Value.Take(i * 8, 8));

                if (entry.Type == 0)
                {
                    if (entry.PropertyId == numericalId)
                    {
                        return Maybe<PropertyId>.OfValue(new PropertyId(entry.PropertyIndex + 0x8000));
                    }
                }
            }

            return Maybe<PropertyId>.NoValue();
        }

        public Maybe<PropertyId> GetPropertyId(Guid propertySet, string propertyName)
        {
            var entryStream =
                propertyContextBasedPropertyReader.ReadProperty(
                    NodePath.OfValue(Globals.NID_NAME_TO_ID_MAP),
                    MAPIProperties.PidTagNameidStreamEntry);

            var stringStream =
                propertyContextBasedPropertyReader.ReadProperty(
                    NodePath.OfValue(Globals.NID_NAME_TO_ID_MAP),
                    MAPIProperties.PidTagNameidStreamString);

            if (entryStream.HasNoValue || stringStream.HasNoValue)
            {
                return Maybe<PropertyId>.NoValue();
            }

            var entriesCount = entryStream.Value.Value.Length / 8;

            for (var i = 0; i < entriesCount; i++)
            {
                var entry = nameIdDecoder.Decode(entryStream.Value.Value.Take(i * 8, 8));

                if (entry.Type == 1)
                {
                    var length = stringStream.Value.Value.Take(entry.PropertyId, 4).ToInt32();

                    var value = stringStream.Value.Value.Take(entry.PropertyId + 4, length);

                    var name = Encoding.Unicode.GetString(value);

                    if (name == propertyName)
                    {
                        return Maybe<PropertyId>.OfValue(new PropertyId(entry.PropertyIndex + 0x8000));
                    }
                }
            }

            return Maybe<PropertyId>.NoValue();
        }
    }
}

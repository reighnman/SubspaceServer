﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS.Utilities
{
    public struct DataLocation
    {
        public readonly int ByteOffset;
        public readonly int NumBytes;

        public DataLocation(int byteOffset, int numBytes)
        {
            ByteOffset = byteOffset;
            NumBytes = numBytes;
        }

        public ArraySegment<byte> ToArraySegment(byte[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            return new ArraySegment<byte>(array, ByteOffset, NumBytes);
        }

        #region byte

        public byte GetByte(byte[] data)
        {
            return LittleEndianBitConverter.ToByte(data, ByteOffset);
        }

        public byte GetByte(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToByte(data, ByteOffset + additionalOffset);
        }

        public void SetByte(byte[] data, byte value)
        {
            LittleEndianBitConverter.WriteByteBits(value, data, ByteOffset);
        }

        public void SetByte(byte[] data, byte value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteByteBits(value, data, ByteOffset + additionalOffset);
        }

        #endregion

        #region sbyte

        public sbyte GetSByte(byte[] data)
        {
            return LittleEndianBitConverter.ToSByte(data, ByteOffset);
        }

        public sbyte GetSByte(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToSByte(data, ByteOffset + additionalOffset);
        }

        public void SetSByte(byte[] data, sbyte value)
        {
            LittleEndianBitConverter.WriteSByteBits(value, data, ByteOffset);
        }

        public void SetSByte(byte[] data, sbyte value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteSByteBits(value, data, ByteOffset + additionalOffset);
        }

        #endregion

        #region uint16

        public ushort GetUInt16(byte[] data)
        {
            return LittleEndianBitConverter.ToUInt16(data, ByteOffset);
        }

        public ushort GetUInt16(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToUInt16(data, ByteOffset + additionalOffset);
        }

        public void SetUInt16(byte[] data, ushort value)
        {
            LittleEndianBitConverter.WriteUInt16Bits(value, data, ByteOffset);
        }

        public void SetUInt16(byte[] data, ushort value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteUInt16Bits(value, data, ByteOffset + additionalOffset);
        }

        #endregion

        #region int16

        public short GetInt16(byte[] data)
        {
            return LittleEndianBitConverter.ToInt16(data, ByteOffset);
        }

        public short GetInt16(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToInt16(data, ByteOffset + additionalOffset);
        }

        public void SetInt16(byte[] data, short value)
        {
            LittleEndianBitConverter.WriteInt16Bits(value, data, ByteOffset);
        }

        public void SetInt16(byte[] data, short value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteInt16Bits(value, data, ByteOffset + additionalOffset);
        }

        #endregion

        #region uint32

        public uint GetUInt32(byte[] data)
        {
            return LittleEndianBitConverter.ToUInt32(data, ByteOffset);
        }

        public uint GetUInt32(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToUInt32(data, ByteOffset + additionalOffset);
        }

        public void SetUInt32(byte[] data, uint value)
        {
            LittleEndianBitConverter.WriteUInt32Bits(value, data, ByteOffset);
        }

        public void SetUInt32(byte[] data, uint value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteUInt32Bits(value, data, ByteOffset + additionalOffset);
        }

        #endregion

        #region int32

        public int GetInt32(byte[] data)
        {
            return LittleEndianBitConverter.ToInt32(data, ByteOffset);
        }

        public int GetInt32(byte[] data, int additionalOffset)
        {
            return LittleEndianBitConverter.ToInt32(data, ByteOffset + additionalOffset);
        }

        public void SetInt32(byte[] data, int value)
        {
            LittleEndianBitConverter.WriteInt32Bits(value, data, ByteOffset);
        }

        public void SetInt32(byte[] data, int value, int additionalOffset)
        {
            LittleEndianBitConverter.WriteInt32Bits(value, data, ByteOffset + additionalOffset);
        }

        #endregion
    }

    public struct ByteDataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public ByteDataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 1)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 1 byte");

            _dataLocation = dataLocation;
        }

        public static explicit operator ByteDataLocation(DataLocation dataLocation)
        {
            return new ByteDataLocation(dataLocation);
        }

        public static implicit operator DataLocation(ByteDataLocation byteDataLocation)
        {
            return byteDataLocation._dataLocation;
        }

        public byte GetValue(byte[] data)
        {
            return _dataLocation.GetByte(data);
        }

        public byte GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetByte(data, additionalOffset);
        }

        public void SetValue(byte[] data, byte value)
        {
            _dataLocation.SetByte(data, value);
        }

        public void SetValue(byte[] data, byte value, int additionalOffset)
        {
            _dataLocation.SetByte(data, value, additionalOffset);
        }
    }

    public struct SByteDataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public SByteDataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 1)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 1 byte");

            _dataLocation = dataLocation;
        }

        public static explicit operator SByteDataLocation(DataLocation dataLocation)
        {
            return new SByteDataLocation(dataLocation);
        }

        public static implicit operator DataLocation(SByteDataLocation sbyteDataLocation)
        {
            return sbyteDataLocation._dataLocation;
        }

        public sbyte GetValue(byte[] data)
        {
            return _dataLocation.GetSByte(data);
        }

        public sbyte GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetSByte(data, additionalOffset);
        }

        public void SetValue(byte[] data, sbyte value)
        {
            _dataLocation.SetSByte(data, value);
        }

        public void SetValue(byte[] data, sbyte value, int additionalOffset)
        {
            _dataLocation.SetSByte(data, value, additionalOffset);
        }
    }

    public struct UInt16DataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public UInt16DataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 2)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 2 bytes");

            _dataLocation = dataLocation;
        }

        public static explicit operator UInt16DataLocation(DataLocation dataLocation)
        {
            return new UInt16DataLocation(dataLocation);
        }

        public static implicit operator DataLocation(UInt16DataLocation uint16DataLocation)
        {
            return uint16DataLocation._dataLocation;
        }

        public ushort GetValue(byte[] data)
        {
            return _dataLocation.GetUInt16(data);
        }

        public ushort GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetUInt16(data, additionalOffset);
        }

        public void SetValue(byte[] data, ushort value)
        {
            _dataLocation.SetUInt16(data, value);
        }

        public void SetValue(byte[] data, ushort value, int additionalOffset)
        {
            _dataLocation.SetUInt16(data, value, additionalOffset);
        }
    }

    public struct Int16DataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public Int16DataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 2)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 2 bytes");

            _dataLocation = dataLocation;
        }

        public static explicit operator Int16DataLocation(DataLocation dataLocation)
        {
            return new Int16DataLocation(dataLocation);
        }

        public static implicit operator DataLocation(Int16DataLocation int16DataLocation)
        {
            return int16DataLocation._dataLocation;
        }

        public short GetValue(byte[] data)
        {
            return _dataLocation.GetInt16(data);
        }

        public short GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetInt16(data, additionalOffset);
        }

        public void SetValue(byte[] data, short value)
        {
            _dataLocation.SetInt16(data, value);
        }

        public void SetValue(byte[] data, short value, int additionalOffset)
        {
            _dataLocation.SetInt16(data, value, additionalOffset);
        }
    }

    public struct UInt32DataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public UInt32DataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 4)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 4 bytes");

            _dataLocation = dataLocation;
        }

        public static explicit operator UInt32DataLocation(DataLocation dataLocation)
        {
            return new UInt32DataLocation(dataLocation);
        }

        public static implicit operator DataLocation(UInt32DataLocation uint32DataLocation)
        {
            return uint32DataLocation._dataLocation;
        }

        public uint GetValue(byte[] data)
        {
            return _dataLocation.GetUInt32(data);
        }

        public uint GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetUInt32(data, additionalOffset);
        }

        public void SetValue(byte[] data, uint value)
        {
            _dataLocation.SetUInt32(data, value);
        }

        public void SetValue(byte[] data, uint value, int additionalOffset)
        {
            _dataLocation.SetUInt32(data, value, additionalOffset);
        }
    }

    public struct Int32DataLocation
    {
        private DataLocation _dataLocation;

        public int ByteOffset
        {
            get { return _dataLocation.ByteOffset; }
        }

        public int NumBytes
        {
            get { return _dataLocation.NumBytes; }
        }

        public Int32DataLocation(DataLocation dataLocation)
        {
            if (dataLocation.NumBytes != 4)
                throw new ArgumentOutOfRangeException("dataLocation", "must represent 4 bytes");

            _dataLocation = dataLocation;
        }

        public static explicit operator Int32DataLocation(DataLocation dataLocation)
        {
            return new Int32DataLocation(dataLocation);
        }

        public static implicit operator DataLocation(Int32DataLocation int32DataLocation)
        {
            return int32DataLocation._dataLocation;
        }

        public int GetValue(byte[] data)
        {
            return _dataLocation.GetInt32(data);
        }

        public int GetValue(byte[] data, int additionalOffset)
        {
            return _dataLocation.GetInt32(data, additionalOffset);
        }

        public void SetValue(byte[] data, int value)
        {
            _dataLocation.SetInt32(data, value);
        }

        public void SetValue(byte[] data, int value, int additionalOffset)
        {
            _dataLocation.SetInt32(data, value, additionalOffset);
        }
    }
}

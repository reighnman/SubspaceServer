﻿using SS.Utilities;
using System;
using System.Runtime.InteropServices;

namespace SS.Packets.Billing
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct B2S_UserChannelChat
    {
        #region Static members

        public static readonly int MinLength;
        public static readonly int MaxLength;
        public static readonly int LengthWithoutText;

        static B2S_UserChannelChat()
        {
            MaxLength = Marshal.SizeOf<B2S_UserChannelChat>();
            LengthWithoutText = MaxLength - TextBytesLength;
            MinLength = LengthWithoutText + 1;
        }

        #endregion

        public byte Type;
        private int connectionId;
        public byte Channel;
        private fixed byte textBytes[TextBytesLength];

        #region Helpers

        public int ConnectionId => LittleEndianConverter.Convert(connectionId);

        private const int TextBytesLength = 250;
        public Span<byte> TextBytes => MemoryMarshal.CreateSpan(ref textBytes[0], TextBytesLength);

        public Span<byte> GetTextBytes(int packetLength) => TextBytes[..Math.Min(packetLength - LengthWithoutText, TextBytesLength)];

        #endregion
    }
}

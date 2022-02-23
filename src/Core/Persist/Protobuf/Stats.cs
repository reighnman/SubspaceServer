// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: Stats.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace SS.Core.Persist.Protobuf {

  /// <summary>Holder for reflection information generated from Stats.proto</summary>
  public static partial class StatsReflection {

    #region Descriptor
    /// <summary>File descriptor for Stats.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static StatsReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CgtTdGF0cy5wcm90bxIYc3MuY29yZS5wZXJzaXN0LnByb3RvYnVmGh5nb29n",
            "bGUvcHJvdG9idWYvZHVyYXRpb24ucHJvdG8aH2dvb2dsZS9wcm90b2J1Zi90",
            "aW1lc3RhbXAucHJvdG8iugEKCFN0YXRJbmZvEhQKCmludDMyVmFsdWUYAiAB",
            "KAVIABIsCgdlbGFwc2VkGAMgASgLMhkuZ29vZ2xlLnByb3RvYnVmLkR1cmF0",
            "aW9uSAASLwoJdGltZXN0YW1wGAQgASgLMhouZ29vZ2xlLnByb3RvYnVmLlRp",
            "bWVzdGFtcEgAEhUKC2RvdWJsZVZhbHVlGAUgASgBSAASFQoLdWludDY0VmFs",
            "dWUYBiABKARIAEILCglzdGF0X2luZm8ipgEKC1BsYXllclN0YXRzEkMKB3N0",
            "YXRNYXAYASADKAsyMi5zcy5jb3JlLnBlcnNpc3QucHJvdG9idWYuUGxheWVy",
            "U3RhdHMuU3RhdE1hcEVudHJ5GlIKDFN0YXRNYXBFbnRyeRILCgNrZXkYASAB",
            "KAUSMQoFdmFsdWUYAiABKAsyIi5zcy5jb3JlLnBlcnNpc3QucHJvdG9idWYu",
            "U3RhdEluZm86AjgBIkEKCkVuZGluZ1RpbWUSMwoPZW5kaW5nVGltZXN0YW1w",
            "GAEgASgLMhouZ29vZ2xlLnByb3RvYnVmLlRpbWVzdGFtcEIbqgIYU1MuQ29y",
            "ZS5QZXJzaXN0LlByb3RvYnVmYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Protobuf.WellKnownTypes.DurationReflection.Descriptor, global::Google.Protobuf.WellKnownTypes.TimestampReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::SS.Core.Persist.Protobuf.StatInfo), global::SS.Core.Persist.Protobuf.StatInfo.Parser, new[]{ "Int32Value", "Elapsed", "Timestamp", "DoubleValue", "Uint64Value" }, new[]{ "StatInfo" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::SS.Core.Persist.Protobuf.PlayerStats), global::SS.Core.Persist.Protobuf.PlayerStats.Parser, new[]{ "StatMap" }, null, null, null, new pbr::GeneratedClrTypeInfo[] { null, }),
            new pbr::GeneratedClrTypeInfo(typeof(global::SS.Core.Persist.Protobuf.EndingTime), global::SS.Core.Persist.Protobuf.EndingTime.Parser, new[]{ "EndingTimestamp" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class StatInfo : pb::IMessage<StatInfo>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<StatInfo> _parser = new pb::MessageParser<StatInfo>(() => new StatInfo());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<StatInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SS.Core.Persist.Protobuf.StatsReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StatInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StatInfo(StatInfo other) : this() {
      switch (other.StatInfoCase) {
        case StatInfoOneofCase.Int32Value:
          Int32Value = other.Int32Value;
          break;
        case StatInfoOneofCase.Elapsed:
          Elapsed = other.Elapsed.Clone();
          break;
        case StatInfoOneofCase.Timestamp:
          Timestamp = other.Timestamp.Clone();
          break;
        case StatInfoOneofCase.DoubleValue:
          DoubleValue = other.DoubleValue;
          break;
        case StatInfoOneofCase.Uint64Value:
          Uint64Value = other.Uint64Value;
          break;
      }

      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StatInfo Clone() {
      return new StatInfo(this);
    }

    /// <summary>Field number for the "int32Value" field.</summary>
    public const int Int32ValueFieldNumber = 2;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int Int32Value {
      get { return statInfoCase_ == StatInfoOneofCase.Int32Value ? (int) statInfo_ : 0; }
      set {
        statInfo_ = value;
        statInfoCase_ = StatInfoOneofCase.Int32Value;
      }
    }

    /// <summary>Field number for the "elapsed" field.</summary>
    public const int ElapsedFieldNumber = 3;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Protobuf.WellKnownTypes.Duration Elapsed {
      get { return statInfoCase_ == StatInfoOneofCase.Elapsed ? (global::Google.Protobuf.WellKnownTypes.Duration) statInfo_ : null; }
      set {
        statInfo_ = value;
        statInfoCase_ = value == null ? StatInfoOneofCase.None : StatInfoOneofCase.Elapsed;
      }
    }

    /// <summary>Field number for the "timestamp" field.</summary>
    public const int TimestampFieldNumber = 4;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Protobuf.WellKnownTypes.Timestamp Timestamp {
      get { return statInfoCase_ == StatInfoOneofCase.Timestamp ? (global::Google.Protobuf.WellKnownTypes.Timestamp) statInfo_ : null; }
      set {
        statInfo_ = value;
        statInfoCase_ = value == null ? StatInfoOneofCase.None : StatInfoOneofCase.Timestamp;
      }
    }

    /// <summary>Field number for the "doubleValue" field.</summary>
    public const int DoubleValueFieldNumber = 5;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public double DoubleValue {
      get { return statInfoCase_ == StatInfoOneofCase.DoubleValue ? (double) statInfo_ : 0D; }
      set {
        statInfo_ = value;
        statInfoCase_ = StatInfoOneofCase.DoubleValue;
      }
    }

    /// <summary>Field number for the "uint64Value" field.</summary>
    public const int Uint64ValueFieldNumber = 6;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ulong Uint64Value {
      get { return statInfoCase_ == StatInfoOneofCase.Uint64Value ? (ulong) statInfo_ : 0UL; }
      set {
        statInfo_ = value;
        statInfoCase_ = StatInfoOneofCase.Uint64Value;
      }
    }

    private object statInfo_;
    /// <summary>Enum of possible cases for the "stat_info" oneof.</summary>
    public enum StatInfoOneofCase {
      None = 0,
      Int32Value = 2,
      Elapsed = 3,
      Timestamp = 4,
      DoubleValue = 5,
      Uint64Value = 6,
    }
    private StatInfoOneofCase statInfoCase_ = StatInfoOneofCase.None;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StatInfoOneofCase StatInfoCase {
      get { return statInfoCase_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void ClearStatInfo() {
      statInfoCase_ = StatInfoOneofCase.None;
      statInfo_ = null;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as StatInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(StatInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Int32Value != other.Int32Value) return false;
      if (!object.Equals(Elapsed, other.Elapsed)) return false;
      if (!object.Equals(Timestamp, other.Timestamp)) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.Equals(DoubleValue, other.DoubleValue)) return false;
      if (Uint64Value != other.Uint64Value) return false;
      if (StatInfoCase != other.StatInfoCase) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (statInfoCase_ == StatInfoOneofCase.Int32Value) hash ^= Int32Value.GetHashCode();
      if (statInfoCase_ == StatInfoOneofCase.Elapsed) hash ^= Elapsed.GetHashCode();
      if (statInfoCase_ == StatInfoOneofCase.Timestamp) hash ^= Timestamp.GetHashCode();
      if (statInfoCase_ == StatInfoOneofCase.DoubleValue) hash ^= pbc::ProtobufEqualityComparers.BitwiseDoubleEqualityComparer.GetHashCode(DoubleValue);
      if (statInfoCase_ == StatInfoOneofCase.Uint64Value) hash ^= Uint64Value.GetHashCode();
      hash ^= (int) statInfoCase_;
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (statInfoCase_ == StatInfoOneofCase.Int32Value) {
        output.WriteRawTag(16);
        output.WriteInt32(Int32Value);
      }
      if (statInfoCase_ == StatInfoOneofCase.Elapsed) {
        output.WriteRawTag(26);
        output.WriteMessage(Elapsed);
      }
      if (statInfoCase_ == StatInfoOneofCase.Timestamp) {
        output.WriteRawTag(34);
        output.WriteMessage(Timestamp);
      }
      if (statInfoCase_ == StatInfoOneofCase.DoubleValue) {
        output.WriteRawTag(41);
        output.WriteDouble(DoubleValue);
      }
      if (statInfoCase_ == StatInfoOneofCase.Uint64Value) {
        output.WriteRawTag(48);
        output.WriteUInt64(Uint64Value);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (statInfoCase_ == StatInfoOneofCase.Int32Value) {
        output.WriteRawTag(16);
        output.WriteInt32(Int32Value);
      }
      if (statInfoCase_ == StatInfoOneofCase.Elapsed) {
        output.WriteRawTag(26);
        output.WriteMessage(Elapsed);
      }
      if (statInfoCase_ == StatInfoOneofCase.Timestamp) {
        output.WriteRawTag(34);
        output.WriteMessage(Timestamp);
      }
      if (statInfoCase_ == StatInfoOneofCase.DoubleValue) {
        output.WriteRawTag(41);
        output.WriteDouble(DoubleValue);
      }
      if (statInfoCase_ == StatInfoOneofCase.Uint64Value) {
        output.WriteRawTag(48);
        output.WriteUInt64(Uint64Value);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (statInfoCase_ == StatInfoOneofCase.Int32Value) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Int32Value);
      }
      if (statInfoCase_ == StatInfoOneofCase.Elapsed) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Elapsed);
      }
      if (statInfoCase_ == StatInfoOneofCase.Timestamp) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Timestamp);
      }
      if (statInfoCase_ == StatInfoOneofCase.DoubleValue) {
        size += 1 + 8;
      }
      if (statInfoCase_ == StatInfoOneofCase.Uint64Value) {
        size += 1 + pb::CodedOutputStream.ComputeUInt64Size(Uint64Value);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(StatInfo other) {
      if (other == null) {
        return;
      }
      switch (other.StatInfoCase) {
        case StatInfoOneofCase.Int32Value:
          Int32Value = other.Int32Value;
          break;
        case StatInfoOneofCase.Elapsed:
          if (Elapsed == null) {
            Elapsed = new global::Google.Protobuf.WellKnownTypes.Duration();
          }
          Elapsed.MergeFrom(other.Elapsed);
          break;
        case StatInfoOneofCase.Timestamp:
          if (Timestamp == null) {
            Timestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
          }
          Timestamp.MergeFrom(other.Timestamp);
          break;
        case StatInfoOneofCase.DoubleValue:
          DoubleValue = other.DoubleValue;
          break;
        case StatInfoOneofCase.Uint64Value:
          Uint64Value = other.Uint64Value;
          break;
      }

      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 16: {
            Int32Value = input.ReadInt32();
            break;
          }
          case 26: {
            global::Google.Protobuf.WellKnownTypes.Duration subBuilder = new global::Google.Protobuf.WellKnownTypes.Duration();
            if (statInfoCase_ == StatInfoOneofCase.Elapsed) {
              subBuilder.MergeFrom(Elapsed);
            }
            input.ReadMessage(subBuilder);
            Elapsed = subBuilder;
            break;
          }
          case 34: {
            global::Google.Protobuf.WellKnownTypes.Timestamp subBuilder = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            if (statInfoCase_ == StatInfoOneofCase.Timestamp) {
              subBuilder.MergeFrom(Timestamp);
            }
            input.ReadMessage(subBuilder);
            Timestamp = subBuilder;
            break;
          }
          case 41: {
            DoubleValue = input.ReadDouble();
            break;
          }
          case 48: {
            Uint64Value = input.ReadUInt64();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 16: {
            Int32Value = input.ReadInt32();
            break;
          }
          case 26: {
            global::Google.Protobuf.WellKnownTypes.Duration subBuilder = new global::Google.Protobuf.WellKnownTypes.Duration();
            if (statInfoCase_ == StatInfoOneofCase.Elapsed) {
              subBuilder.MergeFrom(Elapsed);
            }
            input.ReadMessage(subBuilder);
            Elapsed = subBuilder;
            break;
          }
          case 34: {
            global::Google.Protobuf.WellKnownTypes.Timestamp subBuilder = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            if (statInfoCase_ == StatInfoOneofCase.Timestamp) {
              subBuilder.MergeFrom(Timestamp);
            }
            input.ReadMessage(subBuilder);
            Timestamp = subBuilder;
            break;
          }
          case 41: {
            DoubleValue = input.ReadDouble();
            break;
          }
          case 48: {
            Uint64Value = input.ReadUInt64();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class PlayerStats : pb::IMessage<PlayerStats>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<PlayerStats> _parser = new pb::MessageParser<PlayerStats>(() => new PlayerStats());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<PlayerStats> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SS.Core.Persist.Protobuf.StatsReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PlayerStats() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PlayerStats(PlayerStats other) : this() {
      statMap_ = other.statMap_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public PlayerStats Clone() {
      return new PlayerStats(this);
    }

    /// <summary>Field number for the "statMap" field.</summary>
    public const int StatMapFieldNumber = 1;
    private static readonly pbc::MapField<int, global::SS.Core.Persist.Protobuf.StatInfo>.Codec _map_statMap_codec
        = new pbc::MapField<int, global::SS.Core.Persist.Protobuf.StatInfo>.Codec(pb::FieldCodec.ForInt32(8, 0), pb::FieldCodec.ForMessage(18, global::SS.Core.Persist.Protobuf.StatInfo.Parser), 10);
    private readonly pbc::MapField<int, global::SS.Core.Persist.Protobuf.StatInfo> statMap_ = new pbc::MapField<int, global::SS.Core.Persist.Protobuf.StatInfo>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::MapField<int, global::SS.Core.Persist.Protobuf.StatInfo> StatMap {
      get { return statMap_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as PlayerStats);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(PlayerStats other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!StatMap.Equals(other.StatMap)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= StatMap.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      statMap_.WriteTo(output, _map_statMap_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      statMap_.WriteTo(ref output, _map_statMap_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      size += statMap_.CalculateSize(_map_statMap_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(PlayerStats other) {
      if (other == null) {
        return;
      }
      statMap_.Add(other.statMap_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            statMap_.AddEntriesFrom(input, _map_statMap_codec);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            statMap_.AddEntriesFrom(ref input, _map_statMap_codec);
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class EndingTime : pb::IMessage<EndingTime>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<EndingTime> _parser = new pb::MessageParser<EndingTime>(() => new EndingTime());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<EndingTime> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SS.Core.Persist.Protobuf.StatsReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public EndingTime() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public EndingTime(EndingTime other) : this() {
      endingTimestamp_ = other.endingTimestamp_ != null ? other.endingTimestamp_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public EndingTime Clone() {
      return new EndingTime(this);
    }

    /// <summary>Field number for the "endingTimestamp" field.</summary>
    public const int EndingTimestampFieldNumber = 1;
    private global::Google.Protobuf.WellKnownTypes.Timestamp endingTimestamp_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Protobuf.WellKnownTypes.Timestamp EndingTimestamp {
      get { return endingTimestamp_; }
      set {
        endingTimestamp_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as EndingTime);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(EndingTime other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(EndingTimestamp, other.EndingTimestamp)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (endingTimestamp_ != null) hash ^= EndingTimestamp.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (endingTimestamp_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(EndingTimestamp);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (endingTimestamp_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(EndingTimestamp);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (endingTimestamp_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(EndingTimestamp);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(EndingTime other) {
      if (other == null) {
        return;
      }
      if (other.endingTimestamp_ != null) {
        if (endingTimestamp_ == null) {
          EndingTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
        }
        EndingTimestamp.MergeFrom(other.EndingTimestamp);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (endingTimestamp_ == null) {
              EndingTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(EndingTimestamp);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (endingTimestamp_ == null) {
              EndingTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(EndingTimestamp);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code

// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: StaticFlags.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace SS.Core.Persist.Protobuf {

  /// <summary>Holder for reflection information generated from StaticFlags.proto</summary>
  public static partial class StaticFlagsReflection {

    #region Descriptor
    /// <summary>File descriptor for StaticFlags.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static StaticFlagsReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChFTdGF0aWNGbGFncy5wcm90bxIYc3MuY29yZS5wZXJzaXN0LnByb3RvYnVm",
            "IjoKD1N0YXRpY0ZsYWdzRGF0YRITCgttYXBDaGVja3N1bRgBIAEoDRISCgpv",
            "d25lckZyZXFzGAIgAygFQhuqAhhTUy5Db3JlLlBlcnNpc3QuUHJvdG9idWZi",
            "BnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::SS.Core.Persist.Protobuf.StaticFlagsData), global::SS.Core.Persist.Protobuf.StaticFlagsData.Parser, new[]{ "MapChecksum", "OwnerFreqs" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class StaticFlagsData : pb::IMessage<StaticFlagsData>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<StaticFlagsData> _parser = new pb::MessageParser<StaticFlagsData>(() => new StaticFlagsData());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<StaticFlagsData> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::SS.Core.Persist.Protobuf.StaticFlagsReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StaticFlagsData() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StaticFlagsData(StaticFlagsData other) : this() {
      mapChecksum_ = other.mapChecksum_;
      ownerFreqs_ = other.ownerFreqs_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public StaticFlagsData Clone() {
      return new StaticFlagsData(this);
    }

    /// <summary>Field number for the "mapChecksum" field.</summary>
    public const int MapChecksumFieldNumber = 1;
    private uint mapChecksum_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public uint MapChecksum {
      get { return mapChecksum_; }
      set {
        mapChecksum_ = value;
      }
    }

    /// <summary>Field number for the "ownerFreqs" field.</summary>
    public const int OwnerFreqsFieldNumber = 2;
    private static readonly pb::FieldCodec<int> _repeated_ownerFreqs_codec
        = pb::FieldCodec.ForInt32(18);
    private readonly pbc::RepeatedField<int> ownerFreqs_ = new pbc::RepeatedField<int>();
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::RepeatedField<int> OwnerFreqs {
      get { return ownerFreqs_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as StaticFlagsData);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(StaticFlagsData other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (MapChecksum != other.MapChecksum) return false;
      if(!ownerFreqs_.Equals(other.ownerFreqs_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (MapChecksum != 0) hash ^= MapChecksum.GetHashCode();
      hash ^= ownerFreqs_.GetHashCode();
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
      if (MapChecksum != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(MapChecksum);
      }
      ownerFreqs_.WriteTo(output, _repeated_ownerFreqs_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (MapChecksum != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(MapChecksum);
      }
      ownerFreqs_.WriteTo(ref output, _repeated_ownerFreqs_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (MapChecksum != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(MapChecksum);
      }
      size += ownerFreqs_.CalculateSize(_repeated_ownerFreqs_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(StaticFlagsData other) {
      if (other == null) {
        return;
      }
      if (other.MapChecksum != 0) {
        MapChecksum = other.MapChecksum;
      }
      ownerFreqs_.Add(other.ownerFreqs_);
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
          case 8: {
            MapChecksum = input.ReadUInt32();
            break;
          }
          case 18:
          case 16: {
            ownerFreqs_.AddEntriesFrom(input, _repeated_ownerFreqs_codec);
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
          case 8: {
            MapChecksum = input.ReadUInt32();
            break;
          }
          case 18:
          case 16: {
            ownerFreqs_.AddEntriesFrom(ref input, _repeated_ownerFreqs_codec);
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

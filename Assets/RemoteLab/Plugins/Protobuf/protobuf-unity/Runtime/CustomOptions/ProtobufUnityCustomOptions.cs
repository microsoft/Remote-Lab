// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: protobuf_unity_custom_options.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
/// <summary>Holder for reflection information generated from protobuf_unity_custom_options.proto</summary>
public static partial class ProtobufUnityCustomOptionsReflection {

  #region Descriptor
  /// <summary>File descriptor for protobuf_unity_custom_options.proto</summary>
  public static pbr::FileDescriptor Descriptor {
    get { return descriptor; }
  }
  private static pbr::FileDescriptor descriptor;

  static ProtobufUnityCustomOptionsReflection() {
    byte[] descriptorData = global::System.Convert.FromBase64String(
        string.Concat(
          "CiNwcm90b2J1Zl91bml0eV9jdXN0b21fb3B0aW9ucy5wcm90bxogZ29vZ2xl",
          "L3Byb3RvYnVmL2Rlc2NyaXB0b3IucHJvdG86MAoHcHJpdmF0ZRIdLmdvb2ds",
          "ZS5wcm90b2J1Zi5GaWVsZE9wdGlvbnMYzbEDIAEoCDo6Cg9wcml2YXRlX21l",
          "c3NhZ2USHy5nb29nbGUucHJvdG9idWYuTWVzc2FnZU9wdGlvbnMYzrEDIAEo",
          "CGIGcHJvdG8z"));
    descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
        new pbr::FileDescriptor[] { global::Google.Protobuf.Reflection.DescriptorReflection.Descriptor, },
        new pbr::GeneratedClrTypeInfo(null, new pb::Extension[] { ProtobufUnityCustomOptionsExtensions.Private, ProtobufUnityCustomOptionsExtensions.PrivateMessage }, null));
  }
  #endregion

}
/// <summary>Holder for extension identifiers generated from the top level of protobuf_unity_custom_options.proto</summary>
public static partial class ProtobufUnityCustomOptionsExtensions {
  /// <summary>
  /// Post-process a tighter accessor control to the generated field. The intention is that you will provide your own accessor in your partial class.
  /// - If normal fields, the setter is private while getter still remains public.
  /// - If repeated or map fields, the getter is private.
  /// </summary>
  public static readonly pb::Extension<global::Google.Protobuf.Reflection.FieldOptions, bool> Private =
    new pb::Extension<global::Google.Protobuf.Reflection.FieldOptions, bool>(55501, pb::FieldCodec.ForBool(444008, false));
  /// <summary>
  ///It is like you apply `private` custom options to all fields in the message.
  /// </summary>
  public static readonly pb::Extension<global::Google.Protobuf.Reflection.MessageOptions, bool> PrivateMessage =
    new pb::Extension<global::Google.Protobuf.Reflection.MessageOptions, bool>(55502, pb::FieldCodec.ForBool(444016, false));
}


#endregion Designer generated code
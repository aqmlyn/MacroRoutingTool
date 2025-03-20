using System;
using YamlDotNet.Serialization;

namespace Celeste.Mod.MacroRoutingTool.Data;

public abstract class MRTExport {
    /// <summary>
    /// Parses <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-conformant
    /// text into an object.<br/><br/>
    /// To change the reader's config (see the Deserialization pages on the <seealso href="https://github.com/aaubry/YamlDotNet/wiki">YamlDotNet wiki</seealso>),
    /// <list type="number">
    /// <item><b>Subscribe a method to this class's OnBuildReader.</b> The method will receive a <see cref="DeserializerBuilder"/>
    /// to modify and return the modified one.</item>
    /// <item><b>Call this class's BuildReader.</b> A new builder with MRT's config and OnBuildReader's changes will
    /// build a <see cref="Deserializer"/> assigned to this class's Reader.</item>
    /// </list>
    /// </summary>
    public static Deserializer Reader = null;

    /// <summary>
    /// Called by this class's BuildReader to modify the builder used to build the reader.<br/>
    /// See the Deserialization pages on the <seealso href="https://github.com/aaubry/YamlDotNet/wiki">YamlDotNet wiki</seealso>
    /// for info on what can be modified and how to modify it.
    /// </summary>
    public static Func<DeserializerBuilder, DeserializerBuilder> OnBuildReader = null;

    /// <summary>
    /// (Re)builds this class's Reader. Use to apply changes specified in this class's OnBuildReader.
    /// </summary>
    public static void BuildReader() {
        DeserializerBuilder builder = new();
        builder = OnBuildReader?.Invoke(builder) ?? builder;
        Reader = (Deserializer)builder.Build();
    }

    /// <summary>
    /// Converts an object into <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-conformant
    /// text.<br/><br/>
    /// To change the writer's config (see the Serialization pages on the <seealso href="https://github.com/aaubry/YamlDotNet/wiki">YamlDotNet wiki</seealso>),
    /// <list type="number">
    /// <item><b>Subscribe a method to this class's OnBuildWriter.</b> The method will receive a <see cref="SerializerBuilder"/>
    /// to modify and return the modified one.</item>
    /// <item><b>Call this class's BuildWriter.</b> A new builder with MRT's config and OnBuildWriter's changes will
    /// build a <see cref="Serializer"/> assigned to this class's Writer.</item>
    /// </list>
    /// </summary>
    public static Serializer Writer = null;

    /// <summary>
    /// Called by this class's BuildWriter to modify the builder used to build the writer.<br/>
    /// See the Serialization pages on the <seealso href="https://github.com/aaubry/YamlDotNet/wiki">YamlDotNet wiki</seealso>
    /// for info on what can be modified and how to modify it.
    /// </summary>
    public static Func<SerializerBuilder, SerializerBuilder> OnBuildWriter = null;

    /// <summary>
    /// (Re)builds this class's Writer. Use to apply changes specified in this class's OnBuildWriter.
    /// </summary>
    public static void BuildWriter() {
        SerializerBuilder builder = new();
        builder = OnBuildWriter?.Invoke(builder) ?? builder;
        Writer = (Serializer)builder.Build();
    }
}
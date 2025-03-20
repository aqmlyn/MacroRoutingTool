using System;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
        builder = builder
            .WithTypeConverter(NumericExpressionConverter.Instance)
            .WithTypeConverter(AreaDataConverter.Instance)
            .WithTypeConverter(GuidConverter.Instance);
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
        builder = builder
            .WithTypeConverter(NumericExpressionConverter.Instance)
            .WithTypeConverter(AreaDataConverter.Instance)
            .WithTypeConverter(GuidConverter.Instance);
        builder = OnBuildWriter?.Invoke(builder) ?? builder;
        Writer = (Serializer)builder.Build();
    }
}

/// <summary>
/// Contains methods that use YamlDotNet to read and write <see cref="NumericExpression"/>s to/from YAML-compliant strings.
/// </summary>
public class NumericExpressionConverter : IYamlTypeConverter {
    public static NumericExpressionConverter Instance = new();

    public bool Accepts(Type type) => type == typeof(Logic.NumericExpression);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
        string source = parser.Consume<Scalar>().Value;
        if (string.IsNullOrWhiteSpace(source)) {
            return null;
        }
        Logic.NumericExpression.TryParse(source, out Logic.NumericExpression exp, out _);
        return exp;
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer) {
        if (value == null) {
            emitter.Emit(new Scalar(""));
        } else {
            emitter.Emit(new Scalar(((Logic.NumericExpression)value).Source));
        }
    }
}

/// <summary>
/// Contains methods that use YamlDotNet to read and write <see cref="AreaData"/>s to/from YAML-compliant strings.
/// </summary>
public class AreaDataConverter : IYamlTypeConverter {
    public static AreaDataConverter Instance = new();

    public bool Accepts(Type type) => type == typeof(AreaData);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
        string sid = parser.Consume<Scalar>().Value;
        if (string.IsNullOrWhiteSpace(sid)) {
            return null;
        }
        return AreaData.Areas.FirstOrDefault(area => area.SID == sid, null);
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer) {
        emitter.Emit(new Scalar(((AreaData)value).SID));
    }
}

public class GuidConverter : IYamlTypeConverter {
    public static GuidConverter Instance = new();

    public bool Accepts(Type type) => type == typeof(Guid);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
        long line = parser.Current.Start.Line;
        string guidStr = parser.Consume<Scalar>().Value;
        if (!string.IsNullOrWhiteSpace(guidStr) && Guid.TryParse(guidStr, out Guid guidObj)) {
            return guidObj;
        }
        string errorMsg = string.Format(MRTDialog.ParseGUIDFail, line, UI.GraphViewer.IO.CurrentDisplayPath);
        Logger.Warn("MacroRoutingTool/Parse/YAML", errorMsg);
        return new Guid();
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer) {
        emitter.Emit(new Scalar(((Guid)value).ToString()));
    }
}
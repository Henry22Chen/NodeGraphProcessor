using UnityEngine;
using GraphProcessor;
using System;
using NodeGraphProcessor.Examples;
using System.Collections.Generic;

public class CustomConvertions : ITypeAdapter, ITypeConversion<float, Vector4>, ITypeConversion<Vector4, float>
{
    public static Vector4 ConvertFloatToVector4(float from) => new Vector4(from, from, from, from);
    public static float ConvertVector4ToFloat(Vector4 from) => from.x;

    public override IEnumerable<(Type, Type)> GetIncompatibleTypes()
    {
        yield return (typeof(ConditionalLink), typeof(object));
        yield return (typeof(RelayNode.PackedRelayData), typeof(object));
    }

    Vector4 ITypeConversion<float, Vector4>.ConvertTo(float input)
    {
        return new Vector4(input, input, input, input);
    }

    float ITypeConversion<Vector4, float>.ConvertTo(Vector4 input)
    {
        return input.x;
    }
}
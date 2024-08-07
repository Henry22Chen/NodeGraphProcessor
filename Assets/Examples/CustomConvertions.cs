using UnityEngine;
using GraphProcessor;
using System;
using NodeGraphProcessor.Examples;
using System.Collections.Generic;
using UnityEngine.Windows;

public class CustomConvertions : ITypeAdapter
{
    public static Vector4 ConvertFloatToVector4(float from) => new Vector4(from, from, from, from);
    public static float ConvertVector4ToFloat(Vector4 from) => from.x;

    public override IEnumerable<(Type, Type)> GetIncompatibleTypes()
    {
        yield return (typeof(ConditionalLink), typeof(object));
        yield return (typeof(RelayNode.PackedRelayData), typeof(object));
    }

    public override IEnumerable<(Type, Type, Delegate)> GetConvertionDelegates()
    {
        yield return CreateConversionDelegate<Vector4, float>((input) => input.x);
        yield return CreateConversionDelegate<float, Vector4>((input) => new Vector4(input, input, input, input));
    }
}
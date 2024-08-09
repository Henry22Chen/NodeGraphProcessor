using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraphProcessor
{
	[Serializable]
	public abstract class ExposedParameter : ISerializationCallbackReceiver
	{
        [Serializable]
        public class Settings
        {
            public bool isHidden = false;
            public bool expanded = false;

            [SerializeField]
            internal string guid = null;

            public override bool Equals(object obj)
            {
                if (obj is Settings s && s != null)
                    return Equals(s);
                else
                    return false;
            }

            public virtual bool Equals(Settings param)
                => isHidden == param.isHidden && expanded == param.expanded;

            public override int GetHashCode() => base.GetHashCode();
        }

		public string				guid; // unique id to keep track of the parameter
		public string				name;
		[Obsolete("Use GetValueType()")]
		public string				type;
		[Obsolete("Use value instead")]
		public SerializableObject	serializedValue;
		public bool					input = true;
        [SerializeReference]
		public Settings             settings;
		public string shortType => GetValueType()?.Name;

        public void Initialize(string name, object value)
        {
			guid = Guid.NewGuid().ToString(); // Generated once and unique per parameter
            settings = CreateSettings();
            settings.guid = guid;
			this.name = name;
			this.value = value;
        }

        public abstract bool TryReadValue<T>(out T value);

        public abstract bool SetValueByNode(NodePort port);

        public abstract bool SetValue<T>(T value);
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			// SerializeReference migration step:
#pragma warning disable CS0618
			if (serializedValue?.value != null) // old serialization system can't serialize null values
			{
				value = serializedValue.value;
				Debug.Log("Migrated: " + serializedValue.value + " | " + serializedValue.serializedName);
				serializedValue.value = null;
			}
#pragma warning restore CS0618
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        protected virtual Settings CreateSettings() => new Settings();

        public virtual object value { get; set; }
        public virtual Type GetValueType() => value == null ? typeof(object) : value.GetType();

        static Dictionary<Type, Type> exposedParameterTypeCache = new Dictionary<Type, Type>();
        internal ExposedParameter Migrate()
        {
            if (exposedParameterTypeCache.Count == 0)
            {
                foreach (var type in AppDomain.CurrentDomain.GetAllTypes())
                {
                    if (type.IsSubclassOf(typeof(ExposedParameter)) && !type.IsAbstract)
                    {
                        var paramType = Activator.CreateInstance(type) as ExposedParameter;
                        exposedParameterTypeCache[paramType.GetValueType()] = type;
                    }
                }
            }
#pragma warning disable CS0618 // Use of obsolete fields
            var oldType = Type.GetType(type);
#pragma warning restore CS0618
            if (oldType == null || !exposedParameterTypeCache.TryGetValue(oldType, out var newParamType))
                return null;
            
            var newParam = Activator.CreateInstance(newParamType) as ExposedParameter;

            newParam.guid = guid;
            newParam.name = name;
            newParam.input = input;
            newParam.settings = newParam.CreateSettings();
            newParam.settings.guid = guid;

            return newParam;
     
        }

        public static bool operator ==(ExposedParameter param1, ExposedParameter param2)
        {
            if (ReferenceEquals(param1, null) && ReferenceEquals(param2, null))
                return true;
            if (ReferenceEquals(param1, param2))
                return true;
            if (ReferenceEquals(param1, null))
                return false;
            if (ReferenceEquals(param2, null))
                return false;

            return param1.Equals(param2);
        }

        public static bool operator !=(ExposedParameter param1, ExposedParameter param2) => !(param1 == param2);

        public bool Equals(ExposedParameter parameter) => guid == parameter.guid;

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;
            else
                return Equals((ExposedParameter)obj);
        }

        public override int GetHashCode() => guid.GetHashCode();

        public ExposedParameter Clone()
        {
            var clonedParam = Activator.CreateInstance(GetType()) as ExposedParameter;

            clonedParam.guid = guid;
            clonedParam.name = name;
            clonedParam.input = input;
            clonedParam.settings = settings;
            clonedParam.value = value;

            return clonedParam;
        }
	}

    // Due to polymorphic constraints with [SerializeReference] we need to explicitly create a class for
    // every parameter type available in the graph (i.e. templating doesn't work)
    [System.Serializable]
    public class ColorParameter : TypedExposedParameter<Color>
    {
        public enum ColorMode
        {
            Default,
            HDR
        }

        [Serializable]
        public class ColorSettings : Settings
        {
            public ColorMode mode;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((ColorSettings)param).mode;
        }

        public override object value { get => val; set => val = (Color)value; }
        protected override Settings CreateSettings() => new ColorSettings();
    }

    [System.Serializable]
    public class FloatParameter : TypedExposedParameter<float>
    {
        public enum FloatMode
        {
            Default,
            Slider,
        }

        [Serializable]
        public class FloatSettings : Settings
        {
            public FloatMode mode;
            public float min = 0;
            public float max = 1;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((FloatSettings)param).mode && min == ((FloatSettings)param).min && max == ((FloatSettings)param).max;
        }

        public override object value { get => val; set => val = (float)value; }
        protected override Settings CreateSettings() => new FloatSettings();
    }

    [System.Serializable]
    public class Vector2Parameter : TypedExposedParameter<Vector2>
    {
        public enum Vector2Mode
        {
            Default,
            MinMaxSlider,
        }

        [Serializable]
        public class Vector2Settings : Settings
        {
            public Vector2Mode mode;
            public float min = 0;
            public float max = 1;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((Vector2Settings)param).mode && min == ((Vector2Settings)param).min && max == ((Vector2Settings)param).max;
        }

        public override object value { get => val; set => val = (Vector2)value; }
        protected override Settings CreateSettings() => new Vector2Settings();
    }

    [System.Serializable]
    public class Vector3Parameter : TypedExposedParameter<Vector3>
    {
        public override object value { get => val; set => val = (Vector3)value; }
    }

    [System.Serializable]
    public class Vector4Parameter : TypedExposedParameter<Vector4>
    {
        public override object value { get => val; set => val = (Vector4)value; }
    }

    [System.Serializable]
    public class IntParameter : TypedExposedParameter<int>
    {
        public enum IntMode
        {
            Default,
            Slider,
        }

        [Serializable]
        public class IntSettings : Settings
        {
            public IntMode mode;
            public int min = 0;
            public int max = 10;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((IntSettings)param).mode && min == ((IntSettings)param).min && max == ((IntSettings)param).max;
        }

        public override object value { get => val; set => val = (int)value; }
        protected override Settings CreateSettings() => new IntSettings();
    }

    [System.Serializable]
    public class Vector2IntParameter : TypedExposedParameter<Vector2Int>
    {
        public override object value { get => val; set => val = (Vector2Int)value; }
    }

    [System.Serializable]
    public class Vector3IntParameter : TypedExposedParameter<Vector3Int>
    {
        public override object value { get => val; set => val = (Vector3Int)value; }
    }

    [System.Serializable]
    public class DoubleParameter : TypedExposedParameter<double>
    {
        public override object value { get => val; set => val = (Double)value; }
    }

    [System.Serializable]
    public class LongParameter : TypedExposedParameter<long>
    {
        public override object value { get => val; set => val = (long)value; }
    }

    [System.Serializable]
    public class StringParameter : TypedExposedParameter<string>
    {
        public override object value { get => val; set => val = (string)value; }
    }

    [System.Serializable]
    public class RectParameter : TypedExposedParameter<Rect>
    {
        public override object value { get => val; set => val = (Rect)value; }
    }

    [System.Serializable]
    public class RectIntParameter : TypedExposedParameter<RectInt>
    {
        public override object value { get => val; set => val = (RectInt)value; }
    }

    [System.Serializable]
    public class BoundsParameter : TypedExposedParameter<Bounds>
    {
        public override object value { get => val; set => val = (Bounds)value; }
    }

    [System.Serializable]
    public class BoundsIntParameter : TypedExposedParameter<BoundsInt>
    {
        public override object value { get => val; set => val = (BoundsInt)value; }
    }

    [System.Serializable]
    public class AnimationCurveParameter : TypedExposedParameter<AnimationCurve>
    {       
        public override object value { get => val; set => val = (AnimationCurve)value; }     
    }

    [System.Serializable]
    public class GradientParameter : TypedExposedParameter<Gradient>
    {
        public enum GradientColorMode
        {
            Default,
            HDR,
        }

        [Serializable]
        public class GradientSettings : Settings
        {
            public GradientColorMode mode;

            public override bool Equals(Settings param)
                => base.Equals(param) && mode == ((GradientSettings)param).mode;
        }

        [SerializeField, GradientUsage(true)] Gradient hdrVal;

        public override object value { get => val; set => val = (Gradient)value; }
        protected override Settings CreateSettings() => new GradientSettings();        
    }

    [System.Serializable]
    public class GameObjectParameter : TypedExposedParameter<GameObject>
    {
        public override object value { get => val; set => val = (GameObject)value; }
    }

    [System.Serializable]
    public class BoolParameter : TypedExposedParameter<bool>
    {
        public override object value { get => val; set => val = (bool)value; }
    }

    [System.Serializable]
    public class Texture2DParameter : TypedExposedParameter<Texture2D>
    {
        public override object value { get => val; set => val = (Texture2D)value; }
    }

    [System.Serializable]
    public class RenderTextureParameter : TypedExposedParameter<RenderTexture>
    {
        public override object value { get => val; set => val = (RenderTexture)value; }        
    }

    [System.Serializable]
    public class MeshParameter : TypedExposedParameter<Mesh>
    {
        public override object value { get => val; set => val = (Mesh)value; }
    }

    [System.Serializable]
    public class MaterialParameter : TypedExposedParameter<Material>
    {
        public override object value { get => val; set => val = (Material)value; }        
    }

    public abstract class TypedExposedParameter<T0> : ExposedParameter
    {
        [SerializeField] protected T0 val;
        public override Type GetValueType() => typeof(T0);

        public override bool TryReadValue<T>(out T value)
        {
            return BaseNode.TryConvertValue(ref val, out value);
        }

        public override bool SetValueByNode(NodePort port)
        {
            return port.TryReadInputValue(out val);
        }

        public override bool SetValue<T>(T value)
        {
            return BaseNode.TryConvertValue(ref value, out val);
        }
    }
}
using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Animation.Animators;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Reactive;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a KeyFrame that is used for
    /// <see cref="Animator{T}"/> objects.
    /// </summary>
    public class AnimatorKeyFrame : AvaloniaObject
    {
        public static readonly DirectProperty<AnimatorKeyFrame, object> ValueProperty =
            AvaloniaProperty.RegisterDirect<AnimatorKeyFrame, object>(nameof(Value), k => k.Value, (k, v) => k.Value = v);

        public AnimatorKeyFrame()
        {

        }

        public AnimatorKeyFrame(Type handlerAnimatorType, Cue cue, AnimationTarget target)
        {
            HandlerAnimatorType = handlerAnimatorType;
            Target = target;            
            Cue = cue;
        }

        internal bool isNeutral;
        public Type HandlerAnimatorType { get; }
        public Cue Cue { get; }
        public AnimationTarget Target {get; }

        private object _value;

        public object Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        public IDisposable BindSetter(IAnimationSetter setter, Animatable targetControl)
        {
            var value = setter.Value;

            if (value is IBinding binding)
            {
                return this.Bind(ValueProperty, binding, targetControl);
            }
            else
            {
                return this.Bind(ValueProperty, ObservableEx.SingleValue(value).ToBinding(), targetControl);
            }
        }

        public T GetTypedValue<T>()
        {
            var typeConv = TypeDescriptor.GetConverter(typeof(T));

            if (Value == null)
            {
                throw new ArgumentNullException($"KeyFrame value can't be null.");
            }
            if (Value is T typedValue)
            {
                return typedValue;
            }
            if (!typeConv.CanConvertTo(Value.GetType()))
            {
                throw new InvalidCastException($"KeyFrame value doesnt match property type.");
            }

            return (T)typeConv.ConvertTo(Value, typeof(T));
        }
    }
}

using System.Windows;
using System.Windows.Media.Animation;

namespace BackupUtility.Helpers
{
    public class GridLengthAnim : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty); set => SetValue(FromProperty, value);
        }

        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnim), new PropertyMetadata(new GridLength(0)));

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty); set => SetValue(ToProperty, value);
        }

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnim), new PropertyMetadata(new GridLength(0)));

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            GridLength fromVal = (GridLength)GetValue(FromProperty);
            GridLength toVal = (GridLength)GetValue(ToProperty);

            if (fromVal.GridUnitType != toVal.GridUnitType)
            {
                if (fromVal.GridUnitType == GridUnitType.Star && toVal.GridUnitType == GridUnitType.Pixel)
                {
                    
                }
                else if (fromVal.GridUnitType == GridUnitType.Pixel && toVal.GridUnitType == GridUnitType.Star)
                {
                    
                }
            }

            // Simple linear interpolation for pixel values
            if (fromVal.GridUnitType == GridUnitType.Pixel && toVal.GridUnitType == GridUnitType.Pixel)
            {
                double from = fromVal.Value;
                double to = toVal.Value;
                double progress = animationClock.CurrentProgress.GetValueOrDefault();
                double result = from + ((to - from) * progress);
                return new GridLength(result, GridUnitType.Pixel);
            }
            else if (fromVal.GridUnitType == GridUnitType.Star && toVal.GridUnitType == GridUnitType.Star)
            {
                double from = fromVal.Value;
                double to = toVal.Value;
                double progress = animationClock.CurrentProgress.GetValueOrDefault();
                double result = from + ((to - from) * progress);
                return new GridLength(result, GridUnitType.Star);
            }
            else if (fromVal.GridUnitType == GridUnitType.Auto && toVal.GridUnitType == GridUnitType.Pixel)
            {
                // Treat Auto as 0 for animation purposes (or actual measured size)
                double from = 0; // Or get actual auto size if possible
                if (animationClock.CurrentProgress.GetValueOrDefault() < 1.0)
                {
                    // Animate from 0 up to `to`
                    double progress = animationClock.CurrentProgress.GetValueOrDefault();
                    double result = from + (toVal.Value * progress);
                    return new GridLength(result, GridUnitType.Pixel);
                }
                return toVal; // Once progress is 1.0, just return the "To" value
            }
            else if (fromVal.GridUnitType == GridUnitType.Pixel && toVal.GridUnitType == GridUnitType.Auto)
            {
                // Animate from `from` down to 0 for Auto
                double to = 0; // Or actual auto size
                if (animationClock.CurrentProgress.GetValueOrDefault() < 1.0)
                {
                    double progress = animationClock.CurrentProgress.GetValueOrDefault();
                    double result = fromVal.Value + ((to - fromVal.Value) * progress);
                    return new GridLength(result, GridUnitType.Pixel);
                }
                return toVal; // Once progress is 1.0, just return the "To" value
            }


            // Fallback for incompatible or unhandled types, or if progress is null
            return animationClock.CurrentProgress.HasValue ? toVal : fromVal;
        }


        protected override Freezable CreateInstanceCore() => new GridLengthAnim();
    }
}

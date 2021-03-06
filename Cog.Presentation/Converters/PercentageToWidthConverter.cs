using System;
using System.Globalization;
using System.Windows.Data;

namespace SIL.Cog.Presentation.Converters
{
	public class PercentageToWidthConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is double && values[1] is double)
			{
				var currentValue = (double) values[0];
				var maxValue = Math.Max(0.2, (double) values[1]);
				var maxWidth = int.Parse((string) parameter, CultureInfo.InvariantCulture);

				return maxWidth * (currentValue / maxValue);
			}
			return null;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

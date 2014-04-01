﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Microsoft.CSharp.RuntimeBinder;
using Expression = System.Linq.Expressions.Expression;

namespace QuickConverter
{
	public class DynamicSingleConverter : IValueConverter
	{
		private static Dictionary<Type, Func<object, object>> castFunctions = new Dictionary<Type, Func<object, object>>();

		public string ConvertExpression { get; private set; }
		public string ConvertBackExpression { get; private set; }
		public Exception LastException { get; private set; }

		public Type PType { get; private set; }

		public object[] ToValues { get { return _toValues; } }
		public object[] FromValues { get { return _fromValues; } }

		private Func<object, object[], object> _converter;
		private Func<object, object[], object> _convertBack;
		private object[] _toValues;
		private object[] _fromValues;
		public DynamicSingleConverter(Func<object, object[], object> converter, Func<object, object[], object> convertBack, object[] toValues, object[] fromValues, string convertExp, string convertBackExp, Type pType)
		{
			_converter = converter;
			_convertBack = convertBack;
			_toValues = toValues;
			_fromValues = fromValues;
			ConvertExpression = convertExp;
			ConvertBackExpression = convertBackExp;
			PType = pType;
		}

		private object DoConversion(object value, Type targetType, Func<object, object[], object> func, object[] values, bool enforceType)
		{
			if (enforceType && PType != null)
			{
				if (value != null)
				{
					if (!PType.IsInstanceOfType(value))
						return DependencyProperty.UnsetValue;
				}
				else if (PType.IsValueType)
					return DependencyProperty.UnsetValue;
			}

			object result = value;
			if (func != null)
			{
				try { result = func(result, values); }
				catch (Exception e)
				{
					LastException = e;
					return null; 
				}
			}

			if (result == null)
				return null;

			if (targetType == typeof(string))
				return result.ToString();

			Func<object, object> cast;
			if (!castFunctions.TryGetValue(targetType, out cast))
			{
				ParameterExpression par = Expression.Parameter(typeof(object));
				cast = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.Dynamic(Binder.Convert(CSharpBinderFlags.ConvertExplicit, targetType, typeof(object)), targetType, par), typeof(object)), par).Compile();
				castFunctions.Add(targetType, cast);
			}

			result = cast(result);
			return result;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return DoConversion(value, targetType, _converter, _toValues, true);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return DoConversion(value, targetType, _convertBack, _fromValues, false);
		}
	}
}

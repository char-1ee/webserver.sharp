using System;
using System.Data;
using System.ComponentModel;
using System.Reflection;

namespace ValueConverter
{
	public class ConverterException : ApplicationException
	{
		public ConverterException(string msg)
			: base(msg)
		{
		}
	}

	public class Converter
	{
		public static object Convert(object src, Type destType)
		{
			object ret = src;

			if ((src != null) && (src != DBNull.Value))
			{
				Type srcType = src.GetType();

				if ((srcType.FullName == "System.Object") || (destType.FullName == "System.Object"))
				{
					ret = src;
				}
				else
				{
					if (srcType != destType)
					{
						TypeConverter tcSrc = TypeDescriptor.GetConverter(srcType);
						TypeConverter tcDest = TypeDescriptor.GetConverter(destType);

						if (tcSrc.CanConvertTo(destType))
						{
							ret = tcSrc.ConvertTo(src, destType);
						}
						else if (tcDest.CanConvertFrom(srcType))
						{
							if (srcType.FullName == "System.String")
							{
								ret = tcDest.ConvertFromInvariantString((string)src);
							}
							else
							{
								ret = tcDest.ConvertFrom(src);
							}
						}
						else
						{
							// If the target type is a base class of the source type, then we don't need to do any conversion.
							if (destType.IsAssignableFrom(srcType))
							{
								ret = src;
							}
							else
							{
								// If no conversion exists, throw an exception.
								throw new ConverterException("Can't convert from " + src.GetType().FullName + " to " + destType.FullName);
							}
						}
					}
				}
			}
			else if (src == DBNull.Value)
			{
				if (destType.FullName == "System.String")
				{
					// convert DBNull.Value to null for strings.
					ret = null;
				}
			}

			return ret;
		}
	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Omu.ValueInjecter;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Platini.Models
{
    public static class Extensions
    {

        #region EnumExt
        public static string Description(this Enum value)
        {
            if (value.IsDefined())
            {
                DescriptionAttribute[] da = (DescriptionAttribute[])(value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
                if (da.Length > 0)
                    return da[0].Description;
            }
            return value.ToString();
        }
        public static bool IsDefined(this Enum value)
        {
            return Enum.IsDefined(value.GetType(), value);
        }

        #endregion
        public static bool IsNumeric(this string str)
        {
            return Regex.IsMatch(str, "^\\d*\\.*\\d+$", RegexOptions.Compiled);
        }
        public static TEnum ToEnum<TEnum>(this string str)
        {
            return EnumExt.Parse<TEnum>(str);
        }
        /// <summary>
        /// Try to convert the value to enum if fails return default value;
        /// </summary>
        /// <typeparam name="TEnumType"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static TEnum TryToEnum<TEnum>(this string str)
        {
            return str.TryToEnum<TEnum>(default(TEnum));
        }
        /// <summary>
        /// Try to convert the value to enum if fails return default value;
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TEnum TryToEnum<TEnum>(this string str, TEnum defaultValue)
        {
            if (str.IsEnum<TEnum>())
            {
                return str.ToEnum<TEnum>();
            }
            return defaultValue;
        }
        public static bool IsEnum<TEnum>(this string str)
        {
            return EnumExt.IsDefined<TEnum>(str, true);
        }
        public static int ToInt(this string str)
        {
            return str.ConvertTo<int>();
        }
        public static long ToLong(this string str)
        {
            return str.ConvertTo<long>();
        }
        public static bool ToBool(this string str)
        {
            return str.ConvertTo<bool>();
        }
        public static T ConvertTo<T>(this string sValue)
        {
            Type t = typeof(T);
            object value;
            if (!sValue.HasValue())
            {
                if (t == typeof(string))
                {
                    value = string.Empty;
                    return (T)value;
                }
                return default(T);
            }
            if (t == typeof(string))
            {
                value = sValue;
            }
            else if (t == typeof(int))
            {
                value = int.Parse(sValue);
            }
            else if (t == typeof(long))
            {
                value = long.Parse(sValue);
            }
            else if (t == typeof(float))
            {
                value = float.Parse(sValue);
            }
            else if (t == typeof(bool))
            {
                value = bool.Parse(sValue);
            }
            else if (t == typeof(DateTime))
            {
                value = DateTime.ParseExact(sValue, "s", null);
            }
            else
            {
                throw new NotSupportedException("Data Type '" + typeof(T) + "' not supported");
            }
            return (T)value;
        }
        public static bool TryConvertTo<T>(this string sValue, out T val)
        {
            if (!sValue.HasValue())
            {
                if (typeof(T) == typeof(string))
                {
                    val = (T)((object)String.Empty);
                }
                else
                {
                    val = default(T);
                }
                return false;
            }
            bool rtnValue = false;
            object outValue = false;
            if (typeof(T) == typeof(string))
            {
                outValue = sValue;
            }
            else if (typeof(T) == typeof(int))
            {
                int value;
                rtnValue = int.TryParse(sValue, out value);
                outValue = value;
            }
            else if (typeof(T) == typeof(long))
            {
                long value;
                rtnValue = long.TryParse(sValue, out value);
                outValue = value;
            }
            else if (typeof(T) == typeof(float))
            {
                float value;
                rtnValue = float.TryParse(sValue, out value);
                outValue = value;
            }
            else if (typeof(T) == typeof(bool))
            {
                bool value;
                rtnValue = bool.TryParse(sValue, out value);
                outValue = value;
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime t;
                rtnValue = DateTime.TryParseExact(sValue, "s", null, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out t);
                outValue = t;
            }
            else
            {
                throw new NotSupportedException("Data Type '" + typeof(T) + "' not supported.(value='" + sValue + "') ");
            }
            val = (T)outValue;
            return rtnValue;
        }
        public static bool HasValue(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static ICollection<TTo> InjectFrom<TFrom, TTo>(this ICollection<TTo> to, IEnumerable<TFrom> from) where TTo : new()
        {
            foreach (var source in from)
            {
                var target = new TTo();
                target.InjectFrom<IgnoreCaseInjection>(source);
                to.Add(target);
            }
            return to;
        }
        public static ICollection<TTo> InjectFrom<TFrom, TTo>(this ICollection<TTo> to, params IEnumerable<TFrom>[] sources) where TTo : new()
        {
            foreach (var from in sources)
            {
                foreach (var source in from)
                {
                    var target = new TTo();
                    target.InjectFrom<IgnoreCaseInjection>(source);
                    to.Add(target);
                }
            }
            return to;
        }

        public static List<TTo> InjectFrom<TFrom, TTo>(this List<TTo> to, IEnumerable<TFrom> from) where TTo : new()
        {
            foreach (var source in from)
            {
                var target = new TTo();
                target.InjectFrom<IgnoreCaseInjection>(source);
                to.Add(target);
            }
            return to;
        }

        public static object InjectClass(this object target, object source)
        {
            target.InjectFrom<IgnoreCaseInjection>(source);
            return target;
        }
    }

    public class IgnoreCaseInjection : ConventionInjection
    {
        protected override bool Match(ConventionInfo c)
        {
            return String.Compare(c.SourceProp.Name, c.TargetProp.Name, StringComparison.OrdinalIgnoreCase) == 0 && c.SourceProp.Type == c.TargetProp.Type;
        }
    }

    public static class EnumExt
    {
        /// <summary>
        /// Checks Value is defined in enum or not, 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <returns>return false if value is null or empty</returns>
        public static bool IsDefined<T>(string value, bool ignoreCase)
        {
            return IsDefined(typeof(T), value, ignoreCase);
        }
        /// <summary>
        /// Checks Value is defined in enum or not, 
        /// </summary>
        /// <param name="enumType"> </param>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <returns>return false if value is null or empty</returns>
        public static bool IsDefined(Type enumType, string value, bool ignoreCase)
        {
            if (!enumType.IsEnum)
            {
                throw new Exception(string.Format("{0} is not a valid Enum.", enumType));
            }
            if (!value.HasValue())
            {
                return false;
            }
            if (value.IsNumeric())
            {
                return Enum.IsDefined(enumType, int.Parse(value));
            }
            return Enum.GetNames(enumType).Any(f => (ignoreCase ? f.Equals(value, StringComparison.CurrentCultureIgnoreCase) : f.Equals(value)));
        }
        public static T Parse<T>(string value, bool ignoreCase = true)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }
    }

    public static class EnumFormType
    {
        public static T GetValueFromDescription<T>(string description)
        {

            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            return default(T);
        }
    }

    public class GuidHelper
    {
        public static string NameUUIDFromBytes(byte[] input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(input);
            hash[6] &= 0x0f;
            hash[6] |= 0x30;
            hash[8] &= 0x3f;
            hash[8] |= 0x80;
            string hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            return hex.Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-");
        }

        public static Guid NameGuidFromBytes(byte[] input)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(input);
            hash[6] &= 0x0f;
            hash[6] |= 0x30;
            hash[8] &= 0x3f;
            hash[8] |= 0x80;

            byte temp = hash[6];
            hash[6] = hash[7];
            hash[7] = temp;

            temp = hash[4];
            hash[4] = hash[5];
            hash[5] = temp;

            temp = hash[0];
            hash[0] = hash[3];
            hash[3] = temp;

            temp = hash[1];
            hash[1] = hash[2];
            hash[2] = temp;
            return new Guid(hash);
        }
    }

    public class ImgHelper
    {
        public static byte[] ResizeImage(string ImagePath, int Width, int Height, string BackColor)
        {
            byte[] byteArray = new byte[0];
            using (System.Drawing.Image OriginalImage = System.Drawing.Image.FromFile(ImagePath))
            {
                int oldWidth = OriginalImage.Width;
                int oldHeight = OriginalImage.Height;

                if (Width == 0 && Height > 0)
                {
                    Width = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (Width > 0 && Height == 0)
                {
                    Height = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (Width == 0 && Height == 0)
                {
                    Width = oldWidth;
                    Height = oldHeight;
                }
                else
                {
                    //do nothing
                }


                int newWidth = Width;
                int newHeight = Height;

                if (oldWidth == Width && oldHeight == Height)
                {
                    newWidth = Width;
                    newHeight = Height;
                }
                else if (oldWidth > Width && oldHeight > Height)
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;

                }
                else if (oldWidth > Width && oldHeight <= Height)
                {
                    newWidth = Width;
                    newHeight = Convert.ToInt32(Convert.ToDecimal(newWidth) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (oldWidth <= Width && oldHeight > Height)
                {
                    newHeight = Height;
                    newWidth = Convert.ToInt32(Convert.ToDecimal(newHeight) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (oldWidth < Width && oldHeight < Height)
                {
                    newWidth = oldWidth;
                    newHeight = oldHeight;
                }
                else
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;
                }

                if (newWidth == 0)
                    newWidth = 1;
                if (newHeight == 0)
                    newHeight = 1;

                int newX = 0;
                int newY = 0;

                if (newWidth <= Width)
                {
                    newX = Convert.ToInt32((Width - newWidth) / 2);
                }
                if (newHeight <= Height)
                {
                    newY = Convert.ToInt32((Height - newHeight) / 2);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    PixelFormat pixelFormat = OriginalImage.PixelFormat;
                    if (pixelFormat.ToString().Contains("Indexed"))
                        pixelFormat = PixelFormat.Format32bppArgb;

                    using (Bitmap NewImage = new Bitmap(Width, Height, pixelFormat))
                    {
                        NewImage.SetResolution(72, 72);
                        using (Graphics newGraphics = Graphics.FromImage(NewImage))
                        {
                            if (!string.IsNullOrEmpty(BackColor))
                                newGraphics.Clear(ColorTranslator.FromHtml(BackColor));
                            else
                                newGraphics.Clear(Color.Transparent);
                            //newGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                            newGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            newGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            newGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            newGraphics.CompositingQuality = CompositingQuality.HighQuality;
                            newGraphics.DrawImage(OriginalImage, newX, newY, newWidth, newHeight);
                            NewImage.Save(stream, ImageFormat.Jpeg);
                            //NewImage.Save(ImagePath.Replace(".jpeg", "-" + DateTime.Now.Ticks + ".jpg"), ImageFormat.Jpeg);
                            byteArray = stream.ToArray();
                        }
                    }
                }
            }
            return byteArray;
        }


        public static byte[] ResizeImage(string ImagePath, int Width, int Height, string BackColor, int quality, ImageFormat format)
        {
            byte[] byteArray = new byte[0];
            using (System.Drawing.Image OriginalImage = System.Drawing.Image.FromFile(ImagePath))
            {
                int oldWidth = OriginalImage.Width;
                int oldHeight = OriginalImage.Height;

                if (Width == 0 && Height > 0)
                {
                    Width = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (Width > 0 && Height == 0)
                {
                    Height = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (Width == 0 && Height == 0)
                {
                    Width = oldWidth;
                    Height = oldHeight;
                }
                else
                {
                    //do nothing
                }


                int newWidth = Width;
                int newHeight = Height;

                if (oldWidth == Width && oldHeight == Height)
                {
                    newWidth = Width;
                    newHeight = Height;
                }
                else if (oldWidth > Width && oldHeight > Height)
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;

                }
                else if (oldWidth > Width && oldHeight <= Height)
                {
                    newWidth = Width;
                    newHeight = Convert.ToInt32(Convert.ToDecimal(newWidth) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (oldWidth <= Width && oldHeight > Height)
                {
                    newHeight = Height;
                    newWidth = Convert.ToInt32(Convert.ToDecimal(newHeight) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (oldWidth < Width && oldHeight < Height)
                {
                    newWidth = oldWidth;
                    newHeight = oldHeight;
                }
                else
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;
                }

                if (newWidth == 0)
                    newWidth = 1;
                if (newHeight == 0)
                    newHeight = 1;

                int newX = 0;
                int newY = 0;

                if (newWidth <= Width)
                {
                    newX = Convert.ToInt32((Width - newWidth) / 2);
                }
                if (newHeight <= Height)
                {
                    newY = Convert.ToInt32((Height - newHeight) / 2);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    PixelFormat pixelFormat = OriginalImage.PixelFormat;
                    if (pixelFormat.ToString().Contains("Indexed"))
                        pixelFormat = PixelFormat.Format32bppArgb;

                    using (Bitmap NewImage = new Bitmap(Width, Height, pixelFormat))
                    {
                        NewImage.SetResolution(72, 72);
                        using (Graphics newGraphics = Graphics.FromImage(NewImage))
                        {
                            if (!string.IsNullOrEmpty(BackColor))
                                newGraphics.Clear(ColorTranslator.FromHtml(BackColor));
                            else
                                newGraphics.Clear(Color.Transparent);
                            //newGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                            newGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            newGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            newGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            newGraphics.CompositingQuality = CompositingQuality.HighQuality;
                            newGraphics.DrawImage(OriginalImage, newX, newY, newWidth, newHeight);
                            ImageCodecInfo jgpEncoder = GetEncoder(format);
                            if (jgpEncoder != null)
                            {
                                var encoder = System.Drawing.Imaging.Encoder.Quality;
                                var encodeParams = new EncoderParameters(1);
                                var encodeParam = new EncoderParameter(encoder, quality);
                                encodeParams.Param[0] = encodeParam;
                                NewImage.Save(stream, jgpEncoder, encodeParams);
                            }
                            else
                                NewImage.Save(stream, ImageFormat.Jpeg);

                            //NewImage.Save(ImagePath.Replace(".jpeg", "-" + DateTime.Now.Ticks + ".jpg"), ImageFormat.Jpeg);
                            byteArray = stream.ToArray();
                        }
                    }
                }
            }
            return byteArray;
        }

        public static byte[] ResizeImage(Stream ImagePath, int Width, int Height, string BackColor, int quality, ImageFormat format)
        {
            byte[] byteArray = new byte[0];
            using (var OriginalImage = new Bitmap(ImagePath, true))
            {
                int oldWidth = OriginalImage.Width;
                int oldHeight = OriginalImage.Height;

                if (Width == 0 && Height > 0)
                {
                    Width = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (Width > 0 && Height == 0)
                {
                    Height = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (Width == 0 && Height == 0)
                {
                    Width = oldWidth;
                    Height = oldHeight;
                }
                else
                {
                    //do nothing
                }


                int newWidth = Width;
                int newHeight = Height;

                if (oldWidth == Width && oldHeight == Height)
                {
                    newWidth = Width;
                    newHeight = Height;
                }
                else if (oldWidth > Width && oldHeight > Height)
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;

                }
                else if (oldWidth > Width && oldHeight <= Height)
                {
                    newWidth = Width;
                    newHeight = Convert.ToInt32(Convert.ToDecimal(newWidth) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));
                }
                else if (oldWidth <= Width && oldHeight > Height)
                {
                    newHeight = Height;
                    newWidth = Convert.ToInt32(Convert.ToDecimal(newHeight) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                }
                else if (oldWidth < Width && oldHeight < Height)
                {
                    newWidth = oldWidth;
                    newHeight = oldHeight;
                }
                else
                {
                    int xWidth = Convert.ToInt32(Convert.ToDecimal(Height) / Convert.ToDecimal(oldHeight) * Convert.ToDecimal(oldWidth));
                    int xHeight = Convert.ToInt32(Convert.ToDecimal(Width) / Convert.ToDecimal(oldWidth) * Convert.ToDecimal(oldHeight));

                    if (xHeight < Height && xHeight > xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth > xHeight)
                        newWidth = xWidth;
                    else if (xHeight < Height && xHeight < xWidth)
                        newHeight = xHeight;
                    else if (xWidth < Width && xWidth < xHeight)
                        newWidth = xWidth;
                    else
                        newHeight = xHeight;
                }

                if (newWidth == 0)
                    newWidth = 1;
                if (newHeight == 0)
                    newHeight = 1;

                int newX = 0;
                int newY = 0;

                if (newWidth <= Width)
                {
                    newX = Convert.ToInt32((Width - newWidth) / 2);
                }
                if (newHeight <= Height)
                {
                    newY = Convert.ToInt32((Height - newHeight) / 2);
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    PixelFormat pixelFormat = OriginalImage.PixelFormat;
                    if (pixelFormat.ToString().Contains("Indexed"))
                        pixelFormat = PixelFormat.Format32bppArgb;

                    using (Bitmap NewImage = new Bitmap(Width, Height, pixelFormat))
                    {
                        NewImage.SetResolution(OriginalImage.HorizontalResolution, OriginalImage.VerticalResolution);
                        using (Graphics newGraphics = Graphics.FromImage(NewImage))
                        {
                            if (!string.IsNullOrEmpty(BackColor))
                                newGraphics.Clear(ColorTranslator.FromHtml(BackColor));
                            else
                                newGraphics.Clear(Color.Transparent);
                            //newGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                            newGraphics.SmoothingMode = SmoothingMode.HighQuality;
                            newGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            newGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            newGraphics.CompositingQuality = CompositingQuality.HighQuality;
                            newGraphics.DrawImage(OriginalImage, newX, newY, newWidth, newHeight);
                            ImageCodecInfo jgpEncoder = GetEncoder(format);
                            if (jgpEncoder != null)
                            {
                                var encoder = System.Drawing.Imaging.Encoder.Quality;
                                var encodeParams = new EncoderParameters(1);
                                var encodeParam = new EncoderParameter(encoder, quality);
                                encodeParams.Param[0] = encodeParam;
                                NewImage.Save(stream, jgpEncoder, encodeParams);
                            }
                            else
                                NewImage.Save(stream, ImageFormat.Jpeg);

                            //NewImage.Save(ImagePath.Replace(".jpeg", "-" + DateTime.Now.Ticks + ".jpg"), ImageFormat.Jpeg);
                            byteArray = stream.ToArray();
                        }
                    }
                }
            }
            return byteArray;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
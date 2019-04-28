﻿namespace Tangle.Net.Area.Codes.Entity
{
  using System;
  using System.Linq;
  using System.Text.RegularExpressions;

  using Google.OpenLocationCode;

  using Tangle.Net.Area.Codes.Services;
  using Tangle.Net.Entity;

  public class IotaAreaCode : TryteString
  {
    private IotaCodeArea area;

    public IotaAreaCode(string trytes)
      : base(trytes)
    {
      if (!IsValid(trytes) && !IsValidPartial(trytes))
      {
        throw new Exception("Given value is not a valid area or partial area code!");
      }
    }

    public IotaCodeArea Area => this.area ?? (this.area = this.Decode());

    public int CodePrecision => this.Value.Replace("9", string.Empty).Replace("A", string.Empty).Length;

    public static IotaAreaCode Encode(double latitude, double longitude, int precision = OpenLocationCode.CodePrecisionNormal)
    {
      if (!IotaAreaCodeDimension.Precision.Contains(precision))
      {
        throw new ArgumentException($"Invalid precision. Allowed values are {string.Join(", ", IotaAreaCodeDimension.Precision)}");
      }

      return FromOpenLocationCode(OpenLocationCode.Encode(latitude, longitude, precision));
    }

    public static IotaAreaCode FromOpenLocationCode(string openLocationCode)
    {
      if (!OpenLocationCode.IsValid(openLocationCode))
      {
        throw new Exception($"Invalid Open Location Code {openLocationCode}");
      }

      var iotaAreaCodeValue = string.Empty;

      foreach (var character in openLocationCode)
      {
        var index = Alphabet.OpenLocationCode.IndexOf(character);
        iotaAreaCodeValue += Alphabet.IotaAreaCode[index];
      }

      return new IotaAreaCode(iotaAreaCodeValue);
    }

    public static bool IsValid(string iotaAreaCode)
    {
      if (!Regex.IsMatch(iotaAreaCode, $"^[${Alphabet.IotaAreaCode}]*$"))
      {
        return false;
      }

      return OpenLocationCode.IsValid(ToOpenLocationCode(iotaAreaCode));
    }

    public static bool IsValidPartial(string iotaAreaCode)
    {
      if (iotaAreaCode.Length > 9 || !iotaAreaCode.EndsWith("AA9"))
      {
        return false;
      }

      var remaining = Regex.Replace(iotaAreaCode, "A*9$", string.Empty);
      if (remaining.Length < 2 || remaining.Length % 2 == 1)
      {
        return false;
      }

      return Regex.IsMatch(remaining, $"^[${Alphabet.IotaAreaCode.Substring(0, 20)}]*$");
    }

    public static IotaAreaCode Extract(string trytes)
    {
      var regex = "([" + Alphabet.IotaAreaCode.Substring(0, 9) + "][" + Alphabet.IotaAreaCode.Substring(0, 18) + "]["
                  + Alphabet.IotaAreaCode.Substring(0, 21) + "]{6}9(?:[" + Alphabet.IotaAreaCode.Substring(0, 20) + "]{2,3})?)";

      var match = Regex.Match(trytes, regex);
      if (!match.Success)
      {
        throw new ArgumentException("Given trytes are not a valid Iota Area Code!");
      }

      return new IotaAreaCode(match.Value);
    }

    public static string ToOpenLocationCode(string iotaAreaCode)
    {
      var openLocationCode = string.Empty;

      foreach (var character in iotaAreaCode)
      {
        var index = Alphabet.IotaAreaCode.IndexOf(character);
        openLocationCode += Alphabet.OpenLocationCode[index];
      }

      return openLocationCode;
    }

    public IotaCodeArea Decode()
    {
      var areaCode = OpenLocationCode.Decode(ToOpenLocationCode(this.Value));
      return new IotaCodeArea
               {
                 CodePrecision = this.CodePrecision,
                 Latitude = areaCode.CenterLatitude,
                 Longitude = areaCode.CenterLongitude,
                 LatitudeHigh = areaCode.Max.Latitude,
                 LongitudeHigh = areaCode.Max.Longitude,
                 LatitudeLow = areaCode.Min.Latitude,
                 LongitudeLow = areaCode.Min.Longitude
               };
    }

    public IotaAreaCode DecreasePrecision()
    {
      if (this.CodePrecision <= IotaAreaCodeDimension.Precision.First())
      {
        throw new Exception($"Precision can not be decreased further than {this.CodePrecision}");
      }

      this.Value = Precision.Calculate(this, IotaAreaCodeDimension.Precision[Array.IndexOf(IotaAreaCodeDimension.Precision, this.CodePrecision) - 1]);
      this.area = null;

      return this;
    }

    public IotaAreaCode SetPrecision(int precision)
    {
      if (!IotaAreaCodeDimension.Precision.Contains(precision))
      {
        throw new ArgumentException($"Invalid precision. Allowed values are {string.Join(", ", IotaAreaCodeDimension.Precision)}");
      }

      this.Value = Precision.Calculate(this, precision);
      this.area = null;

      return this;
    }

    public IotaAreaCode IncreasePrecision()
    {
      if (this.CodePrecision >= IotaAreaCodeDimension.Precision.Last())
      {
        throw new Exception($"Precision can not be increased further than {this.CodePrecision}");
      }

      this.Value = Precision.Calculate(this, IotaAreaCodeDimension.Precision[Array.IndexOf(IotaAreaCodeDimension.Precision, this.CodePrecision) + 1]);
      this.area = null;

      return this;
    }

    public string ToOpenLocationCode()
    {
      return ToOpenLocationCode(this.Value);
    }
  }
}
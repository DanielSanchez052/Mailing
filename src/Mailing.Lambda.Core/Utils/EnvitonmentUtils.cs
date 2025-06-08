using System;

namespace Mailing.Lambda.Core.Utils;

public class EnvitonmentUtils
{
  public static string GetEnvironmentVariable(string name, string defaultValue = "")
  {
    try
    {
      var value = System.Environment.GetEnvironmentVariable(name);
      return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
    catch
    {
      return defaultValue;
    }
  }
}

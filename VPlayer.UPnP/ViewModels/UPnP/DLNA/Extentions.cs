using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Management;
using System.Security.Cryptography;
using System.Diagnostics;

public static class Extentions
{

  public static string ChopOffBefore(this string s, string Before)
  {//Usefull function for chopping up strings
    int End = s.ToUpper().IndexOf(Before.ToUpper());
    if (End > -1)
    {
      return s.Substring(End + Before.Length);
    }
    return s;
  }



  public static string ChopOffAfter(this string s, string After)
  {//Usefull function for chopping up strings
    int End = s.ToUpper().IndexOf(After.ToUpper());
    if (End > -1)
    {
      return s.Substring(0, End);
    }
    return s;
  }

  public static string ReplaceIgnoreCase(this string Source, string Pattern, string Replacement)
  {// using \\$ in the pattern will screw this regex up
   //return Regex.Replace(Source, Pattern, Replacement, RegexOptions.IgnoreCase);

    if (Regex.IsMatch(Source, Pattern, RegexOptions.IgnoreCase))
      Source = Regex.Replace(Source, Pattern, Replacement, RegexOptions.IgnoreCase);
    return Source;
  }

}



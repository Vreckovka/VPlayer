using System;

namespace VPlayer.Player.UserControls
{
  public class LightingSystem
  {
    private static readonly double[] LightMountX = { 0.15, 0.30, 0.70, 0.85 };
    private static readonly double[] FanPosition = { -1.5, -0.5, 0.5, 1.5 };

    public struct LightPose
    {
      public double Angle;
      public double Depth;

      public LightPose(double angle, double depth)
      {
        Angle = angle;
        Depth = depth;
      }
    }

    public static LightPose GetLightPose(int look, int lightIndex, double p, double aspect)
    {
      switch (look)
      {
        case 0:
          return GetFanPose(lightIndex, p);

        case 1:
          return GetCrossPose(lightIndex, p);

        case 2:
          return GetConvergencePose(lightIndex, p, aspect);

        default:
          return GetOrbitPose(lightIndex, p);
      }
    }

    private static LightPose GetFanPose(int lightIndex, double p)
    {
      double commonPan = Math.Sin(p * 0.72) * 16.0 + Math.Sin(p * 0.19 + 0.7) * 5.0;
      double spread = 6.0 + (0.5 + Math.Sin(p * 0.33) * 0.5) * 10.0;
      double angle = commonPan + FanPosition[lightIndex] * spread;
      double depth = 0.5 + Math.Sin(p * 0.47 + lightIndex * 0.35) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetCrossPose(int lightIndex, double p)
    {
      double sweep = Math.Sin(p * 0.88) * 34.0;
      double drift = Math.Sin(p * 0.41 + 1.2) * 6.0;
      double angle;

      switch (lightIndex)
      {
        case 0:
          angle = sweep + drift;
          break;

        case 1:
          angle = -sweep * 0.65 + drift;
          break;

        case 2:
          angle = sweep * 0.65 + drift;
          break;

        default:
          angle = -sweep + drift;
          break;
      }

      double depth = 0.5 + Math.Cos(p * 0.72 + (lightIndex % 2) * Math.PI) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetConvergencePose(int lightIndex, double p, double aspect)
    {
      double targetX = 0.50 + Math.Sin(p * 0.43) * 0.23 + Math.Sin(p * 0.17 + 1.1) * 0.06;
      double targetY = 0.57 + Math.Cos(p * 0.31) * 0.14 + Math.Sin(p * 0.73) * 0.03;

      targetX = Clamp(targetX, 0.20, 0.80);
      targetY = Clamp(targetY, 0.32, 0.86);

      double angle = GetMountedBeamAngle(LightMountX[lightIndex], targetX, targetY, aspect);
      double depth = 0.62 + Math.Sin(p * 0.52 + lightIndex * 0.18) * 0.12;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    private static LightPose GetOrbitPose(int lightIndex, double p)
    {
      double phase = p * 0.92 + lightIndex * 0.78;
      double commonDrift = Math.Sin(p * 0.27) * 9.0;
      double angle = commonDrift + Math.Sin(phase) * 29.0;
      double depth = 0.5 + Math.Cos(phase) * 0.5;

      return new LightPose(Clamp(angle, -55.0, 55.0), Clamp01(depth));
    }

    public static double GetLookIntensity(int look, int lightIndex, double p)
    {
      switch (look)
      {
        case 0:
          {
            double breathing = 0.5 + Math.Sin(p * 0.70 + lightIndex * 0.15) * 0.5;

            return 0.82 + breathing * 0.18;
          }

        case 1:
          {
            double pairOffset = lightIndex < 2 ? 0.0 : Math.PI;
            double wave = 0.5 + Math.Sin(p * 1.15 + pairOffset) * 0.5;

            return Smooth01(Clamp01((wave - 0.18) / 0.32));
          }

        case 2:
          return 1.0;

        default:
          {
            double offset = lightIndex * Math.PI * 0.5;
            double wave = 0.5 + Math.Sin(p * 1.35 + offset) * 0.5;

            return Smooth01(Clamp01((wave - 0.20) / 0.25));
          }
      }
    }

    public static LightPose BlendPose(LightPose from, LightPose to, double t)
    {
      return new LightPose(Lerp(from.Angle, to.Angle, t), Lerp(from.Depth, to.Depth, t));
    }

    public static void SmoothLightIntensity(ref double current, double target, double deltaTime)
    {
      double response = target > current ? 10.0 : 5.0;
      double smoothing = 1.0 - Math.Exp(-response * deltaTime);

      current += (target - current) * smoothing;
    }

    public static double GetMountedBeamAngle(double mountX, double targetX, double targetY, double aspect)
    {
      double horizontalDistance = (targetX - mountX) * aspect;
      double verticalDistance = Math.Max(0.05, targetY);

      return Math.Atan2(horizontalDistance, verticalDistance) * 180.0 / Math.PI;
    }

    public static double PositiveModulo(double value, double modulus)
    {
      double result = value % modulus;

      return result < 0 ? result + modulus : result;
    }

    public static double Clamp01(double value)
    {
      return Math.Max(0.0, Math.Min(1.0, value));
    }

    public static double Clamp(double value, double minimum, double maximum)
    {
      return Math.Max(minimum, Math.Min(maximum, value));
    }

    public static double Smooth01(double value)
    {
      value = Clamp01(value);

      return value * value * (3.0 - 2.0 * value);
    }

    public static double Lerp(double from, double to, double t)
    {
      return from + (to - from) * t;
    }
  }
}




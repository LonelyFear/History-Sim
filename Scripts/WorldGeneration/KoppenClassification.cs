using System;
using Godot;

public class KoppenClassification
{
    public static string[,] GetKoppenMap(WorldGenerator world)
    {
        string[,] koppenMap = new string[world.WorldSize.X, world.WorldSize.Y];
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                if (world.HeightMap[x,y] < 0)
                {
                    koppenMap[x,y] = "W";
                    continue;
                }
                double januaryTemperature = world.WinterTempMap[x,y];
                double julyTemperature = world.SummerTempMap[x,y];

                double januaryRainfall = world.WinterRainfallMap[x,y];
                double julyRainfall = world.SummerRainfallMap[x,y];       

                double averageTemp = world.GetAverageAnnualTemp(x,y);
                double averagePrecipitationTotal = world.GetAnnualRainfall(x,y);

                double minTemp = Math.Min(januaryTemperature, julyTemperature);
                double maxTemp = Math.Max(januaryTemperature, julyTemperature);
                double driestPrecipitation = Math.Min(januaryRainfall, julyRainfall);

                double winterPrecipitation, summerPrecipitation;

                if (januaryTemperature < julyTemperature)
                {
                    winterPrecipitation = januaryRainfall;
                    summerPrecipitation = julyRainfall;
                }
                else
                {
                    winterPrecipitation = julyRainfall;
                    summerPrecipitation = januaryRainfall;
                }

                double seasonsFactor;
                if (januaryTemperature < julyTemperature)
                    seasonsFactor = julyRainfall / (julyRainfall + januaryRainfall);
                else
                    seasonsFactor = januaryRainfall / (julyRainfall + januaryRainfall);
                
                double thresholdB = averageTemp * 20.0;

                if (seasonsFactor >= 0.7)
                    thresholdB += 280.0;
                else if (seasonsFactor >= 0.3)
                    thresholdB += 140.0;
                
                if (averagePrecipitationTotal < thresholdB)
                {
                    if (averagePrecipitationTotal < 0.5 * thresholdB)
                        koppenMap[x,y] = averageTemp > 18.0 ? "BWh" : "BWk";
                    else
                        koppenMap[x,y] =  averageTemp > 18.0 ? "BSh" : "BSk";
                }
                else if (minTemp >= 18.0)
                {
                    if (driestPrecipitation >= 60.0)
                        koppenMap[x,y] =  "Af";
                    else if (driestPrecipitation >= 100.0 - averagePrecipitationTotal / 25.0)
                        koppenMap[x,y] =  "Am";
                    else
                        koppenMap[x,y] =  "Aw";
                }
                else if (maxTemp > 10.0)
                {
                    if (minTemp >= -3.0)
                    {
                        if (winterPrecipitation > 0.7 * (winterPrecipitation + summerPrecipitation))
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Csa" : maxTemp > 18.0 ? "Csb" : "Csc";
                        else if (summerPrecipitation > 0.7 * (winterPrecipitation + summerPrecipitation))
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Cwa" : maxTemp > 18.0 ? "Cwb" : "Cwc";
                        else
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Cfa" : maxTemp > 18.0 ? "Cfb" : "Cfc";
                    }
                    else
                    {
                        if (winterPrecipitation > 0.7 * (winterPrecipitation + summerPrecipitation))
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Dsa" : maxTemp > 18.0 ? "Dsb" : "Dsc";
                        else if (summerPrecipitation > 0.7 * (winterPrecipitation + summerPrecipitation))
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Dwa" : maxTemp > 18.0 ? "Dwb" : "Dwc";
                        else
                            koppenMap[x,y] =  maxTemp > 22.0 ? "Dfa" : maxTemp > 18.0 ? "Dfb" : "Dfc";
                    }
                }
                else
                {
                    koppenMap[x,y] =  maxTemp >= 0.0 ? "ET" : "EF";
                }                
            }            
        }
        return koppenMap;
    }
    public static Color GetColor(string classification)
    {
        Color color = classification switch
        {
            "BWh" => Color.Color8(255, 0, 0),
            "BWk" => Color.Color8(255, 150, 150),
            "BSh" => Color.Color8(245, 163, 0),
            "BSk" => Color.Color8(255, 219, 99),
            "Af" => Color.Color8(0, 0, 255),
            "Am" => Color.Color8(0, 119, 255),
            "Aw" => Color.Color8(70, 169, 255),
            "Csa" => Color.Color8(255, 255, 0),
            "Csb" => Color.Color8(198, 199, 0),
            "Csc" => Color.Color8(150, 150, 0),
            "Cwa" => Color.Color8(150, 255, 150),
            "Cwb" => Color.Color8(99, 199, 100),
            "Cwc" => Color.Color8(50, 150, 51),
            "Cfa" => Color.Color8(198, 255, 78),
            "Cfb" => Color.Color8(102, 255, 51),
            "Cfc" => Color.Color8(51, 191, 1),
            "Dsa" => Color.Color8(255, 0, 254),
            "Dsb" => Color.Color8(198, 1, 199),
            "Dsc" => Color.Color8(150, 50, 149),
            "Dwa" => Color.Color8(171, 177, 255),
            "Dwb" => Color.Color8(90, 119, 219),
            "Dwc" => Color.Color8(76, 81, 181),
            "Dfa" => Color.Color8(0, 255, 255),
            "Dfb" => Color.Color8(56, 199, 255),
            "Dfc" => Color.Color8(0, 126, 126),
            "ET" => Color.Color8(178, 178, 178),
            "EF" => Color.Color8(104, 104, 104),
            "W" => Color.Color8(0, 0, 50),
            "R" => Color.Color8(0, 0, 0),
            _ => Color.Color8(0, 0, 0),
        };
        return color;
    }
}

using System;
using System.Data.Common;
using System.Linq;
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
                if (world.HeightMap[x, y] < 0)
                {
                    koppenMap[x, y] = "W";
                    continue;
                }

                int monthsAbove10 = 0;
                int warmestMonth = world.GetTempForMonth(x,y,0) > world.GetTempForMonth(x,y,6) ? 0 : 6;
                bool warmestMonthsAbove10C = world.GetTempForMonth(x,y,warmestMonth - 1) > 10 && world.GetTempForMonth(x,y,warmestMonth) > 10
                && world.GetTempForMonth(x,y,warmestMonth + 1) > 10 && world.GetTempForMonth(x,y,warmestMonth + 2) > 10;
                for (int i = 0; i < 12; i++)
                {
                    if (world.GetTempForMonth(x,y,i) >= 10)
                    {
                        monthsAbove10++;
                    }
                }
                double januaryTemperature = world.WinterTempMap[x, y];
                double julyTemperature = world.SummerTempMap[x, y];

                double januaryRainfall = world.WinterRainfallMap[x, y];
                double julyRainfall = world.SummerRainfallMap[x, y];

                double averageTemp = world.GetAverageAnnualTemp(x, y);
                double averagePrecipitationTotal = world.GetAnnualRainfall(x, y);

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

                double thresholdB = averageTemp * 20.0;

                if (0.7 * averagePrecipitationTotal >= summerPrecipitation)
                    thresholdB += 280.0;
                else if (0.7 * averagePrecipitationTotal < summerPrecipitation && 0.7 * averagePrecipitationTotal < winterPrecipitation)
                    thresholdB += 140.0;

                if (averagePrecipitationTotal < thresholdB)
                {
                    // Arid
                    string classification = "B";
                    if (averagePrecipitationTotal < 0.5 * thresholdB)
                    {
                        classification +='W';
                    } else
                    {
                        classification +='S';
                    }
                    if (averageTemp > 18f)
                    {
                        classification += 'h';
                    } else
                    {
                        classification += 'k';
                    }
                    koppenMap[x,y] = classification;
                }
                else if (minTemp > 18f)
                {
                    // Tropical
                    if (driestPrecipitation >= 60f)
                    {
                        koppenMap[x,y] = "Af";
                    }
                    else if (driestPrecipitation > 100f - (averagePrecipitationTotal/25f))
                    {
                        koppenMap[x,y] = "Am";
                    }
                    else if (driestPrecipitation <= 100f - (averagePrecipitationTotal/25f))
                    {
                        koppenMap[x,y] = "Aw";
                    }
                }
                else if (maxTemp >= 10f)
                {
                    // Temperate
                    string classification = "D";
                    if (minTemp < 18f && minTemp > -3f)
                    {
                        classification = "C";
                    }
                    // Second Letter
                    if (summerPrecipitation < 30 && summerPrecipitation < winterPrecipitation * 0.3)
                    {
                        classification += "s";
                    } else if (winterPrecipitation < summerPrecipitation * 0.1)
                    {
                        classification += "w";
                    } else
                    {
                        classification += "f";
                    }

                    // Third Letter
                    if (maxTemp >= 22)
                    {
                        classification += "a";
                    } else
                    {
                        if (warmestMonthsAbove10C)
                        {
                            classification += "b";
                        } else if (monthsAbove10 >= 1 && monthsAbove10 <= 3)
                        {
                            classification += "c"; 
                        }                       
                    }
                    if (minTemp < -38)
                    {
                        classification += "d";
                    }
                    koppenMap[x,y] = classification;
                }
                else if (maxTemp < 10f)
                {
                    koppenMap[x, y] = maxTemp >= 0.0 ? "ET" : "EF";
                }
            }
        }
        return koppenMap;
    }
    public static Color GetColor(string classification)
    {
        if (classification != "Csb" && classification != "W")
        {
            //return new Color(0.1f,0.1f,0.1f);
        }
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

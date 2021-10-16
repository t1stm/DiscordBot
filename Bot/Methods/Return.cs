using System;
using AngleSharp.Text;

namespace Bat_Tosho.Methods
{
    public static class Return
    {
        public static TimeSpan StringToTimeSpan(string text)
        {
            int days = 0, hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
            var time = text.Split(":");
            var arrSize = time.Length - 1;
            switch (arrSize)
            {
                case 0:
                    switch (time[arrSize].Contains("."))
                    {
                        case true:
                            var secMillisec = time[arrSize].Split(".");
                            seconds = secMillisec[0].ToInteger(0);
                            milliseconds = secMillisec[1].ToInteger(0);
                            break;
                        case false:
                            seconds = time[arrSize].Replace(":", "").ToInteger(0);
                            break;
                    }

                    break;
                case 1:
                    seconds = time[arrSize].Replace(":", "").ToInteger(0);
                    minutes = time[arrSize - 1].Replace(":", "").ToInteger(0);
                    break;
                case 2:
                    seconds = time[arrSize].Replace(":", "").ToInteger(0);
                    minutes = time[arrSize - 1].Replace(":", "").ToInteger(0);
                    hours = time[arrSize - 2].Replace(":", "").ToInteger(0);
                    break;
            }

            while (hours >= 24)
            {
                days++;
                hours -= 24;
            }

            return new TimeSpan(days, hours, minutes, seconds, milliseconds);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using TwitterUtil.TweetSummary;

namespace GenerateStats
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // FilterGeoSentiments();

            // CollateSentiments();

            // Collate();

            //var be = LoadAlreadyLocationsCollated();
            //DumpGeoOnlyLocations(be);


            //var be = LoadAlreadyLocationsCollated();
            //AnalyseLocations(be);

            // var be = LoadAlreadySentimentsCollated();
            // DumpGeoOnlySentiments(be);
            // AnalyseSentiments(be);


            AnalyseGeoLocations();
        }


        public static void CollateLocations()
        {
            string[] tweets = {@"A:\withRateWithSentiment"};
            Console.WriteLine($"Start {DateTime.Now}");

            var eng = new BatchEngine<LocatedTweet, LocationParametersExtractor, LocationParameters>(8, tweets);
            eng.Process();
            Console.WriteLine($"Have {eng.Count,12:N0} records\n\n");


            const byte nl = (byte) '\n';

            var ser = new DataContractJsonSerializer(typeof(LocationParameters));
            using (var fs = File.Open(@"A:\locationParameters\series-LocationParameters.json", FileMode.Create))
            {
                var ids = new HashSet<string>();
                var cnt = 0;

                foreach (var item in eng.Records())
                {
                    if (ids.Contains(item.PostId)) continue;

                    ids.Add(item.PostId);
                    ser.WriteObject(fs, item);
                    fs.WriteByte(nl);

                    if (++cnt % 500000 == 0) Console.WriteLine($"Written {cnt,12:N0}");
                }
            }
        }


        public static void DumpGeoOnlyLocations(
            BatchEngine<LocationParameters, ReloadLocationParameters, LocationParameters> be)
        {
            const byte nl = (byte) '\n';

            var ser = new DataContractJsonSerializer(typeof(LocationParameters));
            using (var fs = File.Open(@"A:\geoOnlyLocated\geoOnly-LocationParameters.json", FileMode.Create))
            {
                var cnt = 0;

                foreach (var item in be.Records().Where(x => x.GeoEnabled))
                {
                    ser.WriteObject(fs, item);
                    fs.WriteByte(nl);

                    if (++cnt % 50000 == 0) Console.WriteLine($"Written {cnt,12:N0}");
                }
            }
        }


        public static void DumpGeoOnlySentiments(
            BatchEngine<SentimentParametersBase, ReloadSentimentParameters, SentimentParametersBase> be)
        {
            const byte nl = (byte) '\n';

            var ser = new DataContractJsonSerializer(typeof(SentimentParametersBase));
            using (var fs = File.Open(@"A:\geoOnlySentiment\geoOnly-LocationParameters.json", FileMode.Create))
            {
                var cnt = 0;

                foreach (var item in be.Records().Where(x => x.GeoEnabled))
                {
                    ser.WriteObject(fs, item);
                    fs.WriteByte(nl);

                    if (++cnt % 50000 == 0) Console.WriteLine($"Written {cnt,12:N0}");
                }
            }
        }

        public static void CollateSentiments()
        {
            string[] tweets = {@"A:\withRateWithSentiment"};
            Console.WriteLine($"Start {DateTime.Now}");

            var eng = new BatchEngine<TweetScore, SentimentExtractor, SentimentParameters>(8, tweets);
            eng.Process();
            Console.WriteLine($"Have {eng.Count,12:N0} records\n\n");


            const byte nl = (byte) '\n';

            var ser = new DataContractJsonSerializer(typeof(SentimentParameters));
            using (var fs = File.Open(@"A:\sentimentParameters\series-SentimentParameters.json", FileMode.Create))
            {
                var ids = new HashSet<string>();
                var cnt = 0;

                foreach (var item in eng.Records())
                {
                    if (ids.Contains(item.PostId)) continue;
                    ser.WriteObject(fs, item);
                    fs.WriteByte(nl);

                    if (++cnt % 500000 == 0) Console.WriteLine($"Written {cnt,12:N0}");
                }
            }
        }


        public static void FilterGeoSentiments()
        {
            string[] tweets = {@"A:\withRateWithSentiment"};
            Console.WriteLine($"Start {DateTime.Now}");

            var eng = new BatchEngine<TweetScore, GeoSentimentExtractor, UserGeoSentimentParameters>(8, tweets)
            {
                GetGeoLocatedOnly = true
            };
            eng.Process();
            Console.WriteLine($"Have {eng.Count,12:N0} records\n\n");


            const byte nl = (byte) '\n';

            var ser = new DataContractJsonSerializer(typeof(GeoSentimentParameters));
            using (var fs = File.Open(@"A:\geoCombined\series-GeoSentimentParameters.json", FileMode.Create))
            {
                var ids = new HashSet<string>();
                var cnt = 0;

                foreach (var item in eng.Records())
                {
                    if (ids.Contains(item.PostId)) continue;
                    ser.WriteObject(fs, item);
                    fs.WriteByte(nl);

                    if (++cnt % 500000 == 0) Console.WriteLine($"Written {cnt,12:N0}");
                }
            }
        }


        public static BatchEngine<LocationParameters, ReloadLocationParameters, LocationParameters>
            LoadAlreadyLocationsCollated(string altSrc = @"A:\locationParameters")
        {
            string[] locations = {altSrc};
            Console.WriteLine($"Start {DateTime.Now}");

            var eng = new BatchEngine<LocationParameters, ReloadLocationParameters, LocationParameters>(8, locations);
            eng.Process();

            Console.WriteLine($"Have {eng.Count,12:N0} records\n\n");
            return eng;
        }

        public static BatchEngine<SentimentParametersBase, ReloadSentimentParameters, SentimentParametersBase>
            LoadAlreadySentimentsCollated(string altSrc = @"A:\sentimentParameters")
        {
            string[] locations = {@"A:\sentimentParameters"};
            Console.WriteLine($"Start {DateTime.Now}");

            var eng = new
                BatchEngine<SentimentParametersBase, ReloadSentimentParameters, SentimentParametersBase>
                (8, locations);
            eng.Process();

            Console.WriteLine($"Have {eng.Count,12:N0} records\n\n");
            return eng;
        }


        public static void AnalyseLocations(
            BatchEngine<LocationParameters, ReloadLocationParameters, LocationParameters> be)
        {
            Console.WriteLine($"Analysing \n");

            // ---------------------------------------------------------
            // collate the base statistics

            // by city, by year-month
            var collateByCity = be.Records().AsParallel()
                .GroupBy(x => x.Location)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => d.LocalTime.ToString("yyyy-MM-01"))
                        .ToDictionary(t => t.Key,
                            t => t.GroupBy(g => new {Place = g.HasPlace, Exact = g.GeoEnabled})
                                .ToDictionary(c => c.Key, c => c.Count())));

            var ti = CultureInfo.CurrentCulture.TextInfo;

            using (var ofs = new StreamWriter(@"..\..\volumeStats.csv"))
            {
                ofs.WriteLine($"Location,YearMth,HasPlace,HasGeo,Count");

                foreach (var kvp in collateByCity.OrderBy(x => x.Key))
                foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                foreach (var vcp in vgp.Value)
                    ofs.WriteLine(
                        $"{ti.ToTitleCase(kvp.Key)},{vgp.Key},{(vcp.Key.Place ? 'T' : 'F')},{(vcp.Key.Exact ? 'T' : 'F')},{vcp.Value}");
            }

            // ---------------------------------------------------------
            // extract unique locations

            var locs = be.Records().AsParallel()
                .Where(x => x.GeoEnabled && x.Yloc.HasValue && x.Xloc.HasValue)
                .GroupBy(x => new {Y = x.Yloc.Value, X = x.Xloc.Value})
                .ToDictionary(x => x.Key, x => x.Count());

            using (var ofs = new StreamWriter($@"..\..\locations.csv"))
            {
                ofs.WriteLine("Yloc,Xloc,Count");
                foreach (var kvp in locs.OrderByDescending(x => x.Value))
                    ofs.WriteLine($"{kvp.Key.Y},{kvp.Key.X},{kvp.Value}");
            }


            // ---------------------------------------------------------
            // extract time of day

            // by city, by year-month
            var timeOfDay = be.Records().AsParallel()
                .GroupBy(d => d.LocalTime.DayOfWeek)
                .ToDictionary(x => x.Key, x => x.GroupBy(d => d.LocalTime.ToString("HH"))
                    .ToDictionary(d => d.Key, d => d.Count()));

            using (var ofs = new StreamWriter($@"..\..\timeOfDayActivity.csv"))
            {
                ofs.WriteLine("DayOfWeek,Hour,Count");
                foreach (var dvp in timeOfDay)
                foreach (var kvp in dvp.Value)
                    ofs.WriteLine($"{dvp.Key},{kvp.Key},{kvp.Value}");
            }


            // ---------------------------------------------------------
            // extract unique users

            // by city, by year-month
            var usersMth = be.Records().AsParallel()
                .Where(x => x.GeoEnabled)
                .GroupBy(d => d.LocalTime.ToString("yyyy-MM-01"))
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(u => u.UserId)
                        .ToDictionary(t => t.Key, t => t.Count()));

            using (var ofs = new StreamWriter($@"..\..\geoUsersPerMth.csv"))
            {
                ofs.WriteLine("YearMth,DistinctUsers");
                foreach (var kvp in usersMth)
                    ofs.WriteLine($"{kvp.Key},{kvp.Value.Count}");
            }


            // ---------------------------------------------------------
            // --  extract muli-city journeys  -  place located


            var collateByUserLocation = be.Records().AsParallel()
                .Where(x => x.HasPlace)
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(u => u.Location)
                        .ToDictionary(u => u.Key, u => u.ToList()));


            // determine most frequent tweet location for user
            var mostFreqLocationByUser = collateByUserLocation.AsParallel()
                .Where(x => x.Value.Count > 1) // more than 1 city
                .ToDictionary(x => x.Key,
                    x => x.Value.OrderByDescending(p => p.Value.Count).First().Key);


            using (var ofs = new StreamWriter($@"..\..\userHomeCity.csv"))
            {
                ofs.WriteLine("UserId,City");
                foreach (var kvp in mostFreqLocationByUser) ofs.WriteLine($"{kvp.Key},{kvp.Value}");
            }


            var coords = new Dictionary<string, double[]>
            {
                {"melbourne", new[] {-37.813611, 144.963056}},
                {"adelaide", new[] {-34.928889, 138.601111}},
                {"brisbane", new[] {-27.466667, 153.033333}},
                {"canberra", new[] {-35.3075, 149.124417}},
                {"hobart", new[] {-42.880556, 147.325}},
                {"perth", new[] {-31.952222, 115.858889}},
                {"sydney", new[] {-33.865, 151.209444}}
            };


            var toJourneys = mostFreqLocationByUser.AsParallel()
                .SelectMany(x =>
                    collateByUserLocation[x.Key].Select(u =>
                        new
                        {
                            User = x.Key,
                            Source = x.Value,
                            Target = u.Key,
                            Trips = u.Value.Count
                        }))
                .Where(x => x.Source != x.Target)
                .ToList();


            var toJourneysUserCount = toJourneys.AsParallel()
                .GroupBy(x => new {x.Source, x.Target})
                .ToDictionary(x => x.Key, x => x.Count());

            var toJourneysTargetTweetCount = toJourneys.AsParallel()
                .GroupBy(x => new {x.Source, x.Target})
                .ToDictionary(x => x.Key, x => x.Sum(i => i.Trips));


            using (var ofs = new StreamWriter(@"..\..\geoJourneysUsers.csv"))
            {
                ofs.WriteLine("Source,X1,Y1,X2,Y2,Target,Users");
                foreach (var kvp in toJourneysUserCount)
                {
                    ofs.Write($"{kvp.Key.Source}");
                    ofs.Write($",{coords[kvp.Key.Source][1]},{coords[kvp.Key.Source][0]}");
                    ofs.Write($",{coords[kvp.Key.Target][1]},{coords[kvp.Key.Target][0]}");
                    ofs.WriteLine($",{kvp.Key.Target},{kvp.Value}");
                }
            }

            using (var ofs = new StreamWriter(@"..\..\geoJourneysActivity.csv"))
            {
                ofs.WriteLine("Source,X1,Y1,X2,Y2,Target,Tweets");
                foreach (var kvp in toJourneysTargetTweetCount)
                {
                    ofs.Write($"{kvp.Key.Source}");
                    ofs.Write($",{coords[kvp.Key.Source][1]},{coords[kvp.Key.Source][0]}");
                    ofs.Write($",{coords[kvp.Key.Target][1]},{coords[kvp.Key.Target][0]}");
                    ofs.WriteLine($",{kvp.Key.Target},{kvp.Value}");
                }
            }
        }


        public static void AnalyseSentiments(
            BatchEngine<SentimentParametersBase, ReloadSentimentParameters, SentimentParametersBase> be)
        {
            Console.WriteLine($"Analysing \n");

            // ---------------------------------------------------------
            // collate the base statistics

            // by city, by year-month
            var collateByCity = be.Records().AsParallel()
                .GroupBy(x => x.Location)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => d.LocalTime.ToString("yyyy-MM-01"))
                        .ToDictionary(t => t.Key,
                            t => t.Average(i => i.Compound)));

            var ti = CultureInfo.CurrentCulture.TextInfo;

            using (var ofs = new StreamWriter(@"..\..\sentimentStats.csv"))
            {
                ofs.WriteLine($"Location,YearMth,AverageSentiment");

                foreach (var kvp in collateByCity.OrderBy(x => x.Key))
                foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                    ofs.WriteLine(
                        $"{ti.ToTitleCase(kvp.Key)},{vgp.Key},{vgp.Value}");
            }


            var collateExcludeNeuturalByCity = be.Records().AsParallel()
                .GroupBy(x => x.Location)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => d.LocalTime.ToString("yyyy-MM-01"))
                        .ToDictionary(t => t.Key,
                            t => t.Where(i => !(-0.05 <= i.Compound && i.Compound <= 0.05))
                                .Average(i => i.Compound)));

            using (var ofs = new StreamWriter(@"..\..\sentimentStatsExcludedNetural.csv"))
            {
                ofs.WriteLine($"Location,YearMth,AverageSentiment");

                foreach (var kvp in collateExcludeNeuturalByCity.OrderBy(x => x.Key))
                foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                    ofs.WriteLine(
                        $"{ti.ToTitleCase(kvp.Key)},{vgp.Key},{vgp.Value}");
            }


            // bucket by city & sentiment band -  2% bands
            // by city, by year-month
            var collateByCityBand = be.Records().AsParallel()
                .GroupBy(x => x.Location)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => 5 * (int) (d.Compound * 20 + 0.5))
                        .ToDictionary(t => t.Key,
                            t => t.Count()));


            using (var ofs = new StreamWriter(@"..\..\sentimentStatsBanded.csv"))
            {
                ofs.WriteLine($"Location,SentimentBand,Count,Total");

                foreach (var kvp in collateByCityBand.OrderBy(x => x.Key))
                {
                    var total = kvp.Value.Sum(x => x.Value);
                    foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                        ofs.WriteLine(
                            $"{ti.ToTitleCase(kvp.Key)},{vgp.Key},{vgp.Value},{total}");
                }
            }


            // ---------------------------------------------------------
            // extract time of day

            // by dayOfWeek, timeOfDay
            var timeOfDay = be.Records().AsParallel()
                .GroupBy(d => d.LocalTime.DayOfWeek)
                .ToDictionary(x => x.Key, x => x.GroupBy(d => d.LocalTime.ToString("HH"))
                    .ToDictionary(d => d.Key, d => d.Average(i => i.Compound)));

            using (var ofs = new StreamWriter($@"..\..\sentimentTimeOfDayActivity.csv"))
            {
                ofs.WriteLine("DayOfWeek,Hour,Average");
                foreach (var dvp in timeOfDay)
                foreach (var kvp in dvp.Value)
                    ofs.WriteLine($"{dvp.Key},{kvp.Key},{kvp.Value}");
            }


            // bucket by city & sentiment band -  2% bands
            // by city, by year-month
            var timeOfDayBand = be.Records().AsParallel()
                .GroupBy(d => d.LocalTime.DayOfWeek)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => 5 * (int) (d.Compound * 20 + 0.5))
                        .ToDictionary(t => t.Key,
                            t => t.Count()));


            using (var ofs = new StreamWriter(@"..\..\sentimentStatsDayBanded.csv"))
            {
                ofs.WriteLine($"DayOfWeek,SentimentBand,Count,Total");

                foreach (var kvp in timeOfDayBand.OrderBy(x => x.Key))
                {
                    var total = kvp.Value.Sum(x => x.Value);
                    foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                        ofs.WriteLine(
                            $"{kvp.Key},{vgp.Key},{vgp.Value},{total}");
                }
            }


            // bucket by city & sentiment band -  2% bands
            // by city, by year-month
            var timeOfHourBand = be.Records().AsParallel()
                .GroupBy(d => d.LocalTime.ToString("HH"))
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(d => 5 * (int) (d.Compound * 20 + 0.5))
                        .ToDictionary(t => t.Key,
                            t => t.Count()));

            using (var ofs = new StreamWriter(@"..\..\sentimentStatsHourBanded.csv"))
            {
                ofs.WriteLine($"Hour,SentimentBand,Count,Total");

                foreach (var kvp in timeOfHourBand.OrderBy(x => x.Key))
                {
                    var total = kvp.Value.Sum(x => x.Value);
                    foreach (var vgp in kvp.Value.OrderBy(x => x.Key))
                        ofs.WriteLine(
                            $"{kvp.Key},{vgp.Key},{vgp.Value},{total}");
                }
            }
        }


        public static void AnalyseGeoLocations()
        {
            Console.WriteLine($"Analysing Geos \n");

            var src = @"A:\geoCombined\";
            var jr = new JsonRead<GeoSentimentParameters>(new[]{src});
            jr.DoLoad();


                // ---------------------------------------------------------
                // --  extract muli-city journeys  -  geo_located

            var collateByGeoUserLocation = jr.Records
                .Where(x => x.GeoEnabled)
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key,
                    x => x.GroupBy(u => u.Location)
                        .ToDictionary(u => u.Key, u => u.ToList()));


            // determine most frequent tweet location for user
            var mostFreqLocationByGeoUser = collateByGeoUserLocation
                .Where(x => x.Value.Count > 1) // more than 1 city
                .ToDictionary(x => x.Key,
                    x => x.Value.OrderByDescending(p => p.Value.Count).First().Key);


            var toGeoJourneys = mostFreqLocationByGeoUser
                .SelectMany(x =>
                    collateByGeoUserLocation[x.Key]
                        .Where(u => x.Value != u.Key)
                        .Select(u => new
                        {
                            User = x.Key,
                            SourceCity = x.Value,
                            TargetCity = u.Key,
                            Locations = u.Value
                        })).GroupBy(
                    x => new
                    {
                        x.SourceCity,
                        x.TargetCity
                    }).ToDictionary(
                    x => x.Key,
                    x => x.SelectMany(j => j.Locations.Select(i => new
                        {
                            X2 = i.Xloc,
                            Y2 = i.Yloc,
                            i.LocalTime
                        }).ToList()
                    ));


            var fromGeoJourneys = collateByGeoUserLocation
                .Where(x => x.Value.Any(c => mostFreqLocationByGeoUser.ContainsKey(x.Key) && c.Key == mostFreqLocationByGeoUser[x.Key]))
                .SelectMany(x => x.Value
                    .Where(u => u.Key != mostFreqLocationByGeoUser[x.Key])
                    .Select(u => new
                    {
                        User = x.Key,
                        SourceCity = mostFreqLocationByGeoUser[x.Key],
                        TargetCity = u.Key,
                        Locations = x.Value[mostFreqLocationByGeoUser[x.Key]]
                    })).GroupBy(
                    x => new
                    {
                        x.SourceCity,
                        x.TargetCity
                    })
                .ToDictionary(
                    x => x.Key,
                    x => x.SelectMany(j => j.Locations
                        .Select(i => new
                        {
                            X1 = i.Xloc,
                            Y1 = i.Yloc,
                            i.LocalTime
                        }).ToList()
                    ));


            using (var ofs = new StreamWriter($@"..\..\geoToJourneys.csv"))
            {
                ofs.WriteLine("SourceCity,TargetCity,X2,Y2,LocalTime");
                foreach (var kvp in toGeoJourneys)
                foreach (var loc in kvp.Value)

                    ofs.WriteLine(
                        $"{kvp.Key.SourceCity},{kvp.Key.TargetCity},{loc.X2},{loc.Y2},{loc.LocalTime:s}");
            }

            using (var ofs = new StreamWriter($@"..\..\geoFromJourneys.csv"))
            {
                ofs.WriteLine("SourceCity,TargetCity,X1,Y1,LocalTime");
                foreach (var kvp in fromGeoJourneys)
                foreach (var loc in kvp.Value)

                    ofs.WriteLine(
                        $"{kvp.Key.SourceCity},{kvp.Key.TargetCity},{loc.X1},{loc.Y1},{loc.LocalTime:s}");
            }
        }
    }
}
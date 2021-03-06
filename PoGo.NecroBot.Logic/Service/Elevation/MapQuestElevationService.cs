﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Caching;
using Newtonsoft.Json;
using PoGo.NecroBot.Logic.Exceptions;
using PoGo.NecroBot.Logic.Model.Settings;

namespace PoGo.NecroBot.Logic.Service.Elevation
{
    public class MapQuestResponse
    {
        public List<ElevationProfiles> elevationProfile { get; set; }
    }

    public class ElevationProfiles
    {
        public double distance { get; set; }
        public double height { get; set; }
    }

    public class MapQuestElevationService : BaseElevationService
    {
        private string mapQuestDemoApiKey = $"Kmjtd|luua2qu7n9,7a=o5-lzbgq";

        public MapQuestElevationService(GlobalSettings settings, LRUCache<string, double> cache) : base(settings, cache)
        {
            if (!string.IsNullOrEmpty(mapQuestDemoApiKey))
                _apiKey = mapQuestDemoApiKey;
        }

        public override string GetServiceId()
        {
            return "MapQuest Elevation Service";
        }

        public override double GetElevationFromWebService(double lat, double lng)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return 0;

            try
            {
                string url = $"https://open.mapquestapi.com/elevation/v1/profile?key={_apiKey}&callback=handleHelloWorldResponse&shapeFormat=raw&latLngCollection={lat},{lng}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                request.ContentType = "application/json";
                request.Referer = "https://open.mapquestapi.com/elevation/";
                request.ReadWriteTimeout = 2000;
                string responseFromServer = "";

                using (WebResponse response = request.GetResponse())
                {
                    using (Stream dataStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        responseFromServer = reader.ReadToEnd();
                        responseFromServer = responseFromServer.Replace("handleHelloWorldResponse(", "");
                        responseFromServer = responseFromServer.Replace("]}});", "]}}");
                        MapQuestResponse mapQuestResponse = JsonConvert.DeserializeObject<MapQuestResponse>(responseFromServer);
                        if (mapQuestResponse.elevationProfile != null &&
                            mapQuestResponse.elevationProfile.Count > 0 &&
                            mapQuestResponse.elevationProfile[0].height > -100)
                        {
                            return mapQuestResponse.elevationProfile[0].height;
                        }

                        // All error handling is handled inside of the ElevationService.
                    }
                }
            }
            catch (ActiveSwitchByRuleException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                // If we get here for any reason, then just drop down and return 0.
            }

            return 0;
        }
    }
}
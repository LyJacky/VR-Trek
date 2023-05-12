﻿using System;
using System.Collections.Generic;

namespace TrekVRApplication {

    public class TrekToolsWebService : IToolsWebService {

        private const string Endpoint = "https://trek.nasa.gov/mars/arcgis/rest/services/mola128_mola64_merge_90Nto90S_SimpleC_clon0/ImageServer";

        public static TrekToolsWebService Instance { get; } = new TrekToolsWebService();


        public void GetDistance(string points, Action<string> callback) {

            IDictionary<string, string> paramsMap = new Dictionary<string, string>() {
                { "endpoint", Endpoint },
                { "path", points },
                { "radiusInMeters", Mars.Radius.ToString() }
            };

            string url = HttpRequestUtils.AppendParams("https://trek.nasa.gov/mars/TrekServices/ws/elevationProfile/distance", paramsMap);

            HttpClient.Get(url, res => {
                string responseBody = HttpClient.GetReponseBody(res);
                callback?.Invoke(responseBody);
            });

        }

        public void GetHeightProfile(string points, int sampleCount, Action<string> callback) {

            IDictionary<string, string> paramsMap = new Dictionary<string, string>() {
                { "endpoint", Endpoint },
                { "path", points },
                { "numberOfPoints", sampleCount.ToString() },
                { "radiusInMeters", Mars.Radius.ToString() }
            };

            string url = HttpRequestUtils.AppendParams("https://trek.nasa.gov/mars/TrekServices/ws/elevationProfile/calculate", paramsMap);

            HttpClient.Get(url, res => {
                string responseBody = HttpClient.GetReponseBody(res);
                callback?.Invoke(responseBody);
            });

        }


    }

}
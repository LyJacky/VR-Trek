﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TrekVRApplication.SearchResponse;
using UnityEngine;

namespace TrekVRApplication {

    public class TrekSearchWebService : ISearchWebService {

        private const string BaseUrl = "https://trek.nasa.gov/mars/TrekServices/ws/index/eq/searchItems?&&&&proj=urn:ogc:def:crs:EPSG::104905";

        /// <summary>
        ///     Limit search results to 400 items.
        /// </summary>
        private const int ResultsLimit = 400;

        public static TrekSearchWebService Instance { get; } = new TrekSearchWebService();

        private int? _searchListActiveIndex = null;
        public int? SearchListActiveIndex {
            get => _searchListActiveIndex;
            set {
                _searchListActiveIndex = value;
                OnSearchListActiveIndexChange.Invoke(value);
            }
        }

        public event Action<int?> OnSearchListActiveIndexChange = index => { };

        #region Cached results

        private SearchResult _facetInfo;
        private SearchResult _bookmarks;
        private SearchResult _datasets;
        private SearchResult _nomenclatures;
        private SearchResult _products;

        #endregion

        private TrekSearchWebService() {

        }

        public void ClearCache() {
            _facetInfo = null;
            _bookmarks = null;
        }

        public void GetFacetInfo(Action<SearchResult> callback, bool forceRefresh = false) {
            if (forceRefresh) {
                _facetInfo = null;
            }
            if (_facetInfo) {
                callback?.Invoke(_facetInfo);
                return;
            }
            IDictionary<string, string> paramsMap = new Dictionary<string, string>() {
                { "start", "0" },
                { "rows", "0" } // Only get the facet data.
            };
            string searchUrl = HttpRequestUtils.AppendParams(BaseUrl, paramsMap);
            HttpClient.Get(searchUrl, (res) => {
                string responseBody = HttpClient.GetReponseBody(res);
                _facetInfo = DeserializeResults(responseBody);
                callback?.Invoke(_facetInfo);
            });
        }

        [Obsolete("Use the dedicated bookmarks service to retrieve the VR specific bookmarks.")]
        public void GetBookmarks(Action<SearchResult> callback, bool forceRefresh = false) {
            if (forceRefresh) {
                _bookmarks = null;
            }
            if (_bookmarks) {
                callback?.Invoke(_bookmarks);
                return;
            }
            Search(
                new SearchParameters() {
                    ItemType = SearchItemType.Bookmark
                }, 
                (res) => {
                    _bookmarks = res;
                    callback?.Invoke(_bookmarks);
                }
            );
        }

        public void GetDatasets(Action<SearchResult> callback, bool forceRefresh = false) {
            if (forceRefresh) {
                _datasets = null;
            }
            if (_datasets) {
                callback?.Invoke(_datasets);
                return;
            }
            Search(
                new SearchParameters() {
                    ItemType = SearchItemType.Dataset
                },
                (res) => {
                    _datasets = res;
                    callback?.Invoke(_datasets);
                }
            );
        }

        public void GetNomenclatures(Action<SearchResult> callback, bool forceRefresh = false) {
            if (forceRefresh) {
                _nomenclatures = null;
            }
            if (_nomenclatures) {
                callback?.Invoke(_nomenclatures);
                return;
            }
            Search(
                new SearchParameters() {
                    ItemType = SearchItemType.Nomenclature
                },
                (res) => {
                    _nomenclatures = res;
                    callback?.Invoke(_nomenclatures);
                }
            );
        }

        public void GetNomenclatures(IBoundingBox boundingBox, int limit, Action<SearchResult> callback) {
            Search(new SearchParameters() {
                ItemType = SearchItemType.Nomenclature,
                BoundingBox = boundingBox.ToString(","),
                Limit = limit
            }, callback);
        }

        public void GetProducts(Action<SearchResult> callback, bool forceRefresh = false) {
            if (forceRefresh) {
                _products = null;
            }
            if (_products) {
                callback?.Invoke(_products);
                return;
            }
            Search(
                new SearchParameters() {
                    ItemType = SearchItemType.Product
                },
                (res) => {
                    _products = res;
                    callback?.Invoke(_products);
                }
            );
        }

        public void Search(SearchParameters searchParams, Action<SearchResult> callback) {

            GetFacetInfo(data => {

                // Get the total count of the item type from the facet info.
                // If a count doesn't exist for the item type, then the count
                // is the total number of all items.
                if (!data.FacetInfo.ItemType.TryGetValue(searchParams.ItemType, out int itemCount)) {
                    itemCount = data.FacetInfo.ItemType.Values.Aggregate(0, (sum, val) => sum + val);
                }

                int limit = Math.Min(ResultsLimit, itemCount);
                if (searchParams.Limit != null) {
                    limit = Math.Min(limit, (int)searchParams.Limit);
                }

                Search(searchParams, limit, callback);
            });
        }

        private void Search(SearchParameters searchParams, int limit, Action<SearchResult> callback) {

            IDictionary<string, string> paramsMap = new Dictionary<string, string>() {
                { "start", "0" }, // Always start at index 0.
                { "rows", $"{limit}" }
            };

            string facetKeys = "", facetValues = "";
            if (searchParams.ItemType > 0) {
                AppendFacetQuery(ref facetKeys, "itemType");
                AppendFacetQuery(ref facetValues, searchParams.ItemType.GetSearchQueryTerm());
            }
            if (!string.IsNullOrEmpty(searchParams.ProductType)) {
                AppendFacetQuery(ref facetKeys, "productType");
                AppendFacetQuery(ref facetValues, searchParams.ProductType);
            }
            if (!string.IsNullOrEmpty(searchParams.Mission)) {
                AppendFacetQuery(ref facetKeys, "mission");
                AppendFacetQuery(ref facetValues, searchParams.Mission);
            }
            if (!string.IsNullOrEmpty(searchParams.Instrument)) {
                AppendFacetQuery(ref facetKeys, "instrument");
                AppendFacetQuery(ref facetValues, searchParams.Instrument);
            }
            paramsMap.Add("facetKeys", facetKeys);
            paramsMap.Add("facetValues", facetValues);

            if (!string.IsNullOrEmpty(searchParams.Search)) {
                paramsMap.Add("key", searchParams.Search);
            }

            if (!string.IsNullOrEmpty(searchParams.BoundingBox)) {
                paramsMap.Add("bbox", searchParams.BoundingBox);
            }

            string searchUrl = HttpRequestUtils.AppendParams(BaseUrl, paramsMap);
            HttpClient.Get(searchUrl, res => {
                string responseBody = HttpClient.GetReponseBody(res);
                SearchResult searchResult = DeserializeResults(responseBody);
                callback?.Invoke(searchResult);
            });

        }

        private SearchResult DeserializeResults(string json) {
            try {
                Result result = JsonConvert.DeserializeObject<Result>(json, JsonConfig.SerializerSettings);
                return new SearchResult(result);
            }
            catch (Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        private void AppendFacetQuery(ref string dest, string add) {
            dest = dest + (dest.Length > 0 ? "|" : "") + add;
        }

    }

}

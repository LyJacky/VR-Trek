﻿using System;
using System.Collections.Generic;

namespace TrekVRApplication {

    public interface ISearchWebService {

        int? SearchListActiveIndex { get; set; }

        event Action<int?> OnSearchListActiveIndexChange;

        void ClearCache();

        void GetFacetInfo(Action<SearchResult> callback, bool forceRefresh = false);

        [Obsolete("Use the dedicated bookmarks service to retrieve the VR specific bookmarks.")]
        void GetBookmarks(Action<SearchResult> callback, bool forceRefresh = false);

        void GetDatasets(Action<SearchResult> callback, bool forceRefresh = false);

        void GetNomenclatures(Action<SearchResult> callback, bool forceRefresh = false);

        void GetNomenclatures(IBoundingBox boundingBox, int limit, Action<SearchResult> callback);

        void GetProducts(Action<SearchResult> callback, bool forceRefresh = false);

        void Search(SearchParameters searchParms, Action<SearchResult> callback);

    }

}

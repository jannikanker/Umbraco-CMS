﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.PublishedCache;

namespace Umbraco.Web.PropertyEditors.ValueConverters
{
    public sealed class BlockEditorConverter
    {
        private readonly IPublishedSnapshotAccessor _publishedSnapshotAccessor;
        private readonly IPublishedModelFactory _publishedModelFactory;

        public BlockEditorConverter(IPublishedSnapshotAccessor publishedSnapshotAccessor, IPublishedModelFactory publishedModelFactory)
        {
            _publishedSnapshotAccessor = publishedSnapshotAccessor;
            _publishedModelFactory = publishedModelFactory;
        }

        public IPublishedElement ConvertToElement(
            JObject sourceObject, string contentTypeKeyPropertyKey,
            PropertyCacheLevel referenceCacheLevel, bool preview)
        {
            var elementTypeKey = sourceObject[contentTypeKeyPropertyKey]?.ToObject<string>();
            if (string.IsNullOrEmpty(elementTypeKey))
                return null;

            // only convert element types - content types will cause an exception when PublishedModelFactory creates the model
            // TODO: make this work with keys.
            var publishedContentType = _publishedSnapshotAccessor.PublishedSnapshot.Content.GetContentType(elementTypeKey);
            if (publishedContentType == null || publishedContentType.IsElement == false)
                return null;

            var propertyValues = sourceObject.ToObject<Dictionary<string, object>>();

            if (!propertyValues.TryGetValue("key", out var keyo) || !Guid.TryParse(keyo.ToString(), out var key))
            {
                if (propertyValues.TryGetValue("udi", out var udio) && udio is string udis && GuidUdi.TryParse(udis, out var udi))
                {
                    key = udi.Guid;
                }
                else
                {
                    key = Guid.Empty;
                }
            }

            IPublishedElement element = new PublishedElement(publishedContentType, key, propertyValues, preview, referenceCacheLevel, _publishedSnapshotAccessor);
            element = _publishedModelFactory.CreateModel(element);
            return element;
        }
    }
}

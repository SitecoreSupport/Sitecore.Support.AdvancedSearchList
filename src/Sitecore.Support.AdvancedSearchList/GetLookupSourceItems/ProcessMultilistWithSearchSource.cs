
namespace Sitecore.Support.Pipelines.GetLookupSourceItems
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Sitecore.Collections;
  using Sitecore.Configuration;
  using Sitecore.ContentSearch;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.ContentSearch.SearchTypes;
  using Sitecore.ContentSearch.Security;
  using Sitecore.ContentSearch.Utilities;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Pipelines.GetLookupSourceItems;
  using Sitecore.SecurityModel;
  using Sitecore.Support.ASL.Datasource;

  public class ProcessMultilistWithSearchSource
  {
    private const string IndexPrefix = "index:";

    public void Process(GetLookupSourceItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      if (!args.Source.StartsWith(IndexPrefix, StringComparison.InvariantCulture))
      {
        return;
      }

      var sourceValue = args.Source.Substring(IndexPrefix.Length);

      var sourceParser = new MlsSourceToSearchStringModel();
      
      string indexName; 
      ID rootItemId;
      var model = sourceParser.BuildSearchStringModel(args.Item, sourceValue, out indexName, out rootItemId);

      var index = this.ResolveIndex(indexName, rootItemId);


      using (IProviderSearchContext context = index.CreateSearchContext(SearchSecurityOptions.Default))
      {
        var source = LinqHelper.CreateQuery<SitecoreUISearchResultItem>(context, model) as IEnumerable<SitecoreUISearchResultItem>;

        if (Settings.GetBoolSetting("ContentSearch.EnableSearchDebug", false))
        {
          var sourceArray = source.ToArray();
          if (sourceArray.Length > 100)
          {
            SearchLog.Log.Info("SUPPORT Index returns more than 100 documents for the current data source.");
          }

          source = sourceArray;
        }

        // TODO Check if latest version is applied and only items are applied
        var uniqueItems = this.OptimizedDistinctWithKeptOrder(source).ToArray();
        args.Result.AddRange(uniqueItems);
        args.AbortPipeline();
      }
    }

    protected virtual ISearchIndex ResolveIndex(string indexName, ID rootItemID)
    {
      if (!string.IsNullOrWhiteSpace(indexName))
      {
        return ContentSearchManager.GetIndex(indexName);
      }

      using (new SecurityDisabler())
      {
        var item = Context.ContentDatabase.GetItem(rootItemID);
        return ContentSearchManager.GetIndex((SitecoreIndexableItem)item);
      }
    }

    protected IEnumerable<Item> OptimizedDistinctWithKeptOrder(IEnumerable<SitecoreUISearchResultItem> enumerable)
    {
      Assert.ArgumentNotNull(enumerable, "enumerable");
      var proccessedElements = new Set<string>();

      foreach (var element in enumerable)
      {
        if (element == null || proccessedElements.Contains(element.Id))
        {
          continue;
        }

        proccessedElements.Add(element.Id);

        // Applying security
        var item = element.GetItem();

        if (item == null)
        {
          continue;
        }

        yield return item;
      }
    }


    protected class MlsSourceToSearchStringModel : MlsDataSource
    {
      public List<SearchStringModel> BuildSearchStringModel (Item dataItem, string sourceValue, out string indexName, out ID rootItemId)
      {
        Assert.ArgumentNotNull(dataItem, "dataItem");
        Assert.ArgumentNotNull(sourceValue, "sourceValue");

        var dsParams = this.BuildNameValueCollection(sourceValue);
        return this.ExtractFilters(dsParams, q => dataItem.Axes.SelectSingleItem(q), out rootItemId, out indexName);
      }
    }
  }
}

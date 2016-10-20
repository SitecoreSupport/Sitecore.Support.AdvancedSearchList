
namespace Sitecore.Support.ASL.Datasource
{
  using System;
  using System.Collections.Generic;
  using System.Collections.Specialized;
  using System.Linq;
  using System.Web;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.ContentSearch.Utilities;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;

  public class MlsDataSource : IDataSourceReader
  {
    const string SortAsc = "asc";
    const string SortDesc = "desc";
    const string FastQueryPrefix = "fast:";
    const string QueryPrefix = "query:";

    readonly static string[] Separator = new[] { "|" };
    readonly static string[] SubSeparator = new[] { ":" };

    public string GetDatasource(Item dataItem, TemplateFieldItem fieldItem, out string indexName, out ID rootItemId)
    {
      Assert.ArgumentNotNull(dataItem, "dataItem");
      Assert.ArgumentNotNull(fieldItem, "fieldName");

      var dsParams = this.BuildNameValueCollection(fieldItem.Source);

      var filters = this.ExtractFilters(dsParams, q => dataItem.Axes.SelectSingleItem(q), out rootItemId, out indexName);

      return this.ComposeString(filters);
    }

    public bool IsUsed(Item dataItem, TemplateFieldItem fieldItem)
    {
      var value = fieldItem.Source;
      return !string.IsNullOrWhiteSpace(value) && value.Contains("=");
    }

    protected virtual string ComposeString(List<SearchStringModel> filters)
    {
      Assert.ArgumentNotNull(filters, "filters");
      return string.Join(";", filters);
    }

    protected virtual NameValueCollection BuildNameValueCollection(string source)
    {
      Assert.ArgumentNotNull(source, "source");
      return StringUtil.GetNameValues(source, '=', '&');
    }

    protected virtual List<SearchStringModel> ExtractFilters(NameValueCollection values, Func<string, Item> resolveItemByQuery, out ID location, out string indexName)
    {
      // Supported parameters:
      /* 
         StartSearchLocation
         Language      
         SortField
         IndexName
         TemplateFilter
         FullTextQuery
         TemplateFilter 
         Filter
      */

      Assert.ArgumentNotNull(values, "values");

      location = null;
      indexName = null;

      var parsedParams = new List<SearchStringModel>();

      foreach (string str in values.AllKeys)
      {
        values[str] = HttpUtility.JavaScriptStringEncode(values[str]);
      }

      // Parse StartSearchLocation
      string locationParam = values["StartSearchLocation"];
      if (!string.IsNullOrWhiteSpace(locationParam))
      {
        location = this.PrepareLocationFilter(resolveItemByQuery, locationParam);
        parsedParams.Add(new SearchStringModel("location", location.ToString(), "must"));
      }
      
      // Parse FullTextQuery
      string fullTextPapam = values["FullTextQuery"];
      if (!string.IsNullOrWhiteSpace(fullTextPapam))
      {
        // TODO Check if ecaping is required
        parsedParams.Add(new SearchStringModel("text", fullTextPapam.Trim(), "must"));
      }

      // Parse TemplateFilter
      string templatesParam = values["TemplateFilter"];
      if (!string.IsNullOrWhiteSpace(templatesParam))
      {
        var templateIDs = templatesParam.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        var validtemplateIDs = templateIDs.Where(ID.IsID).ToArray();
        if (validtemplateIDs.Length > 0)
        {   
          foreach (var validtemplateID in validtemplateIDs)
          {
            parsedParams.Add(new SearchStringModel("template", validtemplateID, "should"));
          }
        }

        if (templateIDs.Length != validtemplateIDs.Length)
        {
          CrawlingLog.Log.Warn(string.Format("ASL Some specified IDs are incorrect '{0}'... Parameter will be skipped", templatesParam));
        }
      }

      // Parse Language
      string languageParam = values["Language"];
      if (!string.IsNullOrWhiteSpace(languageParam))
      {
        parsedParams.Add(new SearchStringModel("language", languageParam.Trim(), "must"));
      }

      // Parse Sort
      string sortParam = values["SortField"];
      bool isAsc;
      string fieldName;
      if (!string.IsNullOrWhiteSpace(sortParam) && this.TryParseSortParam(sortParam, out isAsc, out fieldName))
      {
        parsedParams.Add(new SearchStringModel("sort", fieldName, isAsc ? SortAsc : SortDesc));
      }

      // Parse Filter
      string filterParam = values["Filter"];
      if (filterParam != null)
      {
        this.ParseAndAppendFilterValues(filterParam, parsedParams);
      }

      // Parse IndexName
      indexName = values["IndexName"];
      if (!string.IsNullOrWhiteSpace(indexName))
      {
        indexName = indexName.Trim().ToLower();
        location = null;
      }

      return parsedParams;
    }

    protected void ParseAndAppendFilterValues(string filterValue, List<SearchStringModel> parsedParams)
    {
      Assert.ArgumentNotNull(filterValue, "filterValue");
      Assert.ArgumentNotNull(parsedParams, "parsedParams");

      if (filterValue.Length == 0)
      {
        return;
      }

      var clauses = filterValue.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

      foreach (string clause in clauses)
      {
        var clauseParts = clause.Split(SubSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (clauseParts.Length != 2)
        {
          CrawlingLog.Log.Warn(string.Format("ASL Can't parse clause '{0}' of Filter parameter", clause));
          continue;
        }

        var value = clauseParts[1].Trim();
        if (value.Length < 1)
        {
          value = null;
        }

        var opAndField = clauseParts[0].Trim();
        var field = (opAndField[0] == '-' || opAndField[0] == '+') ? opAndField.Substring(1) : opAndField;
        field = field.Trim();

        var operation = (opAndField[0] == '+') ? "must" : opAndField[0] == '-' ? "not" : "should";

        parsedParams.Add(new SearchStringModel("custom", string.Format("{0}|{1}", field, value), operation));
      }
    }

    protected bool TryParseSortParam(string sortValue, out bool isAsc, out string fieldName)
    {
      // _name[desc]
      sortValue = sortValue.Trim();
      var startIndex = sortValue.IndexOf("[", StringComparison.InvariantCulture);
      var endIndex = sortValue.IndexOf("]", StringComparison.InvariantCulture);


      if (startIndex == 0)
      {
        Log.Warn(string.Format("ASL Can't find a field name... Value:'{0}'", sortValue), this);
        isAsc = true;
        fieldName = null;
        return false;
      }

      if (startIndex * endIndex < 0 || endIndex <= startIndex)
      {
        Log.Warn(string.Format("ASL Sort order has an invalid format... Value:'{0}'", sortValue), this);
        isAsc = true;
        fieldName = null;
        return false;
      }

      // Parse field name
      // TODO Check this
      fieldName = sortValue.Substring(0, startIndex).Trim();
      if (string.IsNullOrEmpty(fieldName))
      {
        Log.Warn(string.Format("ASL Can't find a field name... Value:'{0}'", sortValue), this);
        isAsc = true;
        fieldName = null;
        return false;
      }

      // Parse sort order
      // Handles both cases brackets are empty or not present
      if (endIndex == startIndex)
      {
        isAsc = true;
      }
      else
      {
        bool? asc = null;

        // TODO Check this
        var orderStr = sortValue.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        if (orderStr.Equals(SortAsc, StringComparison.InvariantCultureIgnoreCase))
        {
          asc = true;
        }

        if (orderStr.Equals(SortDesc, StringComparison.InvariantCultureIgnoreCase))
        {
          asc = false;
        }

        if (asc == null)
        {
          Log.Warn(string.Format("ASL sort direction is an invalid value... Value:'{0}'", sortValue), this);
          asc = true;
        }

        if (endIndex != sortValue.Length - 1)
        {
          Log.Warn(string.Format("ASL Sort order has an invalid format... Value:'{0}'", sortValue), this);
        }

        isAsc = asc.Value;
      }

      return true;
    }

    protected ID PrepareLocationFilter(Func<string, Item> resolveItemByQuery, string locationFilter)
    {
      if (string.IsNullOrWhiteSpace(locationFilter))
      {
        return ItemIDs.RootID;
      }

      ID id;
      if (ID.TryParse(locationFilter, out id))
      {
        return id;
      }

      if (locationFilter.StartsWith(QueryPrefix))
      {
        locationFilter = locationFilter.Replace("->", "=");
        string query = locationFilter.Substring(QueryPrefix.Length);
        if (query.StartsWith(FastQueryPrefix))
        {
          Log.Warn("ASL The field doesn't support fast queries. Sitecore query will be used instead...", this);
          query = query.Substring(FastQueryPrefix.Length);
        }

        var resItem = resolveItemByQuery(query);
        return resItem.ID;
      }

      Log.Warn("ASL The field can't parse location filter. Root item will be used intead.", this);
      return ItemIDs.RootID;
    }
  }
}

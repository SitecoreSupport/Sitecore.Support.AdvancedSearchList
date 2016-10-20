
namespace Sitecore.Support.ASL.Datasource
{
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.StringExtensions;

  public class PersistentBucketFilterField : IDataSourceReader
  {
    public const string SourcePrefix = "UseFilterFieldAsSource";

    public string GetDatasource(Item dataItem, TemplateFieldItem fieldItem, out string indexName, out ID rootItemId)
    {
      Assert.ArgumentNotNull(dataItem, "dataItem");
      Assert.ArgumentNotNull(fieldItem, "fieldName");

      this.ParseSourceValue(fieldItem.Source, out indexName, out rootItemId);

      return this.GetFilterFieldValue(fieldItem);
    }

    public bool IsUsed(Item dataItem, TemplateFieldItem fieldItem)
    {
      Assert.ArgumentNotNull(dataItem, "dataItem");
      Assert.ArgumentNotNull(fieldItem, "fieldName");
      var rawSource = fieldItem.Source;

      if (string.IsNullOrWhiteSpace(rawSource))
      {
        return false;
      }

      return rawSource.TrimStart().StartsWith(SourcePrefix);
    }

    protected virtual void ParseSourceValue(string rawValue, out string indexName, out ID rootItemId)
    {
      indexName = null;
      rootItemId = null;

      rawValue = rawValue.Trim();

      //SourcePrefix.Length + 1 since ':' must be set after the prefix
      if (rawValue.IsNullOrEmpty() || rawValue.Length <= PersistentBucketFilterField.SourcePrefix.Length + 1)
      {
        return;
      }

      var value = rawValue.Substring(PersistentBucketFilterField.SourcePrefix.Length + 1).TrimStart();

      if (value.IsNullOrEmpty())
      {
        return;
      }

      if (ID.TryParse(value, out rootItemId))
      {
        return;
      }

      indexName = value.ToLowerInvariant();
      rootItemId = null;
    }

    private string GetFilterFieldValue(TemplateFieldItem fieldItem)
    {
      var field = fieldItem.InnerItem.Fields[Sitecore.Buckets.Util.Constants.DefaultFilter];
      return string.IsNullOrWhiteSpace(field.Value) ? null : field.Value;
    }
  }
}

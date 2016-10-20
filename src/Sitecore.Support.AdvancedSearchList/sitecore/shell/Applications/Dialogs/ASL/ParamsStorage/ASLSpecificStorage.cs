
namespace Sitecore.Support.ASL.ParamsStorage
{
  using Sitecore.Data;

  public class ASLSpecificStorage : AbstractWrappedStorage
  {
    public static readonly string ValueParam = "va";
    public static readonly string FieldIdParam = "fld";
    public static readonly string IndexNameParam = "idx";
    public static readonly string IndexResolveItemParam = "rid";
    public static readonly string FiltersParam = "flt";

    public static ASLSpecificStorage GetStorage()
    {
      return UrlHandleStorage.GetWrappedStorage(v => new ASLSpecificStorage(v));
    }

    protected ASLSpecificStorage(UrlHandleStorage innerStorage) : base(innerStorage)
    {
    }

    public string Value
    {
      get
      {
        return this.InnerStorage[ValueParam];
      }

      set
      {
        this.InnerStorage[ValueParam] = value ?? string.Empty;
      }
    }

    public ID FieldId
    {
      get
      {
        var strId = this.InnerStorage[FieldIdParam];
        return string.IsNullOrWhiteSpace(strId) ? null : ID.Parse(strId);
      }

      set
      {
        this.InnerStorage[FieldIdParam] = value == (ID)null ? string.Empty : value.ToString();
      }
    }

    public ID IndexResolveItem
    {
      get
      {
        var strId = this.InnerStorage[IndexResolveItemParam];
        return string.IsNullOrWhiteSpace(strId) ? null : ID.Parse(strId);
      }

      set
      {
        this.InnerStorage[IndexResolveItemParam] = value == (ID)null ? string.Empty : value.ToString();
      }
    }

    public string IndexName
    {
      get
      {
        return this.InnerStorage[IndexNameParam];
      }

      set
      {
        this.InnerStorage[IndexNameParam] = value ?? string.Empty;
      }
    }

    public string Filters
    {
      get
      {
        return this.InnerStorage[FiltersParam];
      }

      set
      {
        this.InnerStorage[FiltersParam] = value ?? string.Empty;
      }
    }
  }
}

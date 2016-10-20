
namespace Sitecore.Support.ASL.ParamsStorage
{
  using System;
  using System.Collections.Specialized;
  using System.Web;
  using Sitecore.Diagnostics;
  using Sitecore.Text;
  using Sitecore.Web;

  public class UrlHandleStorage
  {
    public static readonly string HandleName = "aslhdl";

    private UrlHandle handle;

    protected UrlHandleStorage(UrlHandle handle)
    {
      Assert.IsNotNull(handle, "handle");
      this.handle = handle;
    }

    public void Flush(ref UrlString url)
    {
      this.handle.Add(url, UrlHandleStorage.HandleName);
    }

    public void Clear()
    {
      UrlHandle.DisposeHandle(this.handle);
    }

    public string this[string key]
    {
      get
      {
        return this.handle[key];
      }

      set
      {
        this.handle[key] = value;
      }
    }

    public static T GetWrappedStorage<T>(Func<UrlHandleStorage, T> createMethod) where T : AbstractWrappedStorage
    {
      return createMethod(UrlHandleStorage.GetStorage());
    }

    public static UrlHandleStorage GetStorage()
    {
      var handleId = ExtractHandleId(HttpContext.Current.Request);
      
      if (handleId == null)
      {
        return new UrlHandleStorage(new UrlHandle());
      }

      var qParam = new NameValueCollection
      {
        { UrlHandleStorage.HandleName, handleId }
      };

      var urlHandle = UrlHandle.Get(new UrlString(qParam), UrlHandleStorage.HandleName);

      return new UrlHandleStorage(urlHandle);
    }

    private static string ExtractHandleId(HttpRequest r)
    {
      Assert.ArgumentNotNull(r, "r");

      var handleValue = WebUtil.ExtractUrlParm(UrlHandleStorage.HandleName, r.Url.Query);
      if (string.IsNullOrWhiteSpace(handleValue) && r.UrlReferrer != null)
      {
        handleValue = WebUtil.ExtractUrlParm(UrlHandleStorage.HandleName, r.UrlReferrer.Query);
      }

      return string.IsNullOrWhiteSpace(handleValue) ? null : handleValue;
    }
  }
}

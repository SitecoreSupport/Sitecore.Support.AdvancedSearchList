
namespace Sitecore.Support.ASL.ParamsStorage
{
  using Sitecore.Diagnostics;

  public abstract class AbstractWrappedStorage
  {
    protected AbstractWrappedStorage(UrlHandleStorage innerStorage)
    {
      Assert.ArgumentNotNull(innerStorage, "innerStorage");
      this.InnerStorage = innerStorage;
    }

    public UrlHandleStorage InnerStorage { get; protected set; }
  }
}

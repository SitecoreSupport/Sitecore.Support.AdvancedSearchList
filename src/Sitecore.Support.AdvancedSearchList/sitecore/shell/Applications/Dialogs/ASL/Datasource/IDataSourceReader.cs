namespace Sitecore.Support.ASL.Datasource
{
  using Sitecore.Data;
  using Sitecore.Data.Items;

  public interface IDataSourceReader
  {
    /* Result examples:
        text:panda;custom:_name|home
        +custom:_name|home;template:{76036f5e-cbce-46d1-af0a-4143f9b557aa}
        sort:_name sort:_name[desc]
        +custom:_name|home;-custom:_name|home;custom:_name|home
    */
    
    string GetDatasource(Item dataItem, TemplateFieldItem fieldItem, out string indexName, out ID rootItemID);

    bool IsUsed(Item dataItem, TemplateFieldItem fieldItem);
  }
}

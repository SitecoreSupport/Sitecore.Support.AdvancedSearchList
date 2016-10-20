<%@ Page Language="C#" MasterPageFile="~/sitecore/shell/Applications/Buckets/ItemBucketsSearchResult.Master" AutoEventWireup="true" CodeBehind="ASLSelectItems.aspx.cs" Inherits="Sitecore.Support.ASL.ASLSelectItems" %>


<%@ Register TagPrefix="sc" TagName="BucketSearchUI" Src="./BucketSearchUI.ascx" %>
<%@ OutputCache Location="None" VaryByParam="none" %>
<asp:Content ContentPlaceHolderID="head" runat="server">
<%--TODO: Disabled javascript include--%>
    <script type="text/javascript"  src="./ASL/AdvancedSearchList.js"></script>
    <!-- [if IE] 
<style type="text/css">
    

    body {
        overflow: scroll;
    }

    
</style>
        -->
</asp:Content>

<asp:Content ContentPlaceHolderID="body" runat="server">
    <sc:BucketSearchUI BucketsView="DataSource" runat="server" />
</asp:Content>
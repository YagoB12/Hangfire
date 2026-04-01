namespace PdfServer.Data.Entities;

public class SalesOrderDetail
{
    public int SalesOrderID { get; set; }
    public int SalesOrderDetailID { get; set; }
    public int ProductID { get; set; }
    public short OrderQty { get; set; }
    public decimal UnitPrice { get; set; }

    public SalesOrderHeader? Header { get; set; }
}

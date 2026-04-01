namespace PdfServer.Data.Entities;

public class SalesOrderHeader
{
    public int SalesOrderID { get; set; }
    public int? CustomerID { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalDue { get; set; }

    public ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
}

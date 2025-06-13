using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Data;
using System.IO;
using System.Globalization;

namespace WebMi
{

    public class Globo 
    {

        public string GetSalesByDay(string startDate, string endDate, string fileName) 
        {

            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime fechaInicial = DateTime.ParseExact(startDate, "yyyy-MM-dd", provider);
                DateTime fechaFinal = DateTime.ParseExact(endDate, "yyyy-MM-dd", provider);
                DateTime fechaDeProceso = fechaInicial;

                while (true)
                {
                    string result = GetVentas(fechaDeProceso.ToString("yyyy-MM-dd"), fechaDeProceso.ToString("yyyy-MM-dd"), fileName).GetAwaiter().GetResult();
                    fechaDeProceso = fechaDeProceso.AddDays(1);

                    if (fechaDeProceso > fechaFinal) break;

                }

                return "Ok.";

            }
            catch (Exception e)
            {
                return "Error: " + e.ToString();
            }

        }

        public async Task<string> GetVentas(string StartDate, string FinalDate, string filename)
        {

            try
            {
                MemoryDb mdb = new MemoryDb();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("CREATE TABLE IF NOT EXISTS sales (");
                sb.AppendLine("PartitionKey, RowKey, rfc, guid, dispositivo, venta, cliente, email, vendedor, fechaEmision,");
                sb.AppendLine("vencimiento, horaEmision, tipoDeCambio, serieDocumento, folio, estacion, usuario, usufecha,");
                sb.AppendLine("usuhora, total, formaDePago, latitud, longitud, nombreDispositivo, sucursal, cancelado, areadenegocio,");
                sb.AppendLine("importe, impuesto, estado, tipo_doc, formaPago1, formaPago2, formaPago3, Pago1, Pago2, Pago3, id, sku, idpartida, descripcion,");
                sb.AppendLine("precio, cantidad, fecha, hora, unidad, descuento, costoPromedio, costoUltimo, sku2, descripcion2, cantidad2, cuentaPredial, iva, ieps,");
                sb.AppendLine("impuesto1, impuesto_ds, bonificacion, vdescuento, porcentajeImpuesto, preciobase, nombreImpuesto, iepslitro, corte, usuarionombre, moneda, tipodescuento, claveDescuento, lista, tipoDesc,");
                sb.AppendLine("PRIMARY KEY (PartitionKey, RowKey) )");
                string result = mdb.SQLExec(sb.ToString());

                if (result.StartsWith("Error:"))
                {
                    return result;
                }

                AzureTableStorage<VentasGlobo> vg = new AzureTableStorage<VentasGlobo>("DefaultEndpointsProtocol=https;AccountName=elglobo;AccountKey=2saDHyJXFovbCoRqmvVzhLT9s+KFYe9FAPhnEsEJdLntQh4VxquU5A3sFlLfIrVH4KiD4X/43vOonpLoCwRWXA==", "ventasGlobo");
                TableQuery<VentasGlobo> query = new TableQuery<VentasGlobo>().Where($"PartitionKey ge '{StartDate}' and PartitionKey le '{FinalDate}'");

                List<VentasGlobo> l = await vg.GetMany(query);

                foreach (VentasGlobo item in l)
                {
                    DataTable tData = mdb.SQLTable($"SELECT RowKey FROM sales WHERE PartitionKey = '{item.PartitionKey}' AND RowKey = '{item.RowKey}'");

                    mdb.Reset();

                    if (tData.Rows.Count == 0)
                    {
                        mdb.CreateInsert("sales");
                    }
                    else
                    {
                        mdb.CreateUpdate("sales", $"PartitionKey = '{item.PartitionKey}' AND RowKey = '{item.RowKey}'");
                    }

                    mdb.AddField("PartitionKey", item.PartitionKey);
                    mdb.AddField("RowKey", item.RowKey);
                    mdb.AddField("rfc", item.properties["rfc"].StringValue);
                    mdb.AddField("guid", item.properties["guid"].StringValue);
                    mdb.AddField("dispositivo", item.properties["dispositivo"].StringValue);
                    mdb.AddField("venta", item.properties["venta"].StringValue);
                    mdb.AddField("cliente", item.properties["cliente"].StringValue);
                    mdb.AddField("email", item.properties["email"].StringValue);
                    mdb.AddField("vendedor", item.properties["vendedor"].StringValue);
                    mdb.AddField("fechaEmision", item.properties["fechaEmision"].StringValue);
                    mdb.AddField("vencimiento", item.properties["vencimiento"].StringValue);
                    mdb.AddField("horaEmision", item.properties["horaEmision"].StringValue);
                    mdb.AddField("tipoDeCambio", item.properties["tipoDeCambio"].StringValue);
                    mdb.AddField("serieDocumento", item.properties["serieDocumento"].StringValue);
                    mdb.AddField("folio", item.properties["folio"].StringValue);
                    mdb.AddField("estacion", item.properties["estacion"].StringValue);
                    mdb.AddField("usuario", item.properties["usuario"].StringValue);
                    mdb.AddField("usufecha", item.properties["usufecha"].StringValue);
                    mdb.AddField("usuhora", item.properties["usuhora"].StringValue);
                    mdb.AddField("total", item.properties["total"].StringValue);
                    mdb.AddField("formaDePago", item.properties["formaDePago"].StringValue);
                    mdb.AddField("latitud", item.properties["latitud"].StringValue);
                    mdb.AddField("longitud", item.properties["longitud"].StringValue);
                    mdb.AddField("nombreDispositivo", item.properties["nombreDispositivo"].StringValue);
                    mdb.AddField("sucursal", item.properties["sucursal"].StringValue);
                    mdb.AddField("cancelado", item.properties["cancelado"].StringValue);
                    mdb.AddField("areadenegocio", item.properties["areadenegocio"].StringValue);
                    mdb.AddField("importe", item.properties["importe"].StringValue);
                    mdb.AddField("impuesto", item.properties["impuesto"].StringValue);
                    mdb.AddField("estado", item.properties["estado"].StringValue);
                    mdb.AddField("tipo_doc", item.properties["tipo_doc"].StringValue);
                    mdb.AddField("formaPago1", item.properties["formaPago1"].StringValue);
                    mdb.AddField("formaPago2", item.properties["formaPago2"].StringValue);
                    mdb.AddField("formaPago3", item.properties["formaPago3"].StringValue);
                    mdb.AddField("Pago1", item.properties["Pago1"].StringValue);
                    mdb.AddField("Pago2", item.properties["Pago2"].StringValue);
                    mdb.AddField("Pago3", item.properties["Pago3"].StringValue);
                    mdb.AddField("id", item.properties["id"].StringValue);
                    mdb.AddField("sku", item.properties["sku"].StringValue);
                    mdb.AddField("idpartida", item.properties["idpartida"].StringValue);
                    mdb.AddField("descripcion", item.properties["descripcion"].StringValue);
                    mdb.AddField("precio", item.properties["precio"].StringValue);
                    mdb.AddField("cantidad", item.properties["cantidad"].StringValue);
                    mdb.AddField("fecha", item.properties["fecha"].StringValue);
                    mdb.AddField("hora", item.properties["hora"].StringValue);
                    mdb.AddField("unidad", item.properties["unidad"].StringValue);
                    mdb.AddField("descuento", item.properties["descuento"].StringValue);
                    mdb.AddField("costoPromedio", item.properties["costoPromedio"].StringValue);
                    mdb.AddField("costoUltimo", item.properties["costoUltimo"].StringValue);
                    mdb.AddField("sku2", item.properties["sku2"].StringValue);
                    mdb.AddField("descripcion2", item.properties["descripcion2"].StringValue);
                    mdb.AddField("cantidad2", item.properties["cantidad2"].StringValue);
                    mdb.AddField("cuentaPredial", item.properties["cuentaPredial"].StringValue);
                    mdb.AddField("iva", item.properties["iva"].StringValue);
                    mdb.AddField("ieps", item.properties["ieps"].StringValue);
                    mdb.AddField("impuesto1", item.properties["impuesto1"].StringValue);
                    mdb.AddField("impuesto_ds", item.properties["impuesto_ds"].StringValue);
                    mdb.AddField("bonificacion", item.properties["bonificacion"].StringValue);
                    mdb.AddField("vdescuento", item.properties["vdescuento"].StringValue);
                    mdb.AddField("porcentajeImpuesto", item.properties["porcentajeImpuesto"].StringValue);
                    mdb.AddField("preciobase", item.properties["preciobase"].StringValue);
                    mdb.AddField("nombreImpuesto", item.properties["nombreImpuesto"].StringValue);
                    mdb.AddField("iepslitro", item.properties["iepslitro"].StringValue);
                    mdb.AddField("corte", item.properties["corte"].StringValue);
                    mdb.AddField("usuarionombre", item.properties["usuarionombre"].StringValue);
                    mdb.AddField("moneda", item.properties["moneda"].StringValue);
                    mdb.AddField("tipoDescuento", item.properties["tipoDescuento"].StringValue);
                    mdb.AddField("claveDescuento", item.properties["claveDescuento"].StringValue);
                    mdb.AddField("lista", item.properties["lista"].StringValue);

                    if (item.properties.ContainsKey("tipoDesc"))
                    {
                        mdb.AddField("tipoDesc", item.properties["tipoDesc"]);
                    }

                    result = mdb.Exec();

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                }

                sb.Clear();
                sb.AppendLine("SELECT ");
                sb.AppendLine("areadenegocio As Agencia_ID,");
                sb.AppendLine("cliente As Cliente,");
                sb.AppendLine("folio As Ticket,");
                sb.AppendLine("[Guid] AS GUID,");
                sb.AppendLine("sku As Product_ID,");
                sb.AppendLine("descripcion As Producto_DS,");
                sb.AppendLine("fechaEmision As FechaCal,");
                sb.AppendLine("usuhora As hora,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100)) As ventaPesos,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100)) * (1 + (round(impuesto,2) / 100)) As ventaBruta,");
                sb.AppendLine("round(total,4) As totalTicket,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) As ventaNeta,");
                sb.AppendLine("round(precio,4) As PrecioNeto,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100))  * (round(impuesto,2) / 100) As ImpuestoPesos,");
                sb.AppendLine("round(cantidad,4) As VentaUnidades,");
                sb.AppendLine("round(precio,4) * (1 + (round(impuesto,2) / 100)) As PrecioBruto,");
                sb.AppendLine("round(precioBase,4) - round(precio,4) As QuiebrePrecio,");
                sb.AppendLine("estado As Cancelada_FLG,");
                sb.AppendLine("tipo_doc As ventaTipo,");
                sb.AppendLine("bonificacion As Bonificacion,");
                sb.AppendLine("iepslitro As IEPSLitro,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * ((round(descuento,4) / 100))  * (1 + (round(impuesto,2) / 100)) As Descuento,");
                sb.AppendLine("round(impuesto,2) As Impuesto,");
                sb.AppendLine("formaPago1 As FormaDePago1,");
                sb.AppendLine("pago1 As Pago1,");
                sb.AppendLine("formaPago2 As FormaDePago2,");
                sb.AppendLine("pago2 As Pago2,");
                sb.AppendLine("formaPago3 As FormaDePago3,");
                sb.AppendLine("pago3 As Pago3,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100))  * Case When round(impuesto,2) =    16 Then 0.16 Else 0 End As IVA,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100))  * Case When round(impuesto,2) =     8 Then 0.08 Else 0 End As IEPS,");
                sb.AppendLine("round(precio,4) * round(cantidad,4) * (1 - (round(descuento,4) / 100))  * Case When round(impuesto,2) = 25.28 Then 0.2528 Else 0 End As 'IVA-IEPS',");
                sb.AppendLine("clavedescuento As clavedescuento, ");
                sb.AppendLine("tipodescuento As tipodescuento");
                sb.AppendLine("FROM ");
                sb.AppendLine("sales");

                DataTable t = mdb.SQLTable(sb.ToString());
                string csv_file = filename;
                if (File.Exists(csv_file)) File.Delete(csv_file);

                WebMiTools.StringFunctions.ToCSV(t, csv_file);

                return ("Ok.");

            }
            catch (Exception e)
            {
                return $"Error: GetVentas {e.ToString()}";
            }

        }


        private class VentasGlobo : ITableEntity
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string ETag { get; set; }
            public IDictionary<string, EntityProperty> properties;

            public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
            {
                this.properties = properties;
            }

            public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
            {
                throw new NotImplementedException();
            }
        }

    }


}

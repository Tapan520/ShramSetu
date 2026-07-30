using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShramSetu.Core.Entities;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IGstInvoiceService
{
    Task<GstInvoice> GenerateForSubscriptionAsync(Guid subscriptionId, string gstin, string address, CancellationToken ct = default);
    byte[] GeneratePdf(GstInvoice invoice);
}

public class GstInvoiceService : IGstInvoiceService
{
    private readonly ApplicationDbContext _db;

    public GstInvoiceService(ApplicationDbContext db) => _db = db;

    public async Task<GstInvoice> GenerateForSubscriptionAsync(
        Guid subscriptionId, string gstin, string address, CancellationToken ct = default)
    {
        var sub = await _db.EmployerSubscriptions
            .Include(s => s.Employer)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new InvalidOperationException("Subscription not found.");

        var cgst = sub.AmountPaid * 0.09m;
        var sgst = sub.AmountPaid * 0.09m;

        var inv = new GstInvoice
        {
            Id                = Guid.NewGuid(),
            InvoiceNumber     = $"SS-INV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
            BilledToUserId    = sub.Employer.UserId,
            BilledToGstin     = gstin,
            BilledToName      = sub.Employer.Name,
            BilledToAddress   = address,
            SubscriptionId    = subscriptionId,
            BaseAmount        = sub.AmountPaid,
            CgstRate          = 9,
            SgstRate          = 9,
            CgstAmount        = cgst,
            SgstAmount        = sgst,
            TotalAmount       = sub.AmountPaid + cgst + sgst,
            Status            = GstInvoiceStatus.Issued,
            IssuedAt          = DateTime.UtcNow
        };

        _db.GstInvoices.Add(inv);
        await _db.SaveChangesAsync(ct);
        return inv;
    }

    public byte[] GeneratePdf(GstInvoice inv)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(c => c.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("ShramSetu").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    col.Item().Text("GST Invoice").FontSize(13).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(140).Column(col =>
                {
                    col.Item().AlignRight().Text($"Invoice #").Bold();
                    col.Item().AlignRight().Text(inv.InvoiceNumber).FontSize(10);
                    col.Item().AlignRight().Text(inv.IssuedAt.ToString("dd MMM yyyy")).FontSize(10);
                });
            });

            page.Content().PaddingTop(20).Column(col =>
            {
                col.Item().Text("Bill To:").Bold();
                col.Item().Text(inv.BilledToName);
                col.Item().Text(inv.BilledToAddress).FontColor(Colors.Grey.Medium);
                if (!string.IsNullOrEmpty(inv.BilledToGstin))
                    col.Item().Text($"GSTIN: {inv.BilledToGstin}");

                col.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                    table.Header(h =>
                    {
                        foreach (var head in new[] { "Description", "Base Amount", "CGST (9%)", "SGST (9%)" })
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text(head).Bold();
                    });
                    table.Cell().Padding(6).Text("ShramSetu Subscription Plan");
                    table.Cell().Padding(6).Text($"₹{inv.BaseAmount:N2}");
                    table.Cell().Padding(6).Text($"₹{inv.CgstAmount:N2}");
                    table.Cell().Padding(6).Text($"₹{inv.SgstAmount:N2}");
                });

                col.Item().PaddingTop(12).AlignRight()
                   .Text($"Total (incl. GST): ₹{inv.TotalAmount:N2}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
            });

            page.Footer().AlignCenter()
                .Text("This is a computer generated invoice. ShramSetu GSTIN: 27AAAAA0000A1Z5")
                .FontColor(Colors.Grey.Medium).FontSize(9);
        })).GeneratePdf();
    }
}

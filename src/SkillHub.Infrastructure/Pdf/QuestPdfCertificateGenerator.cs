using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkillHub.Application.DTOs.Reports;
using SkillHub.Application.Interfaces;

namespace SkillHub.Infrastructure.Pdf;

public class QuestPdfCertificateGenerator : IPdfGenerator
{
    static QuestPdfCertificateGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCertificate(CertificateDataDto data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                // Header
                page.Header().BorderBottom(2).BorderColor(Colors.Blue.Darken2).PaddingBottom(15).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("SkillHub").FontSize(28).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text("Centro de Formación y Talleres Prácticos").FontSize(11).FontColor(Colors.Grey.Medium);
                    });

                    row.ConstantItem(140).AlignRight().Column(col =>
                    {
                        col.Item().Text("DOCUMENTO OFICIAL").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Ref: {data.EnrollmentId.ToString()[..8].ToUpperInvariant()}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });

                // Body Content
                page.Content().PaddingVertical(25).Column(col =>
                {
                    col.Spacing(18);

                    // Certificate Title (Required verbatim in Spanish)
                    col.Item().AlignCenter().Text("Constancia de inscripción")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);

                    col.Item().Text(text =>
                    {
                        text.Span("Por medio de la presente, SkillHub hace constar que el participante ");
                        text.Span(data.ParticipantName).Bold().FontColor(Colors.Blue.Darken3);
                        text.Span(" (correo: ");
                        text.Span(data.ParticipantEmail).Bold();
                        text.Span(") se encuentra debidamente inscrito y en estado activo para el taller detallado a continuación:");
                    });

                    // Workshop details card
                    col.Item().Background(Colors.Grey.Lighten4).Padding(16).Column(card =>
                    {
                        card.Spacing(8);

                        card.Item().Row(r =>
                        {
                            r.ConstantItem(130).Text("Código del Taller:").Bold();
                            r.RelativeItem().Text(data.WorkshopCode).Bold().FontColor(Colors.Blue.Darken2);
                        });

                        card.Item().Row(r =>
                        {
                            r.ConstantItem(130).Text("Título del Taller:").Bold();
                            r.RelativeItem().Text(data.WorkshopTitle).Bold();
                        });

                        card.Item().Row(r =>
                        {
                            r.ConstantItem(130).Text("Instructor:").Bold();
                            r.RelativeItem().Text(data.InstructorName);
                        });

                        card.Item().Row(r =>
                        {
                            r.ConstantItem(130).Text("Fecha y Horario:").Bold();
                            r.RelativeItem().Text($"{data.WorkshopStart:dd/MM/yyyy HH:mm} hasta {data.WorkshopEnd:dd/MM/yyyy HH:mm} UTC");
                        });

                        card.Item().Row(r =>
                        {
                            r.ConstantItem(130).Text("Fecha de Inscripción:").Bold();
                            r.RelativeItem().Text($"{data.EnrolledAt:dd/MM/yyyy HH:mm} UTC");
                        });
                    });

                    // Footer notes
                    col.Item().PaddingTop(20).Text("Esta constancia certifica la reserva de cupo activo en los registros institucionales de SkillHub.")
                        .FontSize(10)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });

                // Footer
                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text($"Fecha de generación: {data.GeneratedAt:dd/MM/yyyy HH:mm:ss} UTC")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);

                    row.RelativeItem().AlignRight().Text("SkillHub Platform © 2026")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }
}

using ClosedXML.Excel;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Infrastructure.Excel;

public class ClosedXmlEnrollmentReader : IExcelReader
{
    public IReadOnlyList<ExcelEnrollmentRowDto> ReadEnrollmentRows(Stream excelStream)
    {
        var rows = new List<ExcelEnrollmentRowDto>();

        using var workbook = new XLWorkbook(excelStream);

        // Find the "Enrollments" worksheet, or fallback to the first worksheet
        IXLWorksheet? worksheet = null;
        if (workbook.Worksheets.Contains("Enrollments"))
        {
            worksheet = workbook.Worksheet("Enrollments");
        }
        else
        {
            worksheet = workbook.Worksheets.FirstOrDefault();
        }

        if (worksheet is null)
            throw new DomainException("The uploaded Excel workbook contains no worksheets.");

        var usedRows = worksheet.RowsUsed().ToList();
        if (usedRows.Count <= 1)
            return rows; // Empty or header only

        // Find column indexes for ParticipantEmail and WorkshopCode
        var headerRow = usedRows.First();
        int emailCol = -1;
        int codeCol = -1;

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim().ToLowerInvariant();
            if (header is "participantemail" or "participant email" or "email")
            {
                emailCol = cell.Address.ColumnNumber;
            }
            else if (header is "workshopcode" or "workshop code" or "code")
            {
                codeCol = cell.Address.ColumnNumber;
            }
        }

        // If headers weren't named, default to column 1 and column 2
        if (emailCol == -1) emailCol = 1;
        if (codeCol == -1) codeCol = 2;

        // Iterate data rows (starting from row 2)
        foreach (var row in usedRows.Skip(1))
        {
            var emailVal = row.Cell(emailCol).GetString().Trim();
            var codeVal = row.Cell(codeCol).GetString().Trim();

            // Skip completely empty rows
            if (string.IsNullOrWhiteSpace(emailVal) && string.IsNullOrWhiteSpace(codeVal))
                continue;

            rows.Add(new ExcelEnrollmentRowDto
            {
                RowNumber = row.RowNumber(),
                ParticipantEmail = emailVal,
                WorkshopCode = codeVal
            });
        }

        return rows;
    }
}

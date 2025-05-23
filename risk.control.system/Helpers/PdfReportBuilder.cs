using Gehtsoft.PDFFlow.Builder;
using Gehtsoft.PDFFlow.Models.Enumerations;
using Gehtsoft.PDFFlow.Models.Shared;
using Gehtsoft.PDFFlow.UserUtils;
using Gehtsoft.PDFFlow.Utils;
using risk.control.system.Models.ViewModel;
using System.Globalization;

namespace risk.control.system.Helpers
{
    public class PdfReportBuilder
    {
        public IdData PhotoIdData { get; internal set; }
        public IdData AgentIdData { get; internal set; }
        public IdData PanData { get; internal set; }
        public List<string> WhatsNextData { get; internal set; }
        public DetailedReport DetailedReport { get; internal set; }
        public AgencyDetailData AgencyDetailData { get; internal set; }

        internal static readonly CultureInfo DocumentLocale
            = new CultureInfo("en-US");

        internal const PageOrientation Orientation
            = PageOrientation.Portrait;

        internal static readonly Box Margins = new Box(29, 20, 29, 20);

        internal static readonly XUnit PageWidth =
            (PredefinedSizeBuilder.ToSize(PaperSize.Letter).Width -
                (Margins.Left + Margins.Right));

        internal static readonly FontBuilder FNT9 = Fonts.Helvetica(9f);
        internal static readonly FontBuilder FNT10 = Fonts.Helvetica(10f);
        internal static readonly FontBuilder FNT12 = Fonts.Helvetica(12f);
        internal static readonly FontBuilder FNT12B = Fonts.Helvetica(12f).SetBold(true);
        internal static readonly FontBuilder FNT20 = Fonts.Helvetica(20f);
        internal static readonly FontBuilder FNT19B = Fonts.Helvetica(19f).SetBold();

        internal static readonly FontBuilder FNT8 = Fonts.Helvetica(8f);

        internal static readonly FontBuilder FNT8_G =
            Fonts.Helvetica(8f).SetColor(Color.Gray);

        internal static readonly FontBuilder FNT9B =
            Fonts.Helvetica(9f).SetBold();

        internal static readonly FontBuilder FNT11B =
            Fonts.Helvetica(11f).SetBold();

        internal static readonly FontBuilder FNT15 = Fonts.Helvetica(15f);
        internal static readonly FontBuilder FNT16 = Fonts.Helvetica(16f);

        internal static readonly FontBuilder FNT16_R =
            Fonts.Helvetica(16f).SetColor(Color.Red);
        internal static readonly FontBuilder FNT16_G =
            Fonts.Helvetica(16f).SetColor(Color.Green);
        internal static readonly FontBuilder FNT17 = Fonts.Helvetica(17f);
        internal static readonly FontBuilder FNT18 = Fonts.Helvetica(18f);

        internal static readonly IdInfo EMPTY_ITEM = new IdInfo("", new FontText[0]);
        private string imgPath = string.Empty;

        internal DocumentBuilder Build(string imagePath)
        {
            imgPath = imagePath;
            DocumentBuilder documentBuilder = DocumentBuilder.New();
            var concertSection = documentBuilder.AddSection();
            IdInfo[,] photoItems = GetPhotoItems(PhotoIdData);
            IdInfo[,] panItems = GetPanItems(PanData);
            IdInfo[,] agentItems = GetAgentItems(AgentIdData);

            concertSection
                 .SetOrientation(Orientation)
                 .SetMargins(Margins);

            addTopTable(concertSection);
            FillIdData(concertSection.AddTable(), agentItems, photoItems, panItems);
            addAssessorInfoTable(concertSection);
            addFrame(concertSection);

            return documentBuilder;
        }

        public void addTopTable(SectionBuilder section)
        {
            var concertTable = section.AddTable()
                .SetContentRowStyleBorder(borderBuilder =>
                    borderBuilder.SetStroke(Stroke.None));

            concertTable
                .SetWidth(XUnit.FromPercent(100))
                .AddColumnPercentToTable("", 20)
                .AddColumnPercentToTable("", 30)
                .AddColumnPercentToTable("", 20)
                .AddColumnPercentToTable("", 30);

            var row1Builder = concertTable.AddRow();
            AddLogoImage(row1Builder.AddCell("", 0, 2));
            AddTopTitleData(row1Builder.AddCell("", 3, 0)
                .SetPadding(32, 0, 0, 0));

            var row2Builder = concertTable.AddRow();
            row2Builder.AddCell();
            No(row2Builder.AddCell("").SetFont(FNT10)
                .SetPadding(32, 0, 0, 0));
            FillHeaderFooterData(row2Builder.AddCell());
            FillTitleInfo(row2Builder.AddCell());
        }

        public void addAssessorInfoTable(SectionBuilder section)
        {
            var infoTable = section.AddTable()
                .SetContentRowStyleBorder(borderBuilder =>
                    borderBuilder.SetStroke(Stroke.None));

            infoTable
                .SetMarginTop(9f)
                .SetWidth(XUnit.FromPercent(100))
                .AddColumnPercentToTable("", 50)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25);

            var row3Builder = infoTable.AddRow();
            FillAgentRemarks(start: 0, end: 10, row3Builder.AddCell("").SetFont(FNT10));

            //FillAssessorRemarks(start: 0, end: 10, row3Builder.AddCell("").SetFont(FNT10));

            FillReportSummaryule(row3Builder.AddCell("", 2, 0).SetFont(FNT10));

            var row4Builder = infoTable.AddRow();
            FillAssessmentData(row4Builder.AddCell("").SetFont(FNT12));
            row4Builder.AddCell("")
                .AddImage(PhotoIdData.PersonAddressImage).SetHeight(400)
                .SetMarginTop(9);
            AddContactInfo(row4Builder.AddCell("").SetFont(FNT12));
        }

        private void addFrame(SectionBuilder section)
        {
            var counterFoil = section.AddTable()
                .SetContentRowStyleBorder(borderBuilder =>
                    borderBuilder.SetStroke(Stroke.None));

            counterFoil
                .SetMarginTop(10f)
                .SetWidth(XUnit.FromPercent(100))
                .AddColumnPercentToTable("", 50)
                .AddColumnPercentToTable("", 16)
                .AddColumnPercentToTable("", 20)
                .AddColumnPercentToTable("", 14);

            var row5Builder = counterFoil.AddRow();
            SetDisclaimer(row5Builder.AddCell("")
                .SetPadding(0, 2, 0, 0));
            AddTopTitleData(row5Builder.AddCell("", 3, 0));

            var row6Builder = counterFoil.AddRow();
            row6Builder.AddCell()
                .AddImage(DetailedReport.InsurerLogo).SetHeight(100);
            FillTitle(row6Builder.AddCell());
            FillPersonalInfoCounterFoil(row6Builder.AddCell());
            //row6Builder.AddCell()
            //    .AddQRCodeUrl(DetailedReport.ReportQr, 4, Color.Black, Color.White, false).SetWidth(153);

            var row7Builder = counterFoil.AddRow();
            row7Builder.AddCell();
            row7Builder.AddCell();
            row7Builder.AddCell();
            row7Builder.AddCell(DetailedReport.AgencyName).SetFont(FNT10);
        }

        private void AddTopTitleData(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .AddParagraph(DetailedReport.ReportTitle).SetFont(FNT19B);
            cellBuilder
                .AddParagraph(DetailedReport.ReportTime).SetFont(FNT12)
                .SetBorderStroke(strokeLeft: Stroke.None, strokeTop: Stroke.None, strokeRight: Stroke.None, strokeBottom: Stroke.Solid)
                .SetBorderWidth(2);
        }

        private void AddLogoImage(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(2, 2, 2, 0);
            cellBuilder
                .AddImage(DetailedReport.AgencyLogo).SetHeight(340);
        }

        private void No(TableCellBuilder cellBuilder)
        {
            cellBuilder
               .AddParagraph(DetailedReport.AgencyNameTitle);
            cellBuilder
                .AddParagraph("").SetLineSpacing(1.5f).AddUrl(AgencyDetailData.AgencyDomain);
            //cellBuilder
            //    .AddQRCodeUrl(DetailedReport.ReportQr, 4,
            //                  Color.Black, Color.White, false).SetHeight(100);
        }

        private void FillAgentRemarks(int start, int end, TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(AgencyDetailData.AgentReportTitle).SetFont(FNT12B).SetMargins(10, 10, 1, 4);
            cellBuilder.SetBorderStroke(strokeLeft: Stroke.Solid, strokeTop: Stroke.Solid, strokeRight: Stroke.None, strokeBottom: Stroke.Solid);

            foreach (var item in AgencyDetailData.ReportSummaryDescription)
            {
                cellBuilder.AddParagraph(item.Question).SetFont(FNT9).SetMargins(20, 0, 10, 2);
                cellBuilder.AddParagraph(item.Answer).SetFont(FNT9).SetMargins(20, 0, 10, 2);
            }
        }

        private void FillAssessorRemarks(int start, int end, TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(AgencyDetailData.AgentReportTitle).SetFont(FNT12B).SetMargins(10, 10, 1, 4);
            cellBuilder.SetBorderStroke(strokeLeft: Stroke.Solid, strokeTop: Stroke.Solid, strokeRight: Stroke.None, strokeBottom: Stroke.Solid);

            cellBuilder.AddParagraph(AgencyDetailData.AssessorSummary).SetFont(FNT9).SetMargins(20, 0, 10, 2);

        }
        private void FillReportSummaryule(TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(AgencyDetailData.SupervisorCommentsTitle).SetFont(FNT12B).SetMargins(10, 10, 1, 4);
            cellBuilder.SetBorderStroke(strokeLeft: Stroke.None, strokeTop: Stroke.Solid,
                    strokeRight: Stroke.Solid, strokeBottom: Stroke.Solid);
            cellBuilder.AddParagraph(AgencyDetailData.ReportSummary).SetFont(FNT9).SetLineSpacing(1.2f).SetMargins(10, 0, 10, 4);
        }

        private void FillAssessmentData(TableCellBuilder cellBuilder)
        {
            cellBuilder.SetBorderStroke(Stroke.None);
            cellBuilder.AddParagraph(AgencyDetailData.AssessmentDescriptionTitle).SetFont(FNT12B).SetMargins(0, 20, 1, 4);
            cellBuilder.AddParagraph(AgencyDetailData.WeatherDetail).SetFont(FNT9).SetLineSpacing(1.2f).SetMargins(0, 0, 30, 4);
            cellBuilder.AddParagraph("");
        }

        private void AddContactInfo(TableCellBuilder cellBuilder)
        {
            cellBuilder.SetBorderStroke(Stroke.None).SetPadding(11, 11, 0, 0);
            cellBuilder.AddParagraph(AgencyDetailData.ExpectedAddressTitle).SetFont(FNT12B).SetMargins(0, 9, 1, 4);
            cellBuilder.AddParagraph(AgencyDetailData.AddressVisited).SetFont(FNT9);
            cellBuilder.AddParagraph(AgencyDetailData.ContactAgencyTitle).SetFont(FNT12B).SetMarginTop(10);
            cellBuilder.AddParagraph("").SetFont(FNT9)
                .SetAlignment(HorizontalAlignment.Left)
                .AddUrl(AgencyDetailData.SupervisorEmail);
            cellBuilder.AddParagraph("").SetFont(FNT9)
                .SetAlignment(HorizontalAlignment.Left)
                .AddUrl(AgencyDetailData.AgencyDomain);
            cellBuilder.AddParagraph(AgencyDetailData.AgencyContact).SetFont(FNT9)
                .SetAlignment(HorizontalAlignment.Left);
        }

        private void FillHeaderFooterData(TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(DetailedReport.PolicyNumTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ClaimTypeTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ServiceTypeTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.InsuredAmountTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.PersonOfInterestNameTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.Reason2VerifyTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.VerifyAddressTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph().SetLineSpacing(1.4f);
        }

        private void FillTitleInfo(TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(DetailedReport.PolicyNum).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ClaimType).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ServiceType).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.InsuredAmount).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.PersonOfInterestName).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.Reason2Verify).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.VerifyAddress).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph().SetLineSpacing(1.4f);
            cellBuilder.AddParagraph().SetLineSpacing(1.4f);
            cellBuilder.AddParagraph().SetLineSpacing(1.4f);
        }

        private void SetDisclaimer(TableCellBuilder cellBuilder)
        {
            cellBuilder.AddParagraph(AgencyDetailData.ReportDisclaimer).SetFont(FNT9).SetMarginRight(30);
        }

        private void FillTitle(TableCellBuilder cellBuilder)
        {
            cellBuilder.SetBorderStroke(Stroke.None);
            cellBuilder.AddParagraph(DetailedReport.PolicyNumTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ClaimTypeTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.InsuredAmountTitle).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.PersonOfInterestNameTitle).SetLineSpacing(1.4f);
        }

        private void FillPersonalInfoCounterFoil(TableCellBuilder cellBuilder)
        {
            cellBuilder.SetBorderStroke(Stroke.None).SetBold(true);
            cellBuilder.AddParagraph(DetailedReport.PolicyNum).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.ClaimType).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.InsuredAmount).SetLineSpacing(1.4f);
            cellBuilder.AddParagraph(DetailedReport.PersonOfInterestName).SetLineSpacing(1.4f);
        }

        private void FillIdData(TableBuilder tableBuilder, IdInfo[,] agentId, IdInfo[,] photoId, IdInfo[,] pandId)
        {
            tableBuilder
                .SetWidth(XUnit.FromPercent(100))
                .SetBorder(Stroke.None)
                .AddColumnToTable("", 415.5f)
                .AddColumn("", 138.5f);

            FillIdTableFirstRow(tableBuilder, agentId[0, 0]);
            var rowBuilder = tableBuilder.AddRow();
            rowBuilder.AddCell().AddTable(builder =>
            {
                builder.SetWidth(415.5f);
                FillIdTable(builder, agentId, 1);
            });
            rowBuilder.AddCell(FillAgentMap);

            var rr = tableBuilder.AddRow();
            rr.AddCell().AddTable(b =>
            {
                b.SetWidth(415.5f);
                b.SetBorder(Stroke.Dashed)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercent("", 25);
            });
            rr.AddCell(FillBlank);

            FillIdTableFirstRow(tableBuilder, photoId[0, 0]);
            rowBuilder = tableBuilder.AddRow();
            rowBuilder.AddCell().AddTable(builder =>
            {
                builder.SetWidth(415.5f);
                FillIdTable(builder, photoId, 1);
            });
            rowBuilder.AddCell(FillPhotoMap);

            rr = tableBuilder.AddRow();
            rr.AddCell().AddTable(b =>
            {
                b.SetWidth(415.5f);
                b.SetBorder(Stroke.Dashed)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercent("", 25);
            });
            rr.AddCell(FillBlank);

            FillIdTableFirstRow(tableBuilder, pandId[0, 0]);
            var rb = tableBuilder.AddRow();
            rb.AddCell().AddTable(builder =>
            {
                builder.SetWidth(415.5f);
                FillIdTable(builder, pandId, 1);
            });
            rb.AddCell(FillPanMap);
        }

        private void FillBlank(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(19, 6, 0, 0)
                .AddParagraph("-")
                .SetFont(FNT9)
                .SetMarginBottom(19);
        }

        private void FillAgentMap(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(19, 6, 0, 0)
                .AddParagraph(AgencyDetailData.AddressVisitedTitle)
                .SetFont(FNT9)
                .SetMarginBottom(19);
            cellBuilder.AddImage(AgentIdData.PhotoIdMapPath,
                XSize.FromHeight(108));
            cellBuilder.AddParagraph("")
               .AddUrl(AgentIdData.PhotoIdMapUrl, "map");
        }

        private void FillPhotoMap(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(19, 6, 0, 0)
                .AddParagraph(AgencyDetailData.AddressVisitedTitle)
                .SetFont(FNT9)
                .SetMarginBottom(19);
            cellBuilder.AddImage(PhotoIdData.PhotoIdMapPath,
                XSize.FromHeight(108));
            cellBuilder.AddParagraph("")
               .AddUrl(PhotoIdData.PhotoIdMapUrl, "map");
        }

        private void FillPanMap(TableCellBuilder cellBuilder)
        {
            cellBuilder
                .SetPadding(19, 6, 0, 0)
                .AddParagraph(AgencyDetailData.AddressVisitedTitle)
                .SetFont(FNT9)
                .SetMarginBottom(19);
            cellBuilder.AddImage(PanData.PanMapPath,
                XSize.FromHeight(108));
            cellBuilder.AddParagraph("")
                .AddUrl(PanData.PanMapUrl, "map");
        }


        private IdInfo[,] GetAgentItems(IdData agentIdData)
        {
            IdInfo[,] result =
            {
                {
                new IdInfo("Investigation Type", FNT15,
                    agentIdData.Passenger, 4),
                    EMPTY_ITEM,
                    EMPTY_ITEM,
                    EMPTY_ITEM
                },
                {
                new IdInfo("Agent Name", new FontText[] {
                    new FontText (FNT12, agentIdData.PersonName + " / "),
                    new FontText (FNT12B, agentIdData.Salutation)
                }, 2),
                EMPTY_ITEM,
                new IdInfo("Address Visited", new FontText[] {
                    new FontText (FNT12, agentIdData.ArrivalAirport + " / "),
                    new FontText (FNT12B, agentIdData.ArrivalAbvr)
                }, 2),
                EMPTY_ITEM
                },
                {
                new IdInfo("Match", agentIdData.MatchFont, agentIdData.FaceMatchStatus),
                new IdInfo("Photo", FNT16, "",agentIdData.PhotoIdPath),
                new IdInfo("", FNT16, ""),
                new IdInfo("Status", agentIdData.MatchFont, "", agentIdData.StatusImagePath)
                },
                {
                new IdInfo("Visit Date", FNT16,
                    agentIdData.PhotoIdTime.ToString(
                                "dd MMMM", DocumentLocale)),
                new IdInfo("Time", FNT16,
                    agentIdData.BoardingTill.ToString(
                                "HH:mm", DocumentLocale)),
                new IdInfo("", FNT16, ""),
                new IdInfo("Weather", FNT8,
                    agentIdData.WeatherData)
                }
            };
            return result;
        }
        private IdInfo[,] GetPhotoItems(IdData photoIdData)
        {
            IdInfo[,] result =
            {
                {
                new IdInfo("Investigation Type", FNT15,
                    photoIdData.Passenger, 4),
                    EMPTY_ITEM,
                    EMPTY_ITEM,
                    EMPTY_ITEM
                },
                {
                new IdInfo("Person Name", new FontText[] {
                    new FontText (FNT12, photoIdData.PersonName + " / "),
                    new FontText (FNT12B, photoIdData.Salutation)
                }, 2),
                EMPTY_ITEM,
                new IdInfo("Address Visited", new FontText[] {
                    new FontText (FNT12, photoIdData.ArrivalAirport + " / "),
                    new FontText (FNT12B, photoIdData.ArrivalAbvr)
                }, 2),
                EMPTY_ITEM
                },
                {
                new IdInfo("Match", photoIdData.MatchFont, photoIdData.FaceMatchStatus),
                new IdInfo("Photo", FNT16, "",photoIdData.PhotoIdPath),
                new IdInfo("", FNT16, ""),
                new IdInfo("Status", photoIdData.MatchFont, "", photoIdData.StatusImagePath)
                },
                {
                new IdInfo("Visit Date", FNT16,
                    photoIdData.PhotoIdTime.ToString(
                                "dd MMMM", DocumentLocale)),
                new IdInfo("Time", FNT16,
                    photoIdData.BoardingTill.ToString(
                                "HH:mm", DocumentLocale)),
                new IdInfo("", FNT16, ""),
                new IdInfo("Weather", FNT8,
                    photoIdData.WeatherData)
                }
            };
            return result;
        }

        private IdInfo[,] GetPanItems(IdData panData)
        {
            IdInfo[,] result =
            {
                {
                new IdInfo("Investigation Type", FNT15,
                    panData.Passenger, 4),
                    EMPTY_ITEM,
                    EMPTY_ITEM,
                    EMPTY_ITEM
                },
                {
                new IdInfo("Document Name", new FontText[] {
                    new FontText (FNT12, " / "),
                    new FontText (FNT12B, panData.Salutation)
                }, 2),
                EMPTY_ITEM,
                new IdInfo("Address Visited", new FontText[] {
                    new FontText (FNT12, panData.ArrivalAirport + " / "),
                    new FontText (FNT12B, panData.ArrivalAbvr)
                }, 2),
                EMPTY_ITEM
                },
                {
                new IdInfo("Match", panData.MatchFont, panData.FaceMatchStatus),
                new IdInfo("Image", FNT16, "", panData.PanPhotoPath),
                new IdInfo("", FNT16, " "),
                new IdInfo("Status", panData.MatchFont,"", panData.StatusImagePath)
                },
                {
                new IdInfo("Visit Date", FNT16,
                    panData.PhotoIdTime.ToString(
                                "dd MMMM", DocumentLocale)),
                new IdInfo("Time", FNT16,
                    panData.BoardingTill.ToString(
                                "HH:mm", DocumentLocale)),
                new IdInfo("", FNT16, ""),
                new IdInfo("Pan Scanned Info", FNT8,
                    panData.WeatherData)
                }
            };
            return result;
        }
        private void FillIdTable(TableBuilder tableBuilder,
         IdInfo[,] boardingItems, int startRow = 0)
        {
            tableBuilder
                .SetBorder(Stroke.None)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercentToTable("", 25)
                .AddColumnPercent("", 25);
            int rows = boardingItems.GetLength(0);
            int columns = boardingItems.GetLength(1);
            for (int i = startRow; i < rows; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    var rowBuilder = tableBuilder.AddRow();
                    if (k == 0)
                    {
                        rowBuilder.ApplyStyle(
                            StyleBuilder.New()
                                .SetPaddingTop(4)
                            );
                    }
                    else if (i < rows - 1)
                    {
                        rowBuilder.ApplyStyle(
                            StyleBuilder.New()
                                .SetBorderBottom(0.5f,
                                    Stroke.Solid, Color.Black)
                                .SetPaddingBottom(4)
                            );
                    }
                    for (int j = 0; j < columns; j++)
                    {
                        IdInfo bi = boardingItems[i, j];
                        if (!bi.isEmpty())
                        {
                            var cellBuilder = rowBuilder.AddCell();
                            if (bi.colSpan > 1)
                            {
                                cellBuilder.SetColSpan(bi.colSpan);
                            }
                            if (k == 0)
                            {
                                cellBuilder
                                    .AddParagraph(bi.name).SetFont(FNT9);
                            }
                            else
                            {
                                if (bi.image != null)
                                {
                                    cellBuilder.AddTable(builder =>
                                    {
                                        ImageThenText(builder, bi);
                                    });
                                }
                                else
                                {
                                    TextOnly(cellBuilder.AddParagraph(), bi);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ImageThenText(TableBuilder tableBuilder, IdInfo bi)
        {
            tableBuilder
                .SetWidth(XUnit.FromPercent(100))
                .SetBorder(Stroke.None)
                .AddColumnToTable("", 13)
                .AddColumn("");
            var rowBuilder = tableBuilder.AddRow();
            rowBuilder.AddCell()
                .SetPadding(0, 4, 0, 0)
                //.SetVerticalAlignment(VerticalAlignment.Bottom)
                .AddImage(Path.Combine(imgPath, "images", bi.image),
                    XSize.FromHeight(100));
            TextOnly(rowBuilder.AddCell().AddParagraph(), bi);
        }

        private void TextOnly(ParagraphBuilder paragraphBuilder, IdInfo bi)
        {
            foreach (FontText ft in bi.fontTexts)
            {
                paragraphBuilder.AddText(ft.text).SetFont(ft.font);
            }
        }

        private void FillIdTableFirstRow(TableBuilder tableBuilder,
                IdInfo bi)
        {
            for (int k = 0; k < 2; k++)
            {
                var rowBuilder = tableBuilder.AddRow();
                if (k == 1)
                {
                    rowBuilder.ApplyStyle(
                        StyleBuilder.New()
                            .SetBorderBottom(0.5f, Stroke.Solid, Color.Black)
                            .SetPaddingBottom(6)
                        );
                }
                var cellBuilder = rowBuilder.AddCell();
                cellBuilder.SetColSpan(2);
                if (k == 0)
                {
                    cellBuilder.SetFont(FNT9).AddParagraph(bi.name);
                }
                else
                {
                    if (bi.image != null)
                    {
                        cellBuilder.AddTable(builder =>
                        {
                            ImageThenText(builder, bi);
                        });
                    }
                    else
                    {
                        TextOnly(cellBuilder.AddParagraph(), bi);
                    }
                }
            }
        }


        internal struct IdInfo
        {
            internal string name;
            internal FontText[] fontTexts;
            internal string image;
            internal int colSpan;

            internal IdInfo(string name, FontBuilder font, string text, int colSpan = 1) : this(name, font, text, null, colSpan)
            {
            }

            internal IdInfo(string name, FontBuilder font, string text, string image, int colSpan = 1)
            {
                this.name = name;
                fontTexts = new FontText[] { new FontText(font, text) };
                this.image = image;
                this.colSpan = colSpan;
            }

            internal IdInfo(string name, FontText[] fontTexts, int colSpan = 1)
            {
                this.name = name;
                this.fontTexts = fontTexts;
                this.image = null;
                this.colSpan = colSpan;
            }

            internal bool isEmpty()
            {
                return fontTexts.Length == 0;
            }
        }

        internal struct FontText
        {
            internal FontBuilder font;
            internal string text;

            internal FontText(FontBuilder font, string text)
            {
                this.font = font;
                this.text = text;
            }
        }
    }
}
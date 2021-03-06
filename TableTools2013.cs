﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using acColors = Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Internal;
using CADHelp;

namespace CADHelp
{
    public class TableTools
    {
        public TableTools()
        {
            Initialize();
        }

        public Hashtable HItemTableColumnHeaders
        {
            get
            {
                return mHTColsHItems;
            }
        }

        public Hashtable PanelTableColumnHeaders
        {
            get
            {
                return mHTColsPanels;
            }
        }

        public Hashtable DetailTableColumnHeaders
        {
            get
            {
                return mHTColsDetail;
            }
        }

        Database db = HostApplicationServices.WorkingDatabase;
        Autodesk.AutoCAD.ApplicationServices.Document doc = acadApp.DocumentManager.MdiActiveDocument;
        Editor ed = acadApp.DocumentManager.MdiActiveDocument.Editor;
        
        public static string colRef = "Ref";
        public static string colType = "Type";
        public static string colQty = "Qty";
        public static string colUOM = "U.O.M.";
        public static string colManuf = "Manufacturer";
        public static string colModel = "Model";
        public static string colDesign = "Design";
        public static string colDesc = "Description";
        public static string colSize = "Size";
        public static string colFinish = "Finish";
        public static string colNotes = "Notes";
        public static string colWithGrain = "With Grain";
        public static string colAcrossGrain = "Across Grain";
        public static string colItemNo = "Item #";

        public static int COL_IDX_ITEM = 0;
        public static int COL_IDX_DIM_CROSS_IMPERIAL = 1;
        public static int COL_IDX_DIM_WITH_IMPERIAL = 2;
        //public static int COL_IDX_DIM_CROSS_METRIC = 3;
        //public static int COL_IDX_DIM_WITH_METRIC = 4;
        public static int COL_IDX_COLOR = 3;
        public static int COL_IDX_EDGE_A = 4;
        public static int COL_IDX_EDGE_B = 5;
        public static int COL_IDX_EDGE_C = 6;
        public static int COL_IDX_EDGE_D = 7;
        public static int COL_IDX_NOTES = 8;

        public static int COL_IDX_TOC_ROOM = 0;
        public static int COL_IDX_TOC_PAGE = 1;

        public const string PANEL_TABLE_STYLE_NAME = "PanelTable";
        public const string TOC_TABLE_STYLE_NAME = "TOC";
        public const string WM_STANDARD_TABLE_STYLE_NAME = "WM_Standard";

        Hashtable mHTColsHItems = new Hashtable();
        Hashtable mHTColsPanels = new Hashtable(); 
        Hashtable mHTColsDetail = new Hashtable();

        public static string XD_TABLE_TYPE = "TABLETYPE";
        public static string XD_TABLE_PANELING = "PANELING";
        public static string XD_TABLE_DETAIL = "DETAIL";
        public static string XD_TABLE_HFITEM = "HFITEM";
        public static string XD_TABLE_SILLS = "SILLS";

        private void Initialize()
        {
            // H Item columns
            string[] cols = new string[] {
            colRef,
            colQty,
            colUOM,
            colDesign,
            colManuf,
            colModel,
            colDesc,
            colSize,
            colFinish,
            colNotes,
            colType};
            for (int i = 0; i <= cols.GetUpperBound(0); i++)
            {
                mHTColsHItems.Add(cols[i], i);
            }
            // Panel table columns
            cols = new string[] {
            colItemNo,
            colQty,
            colModel,
            colWithGrain,
            colAcrossGrain,
            colDesc,
            colFinish,
            colNotes};
            for (int i = 0; i <= cols.GetUpperBound(0); i++)
            {
                mHTColsPanels.Add(cols[i], i);
            }
            // Detail table columns
            cols = new string[] {
            colRef,
            colQty,
            colModel,
            colWithGrain,
            colAcrossGrain,
            colDesc,
            colFinish,
            colNotes};
            for (int i = 0; i <= cols.GetUpperBound(0); i++)
            {
                mHTColsDetail.Add(cols[i], i);
            }

        }

        public ObjectId UpdateExistingHFTable(
            ObjectId tableOID,
            System.Windows.Forms.DataGridView dgvTableData,
            string tableName)
        {
            if (tableOID == ObjectId.Null)
            {
                return ObjectId.Null;
            }
            // Get the upper left corner of the existing table
            Point3d ipnt = new Point3d();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Table tbl = (Table)tr.GetObject(tableOID, OpenMode.ForWrite);
                ipnt = tbl.Position;
                tbl.Erase();
                tr.Commit();
            }
            ObjectId resid = ObjectId.Null;

            if (dgvTableData.RowCount > 0)
            {
                resid = CreateNewHFTable(dgvTableData, tableName, ipnt);
            }
            return resid;
        }

        public ObjectId CreateNewHFTable(
            System.Windows.Forms.DataGridView dgvTableData,
            string tableName)
        {
            Point3d ipnt = new Point3d(0, 0, 0);
            ObjectId resID = CreateNewHFTable(dgvTableData, tableName, ipnt);
            return resID;
        }

        public ObjectId CreateNewHFTable(
            System.Windows.Forms.DataGridView dgvTableData,
            string tableName,
            Autodesk.AutoCAD.Geometry.Point3d LowerLeft)
        {
            ObjectId resoid = ObjectId.Null;
            // CADHelp.CADHelper.AddSettingsBlock();
            CADHelp.CADHelper ch = new CADHelp.CADHelper();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                Table tbl = new Table();

                tbl.Position = LowerLeft;
                // CADHelp.CADHelper.AddSettingsBlock();

                tbl.TableStyle = CADHelp.TableTools.GetTableStyle(CADHelp.TableTools.WM_STANDARD_TABLE_STYLE_NAME); // db.Tablestyle;
                tbl.SetSize(dgvTableData.RowCount + 2, dgvTableData.ColumnCount);
                // tbl.SetTextString(0, 0, tableName);
                
                tbl.Cells[0, 0].TextString = tableName;
                Hashtable colWidths = new Hashtable();
                string cellVal = string.Empty;
                for (int i = 0; i < dgvTableData.ColumnCount; i++)
                {

                    // tbl.SetTextString(1, i, dgvTableData.Columns[i].HeaderText);
                    tbl.Cells[1, i].TextString = dgvTableData.Columns[i].HeaderText;
                    colWidths.Add(i, Utils.GetTextExtents(ch.TextStyleStdID, dgvTableData.Columns[i].HeaderText, 3).X);
                }
                // try to get the wingdings text style
                ObjectId tsid=CADHelp.CADHelper.GetTextStyleTable(CADHelp.CADHelper.TextStyleWingDing).ObjectId;
                ObjectId tsStd = CADHelp.CADHelper.GetTextStyleTable(CADHelp.CADHelper.TextStyleStandard).ObjectId;

                for (int i = 2; i < tbl.Rows.Count; i++)
                {
                    for (int j = 0; j < tbl.Columns.Count; j++)
                    {
                        tbl.SetAlignment(i, j, CellAlignment.MiddleCenter);
                        tbl.Cells[i, j].Alignment = CellAlignment.MiddleCenter;
                        int dgvi = i - 2;
                        if (dgvTableData[j, dgvi].Value == null)
                        {
                            cellVal = string.Empty;
                        }
                        else
                        {
                            cellVal = dgvTableData[j, dgvi].Value.ToString();
                        }
                        if (cellVal.Trim().Length > 0)
                        {
                            double? tht = tbl.Cells[i, j].TextHeight;
                            if (tht == null) tht = 1;
                            if (Utils.GetTextExtents(tsStd, cellVal, (double)tht).X > (double)colWidths[j])
                            {
                                colWidths[j] = Utils.GetTextExtents(ch.TextStyleStdID, cellVal, tbl.TextHeight(i, j)).X;
                            }
                            // tbl.SetTextString(i, j, cellVal);
                            tbl.Cells[i, j].TextString = cellVal;
                            if (j == tbl.Columns.Count - 1)
                            {
                                if (tsid != ObjectId.Null) tbl.Cells[i, j].TextStyleId = tsid;
                            }
                        }
                    }
                }
                for (int i = 0; i < colWidths.Count; i++)
                {
                    // set column widths based on maximum text width per column
                    // tbl.SetColumnWidth(i, (double)colWidths[i] + 6);
                    tbl.Columns[i].Width = (double)colWidths[i] + 6;
                }
                // LOCK EVERY CELL

                // tbl.SetCellState(0, 0, CellStates.ContentReadOnly);
                
                for (int r = 1; r < tbl.Rows.Count; r++)
                {
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        if (r > 1)
                        {
                            if (c == (int)mHTColsHItems[colQty]) tbl.SetAlignment(r, c, CellAlignment.MiddleRight);
                            if (c == (int)mHTColsHItems[colUOM]) tbl.SetAlignment(r, c, CellAlignment.MiddleLeft);
                            if (c == (int)mHTColsHItems[colDesc]) tbl.SetAlignment(r, c, CellAlignment.MiddleLeft);
                        }
                        // tbl.SetCellState(r, c, CellStates.ContentReadOnly);
                        tbl.Cells[r, c].State = CellStates.ContentReadOnly;
                    }
                }
                try
                {
                    tbl.BreakOptions = TableBreakOptions.RepeatTopLabels | TableBreakOptions.EnableBreaking | TableBreakOptions.AllowManualHeights | TableBreakOptions.AllowManualPositions;
                    tbl.Layer = "0";
                }
                catch
                {
                }
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace],
                  OpenMode.ForWrite);
                btr.AppendEntity(tbl);
                trans.AddNewlyCreatedDBObject(tbl, true);
                resoid = tbl.ObjectId;
                trans.Commit();
            }
            return resoid;
        }

        private static void LockTableContent(Table tbl, bool LockIt)
        {
            if (LockIt)
            {
                for (int r = 1; r < tbl.Rows.Count; r++)
                {
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        try
                        {
                            // tbl.SetCellState(r, c, CellStates.ContentReadOnly);
                            tbl.Cells[r, c].State = CellStates.ContentReadOnly;
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                for (int r = 1; r < tbl.Rows.Count; r++)
                {
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        try
                        {
                            // tbl.SetCellState(r, c, CellStates.ContentReadOnly);
                            tbl.Cells[r, c].State = CellStates.None;
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }

            }
        }

        internal static Table CreateNewTOCTable(string tableTitleText)
        {
            Table tbl = new Table();
            // Title
            tbl.TableStyle = GetTOCTableStyle();
            tbl.Cells[0, 0].TextString = tableTitleText;

            // Headers
            tbl.InsertRows(1, 5, 1);
            tbl.InsertRowsAndInherit(2, 1, 1);
            tbl.InsertColumns(1, 26, 1);
            tbl.Columns[COL_IDX_TOC_ROOM].Width = 26;
            tbl.Columns[COL_IDX_TOC_PAGE].Width = 30;

            // tbl.Cells[1, 0].TextString = "Item";
            tbl.Cells[1, 0].TextString = "ROOM";
            tbl.Cells[1, 1].TextString = "PAGE(S)";

            // First row
            tbl.InsertRows(2, 4.625, 1);
            // tbl.GenerateLayout();
            tbl.BreakOptions = TableBreakOptions.RepeatTopLabels;
            return tbl;
        }

        internal static Table CreateNewPanelTable(string tableTitleText)
        {
            Table tbl = new Table();
            // Title
            tbl.TableStyle = GetPanelTableStyle();
            tbl.Cells[0, 0].TextString = tableTitleText;

            // Headers
            tbl.InsertRows(1, 5, 1);
            tbl.InsertRowsAndInherit(2, 1, 1);
            tbl.InsertColumns(1, 26, 8);
            tbl.Columns[COL_IDX_ITEM].Width = 26;
            tbl.Columns[COL_IDX_DIM_CROSS_IMPERIAL].Width = 30;
            // tbl.Columns[COL_IDX_DIM_CROSS_METRIC].Width = 28;
            tbl.Columns[COL_IDX_DIM_WITH_IMPERIAL].Width = 30;
            // tbl.Columns[COL_IDX_DIM_WITH_METRIC].Width = 28;
            tbl.Columns[COL_IDX_EDGE_A].Width = 14;
            tbl.Columns[COL_IDX_EDGE_B].Width = 14; 
            tbl.Columns[COL_IDX_EDGE_C].Width = 14;
            tbl.Columns[COL_IDX_EDGE_D].Width = 14;
            tbl.Columns[COL_IDX_NOTES].Width = 80;

            // tbl.Cells[1, 0].TextString = "Item";
            tbl.Cells[1, 1].TextString = "FINISHED DIMENSIONS";
            // merge 2-5
            tbl.MergeCells(CellRange.Create(tbl, 1, 1, 1, 2));
            // tbl.Cells[1, 2].TextString = "Height";
            //tbl.Cells[1, 3].TextString = "Color";
            //tbl.Cells[1, 4].TextString = "Notes";
            //tbl.Cells[1, 5].TextString = "Data";
            tbl.Cells[1, 4].TextString = "EDGES";
            // merge 7-10
            tbl.MergeCells(CellRange.Create(tbl, 1, 4, 1, 7));

            tbl.Cells[2, COL_IDX_ITEM].TextString = "ITEM #";
            tbl.Cells[2, COL_IDX_DIM_CROSS_IMPERIAL].TextString = "ACROSS GRAIN";
            tbl.Cells[2, COL_IDX_DIM_CROSS_IMPERIAL].TextHeight = 2.625;
            tbl.Cells[2, COL_IDX_DIM_WITH_IMPERIAL].TextString = "WITH GRAIN";
            tbl.Cells[2, COL_IDX_DIM_WITH_IMPERIAL].TextHeight = 2.625;
            //tbl.Cells[2, COL_IDX_DIM_CROSS_METRIC].TextString = "ACROSS GRAIN";
            //tbl.Cells[2, COL_IDX_DIM_CROSS_METRIC].TextHeight = 2.625;
            //tbl.Cells[2, COL_IDX_DIM_WITH_METRIC].TextString = "WITH GRAIN";
            //tbl.Cells[2, COL_IDX_DIM_WITH_METRIC].TextHeight = 2.625;
            
            tbl.Cells[2, COL_IDX_COLOR].TextString = "COLOR";
            tbl.Cells[2, COL_IDX_EDGE_A].TextString = "A";
            tbl.Cells[2, COL_IDX_EDGE_A].TextHeight = 2.625; 
            tbl.Cells[2, COL_IDX_EDGE_B].TextString = "B";
            tbl.Cells[2, COL_IDX_EDGE_B].TextHeight = 2.625; 
            tbl.Cells[2, COL_IDX_EDGE_C].TextString = "C";
            tbl.Cells[2, COL_IDX_EDGE_C].TextHeight = 2.625; 
            tbl.Cells[2, COL_IDX_EDGE_D].TextString = "D";
            tbl.Cells[2, COL_IDX_EDGE_D].TextHeight = 2.625; 
            tbl.Cells[2, COL_IDX_NOTES].TextString = "NOTES";

            // First row
            tbl.InsertRows(3, 4.625, 1);
            // tbl.GenerateLayout();
            tbl.BreakOptions = TableBreakOptions.RepeatTopLabels;    
            return tbl;
        }
        
        internal static ObjectId GetPanelTableStyle()
        {
            
            Document doc =  Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            using (DocumentLock dlock = doc.LockDocument())
            {
                // First let us create our custom style,
                //  if it doesn't exist

                string styleName = PANEL_TABLE_STYLE_NAME;
                ObjectId tsId = ObjectId.Null;

                DBDictionary sd = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);

                // Use the style if it already exists
                if (sd.Contains(styleName))
                {
                    tsId = sd.GetAt(styleName);
                }
                else
                {
                    // Otherwise we have to create it
                    TableStyle ts = new TableStyle();

                    #region Formatting of table
                    
                    // Background colors
                    //ts.SetBackgroundColor(CADHelper.clrYellow, (int)(RowType.TitleRow));
                    //ts.SetBackgroundColor(CADHelper.clrWhite, (int)(RowType.HeaderRow));
                    
                    // Text heights
                    ts.SetTextHeight(6, (int)(RowType.TitleRow));
                    ts.SetTextHeight(4, (int)(RowType.HeaderRow));
                    ts.SetTextHeight(2.625, (int)(RowType.DataRow));
                    
                    // Fore colors
                    //ts.SetColor(CADHelper.clrBlue, (int)(RowType.TitleRow | RowType.HeaderRow));
                    // Margins
                    ts.VerticalCellMargin = 1;
                    ts.HorizontalCellMargin = 2;
                    
                    // Alignment
                    ts.SetAlignment(CellAlignment.MiddleCenter, 
                        (int)(RowType.TitleRow | RowType.HeaderRow));
                    // Grid line weight
                    ts.SetGridLineWeight(LineWeight.LineWeight090, 
                        (int)GridLineType.HorizontalBottom, (int)(RowType.HeaderRow));
                    // Grid color
                    ts.SetGridColor(CADHelper.clrBlue, 
                        (int)(GridLineType.OuterGridLines | GridLineType.InnerGridLines), 
                        (int)(RowType.TitleRow | RowType.HeaderRow));
                    
                    #endregion
                    
                    // Add our table style to the dictionary
                    //  and to the transaction
                    tsId = ts.PostTableStyleToDatabase(db, styleName);
                    tr.AddNewlyCreatedDBObject(ts, true);
                }
                tr.Commit();

                return tsId;
            }
            
        }

        internal static ObjectId GetTableStyle(string StyleName)
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            using (DocumentLock dlock = doc.LockDocument())
            {
                // First let us create our custom style,
                //  if it doesn't exist

                // string styleName = TOC_TABLE_STYLE_NAME; // PANEL_TABLE_STYLE_NAME;
                ObjectId tsId = ObjectId.Null;

                DBDictionary sd = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);

                // Use the style if it already exists
                if (sd.Contains(StyleName))
                {
                    tsId = sd.GetAt(StyleName);
                }
                else
                {
                    // Otherwise we have to create it
                    TableStyle ts = new TableStyle();

                    #region Formatting of table
                    switch (StyleName)
                    {
                        case TOC_TABLE_STYLE_NAME:
                            // Background colors
                            //ts.SetBackgroundColor(CADHelper.clrYellow, (int)(RowType.TitleRow));
                            //ts.SetBackgroundColor(CADHelper.clrWhite, (int)(RowType.HeaderRow));

                            // Text heights
                            ts.SetTextHeight(6, (int)(RowType.TitleRow));
                            ts.SetTextHeight(4, (int)(RowType.HeaderRow));
                            ts.SetTextHeight(2.625, (int)(RowType.DataRow));

                            // Fore colors
                            //ts.SetColor(CADHelper.clrBlue, (int)(RowType.TitleRow | RowType.HeaderRow));
                            // Margins
                            ts.VerticalCellMargin = 1;
                            ts.HorizontalCellMargin = 2;

                            // Alignment
                            ts.SetAlignment(CellAlignment.MiddleCenter,
                                (int)(RowType.TitleRow | RowType.HeaderRow));
                            // Grid line weight
                            ts.SetGridLineWeight(LineWeight.LineWeight090,
                                (int)GridLineType.HorizontalBottom, (int)(RowType.HeaderRow));
                            // Grid color
                            ts.SetGridColor(CADHelper.clrBlue,
                                (int)(GridLineType.OuterGridLines | GridLineType.InnerGridLines),
                                (int)(RowType.TitleRow | RowType.HeaderRow));
                            break;
                        case WM_STANDARD_TABLE_STYLE_NAME:

                            // Background colors
                            //ts.SetBackgroundColor(CADHelper.clrYellow, (int)(RowType.TitleRow));
                            //ts.SetBackgroundColor(CADHelper.clrWhite, (int)(RowType.HeaderRow));

                            // Text heights
                            ts.SetTextHeight(5, (int)(RowType.TitleRow));
                            ts.SetTextHeight(3.5, (int)(RowType.HeaderRow));
                            ts.SetTextHeight(2.625, (int)(RowType.DataRow));

                            // Text style
                            ts.SetTextStyle(CADHelp.CADHelper.GetTextStyleTable(CADHelp.CADHelper.TextStyleBold).ObjectId, (int)RowType.HeaderRow);
                            ts.SetTextStyle(CADHelp.CADHelper.GetTextStyleTable(CADHelp.CADHelper.TextStyleBold).ObjectId, (int)RowType.TitleRow);
                            ts.SetTextStyle(CADHelp.CADHelper.GetTextStyleTable(CADHelp.CADHelper.TextStyleStandard).ObjectId, (int)RowType.DataRow);

                            // Fore colors
                            //ts.SetColor(CADHelper.clrBlue, (int)(RowType.TitleRow | RowType.HeaderRow));
                            // Margins
                            ts.VerticalCellMargin = 1;
                            ts.HorizontalCellMargin = 2;

                            // Alignment
                            ts.SetAlignment(CellAlignment.MiddleCenter,
                                (int)(RowType.TitleRow | RowType.HeaderRow));
                            // Grid line weight
                            ts.SetGridLineWeight(LineWeight.LineWeight090,
                                (int)GridLineType.HorizontalBottom, (int)(RowType.HeaderRow));
                            // Grid color
                            ts.SetGridColor(CADHelper.clrYellow,
                                (int)(GridLineType.OuterGridLines | GridLineType.InnerGridLines),
                                (int)(RowType.TitleRow | RowType.HeaderRow));
                            break;
                        default:
                            break;
                    }


                    #endregion

                    // Add our table style to the dictionary
                    //  and to the transaction
                    tsId = ts.PostTableStyleToDatabase(db, StyleName);
                    tr.AddNewlyCreatedDBObject(ts, true);
                }
                tr.Commit();

                return tsId;
            }

        }


        internal static ObjectId GetTOCTableStyle()
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            using (DocumentLock dlock = doc.LockDocument())
            {
                // First let us create our custom style,
                //  if it doesn't exist

                string styleName = TOC_TABLE_STYLE_NAME; // PANEL_TABLE_STYLE_NAME;
                ObjectId tsId = ObjectId.Null;

                DBDictionary sd = (DBDictionary)tr.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead);

                // Use the style if it already exists
                if (sd.Contains(styleName))
                {
                    tsId = sd.GetAt(styleName);
                }
                else
                {
                    // Otherwise we have to create it
                    TableStyle ts = new TableStyle();

                    #region Formatting of table

                    // Background colors
                    //ts.SetBackgroundColor(CADHelper.clrYellow, (int)(RowType.TitleRow));
                    //ts.SetBackgroundColor(CADHelper.clrWhite, (int)(RowType.HeaderRow));

                    // Text heights
                    ts.SetTextHeight(6, (int)(RowType.TitleRow));
                    ts.SetTextHeight(4, (int)(RowType.HeaderRow));
                    ts.SetTextHeight(2.625, (int)(RowType.DataRow));

                    // Fore colors
                    //ts.SetColor(CADHelper.clrBlue, (int)(RowType.TitleRow | RowType.HeaderRow));
                    // Margins
                    ts.VerticalCellMargin = 1;
                    ts.HorizontalCellMargin = 2;

                    // Alignment
                    ts.SetAlignment(CellAlignment.MiddleCenter,
                        (int)(RowType.TitleRow | RowType.HeaderRow));
                    // Grid line weight
                    ts.SetGridLineWeight(LineWeight.LineWeight090,
                        (int)GridLineType.HorizontalBottom, (int)(RowType.HeaderRow));
                    // Grid color
                    ts.SetGridColor(CADHelper.clrBlue,
                        (int)(GridLineType.OuterGridLines | GridLineType.InnerGridLines),
                        (int)(RowType.TitleRow | RowType.HeaderRow));

                    #endregion

                    // Add our table style to the dictionary
                    //  and to the transaction
                    tsId = ts.PostTableStyleToDatabase(db, styleName);
                    tr.AddNewlyCreatedDBObject(ts, true);
                }
                tr.Commit();

                return tsId;
            }

        }

        internal static ObjectIdCollection GetPanelTables()
        {
            ObjectIdCollection oids = new ObjectIdCollection();
            SelectionSet ssTbl = CADHelp.SelectionTools.GetTables(false, "");
            
            if (ssTbl != null)
            {
                Database db = HostApplicationServices.WorkingDatabase;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    for (int i = 0; i < ssTbl.Count; i++)
                    {
                        Table tbl = tr.GetObject(ssTbl[i].ObjectId, OpenMode.ForRead) as Table;
                        // check the table for the xdata TABLETYPE::PANELING stamp
                        string tblType = XDTools.GetMyXDValue(ssTbl[i].ObjectId, "CES", XD_TABLE_TYPE);
                        if (tblType.Equals(XD_TABLE_PANELING))
                        {
                            oids.Add(ssTbl[i].ObjectId);
                        }
                    }
                    tr.Commit();
                }
            }
            return oids;
        }

        public ObjectId CreateNewSillTable(
        System.Windows.Forms.DataGridView dgvTableData,
        string tableName,
        Autodesk.AutoCAD.Geometry.Point3d TopRight)
        {

            ObjectId resoid = ObjectId.Null;
            CADHelp.CADHelper ch = new CADHelp.CADHelper();
            string setBlkName = Autodesk.AutoCAD.DatabaseServices.SymbolUtilityServices.GetBlockNameFromInsertPathName(Westmark.CADNames.CADFilePaths.SETTINGS_BLOCK_PATH);
            ch.DeleteBlockByName(setBlkName, true);
            ObjectId setOID = ch.InsertBlock(setBlkName, new Point3d(0, 0, 0));
            ch.DeleteBlockByName(setBlkName, false);

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                Table tbl = new Table();

                tbl.Position = TopRight;

                tbl.TableStyle = TableTools.GetTableStyle(Westmark.CADNames.SillTable.TableStyleName); // db.Tablestyle;
                // Row count and Column count
                tbl.SetSize((dgvTableData.RowCount + 2), dgvTableData.ColumnCount);

                tbl.Cells[0, 0].TextString = tableName;

                Hashtable colWidths = new Hashtable();
                string cellVal = string.Empty;
                for (int i = 0; i < dgvTableData.ColumnCount; i++)
                {
                    tbl.Cells[1, i].TextString = dgvTableData.Columns[i].HeaderText;
                    colWidths.Add(i, Utils.GetTextExtents(ch.TextStyleStdID, dgvTableData.Columns[i].HeaderText, 3).X);
                }
                
                tbl.HorizontalCellMargin = 2;
                // tbl.Cells[0, 0];
                for (int i = 2; i < tbl.Rows.Count; i++)
                {
                    // tbl.Rows[i].Height = 5.625;
                    for (int j = 0; j < tbl.Columns.Count; j++)
                    {
                        tbl.SetAlignment(i, j, CellAlignment.MiddleCenter);
                        
                        int dgvi = i - 2;
                        if (dgvTableData[j, dgvi].Value == null)
                        {
                            cellVal = string.Empty;
                        }
                        else
                        {
                            cellVal = dgvTableData[j, dgvi].Value.ToString();
                        }
                        if (cellVal.Trim().Length > 0)
                        {
                            if (Utils.GetTextExtents(ch.TextStyleStdID, cellVal, tbl.TextHeight(i, j)).X > (double)colWidths[j])
                            {
                                colWidths[j] = Utils.GetTextExtents(ch.TextStyleStdID, cellVal, tbl.TextHeight(i, j)).X;
                            }
                            tbl.Cells[i, j].TextString = cellVal;
                        }
                    }
                }
                for (int i = 0; i < colWidths.Count; i++)
                {
                    // set column widths based on maximum text width per column
                    tbl.Columns[i].Width = (double)colWidths[i] + 6;
                    if (dgvTableData.Columns[i].HeaderText.Contains("["))
                    {
                        tbl.Columns[i].Width = 24;
                    }
                }
                tbl.Columns[0].Width = 18;
                // LOCK EVERY CELL
                tbl.Cells[0, 0].State = CellStates.ContentReadOnly;
                for (int r = 1; r < tbl.Rows.Count; r++)
                {
                    for (int c = 0; c < tbl.Columns.Count; c++)
                    {
                        tbl.Cells[r, c].State = CellStates.ContentReadOnly;
                    }
                }
                try
                {
                    tbl.BreakOptions = TableBreakOptions.RepeatTopLabels | TableBreakOptions.EnableBreaking | TableBreakOptions.AllowManualHeights | TableBreakOptions.AllowManualPositions;
                    tbl.Layer = "0";
                }
                catch
                {
                }

                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace],
                  OpenMode.ForWrite);
                btr.AppendEntity(tbl);
                trans.AddNewlyCreatedDBObject(tbl, true);
                resoid = tbl.ObjectId;
                trans.Commit();
            }
            return resoid;
        }

        internal ObjectId CreateNewHFTable(System.Windows.Forms.DataGridView dgvTable, string tblTitle, bool UserPick)
        {
            Point3d ipnt = new Point3d(0, 0, 0);
            ObjectId resID = CreateNewHFTable(dgvTable, tblTitle, ipnt);
            if ((UserPick) && (resID != ObjectId.Null))
            {
                CADHelp.JigEnt je = new CADHelp.JigEnt();
                je.DragEnt(resID, ipnt);
            }
            return resID;
        }
    }
}

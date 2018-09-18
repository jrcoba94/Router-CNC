using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Servicios.Model;
using Servicios.Servicios;
using System.Net;
using PagedList;
using System.Data.Entity;
using System.IO;

#region Referencias para la conversión de imagen

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using DXFLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Xml.Linq;

#endregion

namespace ProyectoRoutingCNC.Controllers
{
    public class ProductosController : Controller
    {
        #region Variables para la conversión del archivo de la clase GCodeFromDrill

        private static StringBuilder finalString = new StringBuilder();
        private static StringBuilder gcodeString = new StringBuilder();
        private static bool gcodeUseSpindle = false;            // Switch on/off spindle for Pen down/up (M3/M5)
        private static bool gcodeToolChange = false;            // Apply tool exchange command
        private static bool importComments = true;              // if true insert additional comments into GCode
        private static bool importUnitmm = true;                // convert units if needed

        private static string infoDate = "unknown";
        private static bool infoModeIsAbsolute = true;
        private static string infoUnits = "Inch";
        private static double infoFraction = 0.00001;         // default 1/100000
        private static string[] infoDrill = new string[20];

        #endregion

        #region Variables para la conversión del archivo de la clase GCodeFromDXF

        private static int svgToolMax = 100;            // max amount of tools
        //private static StringBuilder[] gcodeString = new StringBuilder[svgToolMax];
        private static int gcodeStringIndex = 0;
        //private static StringBuilder finalString = new StringBuilder();
        //private static bool gcodeUseSpindle = false; // Switch on/off spindle for Pen down/up (M3/M5)
        private static bool gcodeReduce = false;        // if true remove G1 commands if distance is < limit
        private static double gcodeReduceVal = .1;        // limit when to remove G1 commands

        private static bool gcodeZIncEnable = false;
        private static double gcodeZIncrement = 2;

        private static bool dxfPauseElement = true;     // if true insert GCode pause M0 before each element
        private static bool dxfPausePenDown = true;     // if true insert pause M0 before pen down
        private static bool dxfComments = true;         // if true insert additional comments into GCode
        //private static bool importUnitmm = true;        // convert units if needed

        private static ArrayList drawingList;
        private static ArrayList objectIdentifier;

        private static int dxfBezierAccuracy = 6;       // applied line segments at bezier curves

        #endregion

        #region Variables para la conversión del archivo de la clase GCodeFromFont

        public static string gcFontName = "standard";
        public static string gcText = "";       // text to convert
        public static int gcFont = 0;           // text to convert
        public static int gcAttachPoint = 7;    // origin of text 1 = Top left; 2 = Top center; 3 = Top right; etc
        public static double gcHeight = 1;      // desired Text height
        public static double gcWidth = 1;       // desired Text width
        public static double gcAngle = 0;
        public static double gcSpacing = 1;     // Percentage of default (3-on-5) line spacing to be applied. Valid values range from 0.25 to 4.00.
        public static double gcOffX = 0;
        public static double gcOffY = 0;
        public static double gcLineDistance = 1.5;
        public static double gcFontDistance = 0;

        public static bool gcPauseLine = false;
        public static bool gcPauseWord = false;
        public static bool gcPauseChar = false;

        private static double gcLetterSpacing = 3;  //  # LetterSpacing:     3
        private static double gcWordSpacing = 6.75; //  # WordSpacing:       6.75

        private static double offsetX = 0;
        private static double offsetY = 0;
        private static bool gcodePenIsUp = false;
        private static bool useLFF = false;

        #endregion

        #region Variables para la conversión del archivo de la clase Image

        Bitmap loadedImage;
        Bitmap originalImage;
        Bitmap adjustedImage;
        Bitmap resultImage;
        private static int svgToolMax = 256;            // max amount of tools
        private static StringBuilder[] gcodeString = new StringBuilder[svgToolMax];
        private static int gcodeStringIndex = 0;
        private static StringBuilder finalString = new StringBuilder();
        private static StringBuilder tmpString = new StringBuilder();
        private static int svgToolIndex = 0;            // last index
        private static bool gcodeToolChange = false;          // Apply tool exchange command
        private static bool gcodeSpindleToggle = false; // Switch on/off spindle for Pen down/up (M3/M5)
        private static bool loadFromFile = false;

        private string imagegcode = "";
        public string imageGCode
        { get { return imagegcode; } }

        public GCodeFromImage(bool loadFile = false)
        {
            CultureInfo ci = new CultureInfo(Properties.Settings.Default.language);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            InitializeComponent();
            loadFromFile = loadFile;
        }

        #endregion

        #region Variables para la conversión del archivo de la clase Shape

        private string shapegcode = "";
        public string shapeGCode
        { get { return shapegcode; } }
        //private static StringBuilder gcodeString = new StringBuilder();

        //public float offsetX = 0, offsetY = 0;
        //public GCodeFromShape()
        //{
        //    CultureInfo ci = new CultureInfo(Properties.Settings.Default.language);
        //    Thread.CurrentThread.CurrentCulture = ci;
        //    Thread.CurrentThread.CurrentUICulture = ci;
        //}

        #endregion

        #region Variables para la conversión del archivo de la clase SVG

        //private static int svgToolMax = 100;            // max amount of tools
        //private static StringBuilder[] gcodeString = new StringBuilder[svgToolMax];
        //private static int gcodeStringIndex = 0;
        //private static StringBuilder finalString = new StringBuilder();

        // following settings will be read from Properties.Settings.Default in startConvert()
        private static int svgBezierAccuracy = 6;       // applied line segments at bezier curves
        private static bool svgScaleApply = true;       // try to scale final GCode if true
        private static float svgMaxSize = 100;          // final GCode size (greater dimension) if scale is applied
        private static bool svgClosePathExtend = true;  // if true move to first and second point of path to get overlap
        private static bool svgToolColor = true;        // if true take tool nr. from nearest pallet entry
        private static bool svgToolSort = true;         // if true sort objects by tool-nr. (to avoid back and forth pen change)
        //private static int svgToolIndex = 0;            // last index

        private static bool svgNodesOnly = true;        // if true only do pen-down -up on given coordinates

        private static bool svgPauseElement = true;     // if true insert GCode pause M0 before each element
        private static bool svgPausePenDown = true;     // if true insert pause M0 before pen down
        private static bool svgComments = true;         // if true insert additional comments into GCode

        //private static bool gcodeReduce = false;        // if true remove G1 commands if distance is < limit
        //private static float gcodeReduceVal = .1f;        // limit when to remove G1 commands

        private static float gcodeXYFeed = 2000;        // XY feed to apply for G1

        //private static bool gcodeToolChange = false;          // Apply tool exchange command
        private static int gcodeToolNr = 0;

        // Using Z-Axis for Pen up down
        private static bool gcodeZApply = true;         // if true insert Z movements for Pen up/down
        private static float gcodeZUp = 2;              // Z-up position
        private static float gcodeZDown = -2;           // Z-down position
        private static float gcodeZFeed = 500;          // Z feed to apply for G1

        // Using Spindle pwr. to switch on/off laser
        //private static bool gcodeUseSpindle = false; // Switch on/off spindle for Pen down/up (M3/M5)

        private static bool svgConvertToMM = true;
        private static float gcodeScale = 1;                    // finally scale with this factor if svgScaleApply and svgMaxSize
        private static Matrix[] matrixGroup = new Matrix[10];   // store SVG-Group transformation matrixes
        private static Matrix matrixElement = new Matrix();     // store finally applied matrix
        private static Matrix oldMatrixElement = new Matrix();     // store finally applied matrix

        /// <summary>
        /// Entrypoint for conversion: apply file-path or file-URL
        /// </summary>
        /// <param name="file">String keeping file-name or URL</param>
        /// <returns>String with GCode of imported data</returns>
        private static XElement svgCode;
        private static bool fromClipboard = false;
        private static bool importInMM = false;

        #endregion

        #region Variables para la conversión del archivo de la clase Text

        private string textgcode = "";
        public string textGCode
        { get { return textgcode; } }

        #endregion

        #region Variables para la conversión del archivo de la clase gcodeRelated

        private static string formatCode = "00";
        private static string formatNumber = "0.###";

        private static int gcodeLines = 0;              // counter for GCode lines
        private static float gcodeDistance = 0;         // counter for GCode move distance
        private static int gcodeDownUp = 0;             // counter for GCode Pen Down / Up
        private static float gcodeTime = 0;             // counter for GCode work time
        private static int gcodePauseCounter = 0;       // counter for GCode pause M0 commands
        private static int gcodeToolCounter = 0;       // counter for GCode Tools
        private static string gcodeToolText = "";       // counter for GCode Tools

        //public static float gcodeXYFeed = 1999;        // XY feed to apply for G1
        private static bool gcodeComments = true;       // if true insert additional comments into GCode

        //private static bool gcodeToolChange = false;          // Apply tool exchange command
        private static bool gcodeToolChangeM0 = false;

        // Using Z-Axis for Pen up down
        //private static bool gcodeZApply = true;         // if true insert Z movements for Pen up/down
        //private static float gcodeZUp = 1.999f;         // Z-up position
        //public static float gcodeZDown = -1.999f;      // Z-down position
        //public static float gcodeZFeed = 499;          // Z feed to apply for G1
        public static float gcodeZRepitition;          // Z feed to apply for G1

        // Using Spindle pwr. to switch on/off laser
        //private static bool gcodeSpindleToggle = false; // Switch on/off spindle for Pen down/up (M3/M5)
        public static float gcodeSpindleSpeed = 999; // Spindle speed to apply
        private static string gcodeSpindleCmd = "3"; // Spindle Command M3 / M4

        // Using Spindle-Speed als PWM output to control RC-Servo
        private static bool gcodePWMEnable = false;     // Change Spindle speed for Pen down/up
        private static float gcodePWMUp = 199;          // Spindle speed for Pen-up
        private static float gcodePWMDlyUp = 0;         // Delay to apply after Pen-up (because servo is slow)
        private static float gcodePWMDown = 799;        // Spindle speed for Pen-down
        private static float gcodePWMDlyDown = 0;       // Delay to apply after Pen-down (because servo is slow)

        private static bool gcodeIndividualTool = false;     // Use individual Pen down/up
        private static string gcodeIndividualUp = "";
        private static string gcodeIndividualDown = "";

        private static bool gcodeCompress = false;      // reduce code by avoiding sending again same G-Nr and unchanged coordinates
        public static bool gcodeRelative = false;      // calculate relative coordinates for G91
        private static bool gcodeNoArcs = false;        // replace arcs by line segments
        private static float gcodeAngleStep = 0.1f;

        #endregion

        #region Variables para la conversión del archivo de la clase svgPalette

        public struct palette
        {
            public int toolnr;
            public System.Drawing.Color clr;
            public bool use;
            public int codeSize;
            public int pixelCount;
            public double diff;
            public String name;
        }

        #endregion














        SrvProducto oSrvProducto = new SrvProducto();
        private RoutingCNCEntities db = new RoutingCNCEntities();
        public string UploadDirectory = "";

        // GET: Producto
        public ActionResult Index(int? page)
        {
            try
            {
                ViewBag.MensajeExitoso = TempData["MensajeExitoso"].ToString();               
            }
            catch
            {

            }

            try
            {
                ViewBag.MensajeActualizar = TempData["MensajeActualizar"].ToString();
            }
            catch
            {

            }

            try
            {
                ViewBag.MensajeEliminar = TempData["MensajeEliminar"].ToString();
            }
            catch
            {

            }

            return View(db.Producto.ToList().ToPagedList(page ?? 1, 30));
        }

        public ActionResult CrearSigno()
        {
            return View("Crear");
        }

        public ActionResult Regresar()
        {
            return View("Index");
        }

        public ActionResult Detalles(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Producto oProducto = db.Producto.Find(id);
            if (oProducto == null)
            {
                return HttpNotFound();
            }
            return View("Detalle", oProducto);
        }

        #region Método que se encarga de dar de alta a un nuevo Producto

        // GET: Productos/Create
        public ActionResult Create()
        {
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor");
            return View("Crear");
        }

        // POST: Productos/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Producto producto, HttpPostedFileBase files)
        {
            try
            {


                if (ModelState.IsValid)
                {
                    try
                    {
                        UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                        HttpPostedFileBase file = Request.Files["files"];
                        var directorio = UploadDirectory;
                        string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                        string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                        string resultFileUrl = directorio + resultFileName;
                        string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                        bool hasFile = false;
                        ImagenProducto oImgProduct = null;

                        if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                        {
                            file.SaveAs(resultFilePath);
                            producto.ImagenPortada = /*servername +*/ resultFileUrl.Replace("~/", "");

                            oImgProduct = new ImagenProducto();
                            oImgProduct.Url = producto.ImagenPortada.Replace("~/", "");

                            hasFile = true;
                        }

                        db.Producto.Add(producto);
                        db.SaveChanges();

                        oImgProduct.ProductoID = producto.ProductoID;
                        db.ImagenProducto.Add(oImgProduct);
                        db.SaveChanges();

                        //Mensaje que se imprime en un alert
                        TempData["MensajeExitoso"] = "El registro se agrego de manera exitosa.";
                        ViewBag.MensajeExitoso = TempData["MensajeExitoso"];

                        if (hasFile)
                        {
                            oImgProduct.ProductoID = producto.ProductoID;
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(SrvMessages.getMessageSQL(ex));
                    }
                   
                    return RedirectToAction("Index");
                }
                //}
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }

            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(producto);

        }

        #endregion


        #region Método que se encarga de editar la información de un Producto

        // GET: Productos/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Producto producto = db.Producto.Find(id);
            if (producto == null)
            {
                return HttpNotFound();
            }
          //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View("Editar", producto);
        }

        // POST: Productos/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Producto producto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    UploadDirectory = ProyectoRoutingCNC.Properties.Settings.Default.DirectorioImagenes;
                    HttpPostedFileBase file = Request.Files["files"];
                    var directorio = UploadDirectory;
                    string pathRandom = Path.GetRandomFileName().Replace(/*'.', '-'*/"~/", "");
                    string resultFileName = pathRandom + '_' + file.FileName.Replace("~/", "");
                    string resultFileUrl = directorio + resultFileName;
                    string resultFilePath = System.Web.HttpContext.Current.Request.MapPath(resultFileUrl);

                    bool hasFile = false;
                    ImagenProducto oImgProduct = null;
                    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    {
                        file.SaveAs(resultFilePath);
                        producto.ImagenPortada = /*servername +*/ resultFileUrl.Replace("~/", "");

                        oImgProduct = new ImagenProducto();
                        oImgProduct.Url = producto.ImagenPortada.Replace("~/", "");
                        hasFile = true;
                    }
                    db.SaveChanges();

                    oImgProduct.ProductoID = producto.ProductoID;
                    db.SaveChanges();

                    if (hasFile)
                    {
                        oImgProduct.ProductoID = producto.ProductoID;
                        db.SaveChanges();
                    }

                    db.Entry(producto).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["MensajeActualizar"] = "La información se actualizo correctamente.";
                    ViewBag.MensajeActualizar = TempData["MensajeActualizar"];

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(SrvMessages.getMessageSQL(ex));
            }
            //ViewBag.ProveedorId = new SelectList(db.Proveedor, "ProveedorID", "NombreProveedor", producto.ProveedorID);
            return View(producto);
        }

        #endregion


        #region Método que se encarga de dar de baja a un registro

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Producto producto = db.Producto.Find(id);
            //if (producto == null)
            //{
            //    return HttpNotFound();
            //}
            //db.Producto.Remove(producto);
            //producto.Estatus = false;
            //db.SaveChanges();

            try
            {
                Producto producto = db.Producto.Find(id);
                db.Producto.Remove(producto);
                producto.Estatus = false;
                db.SaveChanges();
                TempData["MensajeEliminar"] = "El registro de elimino correctamente.";
                ViewBag.MensajeEliminar = TempData["MensajeEliminar"];
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                //TempData["MensajeEliminar"] = "No se pudo eliminar el registro.";
                //ViewBag.MensajeEliminar = TempData["MensajeEliminar"];
                throw new Exception(SrvMessages.getMessageSQL(ex));
                return RedirectToAction("Index");
            }

            //return View("Eliminar", producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

            Producto producto = db.Producto.Find(id);
            db.Producto.Remove(producto);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion










        #region Métodos para la conversión del archivo de la clase GCodeFromDill

        public static string ConvertFile(string file)
        {
            if (file == "")
            {
                MessageBox.Show("Empty file name");
                return "";
            }

            gcode.setup();      // initialize GCode creation (get stored settings for export)
            gcodeToolChange = Properties.Settings.Default.importGCTool;
            importComments = Properties.Settings.Default.importSVGAddComments;
            importUnitmm = Properties.Settings.Default.importUnitmm;

            for (int i = 0; i < 20; i++)
                infoDrill[i] = "";

            if (file.Substring(0, 4) == "http")
            {
                MessageBox.Show("Load via http is not supported up to now");
            }
            else
            {
                string file_dri = "", file_drd = "";
                if (file.Substring(file.Length - 3, 3).ToLower() == "dri")      // build up filenames
                {
                    file_dri = file;
                    file_drd = file.Substring(0, file.Length - 3) + "drd";
                }
                else if (file.Substring(file.Length - 3, 3).ToLower() == "drd")      // build up filenames
                {
                    file_drd = file;
                    file_dri = file.Substring(0, file.Length - 3) + "dri";
                }
                else
                {
                    file_drd = file;        // KiCad drl
                    file_dri = "";
                }
                if (File.Exists(file_dri))
                {
                    try
                    {
                        string[] drillInformation = File.ReadAllLines(file_dri);     // get drill information
                        getDrillInfos(drillInformation);
                    }
                    catch (Exception e)
                    { MessageBox.Show("Error '" + e.ToString() + "' in file " + file_dri); return ""; }
                }
                else { MessageBox.Show("Drill information not found : " + file_dri + "\r\nTry to convert *.drd with default settings"); return ""; }

                if (File.Exists(file_drd))
                {
                    try
                    {
                        string[] drillCoordinates = File.ReadAllLines(file_drd);     // get drill coordinates
                        convertDrill(drillCoordinates, file_drd);
                    }
                    catch (Exception e)
                    { MessageBox.Show("Error '" + e.ToString() + "' in file " + file_drd); return ""; }
                }
                else { MessageBox.Show("Drill file does not exist: " + file_drd); return ""; }
            }

            string header = gcode.GetHeader("Drill import", file);
            string footer = gcode.GetFooter();
            gcodeUseSpindle = Properties.Settings.Default.importGCZEnable;

            finalString.Clear();

            if (gcodeUseSpindle) gcode.SpindleOn(finalString, "Start spindle - Option Z-Axis");

            finalString.Append(gcodeString);
            if (gcodeUseSpindle) gcode.SpindleOff(finalString, "Stop spindle - Option Z-Axis");

            return header + finalString.ToString().Replace(',', '.') + footer;
        }


        private static void getDrillInfos(string[] drillInfo)
        {
            foreach (string line in drillInfo)
            {
                string[] part = line.Split(':');
                if (part[0].IndexOf("Date") >= 0) { infoDate = part[1].Trim(); }
                if (part[0].IndexOf("Data Mode") >= 0)
                {
                    infoModeIsAbsolute = (part[1].IndexOf("Absolute") >= 0) ? true : false;
                }
                if (part[0].IndexOf("Units") >= 0)
                { }
                if (part[0].IndexOf("T") >= 0)
                { }
            }
        }

        private static void convertDrill(string[] drillCode, string info)
        {
            gcodeString.Clear();
            if (importComments)
            {
                gcodeString.AppendFormat("( Import Unit    : {0} )\r\n", infoUnits);
                gcodeString.AppendFormat("( Import Fraction: {0} )\r\n", infoFraction);
                gcodeString.Append("( Numbers exported to mm )\r\n");
            }
            gcode.PenUp(gcodeString, "Drill Start ");
            bool isHeader = false;
            foreach (string line in drillCode)
            {
                if (line.IndexOf("%") >= 0)
                { isHeader = (isHeader) ? false : true; }

                if (isHeader)
                {
                    if (importComments)
                        gcodeString.AppendLine("( " + line + " )");
                    if ((line.IndexOf("T") >= 0) && (line.IndexOf("C") >= 0))
                    {
                        string[] part = line.Split('C');
                        int tnr = 0;
                        double dmm = 0;
                        Int32.TryParse(part[0].Substring(1), out tnr);
                        Double.TryParse(part[1].Substring(0), out dmm);
                        dmm = dmm * 25.4;
                        infoDrill[tnr] = part[1] + " Inch = " + dmm.ToString() + " mm";
                    }
                }
                else
                {
                    if (line.IndexOf("T") >= 0)
                    {
                        gcodeString.AppendLine(" ");        // add empty line for better view
                        if (gcodeToolChange)
                        {
                            int tnr = 0;
                            Int32.TryParse(line.Substring(1), out tnr);

                            gcode.Tool(gcodeString, tnr, infoDrill[tnr]);
                        }
                        else
                        { gcodeString.AppendLine("( " + line + " tool change not enabled)"); }
                    }

                    if ((line.IndexOf("X") >= 0) && (line.IndexOf("Y") >= 0))
                    {
                        string[] part = line.Split('Y');
                        double x = 0;
                        double y = 0;
                        double.TryParse(part[0].Substring(1), out x);
                        double.TryParse(part[1].Substring(0), out y);

                        x = x * infoFraction;
                        y = y * infoFraction;
                        if (importUnitmm)
                        {
                            x = x * 25.4;
                            y = y * 25.4;
                        }
                        string cmt = "";
                        if (importComments)
                            cmt = line;
                        gcode.MoveToRapid(gcodeString, (float)x, (float)y, cmt);
                        gcode.PenDown(gcodeString);
                        gcode.PenUp(gcodeString);
                    }
                }
            }
        }

        #endregion

        #region Método para la conversión del archivo de la clase GCodeFromDXF

        public static string convertFromText(string text)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(text);
            MemoryStream stream = new MemoryStream(byteArray);
            loadDXF(stream);
            return convertDXF("from Clipboard");
        }
        public static string ConvertFromFile(string file)
        {
            if (file == "")
            { MessageBox.Show("Empty file name"); return ""; }

            if (file.Substring(0, 4) == "http")
            {
                string content = "";
                using (var wc = new System.Net.WebClient())
                {
                    try { content = wc.DownloadString(file); }
                    catch { MessageBox.Show("Could not load content from " + file); return ""; }
                }
                int pos = content.IndexOf("dxfrw");
                if ((content != "") && (pos >= 0) && (pos < 8))
                {
                    try
                    {
                        byte[] byteArray = Encoding.UTF8.GetBytes(content);
                        MemoryStream stream = new MemoryStream(byteArray);
                        loadDXF(stream);
                    }
                    catch (Exception e)
                    { MessageBox.Show("Error '" + e.ToString() + "' in DXF file " + file); }
                }
                else
                    MessageBox.Show("This is probably not a DXF document.\r\nFirst line: " + content.Substring(0, 50));
            }
            else
            {
                if (File.Exists(file))
                {
                    try
                    {
                        loadDXF(file);
                    }
                    catch (Exception e)
                    { MessageBox.Show("Error '" + e.ToString() + "' in DXF file " + file); return ""; }
                }
                else { MessageBox.Show("File does not exist: " + file); return ""; }
            }
            return convertDXF(file);
        }

        private static string convertDXF(string txt)
        {
            drawingList = new ArrayList();
            objectIdentifier = new ArrayList();
            gcodeStringIndex = 0;
            gcodeString[gcodeStringIndex] = new StringBuilder();
            gcodeString[gcodeStringIndex].Clear();
            importUnitmm = Properties.Settings.Default.importUnitmm;
            gcode.setup();  // initialize GCode creation (get stored settings for export)

            GetVectorDXF();

            string header = gcode.GetHeader("DXF import", txt);
            string footer = gcode.GetFooter();
            gcodeUseSpindle = Properties.Settings.Default.importGCZEnable;

            finalString.Clear();

            if (gcodeUseSpindle) gcode.SpindleOn(finalString, "Start spindle - Option Z-Axis");
            finalString.Append(gcodeString[0]);     //.Replace(',', '.')
            if (gcodeUseSpindle) gcode.SpindleOff(finalString, "Stop spindle - Option Z-Axis");

            string output = "";
            if (Properties.Settings.Default.importSVGRepeatEnable)
            {
                for (int i = 0; i < Properties.Settings.Default.importSVGRepeat; i++)
                    output += finalString.ToString().Replace(',', '.');

                return header + output + footer;
            }
            else
                return header + finalString.ToString().Replace(',', '.') + footer;
        }


        /// <summary>
        /// Load and parse DXF code
        /// </summary>
        /// <param name="filename">String keeping file-name</param>
        /// <returns></returns>
        private static DXFDocument doc;
        private static void loadDXF(string filename)
        {
            doc = new DXFDocument();
            doc.Load(filename);
            //    GetVectorDXF();
        }
        private static void loadDXF(Stream content)
        {
            doc = new DXFDocument();
            doc.Load(content);
            //    GetVectorDXF();
        }
        private static void GetVectorDXF()
        {
            gcodePenUp("DXF Start");
            lastGCX = -1; lastGCY = -1; lastSetGCX = -1; lastSetGCY = -1;
            //            MessageBox.Show("unit "+ doc.Header.MeasurementUnits.Value.ToString());

            foreach (DXFEntity dxfEntity in doc.Entities)
            {
                dxfBezierAccuracy = (int)Properties.Settings.Default.importSVGBezier;
                gcodeReduce = Properties.Settings.Default.importSVGReduce;
                gcodeReduceVal = (double)Properties.Settings.Default.importSVGReduceLimit;

                gcodeZIncEnable = Properties.Settings.Default.importGCZIncEnable;
                gcodeZIncrement = (double)Properties.Settings.Default.importGCZIncrement;

                dxfPauseElement = Properties.Settings.Default.importSVGPauseElement;
                dxfPausePenDown = Properties.Settings.Default.importSVGPausePenDown;
                dxfComments = Properties.Settings.Default.importSVGAddComments;

                if (dxfEntity.GetType() == typeof(DXFInsert))
                {
                    DXFInsert ins = (DXFInsert)dxfEntity;
                    double ins_x = (double)ins.InsertionPoint.X;
                    double ins_y = (double)ins.InsertionPoint.Y;

                    foreach (DXFBlock block in doc.Blocks)
                    {
                        if (block.BlockName.ToString() == ins.BlockName)
                        {
                            if (dxfComments)
                            {
                                gcode.Comment(gcodeString[gcodeStringIndex], "Color: " + block.ColorNumber.ToString());
                                gcode.Comment(gcodeString[gcodeStringIndex], "Block: " + block.BlockName.ToString() + " at " + ins_x.ToString() + " " + ins_y.ToString());
                            }
                            foreach (DXFEntity blockEntity in block.Children)
                            {
                                processEntities(blockEntity, ins_x, ins_y);
                            }
                            if (dxfComments)
                                gcode.Comment(gcodeString[gcodeStringIndex], "Block: " + block.BlockName.ToString() + " end");
                        }
                    }
                }
                else
                    processEntities(dxfEntity);
            }
            if (askPenUp)   // retrieve missing pen up
            { gcode.PenUp(gcodeString[gcodeStringIndex]); askPenUp = false; }
        }

        /// <summary>
        /// Parse DXF entities
        /// </summary>
        /// <param name="entity">Entity to convert</param>
        /// <param name="offsetX">Offset to apply if called by block insertion</param>
        /// <returns></returns>                       
        private static System.Windows.Point[] points;
        private static bool isReduceOk = false;

        /// <summary>
        /// Process entities
        /// </summary>
        private static void processEntities(DXFEntity entity, double offsetX = 0, double offsetY = 0)
        {
            int index = 0;
            double x, y, x2 = 0, y2 = 0, bulge;
            if (dxfComments)
            {
                gcode.Comment(gcodeString[gcodeStringIndex], "Entity: " + entity.GetType().ToString());
                gcode.Comment(gcodeString[gcodeStringIndex], "Color:  " + entity.ColorNumber.ToString());
            }

            if (entity.GetType() == typeof(DXFPointEntity))
            {
                DXFPointEntity point = (DXFPointEntity)entity;
                x = (float)point.Location.X + (float)offsetX;
                y = (float)point.Location.Y + (float)offsetY;
                gcodeStartPath(x, y, "Start Point");
                gcodeStopPath();
            }

            #region DXFLWPolyline
            else if (entity.GetType() == typeof(DXFLWPolyLine))
            {
                DXFLWPolyLine lp = (DXFLWPolyLine)entity;
                index = 0; bulge = 0;
                DXFLWPolyLine.Element coordinate;
                bool roundcorner = false;
                for (int i = 0; i < lp.VertexCount; i++)
                {
                    coordinate = lp.Elements[i];
                    bulge = coordinate.Bulge;
                    x = (double)coordinate.Vertex.X + (double)offsetX;
                    y = (double)coordinate.Vertex.Y + (double)offsetY;
                    if (i == 0)
                    {
                        gcodeStartPath(x, y, "Start LWPolyLine");
                        isReduceOk = true;
                    }
                    else
                    {
                        if (!roundcorner)
                            gcodeMoveTo(x, y, "");
                        if (bulge != 0)
                        {
                            if (i < (lp.VertexCount - 1))
                                AddRoundCorner(lp.Elements[i], lp.Elements[i + 1]);
                            else
                                AddRoundCorner(lp.Elements[i], lp.Elements[0]);
                            roundcorner = true;
                        }
                        else
                            roundcorner = false;
                    }
                    x2 = x; y2 = y;
                }
                if (lp.Flags > 0)
                    gcodeMoveTo((float)lp.Elements[0].Vertex.X, (float)lp.Elements[0].Vertex.Y, "End LWPolyLine");
                gcodeStopPath();
            }
            #endregion
            #region DXFPolyline
            else if (entity.GetType() == typeof(DXFPolyLine))
            {
                DXFPolyLine lp = (DXFPolyLine)entity;
                index = 0;
                foreach (DXFVertex coordinate in lp.Children)
                {
                    if (coordinate.GetType() == typeof(DXFVertex))
                        if (coordinate.Location.X != null && coordinate.Location.Y != null)
                        {
                            x = (float)coordinate.Location.X + (float)offsetX;
                            y = (float)coordinate.Location.Y + (float)offsetY;
                            if (index == 0)
                            {
                                gcodeStartPath(x, y, "Start PolyLine");
                            }
                            else
                                gcodeMoveTo(x, y, "");
                            index++;
                        }
                }
                gcodeStopPath();
            }
            #endregion
            #region DXFLine
            else if (entity.GetType() == typeof(DXFLine))
            {
                DXFLine line = (DXFLine)entity;
                x = (float)line.Start.X + (float)offsetX;
                y = (float)line.Start.Y + (float)offsetY;
                x2 = (float)line.End.X + (float)offsetX;
                y2 = (float)line.End.Y + (float)offsetY;
                isReduceOk = false;
                gcodeStartPath(x, y, "Start Line");
                gcodeMoveTo(x2, y2, "");
                gcodeStopPath();
            }
            #endregion
            #region DXFSpline
            else if (entity.GetType() == typeof(DXFSpline))
            {
                DXFSpline spline = (DXFSpline)entity;
                index = 0;
                double cx0, cy0, cx1, cy1, cx2, cy2, cx3, cy3, cxMirror, cyMirror, lastX, lastY;
                lastX = (double)spline.ControlPoints[0].X + offsetX;
                lastY = (double)spline.ControlPoints[0].Y + offsetY;
                string cmt = "Start Spline " + spline.KnotValues.Count.ToString() + " " + spline.ControlPoints.Count.ToString() + " " + spline.FitPoints.Count.ToString();
                gcodeStartPath(lastX, lastY, cmt);
                isReduceOk = true;

                for (int rep = 0; rep < spline.ControlPointCount; rep += 4)
                {
                    cx0 = (double)spline.ControlPoints[rep].X + offsetX; cy0 = (double)spline.ControlPoints[rep].Y + offsetY;
                    cx1 = (double)spline.ControlPoints[rep + 1].X + offsetX; cy1 = (double)spline.ControlPoints[rep + 1].Y + offsetY;
                    cx2 = (double)spline.ControlPoints[rep + 2].X + offsetX; cy2 = (double)spline.ControlPoints[rep + 2].Y + offsetY;
                    cx3 = (double)spline.ControlPoints[rep + 3].X + offsetX; cy3 = (double)spline.ControlPoints[rep + 3].Y + offsetY;
                    points = new System.Windows.Point[4];
                    points[0] = new System.Windows.Point(cx0, cy0); //(qpx1, qpy1);
                    points[1] = new System.Windows.Point(cx1, cy1); //(qpx1, qpy1);
                    points[2] = new System.Windows.Point(cx2, cy2); //(qpx2, qpy2);
                    points[3] = new System.Windows.Point(cx3, cy3);
                    cxMirror = cx3 - (cx2 - cx3); cyMirror = cy3 - (cy2 - cy3);
                    lastX = cx3; lastY = cy3;
                    var b = GetBezierApproximation(points, dxfBezierAccuracy);
                    for (int i = 1; i < b.Points.Count; i++)
                        gcodeMoveTo((float)b.Points[i].X, (float)b.Points[i].Y, "");
                }
                gcodeStopPath();
            }
            #endregion
            #region DXFCircle
            else if (entity.GetType() == typeof(DXFCircle))
            {
                DXFCircle circle = (DXFCircle)entity;
                x = (float)circle.Center.X + (float)offsetX;
                y = (float)circle.Center.Y + (float)offsetY;
                gcodeStartPath(x + circle.Radius, y, "Start Circle");
                gcode.Arc(gcodeString[gcodeStringIndex], 2, (float)x + (float)circle.Radius, (float)y, -(float)circle.Radius, 0, "");
                gcodeStopPath();
            }
            #endregion

            else if (entity.GetType() == typeof(DXFEllipse))
            {
                DXFEllipse circle = (DXFEllipse)entity;
                gcode.Comment(gcodeString[gcodeStringIndex], "Ellipse: " + circle.ColorNumber.ToString());
            }
            #region DXFArc
            else if (entity.GetType() == typeof(DXFArc))
            {
                DXFArc arc = (DXFArc)entity;

                double X = (double)arc.Center.X + offsetX;
                double Y = (double)arc.Center.Y + offsetY;
                double R = arc.Radius;
                double startAngle = arc.StartAngle;
                double endAngle = arc.EndAngle;
                if (startAngle > endAngle) endAngle += 360;
                double stepwidth = (double)Properties.Settings.Default.importGCSegment;
                float StepAngle = (float)(Math.Asin(stepwidth / R) * 180 / Math.PI);// Settings.Default.page11arcMaxLengLine);
                double currAngle = startAngle;
                index = 0;
                while (currAngle < endAngle)
                {
                    double angle = currAngle * Math.PI / 180;
                    double rx = (double)(X + R * Math.Cos(angle));
                    double ry = (double)(Y + R * Math.Sin(angle));
                    if (index == 0)
                    {
                        gcodeStartPath(rx, ry, "Start Arc");
                        isReduceOk = true;
                    }
                    else
                        gcodeMoveTo(rx, ry, "");
                    currAngle += StepAngle;
                    if (currAngle > endAngle)
                    {
                        double angle2 = endAngle * Math.PI / 180;
                        double rx2 = (double)(X + R * Math.Cos(angle2));
                        double ry2 = (double)(Y + R * Math.Sin(angle2));

                        if (index == 0)
                        {
                            gcodeStartPath(rx2, ry2, "Start Arc");
                        }
                        else
                            gcodeMoveTo(rx2, ry2, "");
                    }
                    index++;
                }
                gcodeStopPath();
            }
            #endregion
            #region DXFMText
            else if (entity.GetType() == typeof(DXFMText))
            {   // https://www.autodesk.com/techpubs/autocad/acad2000/dxf/mtext_dxf_06.htm
                DXFMText txt = (DXFMText)entity;
                xyPoint origin = new xyPoint(0, 0);
                GCodeFromFont.reset();

                foreach (var entry in txt.Entries)
                {
                    if (entry.GroupCode == 1) { GCodeFromFont.gcText = entry.Value.ToString(); }
                    else if (entry.GroupCode == 40) { GCodeFromFont.gcHeight = Convert.ToDouble(entry.Value); }// gcode.Comment(gcodeString[gcodeStringIndex], "Height "+entry.Value); }
                    else if (entry.GroupCode == 41) { GCodeFromFont.gcWidth = Convert.ToDouble(entry.Value); }// gcode.Comment(gcodeString[gcodeStringIndex], "Width "+entry.Value); }
                    else if (entry.GroupCode == 71) { GCodeFromFont.gcAttachPoint = Convert.ToInt16(entry.Value); }// gcode.Comment(gcodeString[gcodeStringIndex], "Origin " + entry.Value); }
                    else if (entry.GroupCode == 10) { GCodeFromFont.gcOffX = Convert.ToDouble(entry.Value); }
                    else if (entry.GroupCode == 20) { GCodeFromFont.gcOffY = Convert.ToDouble(entry.Value); }
                    else if (entry.GroupCode == 50) { GCodeFromFont.gcAngle = Convert.ToDouble(entry.Value); }// gcode.Comment(gcodeString[gcodeStringIndex], "Angle " + entry.Value); }
                    else if (entry.GroupCode == 44) { GCodeFromFont.gcSpacing = Convert.ToDouble(entry.Value); }
                    else if (entry.GroupCode == 7) { GCodeFromFont.gcFontName = entry.Value.ToString(); }
                }
                GCodeFromFont.getCode(gcodeString[gcodeStringIndex]);
            }
            #endregion
            else
                gcode.Comment(gcodeString[gcodeStringIndex], "Unknown: " + entity.GetType().ToString());
        }

        private static PolyLineSegment GetBezierApproximation(System.Windows.Point[] controlPoints, int outputSegmentCount)
        {
            System.Windows.Point[] points = new System.Windows.Point[outputSegmentCount + 1];
            for (int i = 0; i <= outputSegmentCount; i++)
            {
                double t = (double)i / outputSegmentCount;
                points[i] = GetBezierPoint(t, controlPoints, 0, controlPoints.Length);
            }
            return new PolyLineSegment(points, true);
        }
        private static System.Windows.Point GetBezierPoint(double t, System.Windows.Point[] controlPoints, int index, int count)
        {
            if (count == 1)
                return controlPoints[index];
            var P0 = GetBezierPoint(t, controlPoints, index, count - 1);
            var P1 = GetBezierPoint(t, controlPoints, index + 1, count - 1);
            double x = (1 - t) * P0.X + t * P1.X;
            return new System.Windows.Point(x, (1 - t) * P0.Y + t * P1.Y);
        }

        /// <summary>
        /// Calculate round corner of DXFLWPolyLine if Bulge is given
        /// </summary>
        /// <param name="var1">First vertex coord</param>
        /// <param name="var2">Second vertex</param>
        /// <returns></returns>
        private static void AddRoundCorner(DXFLWPolyLine.Element var1, DXFLWPolyLine.Element var2)
        {
            double bulge = var1.Bulge;
            double p1x = (double)var1.Vertex.X;
            double p1y = (double)var1.Vertex.Y;
            double p2x = (double)var2.Vertex.X;
            double p2y = (double)var2.Vertex.Y;

            //Definition of bulge, from Autodesk DXF fileformat specs
            double angle = Math.Abs(Math.Atan(bulge) * 4);
            bool girou = false;

            //For my method, this angle should always be less than 180. 
            if (angle > Math.PI)
            {
                angle = Math.PI * 2 - angle;
                girou = true;
            }

            //Distance between the two vertexes, the angle between Center-P1 and P1-P2 and the arc radius
            double distance = Math.Sqrt(Math.Pow(p1x - p2x, 2) + Math.Pow(p1y - p2y, 2));
            double alpha = (Math.PI - angle) / 2;
            double ratio = distance * Math.Sin(alpha) / Math.Sin(angle);
            if (angle == Math.PI)
                ratio = distance / 2;

            double xc, yc, direction;

            //Used to invert the signal of the calculations below
            if (bulge < 0)
                direction = 1;
            else
                direction = -1;

            //calculate the arc center
            double part = Math.Sqrt(Math.Pow(2 * ratio / distance, 2) - 1);
            if (!girou)
            {
                xc = ((p1x + p2x) / 2) - direction * ((p1y - p2y) / 2) * part;
                yc = ((p1y + p2y) / 2) + direction * ((p1x - p2x) / 2) * part;
            }
            else
            {
                xc = ((p1x + p2x) / 2) + direction * ((p1y - p2y) / 2) * part;
                yc = ((p1y + p2y) / 2) - direction * ((p1x - p2x) / 2) * part;
            }

            string cmt = "";
            if (dxfComments) { cmt = "Bulge " + bulge.ToString(); }
            if (bulge > 0)
                gcode.Arc(gcodeString[gcodeStringIndex], 3, (float)p2x, (float)p2y, (float)(xc - p1x), (float)(yc - p1y), cmt);
            else
                gcode.Arc(gcodeString[gcodeStringIndex], 2, (float)p2x, (float)p2y, (float)(xc - p1x), (float)(yc - p1y), cmt);
        }

        /// <summary>
        /// Transform XY coordinate using matrix and scale  
        /// </summary>
        /// <param name="pointStart">coordinate to transform</param>
        /// <returns>transformed coordinate</returns>
        private static System.Windows.Point translateXY(float x, float y)
        {
            System.Windows.Point coord = new System.Windows.Point(x, y);
            return translateXY(coord);
        }
        private static System.Windows.Point translateXY(System.Windows.Point pointStart)
        {
            return pointStart;// pointResult;
        }
        /// <summary>
        /// Transform I,J coordinate using matrix and scale  
        /// </summary>
        /// <param name="pointStart">coordinate to transform</param>
        /// <returns>transformed coordinate</returns>
        private static System.Windows.Point translateIJ(float i, float j)
        {
            System.Windows.Point coord = new System.Windows.Point(i, j);
            return translateIJ(coord);
        }
        private static System.Windows.Point translateIJ(System.Windows.Point pointStart)
        {
            System.Windows.Point pointResult = pointStart;
            double tmp_i = pointStart.X, tmp_j = pointStart.Y;
            return pointResult;
        }

        /// <summary>
        /// Insert G0, Pen down gcode command
        /// </summary>
        private static void gcodeStartPath(double x, double y, string cmt = "")
        { gcodeStartPath((float)x, (float)y, cmt); }
        private static void gcodeStartPath(float x, float y, string cmt = "")
        {
            if (!dxfComments)
                cmt = "";
            System.Windows.Point coord = translateXY(x, y);
            if (((float)lastGCX == coord.X) && ((float)lastGCY == coord.Y))    // no change in position, no need for pen-up -down
            {
                askPenUp = false;
                gcode.Comment(gcodeString[gcodeStringIndex], cmt);
            }
            else
            {
                if (askPenUp)   // retrieve missing pen up
                {
                    gcode.PenUp(gcodeString[gcodeStringIndex], cmt);
                    isPenDown = false;
                    askPenUp = false;
                }
                lastGCX = coord.X; lastGCY = coord.Y;
                lastSetGCX = coord.X; lastSetGCY = coord.Y;
                gcode.MoveToRapid(gcodeString[gcodeStringIndex], coord, cmt);
            }
            if (!isPenDown)
            {
                if (dxfPausePenDown) { gcode.Pause(gcodeString[gcodeStringIndex], cmt); }
                gcode.PenDown(gcodeString[gcodeStringIndex]);
                isPenDown = true;
            }
            isReduceOk = false;
        }
        private static bool askPenUp = false;
        private static bool isPenDown = false;
        private static void gcodeStopPath(string cmt = "")
        {
            if (!dxfComments)
                cmt = "";
            if (gcodeReduce)
            {
                if ((lastSetGCX != lastGCX) || (lastSetGCY != lastGCY))
                {
                    gcode.MoveTo(gcodeString[gcodeStringIndex], new System.Windows.Point(lastGCX, lastGCY), cmt);
                    lastSetGCX = lastGCX; lastSetGCY = lastGCY;
                }
            }
            if (isPenDown)
            { askPenUp = true; }//
        }

        /// <summary>
        /// Insert G1 gcode command
        /// </summary>
        private static void gcodeMoveTo(double x, double y, string cmt)
        {
            System.Windows.Point coord = new System.Windows.Point(x, y);
            gcodeMoveTo(coord, cmt);
        }
        /// <summary>
        /// Insert G1 gcode command
        /// </summary>
        private static void gcodeMoveTo(float x, float y, string cmt)
        {
            System.Windows.Point coord = new System.Windows.Point(x, y);
            gcodeMoveTo(coord, cmt);
        }

        private static bool rejectPoint = false;
        private static double lastGCX = -1, lastGCY = -1, lastSetGCX = -1, lastSetGCY = -1, distance;
        /// <summary>
        /// Insert G1 gcode command
        /// </summary>
        private static void gcodeMoveTo(System.Windows.Point orig, string cmt)
        {
            if (!dxfComments)
                cmt = "";
            System.Windows.Point coord = translateXY(orig);
            rejectPoint = false;
            if (gcodeReduce && isReduceOk)
            {
                distance = Math.Sqrt(((coord.X - lastSetGCX) * (coord.X - lastSetGCX)) + ((coord.Y - lastSetGCY) * (coord.Y - lastSetGCY)));
                if (distance < gcodeReduceVal)      // discard actual G1 movement
                { rejectPoint = true; }
                else
                { lastSetGCX = coord.X; lastSetGCY = coord.Y; }
            }
            if (!gcodeReduce || !rejectPoint)       // write GCode
            {
                gcode.MoveTo(gcodeString[gcodeStringIndex], coord, cmt);
                lastSetGCX = coord.X; lastSetGCY = coord.Y;
            }
            lastGCX = coord.X; lastGCY = coord.Y;
        }


        /// <summary>
        /// Insert G2/G3 gcode command
        /// </summary>
        private static void gcodeArcToCCW(float x, float y, float i, float j, string cmt)
        {
            System.Windows.Point coordxy = translateXY(x, y);
            System.Windows.Point coordij = translateIJ(i, j);
            if (gcodeReduce && isReduceOk)          // restore last skipped point for accurat G2/G3 use
            {
                if ((lastSetGCX != lastGCX) || (lastSetGCY != lastGCY))
                    gcode.MoveTo(gcodeString[gcodeStringIndex], new System.Windows.Point(lastGCX, lastGCY), cmt);
            }
            gcode.Arc(gcodeString[gcodeStringIndex], 3, coordxy, coordij, cmt);
        }

        /// <summary>
        /// Insert Pen-up gcode command
        /// </summary>
        private static void gcodePenUp(string cmt = "")
        {
            gcode.PenUp(gcodeString[gcodeStringIndex], cmt);
            isPenDown = false;
        }

        #endregion

        #region Métodos para la conversión del archivo de la clase GCodeFromFont

        #region Hershey Font
        public static string[] fontNames = { "Sans 1-stroke","Sans bold","Serif medium","Serif medium italic","Serif bold italic",
                        "Serif bold","Script 1-stroke","Script 1-stroke(alt)","Script medium","Dot Matrix","Gothic English",
                        "Gothic German","Gothic Italian","Greek 1-stroke","Greek medium","Cyrillic",
                        "Japanese","Astrology","Math(lower)","Math(upper)","Markers","Meteorology","Music","Symbolic" };
        // translate selection to real index
        private static int[] fontIndex = { 3, 4, 21, 19, 20, 22, 16, 1, 15, 23, 5, 6, 7, 8, 18, 2, 9, 0, 11, 12, 10, 13, 14, 17, 23 };

        public static int getFontIndex(int i)
        {
            if ((i >= 0) && (i < fontIndex.Length))
                return fontIndex[i];
            return 0;
        }

        // "Sans 1-stroke" -> index=3
        // fontChar[fntIndex][chrIndex] start at char 32 ' '
        // M = pen up, move to, pen up
        // L = movo to
        // char set:  !"#$%&'()*+,-./
        // 0123456789:;<=>?
        // @ABCDEFGHIJKLMNO
        // PQRSTUVWXYZ[\]^_
        // `abcdefghijklmno
        // pqrstuvwxyz{|}~⌂
        private static string[][] fontChar = {
        new string[] {"-8 8","-12 12 M -8 -10 L -4 -8 L -2 -6 L -1 -3 L -1 0 L -2 3 L -4 5 L -8 7 M -8 -10 L -5 -9 L -3 -8 L -1 -6 L 0 -3 M 0 0 L -1 3 L -3 5 L -5 6 L -8 7 M 8 -10 L 5 -9 L 3 -8 L 1 -6 L 0 -3 M 0 0 L 1 3 L 3 5 L 5 6 L 8 7 M 8 -10 L 4 -8 L 2 -6 L 1 -3 L 1 0 L 2 3 L 4 5 L 8 7 M -9 -2 L 9 -2 M -9 -1 L 9 -1","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-13 14 M -1 -12 L -4 -11 L -7 -9 L -9 -6 L -10 -3 L -10 0 L -9 3 L -7 6 L -4 8 L -1 9 L 2 9 L 5 8 L 8 6 L 10 3 L 11 0 L 11 -3 L 10 -6 L 8 -9 L 5 -11 L 2 -12 L -1 -12 M 0 -3 L -1 -2 L -1 -1 L 0 0 L 1 0 L 2 -1 L 2 -2 L 1 -3 L 0 -3 M 0 -2 L 0 -1 L 1 -1 L 1 -2 L 0 -2","-8 9 M -2 -12 L -4 -11 L -3 -9 L -1 -8 M -2 -12 L -3 -11 L -3 -9 M 3 -12 L 5 -11 L 4 -9 L 2 -8 M 3 -12 L 4 -11 L 4 -9 M -1 -8 L -3 -7 L -4 -6 L -5 -4 L -5 -1 L -4 1 L -3 2 L -1 3 L 2 3 L 4 2 L 5 1 L 6 -1 L 6 -4 L 5 -6 L 4 -7 L 2 -8 L -1 -8 M 0 3 L 0 9 M 1 3 L 1 9 M -4 6 L 5 6","-9 10 M 0 -12 L -3 -11 L -5 -9 L -6 -6 L -6 -5 L -5 -2 L -3 0 L 0 1 L 1 1 L 4 0 L 6 -2 L 7 -5 L 7 -6 L 6 -9 L 4 -11 L 1 -12 L 0 -12 M 0 1 L 0 9 M 1 1 L 1 9 M -4 5 L 5 5","-14 14 M -2 -12 L -5 -11 L -8 -9 L -10 -6 L -11 -3 L -11 1 L -10 4 L -8 7 L -5 9 L -2 10 L 2 10 L 5 9 L 8 7 L 10 4 L 11 1 L 11 -3 L 10 -6 L 8 -9 L 5 -11 L 2 -12 L -2 -12 M 0 -12 L 0 10 M -11 -1 L 11 -1","-11 14 M -2 -5 L -5 -4 L -7 -2 L -8 1 L -8 2 L -7 5 L -5 7 L -2 8 L -1 8 L 2 7 L 4 5 L 5 2 L 5 1 L 4 -2 L 2 -4 L -1 -5 L -2 -5 M 11 -11 L 5 -11 L 9 -10 L 3 -4 M 11 -11 L 11 -5 L 10 -9 L 4 -3 M 10 -10 L 4 -4","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16 M 2 -14 L 0 -10 L -1 -7 L -2 -2 L -2 2 L -1 7 L 0 10 L 2 14","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 M -2 -14 L 0 -10 L 1 -7 L 2 -2 L 2 2 L 1 7 L 0 10 L -2 14","-12 10 M -9 -9 L -8 -11 L -6 -12 L -3 -12 L -1 -11 L 0 -9 L 0 -6 L -1 -3 L -2 -1 L -4 1 L -7 3 M -3 -12 L -2 -11 L -1 -9 L -1 -5 L -2 -2 L -4 1 M 4 -12 L 2 9 M 5 -12 L 1 9 M -7 3 L 7 3","-9 10 M -5 -12 L -5 3 M -4 -12 L -5 -1 M -5 -1 L -4 -3 L -3 -4 L -1 -5 L 2 -5 L 5 -4 L 6 -2 L 6 0 L 5 2 L 3 4 M 2 -5 L 4 -4 L 5 -2 L 5 0 L 2 6 L 2 8 L 3 9 L 5 9 L 7 7 M -7 -12 L -4 -12","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-9 10 M 0 -4 L -3 -3 L -5 -1 L -6 2 L -6 3 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 M 0 -10 L -4 -8 L 0 -12 L 0 -4 M 1 -10 L 5 -8 L 1 -12 L 1 -4 M 0 1 L -1 2 L -1 3 L 0 4 L 1 4 L 2 3 L 2 2 L 1 1 L 0 1 M 0 2 L 0 3 L 1 3 L 1 2 L 0 2","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 12 M -1 -10 L 0 -12 L 0 9 M 2 -10 L 1 -12 L 1 9 M -8 -10 L -7 -12 L -7 -5 L -6 -2 L -4 0 L -1 1 L 0 1 M -5 -10 L -6 -12 L -6 -4 L -5 -1 M 9 -10 L 8 -12 L 8 -5 L 7 -2 L 5 0 L 2 1 L 1 1 M 6 -10 L 7 -12 L 7 -4 L 6 -1 M -4 5 L 5 5","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 M -1 -10 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 2 7 L 4 5 L 5 3 L 6 0 L 7 -4 L 7 -8 L 6 -10 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -3 9 M -1 9 L 1 8 L 3 5 L 4 3 L 5 0 L 6 -4 L 6 -9 L 5 -11 L 4 -12","-10 11 M 2 -8 L -3 9 L -1 9 M 5 -12 L 3 -8 L -2 9 M 5 -12 L -1 9 M 5 -12 L 2 -9 L -1 -7 L -3 -6 M 2 -8 L 0 -7 L -3 -6","-10 11 M -3 -7 L -3 -8 L -2 -8 L -2 -6 L -4 -6 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 5 -3 L -5 3 L -7 5 L -9 9 M 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 L 1 -1 M 4 -12 L 5 -11 L 6 -9 L 6 -7 L 5 -5 L 3 -3 L -5 3 M -8 7 L -7 6 L -5 6 L 0 7 L 5 7 L 6 6 M -5 6 L 0 8 L 5 8 M -5 6 L 0 9 L 3 9 L 5 8 L 6 6 L 6 5","-10 11 M -3 -7 L -3 -8 L -2 -8 L -2 -6 L -4 -6 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 L 1 -2 M 6 -11 L 7 -9 L 7 -7 L 6 -5 L 5 -4 M 4 -12 L 5 -11 L 6 -9 L 6 -7 L 5 -5 L 3 -3 L 1 -2 M -1 -2 L 1 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 3 8 L 0 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -6 3 L -6 5 L -7 5 L -7 4 M 4 0 L 5 2 L 5 5 L 4 7 M 1 -2 L 3 -1 L 4 1 L 4 5 L 3 7 L 2 8 L 0 9","-10 11 M 5 -8 L 0 9 L 2 9 M 8 -12 L 6 -8 L 1 9 M 8 -12 L 2 9 M 8 -12 L -8 3 L 8 3","-10 11 M -1 -12 L -6 -2 M -1 -12 L 9 -12 M -1 -11 L 7 -11 M -2 -10 L 3 -10 L 7 -11 L 9 -12 M -6 -2 L -5 -3 L -2 -4 L 1 -4 L 4 -3 L 5 -2 L 6 0 L 6 3 L 5 6 L 3 8 L -1 9 L -4 9 L -6 8 L -7 7 L -8 5 L -8 3 L -6 3 L -6 5 L -7 5 L -7 4 M 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 M 1 -4 L 3 -3 L 4 -1 L 4 3 L 3 6 L 1 8 L -1 9","-10 11 M 7 -8 L 7 -9 L 6 -9 L 6 -7 L 8 -7 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 L -1 -3 L -3 -2 L -4 -1 L -5 1 M -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 4 6 L 5 4 L 5 1 L 4 -1 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -3 9 M 0 9 L 2 8 L 3 7 L 4 4 L 4 0 L 3 -2 L 2 -3","-10 11 M -4 -12 L -6 -6 M 9 -12 L 8 -9 L 6 -6 L 2 -1 L 0 2 L -1 5 L -2 9 M 0 1 L -2 5 L -3 9 M 6 -6 L 0 0 L -2 3 L -3 5 L -4 9 L -2 9 M -5 -9 L -2 -12 L 0 -12 L 5 -9 M -3 -11 L 0 -11 L 5 -9 M -5 -9 L -3 -10 L 0 -10 L 5 -9 L 7 -9 L 8 -10 L 9 -12","-10 11 M 1 -12 L -2 -11 L -3 -10 L -4 -8 L -4 -5 L -3 -3 L -1 -2 L 2 -2 L 5 -3 L 7 -4 L 8 -6 L 8 -9 L 7 -11 L 5 -12 L 1 -12 M 3 -12 L -2 -11 M -2 -10 L -3 -8 L -3 -4 L -2 -3 M -3 -3 L 0 -2 M 1 -2 L 5 -3 M 6 -4 L 7 -6 L 7 -9 L 6 -11 M 7 -11 L 3 -12 M 1 -12 L -1 -10 L -2 -8 L -2 -4 L -1 -2 M 2 -2 L 4 -3 L 5 -4 L 6 -6 L 6 -10 L 5 -12 M -1 -2 L -5 -1 L -7 1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 4 8 L 5 7 L 6 5 L 6 2 L 5 0 L 4 -1 L 2 -2 M 0 -2 L -5 -1 M -4 -1 L -6 1 L -7 3 L -7 6 L -6 8 M -7 8 L -2 9 L 4 8 M 4 7 L 5 5 L 5 2 L 4 0 M 4 -1 L 1 -2 M -1 -2 L -3 -1 L -5 1 L -6 3 L -6 6 L -5 8 L -4 9 M 0 9 L 2 8 L 3 7 L 4 5 L 4 1 L 3 -1 L 2 -2","-10 11 M 6 -4 L 5 -2 L 4 -1 L 2 0 L -1 0 L -3 -1 L -4 -2 L -5 -4 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 4 -12 L 6 -11 L 7 -10 L 8 -7 L 8 -4 L 7 0 L 6 3 L 4 6 L 2 8 L -1 9 L -4 9 L -6 8 L -7 6 L -7 4 L -5 4 L -5 6 L -6 6 L -6 5 M -3 -2 L -4 -4 L -4 -7 L -3 -9 M 6 -10 L 7 -8 L 7 -4 L 6 0 L 5 3 L 3 6 M -1 0 L -2 -1 L -3 -3 L -3 -7 L -2 -10 L -1 -11 L 1 -12 M 4 -12 L 5 -11 L 6 -9 L 6 -4 L 5 0 L 4 3 L 3 5 L 1 8 L -1 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -5 -1 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 3 -1 M -9 9 L 7 9 L 7 4 L 6 9","-10 9 M 7 -11 L 3 -11 L -1 -10 L -4 -8 L -6 -5 L -7 -2 L -7 1 L -6 4 L -4 7 L -1 9 L 3 10 L 7 10 M 7 -11 L 4 -10 L 1 -8 L -1 -5 L -2 -2 L -2 1 L -1 4 L 1 7 L 4 9 L 7 10","-12 13 M -3 -1 L -5 -1 L -7 0 L -8 1 L -9 3 L -9 5 L -8 7 L -7 8 L -5 9 L -3 9 L -1 8 L 0 7 L 1 5 L 1 3 L 0 1 L -1 0 L -3 -1 M 1 -10 L -2 -1 M 8 -8 L 0 0 M 10 -1 L 1 2","-10 10 M -3 -7 L 3 7 M 3 -7 L -3 7 M -7 -3 L 7 3 M 7 -3 L -7 3","-12 12 M -4 4 L -6 3 L -7 3 L -9 4 L -10 6 L -10 7 L -9 9 L -7 10 L -6 10 L -4 9 L -3 7 L -3 6 L -4 4 L -7 0 L -8 -3 L -8 -5 L -7 -8 L -5 -10 L -2 -11 L 2 -11 L 5 -10 L 7 -8 L 8 -5 L 8 -3 L 7 0 L 4 4 L 3 6 L 3 7 L 4 9 L 6 10 L 7 10 L 9 9 L 10 7 L 10 6 L 9 4 L 7 3 L 6 3 L 4 4 M -8 -5 L -7 -7 L -5 -9 L -2 -10 L 2 -10 L 5 -9 L 7 -7 L 8 -5","-12 12 M -4 -5 L -6 -4 L -7 -4 L -9 -5 L -10 -7 L -10 -8 L -9 -10 L -7 -11 L -6 -11 L -4 -10 L -3 -8 L -3 -7 L -4 -5 L -7 -1 L -8 2 L -8 4 L -7 7 L -5 9 L -2 10 L 2 10 L 5 9 L 7 7 L 8 4 L 8 2 L 7 -1 L 4 -5 L 3 -7 L 3 -8 L 4 -10 L 6 -11 L 7 -11 L 9 -10 L 10 -8 L 10 -7 L 9 -5 L 7 -4 L 6 -4 L 4 -5 M -8 4 L -7 6 L -5 8 L -2 9 L 2 9 L 5 8 L 7 6 L 8 4","-12 13 M -8 -5 L -9 -6 L -9 -8 L -8 -10 L -6 -11 L -4 -11 L -2 -10 L -1 -9 L 0 -7 L 1 -2 M -9 -8 L -7 -10 L -5 -10 L -3 -9 L -2 -8 L -1 -6 L 0 -2 L 0 9 M 9 -5 L 10 -6 L 10 -8 L 9 -10 L 7 -11 L 5 -11 L 3 -10 L 2 -9 L 1 -7 L 0 -2 M 10 -8 L 8 -10 L 6 -10 L 4 -9 L 3 -8 L 2 -6 L 1 -2 L 1 9","-10 10 M 0 -12 L -7 8 M -1 -9 L 5 9 M 0 -9 L 6 9 M 0 -12 L 7 9 M -5 3 L 4 3 M -9 9 L -3 9 M 2 9 L 9 9 M -7 8 L -8 9 M -7 8 L -5 9 M 5 8 L 3 9 M 5 7 L 4 9 M 6 7 L 8 9","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 M 6 -10 L 7 -8 L 7 -6 L 6 -4 M 3 -12 L 5 -11 L 6 -9 L 6 -5 L 5 -3 L 3 -2 M -4 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 6 0 L 7 2 L 7 5 L 6 7 M 3 -2 L 5 -1 L 6 1 L 6 6 L 5 8 L 3 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 10 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 4 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -9 9 M 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 M 1 -12 L 3 -11 L 5 -8 L 6 -4 L 6 1 L 5 5 L 3 8 L 1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 10 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 7 -12 L 7 -6 M -4 -2 L 2 -2 M 2 -6 L 2 2 M -9 9 L 7 9 L 7 3 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M 2 -12 L 7 -11 M 4 -12 L 7 -10 M 5 -12 L 7 -9 M 6 -12 L 7 -6 M 2 -6 L 1 -2 L 2 2 M 2 -4 L 0 -2 L 2 0 M 2 -3 L -2 -2 L 2 -1 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9 M 2 9 L 7 8 M 4 9 L 7 7 M 5 9 L 7 6 M 6 9 L 7 3","-11 9 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 7 -12 L 7 -6 M -4 -2 L 2 -2 M 2 -6 L 2 2 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M 2 -12 L 7 -11 M 4 -12 L 7 -10 M 5 -12 L 7 -9 M 6 -12 L 7 -6 M 2 -6 L 1 -2 L 2 2 M 2 -4 L 0 -2 L 2 0 M 2 -3 L -2 -2 L 2 -1 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 12 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 2 9 L 4 8 L 6 8 L 7 9 L 7 1 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 6 2 L 6 7 M 5 1 L 5 7 L 4 8 M 2 1 L 10 1 M 3 1 L 5 2 M 4 1 L 5 3 M 8 1 L 7 3 M 9 1 L 7 2","-12 12 M -7 -12 L -7 9 M -6 -11 L -6 8 M -5 -12 L -5 9 M 5 -12 L 5 9 M 6 -11 L 6 8 M 7 -12 L 7 9 M -10 -12 L -2 -12 M 2 -12 L 10 -12 M -5 -2 L 5 -2 M -10 9 L -2 9 M 2 9 L 10 9 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 3 -12 L 5 -11 M 4 -12 L 5 -10 M 8 -12 L 7 -10 M 9 -12 L 7 -11 M -7 8 L -9 9 M -7 7 L -8 9 M -5 7 L -4 9 M -5 8 L -3 9 M 5 8 L 3 9 M 5 7 L 4 9 M 7 7 L 8 9 M 7 8 L 9 9","-6 6 M -1 -12 L -1 9 M 0 -11 L 0 8 M 1 -12 L 1 9 M -4 -12 L 4 -12 M -4 9 L 4 9 M -3 -12 L -1 -11 M -2 -12 L -1 -10 M 2 -12 L 1 -10 M 3 -12 L 1 -11 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-8 8 M 1 -12 L 1 5 L 0 8 L -1 9 M 2 -11 L 2 5 L 1 8 M 3 -12 L 3 5 L 2 8 L -1 9 L -3 9 L -5 8 L -6 6 L -6 4 L -5 3 L -4 3 L -3 4 L -3 5 L -4 6 L -5 6 M -5 4 L -5 5 L -4 5 L -4 4 L -5 4 M -2 -12 L 6 -12 M -1 -12 L 1 -11 M 0 -12 L 1 -10 M 4 -12 L 3 -10 M 5 -12 L 3 -11","-12 10 M -7 -12 L -7 9 M -6 -11 L -6 8 M -5 -12 L -5 9 M 6 -11 L -5 0 M -2 -2 L 5 9 M -1 -2 L 6 9 M -1 -4 L 7 9 M -10 -12 L -2 -12 M 3 -12 L 9 -12 M -10 9 L -2 9 M 2 9 L 9 9 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 6 -11 M 8 -12 L 6 -11 M -7 8 L -9 9 M -7 7 L -8 9 M -5 7 L -4 9 M -5 8 L -3 9 M 5 7 L 3 9 M 5 7 L 8 9","-9 9 M -4 -12 L -4 9 M -3 -11 L -3 8 M -2 -12 L -2 9 M -7 -12 L 1 -12 M -7 9 L 8 9 L 8 3 M -6 -12 L -4 -11 M -5 -12 L -4 -10 M -1 -12 L -2 -10 M 0 -12 L -2 -11 M -4 8 L -6 9 M -4 7 L -5 9 M -2 7 L -1 9 M -2 8 L 0 9 M 3 9 L 8 8 M 5 9 L 8 7 M 6 9 L 8 6 M 7 9 L 8 3","-13 13 M -8 -12 L -8 8 M -8 -12 L -1 9 M -7 -12 L -1 6 M -6 -12 L 0 6 M 6 -12 L -1 9 M 6 -12 L 6 9 M 7 -11 L 7 8 M 8 -12 L 8 9 M -11 -12 L -6 -12 M 6 -12 L 11 -12 M -11 9 L -5 9 M 3 9 L 11 9 M -10 -12 L -8 -11 M 9 -12 L 8 -10 M 10 -12 L 8 -11 M -8 8 L -10 9 M -8 8 L -6 9 M 6 8 L 4 9 M 6 7 L 5 9 M 8 7 L 9 9 M 8 8 L 10 9","-12 12 M -7 -12 L -7 8 M -7 -12 L 7 9 M -6 -12 L 6 6 M -5 -12 L 7 6 M 7 -11 L 7 9 M -10 -12 L -5 -12 M 4 -12 L 10 -12 M -10 9 L -4 9 M -9 -12 L -7 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11 M -7 8 L -9 9 M -7 8 L -5 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M 5 6 L 6 4 L 7 1 L 7 -4 L 6 -7 L 5 -9 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 1 9 L 3 8 L 5 5 L 6 1 L 6 -4 L 5 -8 L 3 -11 L 1 -12","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -4 -1 M 6 -10 L 7 -8 L 7 -5 L 6 -3 M 3 -12 L 5 -11 L 6 -9 L 6 -4 L 5 -2 L 3 -1 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M 5 6 L 6 4 L 7 1 L 7 -4 L 6 -7 L 5 -9 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 1 9 L 3 8 L 5 5 L 6 1 L 6 -4 L 5 -8 L 3 -11 L 1 -12 M -4 6 L -3 4 L -1 3 L 0 3 L 2 4 L 3 6 L 4 12 L 5 14 L 7 14 L 8 12 L 8 10 M 4 10 L 5 12 L 6 13 L 7 13 M 3 6 L 5 11 L 6 12 L 7 12 L 8 11","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 L -4 -2 M 6 -10 L 7 -8 L 7 -6 L 6 -4 M 3 -12 L 5 -11 L 6 -9 L 6 -5 L 5 -3 L 3 -2 M 0 -2 L 2 -1 L 3 1 L 5 7 L 6 9 L 8 9 L 9 7 L 9 5 M 5 5 L 6 7 L 7 8 L 8 8 M 2 -1 L 3 0 L 6 6 L 7 7 L 8 7 L 9 6 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-10 10 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 1 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -1 9 L -4 8 L -6 6 L -7 3 L -7 9 L -6 6","-10 10 M -8 -12 L -8 -6 M -1 -12 L -1 9 M 0 -11 L 0 8 M 1 -12 L 1 9 M 8 -12 L 8 -6 M -8 -12 L 8 -12 M -4 9 L 4 9 M -7 -12 L -8 -6 M -6 -12 L -8 -9 M -5 -12 L -8 -10 M -3 -12 L -8 -11 M 3 -12 L 8 -11 M 5 -12 L 8 -10 M 6 -12 L 8 -9 M 7 -12 L 8 -6 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-12 12 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -11 M -6 -11 L -6 4 L -5 6 M -5 -12 L -5 4 L -4 7 L -3 8 L -1 9 M -10 -12 L -2 -12 M 4 -12 L 10 -12 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11","-10 10 M -7 -12 L 0 9 M -6 -12 L 0 6 L 0 9 M -5 -12 L 1 6 M 7 -11 L 0 9 M -9 -12 L -2 -12 M 3 -12 L 9 -12 M -8 -12 L -6 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 7 -11 M 8 -12 L 7 -11","-12 12 M -8 -12 L -4 9 M -7 -12 L -4 4 L -4 9 M -6 -12 L -3 4 M 0 -12 L -3 4 L -4 9 M 0 -12 L 4 9 M 1 -12 L 4 4 L 4 9 M 2 -12 L 5 4 M 8 -11 L 5 4 L 4 9 M -11 -12 L -3 -12 M 0 -12 L 2 -12 M 5 -12 L 11 -12 M -10 -12 L -7 -11 M -9 -12 L -7 -10 M -5 -12 L -6 -10 M -4 -12 L -6 -11 M 6 -12 L 8 -11 M 10 -12 L 8 -11","-10 10 M -7 -12 L 5 9 M -6 -12 L 6 9 M -5 -12 L 7 9 M 6 -11 L -6 8 M -9 -12 L -2 -12 M 3 -12 L 9 -12 M -9 9 L -3 9 M 2 9 L 9 9 M -8 -12 L -5 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 4 -12 L 6 -11 M 8 -12 L 6 -11 M -6 8 L -8 9 M -6 8 L -4 9 M 5 8 L 3 9 M 5 7 L 4 9 M 5 7 L 8 9","-11 11 M -8 -12 L -1 -1 L -1 9 M -7 -12 L 0 -1 L 0 8 M -6 -12 L 1 -1 L 1 9 M 7 -11 L 1 -1 M -10 -12 L -3 -12 M 4 -12 L 10 -12 M -4 9 L 4 9 M -9 -12 L -7 -11 M -4 -12 L -6 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-10 10 M 7 -12 L -7 -12 L -7 -6 M 5 -12 L -7 9 M 6 -12 L -6 9 M 7 -12 L -5 9 M -7 9 L 7 9 L 7 3 M -6 -12 L -7 -6 M -5 -12 L -7 -9 M -4 -12 L -7 -10 M -2 -12 L -7 -11 M 2 9 L 7 8 M 4 9 L 7 7 M 5 9 L 7 6 M 6 9 L 7 3","-12 12 M -9 -11 L -8 -7 L -7 -5 L -5 -3 L -2 -2 L 2 -2 L 5 -3 L 7 -5 L 8 -7 L 9 -11 M -9 -11 L -8 -8 L -7 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 7 -6 L 8 -8 L 9 -11 M -2 -3 L -4 -2 L -5 -1 L -6 1 L -6 4 L -5 6 L -3 8 L -1 9 L 1 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 M -2 -2 L -4 -1 L -5 1 L -5 4 L -4 7 M 4 7 L 5 4 L 5 1 L 4 -1 L 2 -2","-7 7 M -7 -12 L 7 12","-12 12 M -5 -8 L -5 4 M -4 -7 L -4 3 M 4 -7 L 4 3 M 5 -8 L 5 4 M -9 -11 L -7 -9 L -5 -8 L -2 -7 L 2 -7 L 5 -8 L 7 -9 L 9 -11 M -9 7 L -7 5 L -5 4 L -2 3 L 2 3 L 5 4 L 7 5 L 9 7","-12 12 M 9 -9 L -6 -9 L -8 -8 L -9 -6 L -9 -4 L -8 -2 L -6 -1 L -4 -1 L -2 -2 L -1 -4 L -1 -6 L -2 -8 L 9 -8 M -9 -5 L -8 -3 L -7 -2 L -5 -1 M -1 -5 L -2 -7 L -3 -8 L -5 -9 M -9 6 L 6 6 L 8 5 L 9 3 L 9 1 L 8 -1 L 6 -2 L 4 -2 L 2 -1 L 1 1 L 1 3 L 2 5 L -9 5 M 9 2 L 8 0 L 7 -1 L 5 -2 M 1 2 L 2 4 L 3 5 L 5 6","-12 11 M -3 3 L -5 2 L -6 2 L -8 3 L -9 5 L -9 6 L -8 8 L -6 9 L -5 9 L -3 8 L -2 6 L -2 5 L -3 3 L -8 -2 L -9 -4 L -9 -7 L -8 -9 L -6 -10 L -3 -11 L 1 -11 L 5 -10 L 7 -8 L 8 -6 L 8 -3 L 7 0 L 4 3 L 3 5 L 3 7 L 4 9 L 6 9 L 7 8 L 8 6 M -5 1 L -7 -2 L -8 -4 L -8 -7 L -7 -9 L -6 -10 M 1 -11 L 4 -10 L 6 -8 L 7 -6 L 7 -3 L 6 0 L 4 3","-11 13 M -10 -7 L -7 -10 L -5 -7 L -5 4 M -8 -9 L -6 -6 L -6 4 M -5 -7 L -2 -10 L 0 -7 L 0 3 M -3 -9 L -1 -6 L -1 3 M 0 -7 L 3 -10 L 5 -7 L 5 9 M 2 -9 L 4 -6 L 4 9 M 5 -7 L 8 -10 L 9 -8 L 10 -5 L 10 -2 L 9 1 L 8 3 L 6 5 L 3 7 L -2 9 M 7 -9 L 8 -8 L 9 -5 L 9 -2 L 8 1 L 7 3 L 5 5 L 2 7 L -2 9","-11 11 M 5 -5 L 3 2 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M 6 -5 L 4 2 L 4 8 M 5 -5 L 7 -5 L 5 2 L 4 6 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9","-9 10 M -2 -12 L -4 -5 L -5 1 L -5 5 L -4 7 L -3 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 2 L 7 0 L 6 -3 L 5 -4 L 3 -5 L 1 -5 L -1 -4 L -2 -3 L -3 -1 L -4 2 M -1 -12 L -3 -5 L -4 -1 L -4 5 L -3 8 M 4 7 L 5 5 L 6 2 L 6 -1 L 5 -3 M -5 -12 L 0 -12 L -2 -5 L -4 2 M 1 9 L 3 7 L 4 5 L 5 2 L 5 -1 L 4 -4 L 3 -5 M -4 -12 L -1 -11 M -3 -12 L -2 -10","-9 9 M 5 -1 L 5 -2 L 4 -2 L 4 0 L 6 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 M -3 -3 L -4 -1 L -5 2 L -5 5 L -4 7 M 0 -5 L -2 -3 L -3 -1 L -4 2 L -4 5 L -3 8 L -2 9","-11 11 M 7 -12 L 4 -1 L 3 3 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M 8 -12 L 5 -1 L 4 3 L 4 8 M 4 -12 L 9 -12 L 5 2 L 4 6 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -5 -3 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9 M 5 -12 L 8 -11 M 6 -12 L 7 -10","-9 9 M -5 4 L -1 3 L 2 2 L 5 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 M -3 -3 L -4 -1 L -5 2 L -5 5 L -4 7 M 0 -5 L -2 -3 L -3 -1 L -4 2 L -4 5 L -3 8 L -2 9","-8 8 M 8 -10 L 8 -11 L 7 -11 L 7 -9 L 9 -9 L 9 -11 L 8 -12 L 6 -12 L 4 -11 L 2 -9 L 1 -7 L 0 -4 L -1 0 L -3 9 L -4 12 L -5 14 L -7 16 M 2 -8 L 1 -5 L 0 0 L -2 9 L -3 12 M 6 -12 L 4 -10 L 3 -8 L 2 -5 L 1 0 L -1 8 L -2 11 L -3 13 L -5 15 L -7 16 L -9 16 L -10 15 L -10 13 L -8 13 L -8 15 L -9 15 L -9 14 M -4 -5 L 7 -5","-10 11 M 6 -5 L 2 9 L 1 12 L -1 15 L -3 16 M 7 -5 L 3 9 L 1 13 M 6 -5 L 8 -5 L 4 9 L 2 13 L 0 15 L -3 16 L -6 16 L -8 15 L -9 14 L -9 12 L -7 12 L -7 14 L -8 14 L -8 13 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 L 3 5 L 4 2 M -4 -3 L -5 -1 L -6 2 L -6 5 L -5 7 M -1 -5 L -3 -3 L -4 -1 L -5 2 L -5 5 L -4 8 L -3 9","-11 11 M -3 -12 L -9 9 L -7 9 M -2 -12 L -8 9 M -6 -12 L -1 -12 L -7 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -2 L 6 1 L 4 6 M 5 -4 L 5 0 L 4 4 L 4 8 M 5 -2 L 3 3 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M -5 -12 L -2 -11 M -4 -12 L -3 -10","-7 6 M 1 -12 L 1 -10 L 3 -10 L 3 -12 L 1 -12 M 2 -12 L 2 -10 M 1 -11 L 3 -11 M -6 -1 L -5 -3 L -3 -5 L -1 -5 L 0 -4 L 1 -2 L 1 1 L -1 6 M 0 -4 L 0 0 L -1 4 L -1 8 M 0 -2 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5","-7 6 M 3 -12 L 3 -10 L 5 -10 L 5 -12 L 3 -12 M 4 -12 L 4 -10 M 3 -11 L 5 -11 M -5 -1 L -4 -3 L -2 -5 L 0 -5 L 1 -4 L 2 -2 L 2 1 L 0 8 L -1 11 L -2 13 L -4 15 L -6 16 L -8 16 L -9 15 L -9 13 L -7 13 L -7 15 L -8 15 L -8 14 M 1 -4 L 1 1 L -1 8 L -2 11 L -3 13 M 1 -2 L 0 2 L -2 9 L -3 12 L -4 14 L -6 16","-11 11 M -3 -12 L -9 9 L -7 9 M -2 -12 L -8 9 M -6 -12 L -1 -12 L -7 9 M 7 -3 L 7 -4 L 6 -4 L 6 -2 L 8 -2 L 8 -4 L 7 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 M -5 1 L -3 1 L -1 2 L 0 3 L 2 7 L 3 8 L 5 8 M -1 3 L 1 7 L 2 8 M -3 1 L -2 2 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 M -5 -12 L -2 -11 M -4 -12 L -3 -10","-6 6 M 2 -12 L -1 -1 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5 M 3 -12 L 0 -1 L -1 3 L -1 8 M -1 -12 L 4 -12 L 0 2 L -1 6 M 0 -12 L 3 -11 M 1 -12 L 2 -10","-18 17 M -17 -1 L -16 -3 L -14 -5 L -12 -5 L -11 -4 L -10 -2 L -10 1 L -12 9 M -11 -4 L -11 1 L -13 9 M -11 -2 L -12 2 L -14 9 L -12 9 M -10 1 L -8 -2 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -2 L 1 1 L -1 9 M 0 -4 L 0 1 L -2 9 M 0 -2 L -1 2 L -3 9 L -1 9 M 1 1 L 3 -2 L 5 -4 L 7 -5 L 9 -5 L 11 -4 L 12 -2 L 12 1 L 10 6 M 11 -4 L 11 0 L 10 4 L 10 8 M 11 -2 L 9 3 L 9 6 L 10 8 L 11 9 L 13 9 L 15 7 L 16 5","-12 12 M -11 -1 L -10 -3 L -8 -5 L -6 -5 L -5 -4 L -4 -2 L -4 1 L -6 9 M -5 -4 L -5 1 L -7 9 M -5 -2 L -6 2 L -8 9 L -6 9 M -4 1 L -2 -2 L 0 -4 L 2 -5 L 4 -5 L 6 -4 L 7 -2 L 7 1 L 5 6 M 6 -4 L 6 0 L 5 4 L 5 8 M 6 -2 L 4 3 L 4 6 L 5 8 L 6 9 L 8 9 L 10 7 L 11 5","-10 10 M -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 4 L -6 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 5 L 7 2 L 7 0 L 6 -3 L 5 -4 L 2 -5 L -1 -5 M -4 -3 L -5 -1 L -6 2 L -6 5 L -5 7 M 4 7 L 5 5 L 6 2 L 6 -1 L 5 -3 M -1 -5 L -3 -3 L -4 -1 L -5 2 L -5 5 L -4 8 L -2 9 M 1 9 L 3 7 L 4 5 L 5 2 L 5 -1 L 4 -4 L 2 -5","-11 11 M -10 -1 L -9 -3 L -7 -5 L -5 -5 L -4 -4 L -3 -2 L -3 1 L -4 5 L -7 16 M -4 -4 L -4 1 L -5 5 L -8 16 M -4 -2 L -5 2 L -9 16 M -3 2 L -2 -1 L -1 -3 L 0 -4 L 2 -5 L 4 -5 L 6 -4 L 7 -3 L 8 0 L 8 2 L 7 5 L 5 8 L 2 9 L 0 9 L -2 8 L -3 5 L -3 2 M 6 -3 L 7 -1 L 7 2 L 6 5 L 5 7 M 4 -5 L 5 -4 L 6 -1 L 6 2 L 5 5 L 4 7 L 2 9 M -12 16 L -4 16 M -8 15 L -11 16 M -8 14 L -10 16 M -7 14 L -6 16 M -8 15 L -5 16","-11 10 M 5 -5 L -1 16 M 6 -5 L 0 16 M 5 -5 L 7 -5 L 1 16 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -5 -3 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9 M -4 16 L 4 16 M 0 15 L -3 16 M 0 14 L -2 16 M 1 14 L 2 16 M 0 15 L 3 16","-9 9 M -8 -1 L -7 -3 L -5 -5 L -3 -5 L -2 -4 L -1 -2 L -1 2 L -3 9 M -2 -4 L -2 2 L -4 9 M -2 -2 L -3 2 L -5 9 L -3 9 M 7 -3 L 7 -4 L 6 -4 L 6 -2 L 8 -2 L 8 -4 L 7 -5 L 5 -5 L 3 -4 L 1 -2 L -1 2","-8 9 M 6 -2 L 6 -3 L 5 -3 L 5 -1 L 7 -1 L 7 -3 L 6 -4 L 3 -5 L 0 -5 L -3 -4 L -4 -3 L -4 -1 L -3 1 L -1 2 L 2 3 L 4 4 L 5 6 M -3 -4 L -4 -1 M -3 0 L -1 1 L 2 2 L 4 3 M 5 4 L 4 8 M -4 -3 L -3 -1 L -1 0 L 2 1 L 4 2 L 5 4 L 5 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -6 5 L -4 5 L -4 7 L -5 7 L -5 6","-7 7 M 2 -12 L -1 -1 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5 M 3 -12 L 0 -1 L -1 3 L -1 8 M 2 -12 L 4 -12 L 0 2 L -1 6 M -4 -5 L 6 -5","-12 12 M -11 -1 L -10 -3 L -8 -5 L -6 -5 L -5 -4 L -4 -2 L -4 1 L -6 6 M -5 -4 L -5 0 L -6 4 L -6 8 M -5 -2 L -7 3 L -7 6 L -6 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 3 M 6 -5 L 4 3 L 4 6 L 5 8 L 6 9 L 8 9 L 10 7 L 11 5 M 7 -5 L 5 3 L 5 8 M 6 -5 L 8 -5 L 6 2 L 5 6","-10 10 M -9 -1 L -8 -3 L -6 -5 L -4 -5 L -3 -4 L -2 -2 L -2 1 L -4 6 M -3 -4 L -3 0 L -4 4 L -4 8 M -3 -2 L -5 3 L -5 6 L -4 8 L -2 9 L 0 9 L 2 8 L 4 6 L 6 3 L 7 -1 L 7 -5 L 6 -5 L 6 -4 L 7 -2","-15 15 M -14 -1 L -13 -3 L -11 -5 L -9 -5 L -8 -4 L -7 -2 L -7 1 L -9 6 M -8 -4 L -8 0 L -9 4 L -9 8 M -8 -2 L -10 3 L -10 6 L -9 8 L -7 9 L -5 9 L -3 8 L -1 6 L 0 3 M 2 -5 L 0 3 L 0 6 L 1 8 L 3 9 L 5 9 L 7 8 L 9 6 L 11 3 L 12 -1 L 12 -5 L 11 -5 L 11 -4 L 12 -2 M 3 -5 L 1 3 L 1 8 M 2 -5 L 4 -5 L 2 2 L 1 6","-11 11 M -8 -1 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -2 L 1 0 M -2 -5 L -1 -4 L -1 0 L -2 4 L -3 6 L -5 8 L -7 9 L -9 9 L -10 8 L -10 6 L -8 6 L -8 8 L -9 8 L -9 7 M 0 -3 L 0 0 L -1 4 L -1 7 M 8 -3 L 8 -4 L 7 -4 L 7 -2 L 9 -2 L 9 -4 L 8 -5 L 6 -5 L 4 -4 L 2 -2 L 1 0 L 0 4 L 0 8 L 1 9 M -2 4 L -2 6 L -1 8 L 1 9 L 3 9 L 5 8 L 7 5","-11 11 M -10 -1 L -9 -3 L -7 -5 L -5 -5 L -4 -4 L -3 -2 L -3 1 L -5 6 M -4 -4 L -4 0 L -5 4 L -5 8 M -4 -2 L -6 3 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 3 6 L 5 2 M 7 -5 L 3 9 L 2 12 L 0 15 L -2 16 M 8 -5 L 4 9 L 2 13 M 7 -5 L 9 -5 L 5 9 L 3 13 L 1 15 L -2 16 L -5 16 L -7 15 L -8 14 L -8 12 L -6 12 L -6 14 L -7 14 L -7 13","-10 10 M 7 -5 L 6 -3 L 4 -1 L -4 5 L -6 7 L -7 9 M 6 -3 L -3 -3 L -5 -2 L -6 0 M 4 -3 L 0 -4 L -3 -4 L -4 -3 M 4 -3 L 0 -5 L -3 -5 L -5 -3 L -6 0 M -6 7 L 3 7 L 5 6 L 6 4 M -4 7 L 0 8 L 3 8 L 4 7 M -4 7 L 0 9 L 3 9 L 5 7 L 6 4","-12 13 M -11 -6 L -8 -9 L -5 -6 L -5 6 M -9 -8 L -6 -5 L -6 6 M -5 -6 L -2 -9 L 1 -6 L 1 6 M -3 -8 L 0 -5 L 0 6 M 1 -6 L 4 -9 L 7 -6 L 7 5 L 9 7 M 3 -8 L 6 -5 L 6 6 L 8 8 L 11 5","-11 11 M 8 -9 L -8 7 M 8 -9 L 5 -8 L -1 -8 M 6 -7 L 3 -7 L -1 -8 M 8 -9 L 7 -6 L 7 0 M 6 -7 L 6 -4 L 7 0 M -1 0 L -8 0 M -2 1 L -5 1 L -8 0 M -1 0 L -1 7 M -2 1 L -2 4 L -1 7","-12 12 M -10 -3 L -8 -7 L -3 3 M -8 -5 L -3 5 L 0 -2 L 5 -2 L 8 -3 L 9 -5 L 9 -7 L 8 -9 L 6 -10 L 5 -10 L 3 -9 L 2 -7 L 2 -5 L 3 -2 L 4 0 L 5 3 L 5 6 L 3 8 M 5 -10 L 4 -9 L 3 -7 L 3 -5 L 5 -1 L 6 2 L 6 5 L 5 7 L 3 8","-12 12 M -9 -3 L -6 -6 L -2 -4 M -7 -5 L -3 -3 L 0 -6 L 3 -4 M -1 -5 L 2 -3 L 5 -6 L 7 -4 M 4 -5 L 6 -3 L 9 -6 M -9 3 L -6 0 L -2 2 M -7 1 L -3 3 L 0 0 L 3 2 M -1 1 L 2 3 L 5 0 L 7 2 M 4 1 L 6 3 L 9 0","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3" },
        new string[] {"-8 8","-5 5 M 0 -12 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -4 -12 L -4 -5 M 4 -12 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -6 9 L -8 8 L -9 7 L -10 5 L -10 3 L -9 1 L -8 0 L -1 -4 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 7 9 L 9 9 L 10 8 L 10 7","-2 2 M 0 -5 L 0 -1","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-11 9 M -11 9 L -9 8 L -6 5 L -3 1 L 1 -6 L 4 -12 L 4 9 L 3 6 L 1 3 L -1 1 L -4 -1 L -6 -1 L -7 0 L -7 2 L -6 4 L -4 6 L -1 8 L 2 9 L 7 9","-12 11 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 6 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 5 -3 L 2 -2 M 1 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9 L 0 9 L -2 8 L -3 6","-10 10 M 2 -6 L 2 -5 L 3 -4 L 5 -4 L 7 -5 L 8 -7 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -4 L -7 0 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 5 4","-11 12 M 2 -12 L 0 -11 L -1 -9 L -2 -5 L -3 1 L -4 4 L -5 6 L -7 8 L -9 9 L -11 9 L -12 8 L -12 6 L -11 5 L -9 5 L -7 6 L -5 8 L -2 9 L 1 9 L 4 8 L 6 6 L 8 2 L 9 -3 L 9 -7 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L 0 -10 L 0 -8 L 1 -5 L 3 -2 L 5 0 L 8 2 L 10 3","-10 10 M 4 -8 L 4 -7 L 5 -6 L 7 -6 L 8 -7 L 8 -9 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -4 -9 L -4 -6 L -3 -4 L -2 -3 L 1 -2 L -2 -2 L -5 -1 L -6 0 L -7 2 L -7 5 L -6 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 6 L 7 4","-10 10 M 0 -6 L -2 -6 L -4 -7 L -5 -9 L -4 -11 L -1 -12 L 2 -12 L 6 -11 L 9 -11 L 11 -12 M 6 -11 L 4 -4 L 2 2 L 0 6 L -2 8 L -4 9 L -6 9 L -8 8 L -9 6 L -9 4 L -8 3 L -6 3 L -4 4 M -1 -2 L 8 -2","-11 12 M -11 9 L -9 8 L -5 4 L -2 -1 L -1 -4 L 0 -8 L 0 -11 L -1 -12 L -2 -12 L -3 -11 L -4 -9 L -4 -6 L -3 -4 L -1 -3 L 3 -3 L 6 -4 L 7 -5 L 8 -7 L 8 -1 L 7 4 L 6 6 L 4 8 L 1 9 L -3 9 L -6 8 L -8 6 L -9 4 L -9 2","-12 12 M -5 -5 L -7 -6 L -8 -8 L -8 -9 L -7 -11 L -5 -12 L -4 -12 L -2 -11 L -1 -9 L -1 -7 L -2 -3 L -4 3 L -6 7 L -8 9 L -10 9 L -11 8 L -11 6 M -5 0 L 4 -3 L 6 -4 L 9 -6 L 11 -8 L 12 -10 L 12 -11 L 11 -12 L 10 -12 L 8 -10 L 6 -6 L 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-9 8 M 5 4 L 3 2 L 1 -1 L 0 -3 L -1 -6 L -1 -9 L 0 -11 L 1 -12 L 3 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -1 L 2 4 L 1 6 L -1 8 L -3 9 L -5 9 L -7 8 L -8 6 L -8 4 L -7 3 L -5 3 L -3 4","-8 7 M 2 12 L 0 9 L -2 4 L -3 -2 L -3 -8 L -2 -11 L 0 -12 L 2 -12 L 3 -11 L 4 -8 L 4 -5 L 3 0 L 0 9 L -2 15 L -3 18 L -4 20 L -6 21 L -7 20 L -7 18 L -6 15 L -4 12 L -2 10 L 1 8 L 5 6","-12 12 M -5 -5 L -7 -6 L -8 -8 L -8 -9 L -7 -11 L -5 -12 L -4 -12 L -2 -11 L -1 -9 L -1 -7 L -2 -3 L -4 3 L -6 7 L -8 9 L -10 9 L -11 8 L -11 6 M 12 -9 L 12 -11 L 11 -12 L 10 -12 L 8 -11 L 6 -9 L 4 -6 L 2 -4 L 0 -3 L -2 -3 M 0 -3 L 1 -1 L 1 6 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-9 10 M -5 0 L -3 0 L 1 -1 L 4 -3 L 6 -5 L 7 -7 L 7 -10 L 6 -12 L 4 -12 L 3 -11 L 2 -9 L 1 -4 L 0 1 L -1 4 L -2 6 L -4 8 L -6 9 L -8 9 L -9 8 L -9 6 L -8 5 L -6 5 L -4 6 L -1 8 L 2 9 L 4 9 L 7 8 L 9 6","-18 15 M -13 -5 L -15 -6 L -16 -8 L -16 -9 L -15 -11 L -13 -12 L -12 -12 L -10 -11 L -9 -9 L -9 -7 L -10 -2 L -11 2 L -13 9 M -11 2 L -8 -6 L -6 -10 L -5 -11 L -3 -12 L -2 -12 L 0 -11 L 1 -9 L 1 -7 L 0 -2 L -1 2 L -3 9 M -1 2 L 2 -6 L 4 -10 L 5 -11 L 7 -12 L 8 -12 L 10 -11 L 11 -9 L 11 -7 L 10 -2 L 8 5 L 8 8 L 9 9 L 10 9 L 12 8 L 13 7 L 15 4","-13 11 M -8 -5 L -10 -6 L -11 -8 L -11 -9 L -10 -11 L -8 -12 L -7 -12 L -5 -11 L -4 -9 L -4 -7 L -5 -2 L -6 2 L -8 9 M -6 2 L -3 -6 L -1 -10 L 0 -11 L 2 -12 L 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -2 L 4 5 L 4 8 L 5 9 L 6 9 L 8 8 L 9 7 L 11 4","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -4 L -7 0 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 1 L 8 -3 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 L 0 -10 L 0 -7 L 1 -4 L 3 -1 L 5 1 L 8 3 L 10 4","-12 13 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 8 -12 L 10 -11 L 11 -10 L 12 -8 L 12 -5 L 11 -3 L 10 -2 L 8 -1 L 5 -1 L 3 -2 L 2 -3","-10 12 M 3 -6 L 2 -4 L 1 -3 L -1 -2 L -3 -2 L -4 -4 L -4 -6 L -3 -9 L -1 -11 L 2 -12 L 5 -12 L 7 -11 L 8 -9 L 8 -5 L 7 -2 L 5 1 L 1 5 L -2 7 L -4 8 L -7 9 L -9 9 L -10 8 L -10 6 L -9 5 L -7 5 L -5 6 L -2 8 L 1 9 L 4 9 L 7 8 L 9 6","-12 13 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 7 -12 L 9 -11 L 10 -10 L 11 -8 L 11 -5 L 10 -3 L 9 -2 L 7 -1 L 4 -1 L 1 -2 L 2 -1 L 3 1 L 3 6 L 4 8 L 6 9 L 8 8 L 9 7 L 11 4","-10 10 M -10 9 L -8 8 L -6 6 L -3 2 L -1 -1 L 1 -5 L 2 -8 L 2 -11 L 1 -12 L 0 -12 L -1 -11 L -2 -9 L -2 -7 L -1 -5 L 1 -3 L 4 -1 L 6 1 L 7 3 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6 L -8 4 L -8 2","-10 9 M 0 -6 L -2 -6 L -4 -7 L -5 -9 L -4 -11 L -1 -12 L 2 -12 L 6 -11 L 9 -11 L 11 -12 M 6 -11 L 4 -4 L 2 2 L 0 6 L -2 8 L -4 9 L -6 9 L -8 8 L -9 6 L -9 4 L -8 3 L -6 3 L -4 4","-13 11 M -8 -5 L -10 -6 L -11 -8 L -11 -9 L -10 -11 L -8 -12 L -7 -12 L -5 -11 L -4 -9 L -4 -7 L -5 -3 L -6 0 L -7 4 L -7 6 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 3 3 L 6 -5 L 8 -12 M 6 -5 L 5 -1 L 4 5 L 4 8 L 5 9 L 6 9 L 8 8 L 9 7 L 11 4","-12 11 M -7 -5 L -9 -6 L -10 -8 L -10 -9 L -9 -11 L -7 -12 L -6 -12 L -4 -11 L -3 -9 L -3 -7 L -4 -3 L -5 0 L -6 4 L -6 7 L -5 9 L -3 9 L -1 8 L 2 5 L 4 2 L 6 -2 L 7 -5 L 8 -9 L 8 -11 L 7 -12 L 6 -12 L 5 -11 L 4 -9 L 4 -7 L 5 -4 L 7 -2 L 9 -1","-15 13 M -10 -5 L -12 -6 L -13 -8 L -13 -9 L -12 -11 L -10 -12 L -9 -12 L -7 -11 L -6 -9 L -6 -6 L -7 9 M 3 -12 L -7 9 M 3 -12 L 1 9 M 15 -12 L 13 -11 L 10 -8 L 7 -4 L 4 2 L 1 9","-12 12 M -4 -6 L -6 -6 L -7 -7 L -7 -9 L -6 -11 L -4 -12 L -2 -12 L 0 -11 L 1 -9 L 1 -6 L -1 3 L -1 6 L 0 8 L 2 9 L 4 9 L 6 8 L 7 6 L 7 4 L 6 3 L 4 3 M 11 -9 L 11 -11 L 10 -12 L 8 -12 L 6 -11 L 4 -9 L 2 -6 L -2 3 L -4 6 L -6 8 L -8 9 L -10 9 L -11 8 L -11 6","-12 11 M -7 -5 L -9 -6 L -10 -8 L -10 -9 L -9 -11 L -7 -12 L -6 -12 L -4 -11 L -3 -9 L -3 -7 L -4 -3 L -5 0 L -6 4 L -6 6 L -5 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 3 L 5 1 L 7 -5 M 9 -12 L 7 -5 L 4 5 L 2 11 L 0 16 L -2 20 L -4 21 L -5 20 L -5 18 L -4 15 L -2 12 L 1 9 L 4 7 L 9 4","-10 11 M 3 -6 L 2 -4 L 1 -3 L -1 -2 L -3 -2 L -4 -4 L -4 -6 L -3 -9 L -1 -11 L 2 -12 L 5 -12 L 7 -11 L 8 -9 L 8 -5 L 7 -2 L 5 2 L 2 5 L -2 8 L -4 9 L -7 9 L -8 8 L -8 6 L -7 5 L -4 5 L -2 6 L -1 7 L 0 9 L 0 12 L -1 15 L -2 17 L -4 20 L -6 21 L -7 20 L -7 18 L -6 15 L -4 12 L -1 9 L 2 7 L 8 4","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-8 8 M 0 -14 L -8 0 M 0 -14 L 8 0","-11 9 M -11 16 L 9 16","-4 4 M 1 -7 L -1 -5 L -1 -3 L 0 -2 L 1 -3 L 0 -4 L -1 -3","-6 10 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 L 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-5 9 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 2 L -4 8 L -3 9 L -2 9 L 0 8 L 2 6 L 3 3 L 3 0 L 4 4 L 5 5 L 7 5 L 9 4","-5 6 M 2 2 L 2 1 L 1 0 L -1 0 L -3 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9 L 1 9 L 4 7 L 6 4","-6 10 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 L 8 -12 M 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-4 6 M -3 7 L -1 6 L 0 5 L 1 3 L 1 1 L 0 0 L -1 0 L -3 1 L -4 3 L -4 6 L -3 8 L -1 9 L 1 9 L 3 8 L 4 7 L 6 4","-3 5 M -3 4 L 1 -1 L 3 -4 L 4 -6 L 5 -9 L 5 -11 L 4 -12 L 2 -11 L 1 -9 L -1 -1 L -4 8 L -7 15 L -8 18 L -8 20 L -7 21 L -5 20 L -4 17 L -3 8 L -2 9 L 0 9 L 2 8 L 3 7 L 5 4","-6 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 M 4 0 L 2 7 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 3 9 L 6 7 L 9 4","-5 10 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 1 L -5 9 M -5 9 L -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-2 5 M 1 -5 L 1 -4 L 2 -4 L 2 -5 L 1 -5 M -2 4 L 0 0 L -2 6 L -2 8 L -1 9 L 0 9 L 2 8 L 3 7 L 5 4","-2 5 M 1 -5 L 1 -4 L 2 -4 L 2 -5 L 1 -5 M -2 4 L 0 0 L -6 18 L -7 20 L -9 21 L -10 20 L -10 18 L -9 15 L -6 12 L -3 10 L -1 9 L 2 7 L 5 4","-5 9 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 1 L -5 9 M -5 9 L -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 2 4 L -1 4 M -1 4 L 1 5 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-3 5 M -3 4 L -1 1 L 2 -4 L 3 -6 L 4 -9 L 4 -11 L 3 -12 L 1 -11 L 0 -9 L -1 -5 L -2 2 L -2 8 L -1 9 L 0 9 L 2 8 L 3 7 L 5 4","-13 12 M -13 4 L -11 1 L -9 0 L -8 1 L -8 2 L -9 6 L -10 9 M -9 6 L -8 4 L -6 1 L -4 0 L -2 0 L -1 1 L -1 2 L -2 6 L -3 9 M -2 6 L -1 4 L 1 1 L 3 0 L 5 0 L 6 1 L 6 3 L 5 6 L 5 8 L 6 9 L 7 9 L 9 8 L 10 7 L 12 4","-8 10 M -8 4 L -6 1 L -4 0 L -3 1 L -3 2 L -4 6 L -5 9 M -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-6 8 M 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 L 3 5 L 3 3 L 2 1 L 0 0 L -1 1 L -1 3 L 0 5 L 2 6 L 5 6 L 7 5 L 8 4","-7 8 M -7 4 L -5 1 L -4 -1 L -5 3 L -11 21 M -5 3 L -4 1 L -2 0 L 0 0 L 2 1 L 3 3 L 3 5 L 2 7 L 1 8 L -1 9 M -5 8 L -3 9 L 0 9 L 3 8 L 5 7 L 8 4","-6 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 M 4 0 L 3 3 L 1 8 L -2 15 L -3 18 L -3 20 L -2 21 L 0 20 L 1 17 L 1 10 L 3 9 L 6 7 L 9 4","-5 8 M -5 4 L -3 1 L -2 -1 L -2 1 L 1 1 L 2 2 L 2 4 L 1 7 L 1 8 L 2 9 L 3 9 L 5 8 L 6 7 L 8 4","-4 7 M -4 4 L -2 1 L -1 -1 L -1 1 L 1 4 L 2 6 L 2 8 L 0 9 M -4 8 L -2 9 L 2 9 L 4 8 L 5 7 L 7 4","-3 6 M -3 4 L -1 1 L 1 -3 M 4 -12 L -2 6 L -2 8 L -1 9 L 1 9 L 3 8 L 4 7 L 6 4 M -2 -4 L 5 -4","-6 9 M -6 4 L -4 0 L -6 6 L -6 8 L -5 9 L -3 9 L -1 8 L 1 6 L 3 3 M 4 0 L 2 6 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-6 9 M -6 4 L -4 0 L -5 5 L -5 8 L -4 9 L -3 9 L 0 8 L 2 6 L 3 3 L 3 0 M 3 0 L 4 4 L 5 5 L 7 5 L 9 4","-9 12 M -6 0 L -8 2 L -9 5 L -9 7 L -8 9 L -6 9 L -4 8 L -2 6 M 0 0 L -2 6 L -2 8 L -1 9 L 1 9 L 3 8 L 5 6 L 6 3 L 6 0 M 6 0 L 7 4 L 8 5 L 10 5 L 12 4","-8 8 M -8 4 L -6 1 L -4 0 L -2 0 L -1 1 L -1 8 L 0 9 L 3 9 L 6 7 L 8 4 M 5 1 L 4 0 L 2 0 L 1 1 L -3 8 L -4 9 L -6 9 L -7 8","-6 9 M -6 4 L -4 0 L -6 6 L -6 8 L -5 9 L -3 9 L -1 8 L 1 6 L 3 3 M 4 0 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 3 9 L 6 7 L 9 4","-6 8 M -6 4 L -4 1 L -2 0 L 0 0 L 2 2 L 2 4 L 1 6 L -1 8 L -4 9 L -2 10 L -1 12 L -1 15 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 5 7 L 8 4","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 5 M 0 -12 L -1 -10 L 0 2 L 1 -10 L 0 -12 M 0 -10 L 0 -4 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -4 -12 L -5 -5 M -3 -12 L -5 -5 M 4 -12 L 3 -5 M 5 -12 L 3 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-15 15 M -10 -12 L -10 9 M -9 -12 L -9 9 M -13 -12 L -6 -12 M -9 -2 L -2 -2 L 1 -1 L 2 0 L 3 2 L 3 5 L 2 7 L 1 8 L -2 9 L -13 9 M -2 -2 L 0 -1 L 1 0 L 2 2 L 2 5 L 1 7 L 0 8 L -2 9 M 9 -12 L 9 9 M 10 -12 L 10 9 M 6 -12 L 13 -12 M 6 9 L 13 9","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -2 -5 M 2 -5 L 9 -5 M -9 9 L 9 9 L 9 14 L 8 9","-13 13 M -8 -5 L -8 9 M -7 -5 L -7 9 M -11 -5 L -4 -5 M -7 2 L -3 2 L 0 3 L 1 5 L 1 6 L 0 8 L -3 9 L -11 9 M -3 2 L -1 3 L 0 5 L 0 6 L -1 8 L -3 9 M 7 -5 L 7 9 M 8 -5 L 8 9 M 4 -5 L 11 -5 M 4 9 L 11 9","-4 4 M 0 -12 L -1 -5 M 1 -12 L -1 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 L 4 16 M 3 -16 L 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 L -3 16 M -4 -16 L -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -4 -10 L -5 -8 L -6 -3 L -6 0 L -5 5 L -4 7 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 5 L 6 0 L 6 -3 L 5 -8 L 4 -10 L 3 -11 L 1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9 M 0 -11 L 0 9 M -4 9 L 5 9","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 3 -2 L -2 0 L -4 1 L -6 3 L -7 6 L -7 9 M 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 2 -2 L -2 0 M -7 7 L -6 6 L -4 6 L 1 8 L 4 8 L 6 7 L 7 6 M -4 6 L 1 9 L 5 9 L 6 8 L 7 6 L 7 4","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 L -1 -3 M 2 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -4 L 2 -3 M 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 5 -1 L 6 2 L 6 5 L 5 7 L 4 8 L 2 9","-10 10 M 2 -10 L 2 9 M 3 -12 L 3 9 M 3 -12 L -8 3 L 8 3 M -1 9 L 6 9","-10 10 M -5 -12 L -7 -2 M -7 -2 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -5 -12 L 5 -12 M -5 -11 L 0 -11 L 5 -12","-10 10 M 5 -9 L 4 -8 L 5 -7 L 6 -8 L 6 -9 L 5 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2 M 0 -12 L -2 -11 L -4 -9 L -5 -7 L -6 -3 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 2 L 5 -1 L 3 -3 L 1 -4","-10 10 M -7 -12 L -7 -6 M -7 -8 L -6 -10 L -4 -12 L -2 -12 L 3 -9 L 5 -9 L 6 -10 L 7 -12 M -6 -10 L -4 -11 L -2 -11 L 3 -9 M 7 -12 L 7 -9 L 6 -6 L 2 -1 L 1 1 L 0 4 L 0 9 M 6 -6 L 1 -1 L 0 1 L -1 4 L -1 9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 6 -6 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -2 -12 L -4 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -3 M 2 -3 L 4 -4 L 5 -6 L 5 -9 L 4 -11 L 2 -12 M -2 -3 L -5 -2 L -6 -1 L -7 1 L -7 5 L -6 7 L -5 8 L -2 9 L 2 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 2 -3 M -2 -3 L -4 -2 L -5 -1 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 M 2 9 L 4 8 L 5 7 L 6 5 L 6 1 L 5 -1 L 4 -2 L 2 -3","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 0 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 5 L -5 6 M -1 1 L -3 0 L -5 -2 L -6 -5 L -6 -6 L -5 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 5 -9 L 6 -6 L 6 0 L 5 4 L 4 6 L 2 8 L 0 9","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -5 -8 L -4 -7 L -5 -6 L -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 1 -12 L 3 -11 L 4 -10 L 5 -8 L 5 -6 L 4 -4 L 2 -2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 0 -12 L -7 9 M 0 -12 L 7 9 M 0 -9 L 6 9 M -5 3 L 4 3 M -9 9 L -3 9 M 3 9 L 9 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 7 -12 L 7 -6 L 6 -12 M -5 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 3 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9","-10 11 M -6 -9 L -7 -12 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 1 9 M -2 -2 L 7 -2","-12 12 M -4 -12 L -4 -6 L -5 2 L -6 6 L -7 8 L -8 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -7 -12 L 10 -12 M -11 9 L 10 9 M -11 9 L -11 16 M -10 9 L -11 16 M 9 9 L 10 16 M 10 9 L 10 16","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M 6 -10 L -6 7 M -10 9 L -3 9 M 3 9 L 10 9","-12 13 M 0 -12 L 0 9 M 1 -12 L 1 9 M -3 -12 L 4 -12 M -2 -9 L -6 -8 L -8 -6 L -9 -3 L -9 0 L -8 3 L -6 5 L -2 6 L 3 6 L 7 5 L 9 3 L 10 0 L 10 -3 L 9 -6 L 7 -8 L 3 -9 L -2 -9 M -2 -9 L -5 -8 L -7 -6 L -8 -3 L -8 0 L -7 3 L -5 5 L -2 6 M 3 6 L 6 5 L 8 3 L 9 0 L 9 -3 L 8 -6 L 6 -8 L 3 -9 M -3 9 L 4 9","-9 9 M -4 -12 L -4 9 M -3 -12 L -3 9 M -7 -12 L 8 -12 L 8 -6 L 7 -12 M -7 9 L 0 9","-15 16 M 0 -12 L 0 9 M 1 -12 L 1 9 M -3 -12 L 4 -12 M -11 -11 L -10 -10 L -11 -9 L -12 -10 L -12 -11 L -11 -12 L -10 -12 L -9 -11 L -8 -9 L -7 -5 L -6 -3 L -4 -2 L 5 -2 L 7 -3 L 8 -5 L 9 -9 L 10 -11 L 11 -12 L 12 -12 L 13 -11 L 13 -10 L 12 -9 L 11 -10 L 12 -11 M -4 -2 L -6 -1 L -7 1 L -8 6 L -9 8 L -10 9 M -4 -2 L -5 -1 L -6 1 L -7 6 L -8 8 L -9 9 L -11 9 L -12 8 L -13 6 M 5 -2 L 7 -1 L 8 1 L 9 6 L 10 8 L 11 9 M 5 -2 L 6 -1 L 7 1 L 8 6 L 9 8 L 10 9 L 12 9 L 13 8 L 14 6 M -3 9 L 4 9","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M 6 -10 L -6 7 M -10 9 L -3 9 M 3 9 L 10 9","-12 11 M -7 -12 L -7 -1 L -6 1 L -3 2 L 0 2 L 3 1 L 5 -1 M -6 -12 L -6 -1 L -5 1 L -3 2 M 5 -12 L 5 9 M 6 -12 L 6 9 M -10 -12 L -3 -12 M 2 -12 L 9 -12 M 2 9 L 9 9","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M -10 -12 L -3 -12 M -6 -2 L 1 -2 L 3 -3 L 4 -5 L 5 -9 L 6 -11 L 7 -12 L 8 -12 L 9 -11 L 9 -10 L 8 -9 L 7 -10 L 8 -11 M 1 -2 L 3 -1 L 4 1 L 5 6 L 6 8 L 7 9 M 1 -2 L 2 -1 L 3 1 L 4 6 L 5 8 L 6 9 L 8 9 L 9 8 L 10 6 M -10 9 L -3 9","-13 12 M -5 -12 L -5 -6 L -6 2 L -7 6 L -8 8 L -9 9 L -10 9 L -11 8 L -11 7 L -10 6 L -9 7 L -10 8 M 6 -12 L 6 9 M 7 -12 L 7 9 M -8 -12 L 10 -12 M 3 9 L 10 9","-12 13 M -7 -12 L -7 9 M -6 -12 L 0 6 M -7 -12 L 0 9 M 7 -12 L 0 9 M 7 -12 L 7 9 M 8 -12 L 8 9 M -10 -12 L -6 -12 M 7 -12 L 11 -12 M -10 9 L -4 9 M 4 9 L 11 9","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M -6 -2 L 6 -2 M -10 9 L -3 9 M 3 9 L 10 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 0 L -6 4 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 4 L 7 0 L 7 -3 L 6 -7 L 5 -9 L 3 -11 L 1 -12","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L 10 -12 M -10 9 L -3 9 M 3 9 L 10 9","-16 17 M -11 -12 L -11 9 M -10 -12 L -10 9 M 0 -12 L 0 9 M 1 -12 L 1 9 M 11 -12 L 11 9 M 12 -12 L 12 9 M -14 -12 L -7 -12 M -3 -12 L 4 -12 M 8 -12 L 15 -12 M -14 9 L 15 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -5 -1 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 3 -1 M -9 9 L -2 9","-11 10 M 6 -9 L 7 -6 L 7 -12 L 6 -9 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9","-9 10 M 0 -12 L 0 9 M 1 -12 L 1 9 M -6 -12 L -7 -6 L -7 -12 L 8 -12 L 8 -6 L 7 -12 M -3 9 L 4 9","-15 16 M -10 -12 L -10 9 M -9 -12 L -9 9 M -13 -12 L -6 -12 M -13 9 L -6 9 M 4 -12 L 1 -11 L -1 -9 L -2 -7 L -3 -3 L -3 0 L -2 4 L -1 6 L 1 8 L 4 9 L 6 9 L 9 8 L 11 6 L 12 4 L 13 0 L 13 -3 L 12 -7 L 11 -9 L 9 -11 L 6 -12 L 4 -12 M 4 -12 L 2 -11 L 0 -9 L -1 -7 L -2 -3 L -2 0 L -1 4 L 0 6 L 2 8 L 4 9 M 6 9 L 8 8 L 10 6 L 11 4 L 12 0 L 12 -3 L 11 -7 L 10 -9 L 8 -11 L 6 -12 M -9 -2 L -3 -2","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 3 -2 M -5 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 3 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9","-16 17 M -11 -12 L -11 9 M -10 -12 L -10 9 M 0 -12 L 0 9 M 1 -12 L 1 9 M 11 -12 L 11 9 M 12 -12 L 12 9 M -14 -12 L -7 -12 M -3 -12 L 4 -12 M 8 -12 L 15 -12 M -14 9 L 15 9 M 14 9 L 15 16 M 15 9 L 15 16","-10 10 M -7 -12 L 6 9 M -6 -12 L 7 9 M 7 -12 L -7 9 M -9 -12 L -3 -12 M 3 -12 L 9 -12 M -9 9 L -3 9 M 3 9 L 9 9","-10 11 M -7 -12 L 0 4 M -6 -12 L 1 4 M 8 -12 L 1 4 L -1 7 L -2 8 L -4 9 L -5 9 L -6 8 L -6 7 L -5 6 L -4 7 L -5 8 M -9 -12 L -3 -12 M 4 -12 L 10 -12","-10 10 M -6 -9 L -7 -12 L -7 -6 L -6 -9 L -4 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 L -1 -3 M 2 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -4 L 2 -3 M 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -3 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 5 -1 L 6 2 L 6 5 L 5 7 L 4 8 L 2 9","-11 10 M -6 -12 L -6 9 M -5 -12 L -5 9 M 1 -6 L 1 2 M -9 -12 L 7 -12 L 7 -6 L 6 -12 M -5 -2 L 1 -2 M -9 9 L 7 9 L 7 3 L 6 9","-7 7 M -7 -12 L 7 12","-12 14 M -2 -12 L -2 9 M -1 -12 L -1 9 M -9 -12 L -10 -6 L -10 -12 L 2 -12 M -1 -2 L 6 -2 L 9 -1 L 10 0 L 11 2 L 11 5 L 10 7 L 9 8 L 6 9 L -5 9 M 6 -2 L 8 -1 L 9 0 L 10 2 L 10 5 L 9 7 L 8 8 L 6 9","-11 11 M 5 -12 L 5 9 M 6 -12 L 6 9 M 9 -12 L -3 -12 L -6 -11 L -7 -10 L -8 -8 L -8 -6 L -7 -4 L -6 -3 L -3 -2 L 5 -2 M -3 -12 L -5 -11 L -6 -10 L -7 -8 L -7 -6 L -6 -4 L -5 -3 L -3 -2 M 0 -2 L -2 -1 L -3 0 L -6 7 L -7 8 L -8 8 L -9 7 M -2 -1 L -3 1 L -5 8 L -6 9 L -8 9 L -9 7 L -9 6 M 2 9 L 9 9","-10 11 M -5 -12 L -5 9 M -4 -12 L -4 9 M -8 -12 L -1 -12 M -4 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -8 9 M 3 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M -10 9 L 10 9 M 9 9 L 10 16 M 10 9 L 10 16","-9 11 M -4 -3 L -4 -2 L -5 -2 L -5 -3 L -4 -4 L -2 -5 L 2 -5 L 4 -4 L 5 -3 L 6 -1 L 6 6 L 7 8 L 8 9 M 5 -3 L 5 6 L 6 8 L 8 9 L 9 9 M 5 -1 L 4 0 L -2 1 L -5 2 L -6 4 L -6 6 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 M -2 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9","-10 10 M 6 -12 L 5 -11 L -1 -9 L -4 -7 L -6 -4 L -7 -1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 M 6 -12 L 5 -10 L 3 -9 L -1 -8 L -4 -6 L -6 -4 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 1 L 5 -2 L 3 -4 L 1 -5","-10 11 M -6 -9 L -7 -12 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 1 9 M -2 -2 L 7 -2","-12 11 M -4 -5 L -4 -1 L -5 5 L -6 8 L -7 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -7 -5 L 9 -5 M -9 9 L -10 14 L -10 9 L 9 9 L 9 14 L 8 9","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -2 -5 M 2 -5 L 9 -5 M -9 9 L -2 9 M 2 9 L 9 9 M 5 -4 L -5 8 M -3 -11 L -3 -12 L -4 -12 L -4 -11 L -3 -9 L -1 -8 L 1 -8 L 3 -9 L 4 -11","-10 11 M 0 -12 L 0 16 M 1 -12 L 1 16 M -3 -12 L 1 -12 M 0 -2 L -1 -4 L -2 -5 L -4 -5 L -6 -4 L -7 -1 L -7 5 L -6 8 L -4 9 L -2 9 L -1 8 L 0 6 M -4 -5 L -5 -4 L -6 -1 L -6 5 L -5 8 L -4 9 M 5 -5 L 6 -4 L 7 -1 L 7 5 L 6 8 L 5 9 M 1 -2 L 2 -4 L 3 -5 L 5 -5 L 7 -4 L 8 -1 L 8 5 L 7 8 L 5 9 L 3 9 L 2 8 L 1 6 M -3 16 L 4 16","-10 8 M -5 -5 L -5 9 M -4 -5 L -4 9 M -8 -5 L 6 -5 L 6 0 L 5 -5 M -8 9 L -1 9","-13 14 M 0 -5 L 0 9 M 1 -5 L 1 9 M -3 -5 L 4 -5 M -8 -4 L -9 -3 L -10 -4 L -9 -5 L -8 -5 L -7 -4 L -5 0 L -4 1 L -2 2 L 3 2 L 5 1 L 6 0 L 8 -4 L 9 -5 L 10 -5 L 11 -4 L 10 -3 L 9 -4 M -2 2 L -4 3 L -5 4 L -7 8 L -8 9 M -2 2 L -4 4 L -6 8 L -7 9 L -9 9 L -10 8 L -11 6 M 3 2 L 5 3 L 6 4 L 8 8 L 9 9 M 3 2 L 5 4 L 7 8 L 8 9 L 10 9 L 11 8 L 12 6 M -3 9 L 4 9","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -2 -5 M 2 -5 L 9 -5 M -9 9 L -2 9 M 2 9 L 9 9 M 5 -4 L -5 8","-11 11 M -6 -5 L -6 2 L -5 4 L -2 5 L 0 5 L 3 4 L 5 2 M -5 -5 L -5 2 L -4 4 L -2 5 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -2 -5 M 2 -5 L 9 -5 M 2 9 L 9 9","-10 10 M -5 -5 L -5 9 M -4 -5 L -4 9 M -8 -5 L -1 -5 M -4 2 L -2 2 L 1 1 L 2 0 L 4 -4 L 5 -5 L 6 -5 L 7 -4 L 6 -3 L 5 -4 M -2 2 L 1 3 L 2 4 L 4 8 L 5 9 M -2 2 L 0 3 L 1 4 L 3 8 L 4 9 L 6 9 L 7 8 L 8 6 M -8 9 L -1 9","-11 11 M -4 -5 L -4 -1 L -5 5 L -6 8 L -7 9 L -8 9 L -9 8 L -8 7 L -7 8 M 5 -5 L 5 9 M 6 -5 L 6 9 M -7 -5 L 9 -5 M 2 9 L 9 9","-11 12 M -6 -5 L -6 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M 6 -5 L 0 9 M 6 -5 L 6 9 M 7 -5 L 7 9 M -9 -5 L -5 -5 M 6 -5 L 10 -5 M -9 9 L -3 9 M 3 9 L 10 9","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -2 -5 M 2 -5 L 9 -5 M -5 2 L 5 2 M -9 9 L -2 9 M 2 9 L 9 9","-10 10 M -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 1 -5 L -1 -5 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 1 L 5 -2 L 3 -4 L 1 -5","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L 9 -5 M -9 9 L -2 9 M 2 9 L 9 9","-15 16 M -10 -5 L -10 9 M -9 -5 L -9 9 M 0 -5 L 0 9 M 1 -5 L 1 9 M 10 -5 L 10 9 M 11 -5 L 11 9 M -13 -5 L -6 -5 M -3 -5 L 4 -5 M 7 -5 L 14 -5 M -13 9 L 14 9","-11 10 M -6 -5 L -6 16 M -5 -5 L -5 16 M -5 -2 L -3 -4 L -1 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -1 9 L -3 8 L -5 6 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -9 -5 L -5 -5 M -9 16 L -2 16","-10 9 M 5 -2 L 4 -1 L 5 0 L 6 -1 L 6 -2 L 4 -4 L 2 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9","-9 10 M 0 -5 L 0 9 M 1 -5 L 1 9 M -5 -5 L -6 0 L -6 -5 L 7 -5 L 7 0 L 6 -5 M -3 9 L 4 9","-14 15 M -9 -5 L -9 9 M -8 -5 L -8 9 M -12 -5 L -5 -5 M -12 9 L -5 9 M 4 -5 L 1 -4 L -1 -2 L -2 1 L -2 3 L -1 6 L 1 8 L 4 9 L 6 9 L 9 8 L 11 6 L 12 3 L 12 1 L 11 -2 L 9 -4 L 6 -5 L 4 -5 M 4 -5 L 2 -4 L 0 -2 L -1 1 L -1 3 L 0 6 L 2 8 L 4 9 M 6 9 L 8 8 L 10 6 L 11 3 L 11 1 L 10 -2 L 8 -4 L 6 -5 M -8 2 L -2 2","-10 10 M -5 -5 L -5 9 M -4 -5 L -4 9 M -8 -5 L 3 -5 L 6 -4 L 7 -2 L 7 -1 L 6 1 L 3 2 M 3 -5 L 5 -4 L 6 -2 L 6 -1 L 5 1 L 3 2 M -4 2 L 3 2 L 6 3 L 7 5 L 7 6 L 6 8 L 3 9 L -8 9 M 3 2 L 5 3 L 6 5 L 6 6 L 5 8 L 3 9","-15 16 M -10 -5 L -10 9 M -9 -5 L -9 9 M 0 -5 L 0 9 M 1 -5 L 1 9 M 10 -5 L 10 9 M 11 -5 L 11 9 M -13 -5 L -6 -5 M -3 -5 L 4 -5 M 7 -5 L 14 -5 M -13 9 L 14 9 L 14 14 L 13 9","-10 10 M -6 -5 L 5 9 M -5 -5 L 6 9 M 6 -5 L -6 9 M -8 -5 L -2 -5 M 2 -5 L 8 -5 M -8 9 L -2 9 M 2 9 L 8 9","-9 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M 6 -5 L 0 9 L -2 13 L -4 15 L -6 16 L -7 16 L -8 15 L -7 14 L -6 15 M -8 -5 L -2 -5 M 2 -5 L 8 -5","-9 9 M -5 -3 L -6 -5 L -6 -1 L -5 -3 L -4 -4 L -2 -5 L 2 -5 L 5 -4 L 6 -2 L 6 -1 L 5 1 L 2 2 M 2 -5 L 4 -4 L 5 -2 L 5 -1 L 4 1 L 2 2 M -1 2 L 2 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -2 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 5 L -5 6 M 2 2 L 4 3 L 5 5 L 5 6 L 4 8 L 2 9","-10 9 M -6 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M 5 1 L 5 -2 L 4 -4 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9","-10 11 M -1 -5 L -1 9 M 0 -5 L 0 9 M -6 -5 L -7 0 L -7 -5 L 3 -5 M 0 2 L 4 2 L 7 3 L 8 5 L 8 6 L 7 8 L 4 9 L -4 9 M 4 2 L 6 3 L 7 5 L 7 6 L 6 8 L 4 9","-11 10 M 4 -5 L 4 9 M 5 -5 L 5 9 M 8 -5 L -3 -5 L -6 -4 L -7 -2 L -7 -1 L -6 1 L -3 2 L 4 2 M -3 -5 L -5 -4 L -6 -2 L -6 -1 L -5 1 L -3 2 M 2 2 L -1 3 L -2 4 L -4 8 L -5 9 M 2 2 L 0 3 L -1 4 L -3 8 L -4 9 L -6 9 L -7 8 L -8 6 M 1 9 L 8 9","-8 9 M -3 -5 L -3 9 M -2 -5 L -2 9 M -6 -5 L 1 -5 M -2 2 L 2 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -6 9 M 2 2 L 4 3 L 5 5 L 5 6 L 4 8 L 2 9","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3"},
        new string[] {"-8 8","-5 5 M 0 -12 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -4 -12 L -4 -5 M 4 -12 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -6 9 L -8 8 L -9 7 L -10 5 L -10 3 L -9 1 L -8 0 L -1 -4 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 7 9 L 9 9 L 10 8 L 10 7","-5 5 M 0 -10 L -1 -11 L 0 -12 L 1 -11 L 1 -9 L 0 -7 L -1 -6","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9 M -5 2 L 5 2","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 M -7 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -7 9","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4","-11 10 M -7 -12 L -7 9 M -7 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 0 9 L -7 9","-10 9 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2 M -6 9 L 7 9","-10 8 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4 L 8 1 M 3 1 L 8 1","-11 11 M -7 -12 L -7 9 M 7 -12 L 7 9 M -7 -2 L 7 -2","-4 4 M 0 -12 L 0 9","-8 8 M 4 -12 L 4 4 L 3 7 L 2 8 L 0 9 L -2 9 L -4 8 L -5 7 L -6 4 L -6 2","-11 10 M -7 -12 L -7 9 M 7 -12 L -7 2 M -2 -3 L 7 9","-10 7 M -6 -12 L -6 9 M -6 9 L 6 9","-12 12 M -8 -12 L -8 9 M -8 -12 L 0 9 M 8 -12 L 0 9 M 8 -12 L 8 9","-11 11 M -7 -12 L -7 9 M -7 -12 L 7 9 M 7 -12 L 7 9","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 L -7 -1","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M 1 5 L 7 11","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 L -7 -2 M 0 -2 L 7 9","-10 10 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-8 8 M 0 -12 L 0 9 M -7 -12 L 7 -12","-11 11 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -12","-9 9 M -8 -12 L 0 9 M 8 -12 L 0 9","-12 12 M -10 -12 L -5 9 M 0 -12 L -5 9 M 0 -12 L 5 9 M 10 -12 L 5 9","-10 10 M -7 -12 L 7 9 M 7 -12 L -7 9","-9 9 M -8 -12 L 0 -2 L 0 9 M 8 -12 L 0 -2","-10 10 M 7 -12 L -7 9 M -7 -12 L 7 -12 M -7 9 L 7 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-8 8 M 0 -14 L -8 0 M 0 -14 L 8 0","-9 9 M -9 16 L 9 16","-4 4 M 1 -7 L -1 -5 L -1 -3 L 0 -2 L 1 -3 L 0 -4 L -1 -3","-9 10 M 6 -5 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-10 9 M -6 -12 L -6 9 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M 6 -12 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 9 M -6 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-5 7 M 5 -12 L 3 -12 L 1 -11 L 0 -8 L 0 9 M -3 -5 L 4 -5","-9 10 M 6 -5 L 6 11 L 5 14 L 4 15 L 2 16 L -1 16 L -3 15 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M -5 -12 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-4 4 M -1 -12 L 0 -11 L 1 -12 L 0 -13 L -1 -12 M 0 -5 L 0 9","-5 5 M 0 -12 L 1 -11 L 2 -12 L 1 -13 L 0 -12 M 1 -5 L 1 12 L 0 15 L -2 16 L -4 16","-9 8 M -5 -12 L -5 9 M 5 -5 L -5 5 M -1 1 L 6 9","-4 4 M 0 -12 L 0 9","-15 15 M -11 -5 L -11 9 M -11 -1 L -8 -4 L -6 -5 L -3 -5 L -1 -4 L 0 -1 L 0 9 M 0 -1 L 3 -4 L 5 -5 L 8 -5 L 10 -4 L 11 -1 L 11 9","-9 10 M -5 -5 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-9 10 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 2 -5 L -1 -5","-10 9 M -6 -5 L -6 16 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 10 M 6 -5 L 6 16 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-7 6 M -3 -5 L -3 9 M -3 1 L -2 -2 L 0 -4 L 2 -5 L 5 -5","-8 9 M 6 -2 L 5 -4 L 2 -5 L -1 -5 L -4 -4 L -5 -2 L -4 0 L -2 1 L 3 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -1 9 L -4 8 L -5 6","-5 7 M 0 -12 L 0 5 L 1 8 L 3 9 L 5 9 M -3 -5 L 4 -5","-9 10 M -5 -5 L -5 5 L -4 8 L -2 9 L 1 9 L 3 8 L 6 5 M 6 -5 L 6 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9","-11 11 M -8 -5 L -4 9 M 0 -5 L -4 9 M 0 -5 L 4 9 M 8 -5 L 4 9","-8 9 M -5 -5 L 6 9 M 6 -5 L -5 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9 L -2 13 L -4 15 L -6 16 L -7 16","-8 9 M 6 -5 L -5 9 M -5 -5 L 6 -5 M -5 9 L 6 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 6 M 0 -12 L 0 2 L 1 2 M 0 -12 L 1 -12 L 1 2 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-9 10 M 0 -16 L 0 13 L 1 13 M 0 -16 L 1 -16 L 1 13 M 5 -9 L 7 -9 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -9 L -6 -7 L -5 -5 L -4 -4 L 4 0 L 5 1 L 6 3 L 6 5 L 5 7 L 2 8 L -1 8 L -3 7 L -4 6 M 5 -9 L 4 -10 L 2 -11 L -1 -11 L -4 -10 L -5 -9 L -5 -7 L -4 -5 L 4 -1 L 6 1 L 7 3 L 7 5 L 6 7 L 5 8 L 2 9 L -1 9 L -4 8 L -6 6 L -4 6 M 6 6 L 3 8","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 13 M 9 -4 L 8 -3 L 9 -2 L 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -5 9 L -8 8 L -9 6 L -9 3 L -8 1 L -2 -3 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 8 9 L 9 9 L 10 8 L 10 7 M -5 9 L -7 8 L -8 6 L -8 3 L -7 1 L -5 -1 M -5 -7 L -4 -5 L 4 6 L 6 8 L 8 9","-4 5 M 1 -12 L 0 -11 L 0 -5 M 1 -11 L 0 -5 M 1 -12 L 2 -11 L 0 -5","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16 M 2 -14 L 0 -10 L -1 -7 L -2 -2 L -2 2 L -1 7 L 0 10 L 2 14","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 M -2 -14 L 0 -10 L 1 -7 L 2 -2 L 2 2 L 1 7 L 0 10 L -2 14","-8 8 M 0 -12 L -1 -11 L 1 -1 L 0 0 M 0 -12 L 0 0 M 0 -12 L 1 -11 L -1 -1 L 0 0 M -5 -9 L -4 -9 L 4 -3 L 5 -3 M -5 -9 L 5 -3 M -5 -9 L -5 -8 L 5 -4 L 5 -3 M 5 -9 L 4 -9 L -4 -3 L -5 -3 M 5 -9 L -5 -3 M 5 -9 L 5 -8 L -5 -4 L -5 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-5 6 M 2 8 L 1 9 L 0 9 L -1 8 L -1 7 L 0 6 L 1 6 L 2 7 L 2 10 L 1 12 L -1 13 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7 M 1 9 L 2 10 M 2 8 L 1 12","-13 13 M -9 0 L 9 0","-5 6 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -3 -11 L -5 -8 L -6 -3 L -6 0 L -5 5 L -3 8 M -4 7 L -1 8 L 1 8 L 4 7 M 3 8 L 5 5 L 6 0 L 6 -3 L 5 -8 L 3 -11 M 4 -10 L 1 -11 L -1 -11 L -4 -10","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9 M -4 -8 L -4 -7 L -2 -8 L 0 -10 L 0 9 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -6 9 M -6 -7 L -5 -7 L -5 -8 L -4 -10 L -2 -11 L 2 -11 L 4 -10 L 5 -8 L 5 -6 L 4 -4 L 2 -1 L -7 9 M -6 8 L 7 8 L 7 9 M -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L -1 -3 M -5 -12 L -5 -11 L 5 -11 M 5 -12 L -2 -3 M -1 -4 L 1 -4 L 4 -3 L 6 -1 L 7 2 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -6 5 M -2 -3 L 1 -3 L 4 -2 L 6 1 M 2 -3 L 5 -1 L 6 2 L 6 3 L 5 6 L 2 8 M 6 4 L 4 7 L 1 8 L -2 8 L -5 7 L -6 5 M -3 8 L -6 6","-10 10 M 3 -9 L 3 9 L 4 9 M 4 -12 L 4 9 M 4 -12 L -7 4 L 8 4 M 3 -9 L -6 4 M -6 3 L 8 3 L 8 4","-10 10 M -5 -12 L -6 -3 M -4 -11 L -5 -4 M -5 -12 L 5 -12 L 5 -11 M -4 -11 L 5 -11 M -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -6 5 M -6 -3 L -5 -3 L -3 -4 L 1 -4 L 4 -3 L 6 0 M 2 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 2 8 M 6 4 L 4 7 L 1 8 L -2 8 L -5 7 L -6 5 M -3 8 L -6 6","-10 10 M 4 -11 L 5 -9 L 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 M 5 -10 L 2 -11 L 0 -11 L -3 -10 M -2 -11 L -4 -8 L -5 -3 L -5 2 L -4 6 L -1 8 M -5 4 L -3 7 L 0 8 L 1 8 L 4 7 L 6 4 M 2 8 L 5 6 L 6 3 L 6 2 L 5 -1 L 2 -3 M 6 1 L 4 -2 L 1 -3 L 0 -3 L -3 -2 L -5 1 M -1 -3 L -4 -1 L -5 2","-10 10 M -7 -12 L 7 -12 L -3 9 M -7 -12 L -7 -11 L 6 -11 M 6 -12 L -4 9 L -3 9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -4 -4 L -2 -3 L 2 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 2 8 L -2 8 L -5 7 L -6 5 L -6 2 L -5 0 L -4 -1 L -2 -2 L 2 -3 L 4 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -4 -11 L -5 -9 L -5 -7 L -4 -5 L -2 -4 L 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -2 -3 L 2 -4 L 4 -5 L 5 -7 L 5 -9 L 4 -11 M 5 -10 L 2 -11 L -2 -11 L -5 -10 M -6 6 L -3 8 M 3 8 L 6 6","-10 10 M 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6 L -5 6 L -4 8 M 5 -5 L 4 -2 L 1 0 M 5 -4 L 3 -1 L 0 0 L -1 0 L -4 -1 L -6 -4 M -2 0 L -5 -2 L -6 -5 L -6 -6 L -5 -9 L -2 -11 M -6 -7 L -4 -10 L -1 -11 L 0 -11 L 3 -10 L 5 -7 M 1 -11 L 4 -9 L 5 -5 L 5 0 L 4 5 L 2 8 M 3 7 L 0 8 L -2 8 L -5 7","-5 6 M 0 -5 L -1 -4 L -1 -3 L 0 -2 L 1 -2 L 2 -3 L 2 -4 L 1 -5 L 0 -5 M 0 -4 L 0 -3 L 1 -3 L 1 -4 L 0 -4 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-5 6 M 0 -5 L -1 -4 L -1 -3 L 0 -2 L 1 -2 L 2 -3 L 2 -4 L 1 -5 L 0 -5 M 0 -4 L 0 -3 L 1 -3 L 1 -4 L 0 -4 M 2 8 L 1 9 L 0 9 L -1 8 L -1 7 L 0 6 L 1 6 L 2 7 L 2 10 L 1 12 L -1 13 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7 M 1 9 L 2 10 M 2 8 L 1 12","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-9 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -1 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 3 -2 L 0 -1 M -6 -7 L -5 -7 L -5 -8 L -4 -10 L -1 -11 L 2 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -3 L 0 -2 M -5 -9 L -2 -11 M 3 -11 L 6 -9 M 6 -5 L 2 -2 M 0 -2 L 0 2 L 1 2 L 1 -2 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 0 -12 L -8 9 M 0 -9 L -7 9 L -8 9 M 0 -9 L 7 9 L 8 9 M 0 -12 L 8 9 M -5 3 L 5 3 M -6 4 L 6 4","-10 10 M -6 -12 L -6 9 M -5 -11 L -5 8 M -6 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 M -5 -11 L 2 -11 L 5 -10 L 6 -8 L 6 -5 L 5 -3 L 2 -2 M -5 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -6 9 M -5 -1 L 2 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 2 8 L -5 8","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4 M 8 -7 L 7 -7 L 6 -9 L 5 -10 L 3 -11 L -1 -11 L -3 -10 L -5 -7 L -6 -4 L -6 1 L -5 4 L -3 7 L -1 8 L 3 8 L 5 7 L 6 6 L 7 4 L 8 4","-10 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -6 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -6 9 M -5 -11 L 1 -11 L 4 -10 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 4 7 L 1 8 L -5 8","-9 10 M -5 -12 L -5 9 M -4 -11 L -4 8 M -5 -12 L 7 -12 M -4 -11 L 7 -11 L 7 -12 M -4 -2 L 2 -2 L 2 -1 M -4 -1 L 2 -1 M -4 8 L 7 8 L 7 9 M -5 9 L 7 9","-9 9 M -5 -12 L -5 9 M -4 -11 L -4 9 L -5 9 M -5 -12 L 7 -12 M -4 -11 L 7 -11 L 7 -12 M -4 -2 L 2 -2 L 2 -1 M -4 -1 L 2 -1","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4 L 8 0 L 3 0 M 8 -7 L 7 -7 L 6 -9 L 5 -10 L 3 -11 L -1 -11 L -3 -10 L -4 -9 L -5 -7 L -6 -4 L -6 1 L -5 4 L -4 6 L -3 7 L -1 8 L 3 8 L 5 7 L 6 6 L 7 4 L 7 1 L 3 1 L 3 0","-11 11 M -7 -12 L -7 9 M -7 -12 L -6 -12 L -6 9 L -7 9 M 7 -12 L 6 -12 L 6 9 L 7 9 M 7 -12 L 7 9 M -6 -2 L 6 -2 M -6 -1 L 6 -1","-4 5 M 0 -12 L 0 9 L 1 9 M 0 -12 L 1 -12 L 1 9","-8 9 M 4 -12 L 4 4 L 3 7 L 1 8 L -1 8 L -3 7 L -4 4 L -5 4 M 4 -12 L 5 -12 L 5 4 L 4 7 L 3 8 L 1 9 L -1 9 L -3 8 L -4 7 L -5 4","-10 11 M -6 -12 L -6 9 L -5 9 M -6 -12 L -5 -12 L -5 9 M 8 -12 L 7 -12 L -5 0 M 8 -12 L -5 1 M -2 -3 L 7 9 L 8 9 M -1 -3 L 8 9","-9 8 M -5 -12 L -5 9 M -5 -12 L -4 -12 L -4 8 M -4 8 L 7 8 L 7 9 M -5 9 L 7 9","-12 12 M -8 -12 L -8 9 M -7 -7 L -7 9 L -8 9 M -7 -7 L 0 9 M -8 -12 L 0 6 M 8 -12 L 0 6 M 7 -7 L 0 9 M 7 -7 L 7 9 L 8 9 M 8 -12 L 8 9","-11 11 M -7 -12 L -7 9 M -6 -9 L -6 9 L -7 9 M -6 -9 L 7 9 M -7 -12 L 6 6 M 6 -12 L 6 6 M 6 -12 L 7 -12 L 7 9","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M -1 -11 L -4 -10 L -6 -7 L -7 -4 L -7 1 L -6 4 L -4 7 L -1 8 L 1 8 L 4 7 L 6 4 L 7 1 L 7 -4 L 6 -7 L 4 -10 L 1 -11 L -1 -11","-10 10 M -6 -12 L -6 9 M -5 -11 L -5 9 L -6 9 M -6 -12 L 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 3 -1 L -5 -1 M -5 -11 L 3 -11 L 5 -10 L 6 -8 L 6 -5 L 5 -3 L 3 -2 L -5 -2","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M -1 -11 L -4 -10 L -6 -7 L -7 -4 L -7 1 L -6 4 L -4 7 L -1 8 L 1 8 L 4 7 L 6 4 L 7 1 L 7 -4 L 6 -7 L 4 -10 L 1 -11 L -1 -11 M 1 6 L 6 11 L 7 11 M 1 6 L 2 6 L 7 11","-10 10 M -6 -12 L -6 9 M -5 -11 L -5 9 L -6 9 M -6 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 L -5 -1 M -5 -11 L 2 -11 L 5 -10 L 6 -8 L 6 -5 L 5 -3 L 2 -2 L -5 -2 M 0 -1 L 6 9 L 7 9 M 1 -1 L 7 9","-10 10 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 2 -1 L 4 0 L 5 1 L 6 3 L 6 6 L 5 7 L 2 8 L -2 8 L -4 7 L -5 6 L -7 6 M 7 -9 L 5 -9 L 4 -10 L 2 -11 L -2 -11 L -5 -10 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 2 -2 L 4 -1 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-8 9 M 0 -11 L 0 9 M 1 -11 L 1 9 L 0 9 M -6 -12 L 7 -12 L 7 -11 M -6 -12 L -6 -11 L 7 -11","-11 11 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -12 M -7 -12 L -6 -12 L -6 3 L -5 6 L -4 7 L -1 8 L 1 8 L 4 7 L 5 6 L 6 3 L 6 -12 L 7 -12","-10 10 M -8 -12 L 0 9 M -8 -12 L -7 -12 L 0 6 M 8 -12 L 7 -12 L 0 6 M 8 -12 L 0 9","-13 13 M -11 -12 L -5 9 M -11 -12 L -10 -12 L -5 6 M 0 -12 L -5 6 M 0 -9 L -5 9 M 0 -9 L 5 9 M 0 -12 L 5 6 M 11 -12 L 10 -12 L 5 6 M 11 -12 L 5 9","-10 10 M -7 -12 L 6 9 L 7 9 M -7 -12 L -6 -12 L 7 9 M 7 -12 L 6 -12 L -7 9 M 7 -12 L -6 9 L -7 9","-9 10 M -7 -12 L 0 -2 L 0 9 L 1 9 M -7 -12 L -6 -12 L 1 -2 M 8 -12 L 7 -12 L 0 -2 M 8 -12 L 1 -2 L 1 9","-10 10 M 6 -12 L -7 9 M 7 -12 L -6 9 M -7 -12 L 7 -12 M -7 -12 L -7 -11 L 6 -11 M -6 8 L 7 8 L 7 9 M -7 9 L 7 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-10 10 M 5 -5 L 5 9 L 6 9 M 5 -5 L 6 -5 L 6 9 M 5 -2 L 3 -4 L 1 -5 L -2 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6 M 5 -2 L 1 -4 L -2 -4 L -4 -3 L -5 -2 L -6 1 L -6 3 L -5 6 L -4 7 L -2 8 L 1 8 L 5 6","-10 10 M -6 -12 L -6 9 L -5 9 M -6 -12 L -5 -12 L -5 9 M -5 -2 L -3 -4 L -1 -5 L 2 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 2 9 L -1 9 L -3 8 L -5 6 M -5 -2 L -1 -4 L 2 -4 L 4 -3 L 5 -2 L 6 1 L 6 3 L 5 6 L 4 7 L 2 8 L -1 8 L -5 6","-9 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 M 6 -2 L 5 -1 L 4 -3 L 2 -4 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 3 L -4 6 L -3 7 L -1 8 L 2 8 L 4 7 L 5 5 L 6 6","-10 10 M 5 -12 L 5 9 L 6 9 M 5 -12 L 6 -12 L 6 9 M 5 -2 L 3 -4 L 1 -5 L -2 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6 M 5 -2 L 1 -4 L -2 -4 L -4 -3 L -5 -2 L -6 1 L -6 3 L -5 6 L -4 7 L -2 8 L 1 8 L 5 6","-9 9 M -5 2 L 6 2 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 M -5 1 L 5 1 L 5 -1 L 4 -3 L 2 -4 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 3 L -4 6 L -3 7 L -1 8 L 2 8 L 4 7 L 5 5 L 6 6","-6 8 M 5 -12 L 3 -12 L 1 -11 L 0 -8 L 0 9 L 1 9 M 5 -12 L 5 -11 L 3 -11 L 1 -10 M 2 -11 L 1 -8 L 1 9 M -3 -5 L 4 -5 L 4 -4 M -3 -5 L -3 -4 L 4 -4","-10 10 M 6 -5 L 5 -5 L 5 10 L 4 13 L 3 14 L 1 15 L -1 15 L -3 14 L -4 13 L -6 13 M 6 -5 L 6 10 L 5 13 L 3 15 L 1 16 L -2 16 L -4 15 L -6 13 M 5 -2 L 3 -4 L 1 -5 L -2 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6 M 5 -2 L 1 -4 L -2 -4 L -4 -3 L -5 -2 L -6 1 L -6 3 L -5 6 L -4 7 L -2 8 L 1 8 L 5 6","-10 10 M -6 -12 L -6 9 L -5 9 M -6 -12 L -5 -12 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9 M -5 -1 L -2 -3 L 0 -4 L 2 -4 L 4 -3 L 5 -1 L 5 9 L 6 9","-4 5 M 0 -12 L -1 -11 L -1 -10 L 0 -9 L 1 -9 L 2 -10 L 2 -11 L 1 -12 L 0 -12 M 0 -11 L 0 -10 L 1 -10 L 1 -11 L 0 -11 M 0 -5 L 0 9 L 1 9 M 0 -5 L 1 -5 L 1 9","-4 5 M 0 -12 L -1 -11 L -1 -10 L 0 -9 L 1 -9 L 2 -10 L 2 -11 L 1 -12 L 0 -12 M 0 -11 L 0 -10 L 1 -10 L 1 -11 L 0 -11 M 0 -5 L 0 16 L 1 16 M 0 -5 L 1 -5 L 1 16","-10 9 M -6 -12 L -6 9 L -5 9 M -6 -12 L -5 -12 L -5 9 M 6 -5 L 5 -5 L -5 5 M 6 -5 L -5 6 M -2 2 L 4 9 L 6 9 M -1 1 L 6 9","-4 5 M 0 -12 L 0 9 L 1 9 M 0 -12 L 1 -12 L 1 9","-15 16 M -11 -5 L -11 9 L -10 9 M -11 -5 L -10 -5 L -10 9 M -10 -1 L -7 -4 L -5 -5 L -2 -5 L 0 -4 L 1 -1 L 1 9 M -10 -1 L -7 -3 L -5 -4 L -3 -4 L -1 -3 L 0 -1 L 0 9 L 1 9 M 1 -1 L 4 -4 L 6 -5 L 9 -5 L 11 -4 L 12 -1 L 12 9 M 1 -1 L 4 -3 L 6 -4 L 8 -4 L 10 -3 L 11 -1 L 11 9 L 12 9","-10 10 M -6 -5 L -6 9 L -5 9 M -6 -5 L -5 -5 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9 M -5 -1 L -2 -3 L 0 -4 L 2 -4 L 4 -3 L 5 -1 L 5 9 L 6 9","-9 10 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 2 -5 L -1 -5 M -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 3 L -4 6 L -3 7 L -1 8 L 2 8 L 4 7 L 5 6 L 6 3 L 6 1 L 5 -2 L 4 -3 L 2 -4 L -1 -4","-10 10 M -6 -5 L -6 16 L -5 16 M -6 -5 L -5 -5 L -5 16 M -5 -2 L -3 -4 L -1 -5 L 2 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 2 9 L -1 9 L -3 8 L -5 6 M -5 -2 L -1 -4 L 2 -4 L 4 -3 L 5 -2 L 6 1 L 6 3 L 5 6 L 4 7 L 2 8 L -1 8 L -5 6","-10 10 M 5 -5 L 5 16 L 6 16 M 5 -5 L 6 -5 L 6 16 M 5 -2 L 3 -4 L 1 -5 L -2 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6 M 5 -2 L 1 -4 L -2 -4 L -4 -3 L -5 -2 L -6 1 L -6 3 L -5 6 L -4 7 L -2 8 L 1 8 L 5 6","-7 7 M -3 -5 L -3 9 L -2 9 M -3 -5 L -2 -5 L -2 9 M -2 1 L -1 -2 L 1 -4 L 3 -5 L 6 -5 M -2 1 L -1 -1 L 1 -3 L 3 -4 L 6 -4 L 6 -5","-8 9 M 6 -2 L 5 -4 L 2 -5 L -1 -5 L -4 -4 L -5 -2 L -4 0 L -2 1 L 3 3 L 5 4 M 4 3 L 5 5 L 5 6 L 4 8 M 5 7 L 2 8 L -1 8 L -4 7 M -3 8 L -4 6 L -5 6 M 6 -2 L 5 -2 L 4 -4 M 5 -3 L 2 -4 L -1 -4 L -4 -3 M -3 -4 L -4 -2 L -3 0 M -4 -1 L -2 0 L 3 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -1 9 L -4 8 L -5 6","-5 6 M 0 -12 L 0 9 L 1 9 M 0 -12 L 1 -12 L 1 9 M -3 -5 L 4 -5 L 4 -4 M -3 -5 L -3 -4 L 4 -4","-10 10 M -6 -5 L -6 5 L -5 8 L -3 9 L 0 9 L 2 8 L 5 5 M -6 -5 L -5 -5 L -5 5 L -4 7 L -2 8 L 0 8 L 2 7 L 5 5 M 5 -5 L 5 9 L 6 9 M 5 -5 L 6 -5 L 6 9","-8 8 M -6 -5 L 0 9 M -6 -5 L -5 -5 L 0 7 M 6 -5 L 5 -5 L 0 7 M 6 -5 L 0 9","-12 12 M -9 -5 L -4 9 M -9 -5 L -8 -5 L -4 6 M 0 -5 L -4 6 M 0 -2 L -4 9 M 0 -2 L 4 9 M 0 -5 L 4 6 M 9 -5 L 8 -5 L 4 6 M 9 -5 L 4 9","-9 9 M -6 -5 L 5 9 L 6 9 M -6 -5 L -5 -5 L 6 9 M 6 -5 L 5 -5 L -6 9 M 6 -5 L -5 9 L -6 9","-8 8 M -6 -5 L 0 9 M -6 -5 L -5 -5 L 0 7 M 6 -5 L 5 -5 L 0 7 L -4 16 M 6 -5 L 0 9 L -3 16 L -4 16","-9 9 M 4 -4 L -6 9 M 6 -5 L -4 8 M -6 -5 L 6 -5 M -6 -5 L -6 -4 L 4 -4 M -4 8 L 6 8 L 6 9 M -6 9 L 6 9","-7 7 M 3 -16 L -4 0 L 3 16","-4 4 M 0 -16 L 0 16","-7 7 M -3 -16 L 4 0 L -3 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-6 6 M 0 -12 L -1 -11 L -3 -10 L -1 -9 L 0 2 M 0 -9 L 1 -10 L 0 -11 L -1 -10 L 0 -9 L 0 2 M 0 -12 L 1 -11 L 3 -10 L 1 -9 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 2 -12 L 4 -11 L 5 -9 L 5 -7 L 7 -8 L 6 -10 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M 6 -8 L 5 -10 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 7 L -6 5 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -5 4 L -5 6 L -4 8 L -2 9","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 7 -4 L 8 -3 L 9 -3 L 10 -4 M 6 -3 L 7 -2 L 9 -2 M 6 -2 L 7 -1 L 8 -1 L 9 -2 L 10 -4 M 7 -4 L 1 2 M 0 3 L -6 9 L -10 4 L -4 -2 M -3 -3 L 1 -7 L -3 -12 L -8 -6 L -2 0 L 2 6 L 4 8 L 6 9 L 8 9 L 9 8 L 10 6 M -6 8 L -9 4 M 0 -7 L -3 -11 M -7 -6 L -2 -1 L 2 5 L 4 7 L 6 8 L 9 8 M -5 8 L -9 3 M 0 -6 L -4 -11 M -7 -7 L -1 -1 L 3 5 L 4 6 L 6 7 L 9 7 L 10 6","-4 5 M 1 -12 L 0 -11 L 0 -5 M 1 -11 L 0 -5 M 1 -12 L 2 -11 L 0 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 M -1 -10 L -2 -7 L -3 -3 L -3 3 L -2 7 L -1 10 M 1 -14 L 0 -12 L -1 -9 L -2 -3 L -2 3 L -1 9 L 0 12 L 1 14","-7 7 M -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16 M 1 -10 L 2 -7 L 3 -3 L 3 3 L 2 7 L 1 10 M -1 -14 L 0 -12 L 1 -9 L 2 -3 L 2 3 L 1 9 L 0 12 L -1 14","-8 8 M 0 -12 L -1 -11 L 1 -1 L 0 0 M 0 -12 L 0 0 M 0 -12 L 1 -11 L -1 -1 L 0 0 M -5 -9 L -4 -9 L 4 -3 L 5 -3 M -5 -9 L 5 -3 M -5 -9 L -5 -8 L 5 -4 L 5 -3 M 5 -9 L 4 -9 L -4 -3 L -5 -3 M 5 -9 L -5 -3 M 5 -9 L 5 -8 L -5 -4 L -5 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-6 6 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-13 13 M -9 0 L 9 0","-6 6 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -3 -10 L -2 -9 L -1 -7 L -1 6 L -3 7 M -1 -9 L -2 -10 L -1 -11 L 0 -9 L 0 7 L 2 8 M -3 -10 L 0 -12 L 1 -10 L 1 6 L 3 7 L 4 7 M -3 7 L -2 7 L 0 8 L 1 9 L 2 8 L 4 7","-10 10 M -6 -10 L -4 -10 L -2 -11 L -1 -12 L 1 -11 L 4 -10 L 6 -10 M -2 -10 L 0 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -10 L 1 -11 M 4 -10 L 4 -2 M 5 -9 L 5 -3 M 6 -10 L 6 -2 L -1 -2 L -4 -1 L -6 1 L -7 4 L -7 9 M -7 9 L -3 7 L 1 6 L 4 6 L 8 7 M -4 8 L -1 7 L 4 7 L 7 8 M -7 9 L -2 8 L 3 8 L 6 9 L 8 7","-10 10 M -6 -10 L -5 -10 L -3 -11 L -2 -12 L 0 -11 L 4 -10 L 6 -10 M -3 -10 L -1 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -11 M 4 -10 L 4 -3 M 5 -9 L 5 -4 M 6 -10 L 6 -3 L 4 -3 L 1 -2 L -1 -1 M -1 -2 L 1 -1 L 4 0 L 6 0 L 6 7 M 5 1 L 5 6 M 4 0 L 4 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M 3 -12 L -7 -2 L -7 3 L 2 3 M 4 3 L 8 3 L 9 4 L 9 2 L 8 3 M -6 -2 L -6 2 M -5 -4 L -5 3 M 2 -11 L 2 6 L 0 7 M 3 -8 L 4 -10 L 3 -11 L 3 7 L 5 8 M 3 -12 L 5 -10 L 4 -8 L 4 6 L 6 7 L 7 7 M 0 7 L 1 7 L 3 8 L 4 9 L 5 8 L 7 7","-10 10 M -6 -12 L -6 -3 M -6 -12 L 6 -12 M -5 -11 L 4 -11 M -6 -10 L 3 -10 L 5 -11 L 6 -12 M 4 -6 L 3 -5 L 1 -4 L -3 -3 L -6 -3 M 1 -4 L 2 -4 L 4 -3 L 4 7 M 3 -5 L 5 -4 L 5 6 M 4 -6 L 5 -5 L 7 -4 L 8 -4 L 6 -3 L 6 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 0 -11 L 2 -12 L 3 -11 L 5 -10 L 6 -10 M 1 -11 L 3 -10 M 0 -11 L 2 -9 L 4 -9 L 6 -10 M -4 -2 L -3 -2 L 1 -3 L 3 -4 L 4 -5 M 1 -3 L 2 -3 L 4 -2 L 4 7 M 3 -4 L 5 -2 L 5 6 M 4 -5 L 5 -4 L 7 -3 L 8 -3 L 6 -2 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -7 -10 L -5 -12 L -2 -11 L 3 -11 L 8 -12 M -6 -11 L -3 -10 L 2 -10 L 5 -11 M -7 -10 L -3 -9 L 0 -9 L 4 -10 L 8 -12 M 8 -12 L 7 -10 L 5 -7 L 1 -3 L -1 0 L -2 3 L -2 6 L -1 9 M 0 -1 L -1 2 L -1 5 L 0 8 M 3 -5 L 1 -2 L 0 1 L 0 4 L 1 7 L -1 9","-10 10 M -6 -9 L -6 -3 M -5 -8 L -5 -4 M -4 -9 L -4 -3 M -6 -9 L -4 -9 L 1 -10 L 3 -11 L 4 -12 M 1 -10 L 2 -10 L 4 -9 L 4 -3 M 3 -11 L 5 -10 L 5 -4 M 4 -12 L 5 -11 L 7 -10 L 8 -10 L 6 -9 L 6 -3 M -6 -3 L -4 -3 L 4 0 L 6 0 M 6 -3 L 4 -3 L -4 0 L -6 0 M -6 0 L -6 6 L -8 7 M -5 1 L -5 6 L -2 8 M -4 0 L -4 6 L -2 7 L -1 8 M 4 0 L 4 7 M 5 1 L 5 6 M 6 0 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 -1 L -8 0 M -5 -9 L -5 0 L -3 1 M -4 -10 L -4 -1 L -2 0 L -1 0 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 0 L -7 0 L -5 1 L -4 2 L -3 1 L -1 0 L 3 -1 L 4 -1 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -8 L -5 -10 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -2 L 1 -1 M -5 -8 L -4 -10 M 4 -10 L 5 -9 L 5 -5 L 4 -4 M -6 -8 L -4 -7 L -4 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 4 -9 L 4 -5 L 3 -3 L 1 -1 M 0 -1 L 0 2 L 1 -1 L -1 -1 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-11 11 M -6 -9 L -4 -11 L -2 -12 L 0 -12 L 1 -11 L 8 5 L 9 6 L 11 6 M -1 -11 L 0 -10 L 7 6 L 8 8 L 9 7 L 7 6 M -4 -11 L -2 -11 L -1 -10 L 6 6 L 7 8 L 8 9 L 9 9 L 11 6 M -6 -5 L -5 -6 L -3 -7 L -2 -7 L -1 -6 M -2 -6 L -2 -5 M -5 -6 L -3 -6 L -2 -4 M -11 9 L -9 7 L -7 6 L -4 6 L -2 7 M -8 7 L -4 7 L -3 8 M -11 9 L -8 8 L -5 8 L -4 9 L -2 7 M 0 -8 L -6 6 M -4 1 L 4 1","-12 12 M -10 -10 L -8 -12 L -5 -12 L -3 -11 L -1 -12 M -7 -11 L -4 -11 M -10 -10 L -8 -11 L -6 -10 L -3 -10 L -1 -12 M -5 -7 L -6 -6 L -7 -4 L -7 -3 L -9 -3 L -10 -2 L -10 0 L -9 -1 L -7 -1 L -7 5 M -6 -5 L -6 3 M -9 -2 L -6 -2 M -5 -7 L -5 2 L -6 4 L -7 5 M 0 -9 L -1 -8 L -2 -6 L -2 3 M -1 -7 L -1 1 M 0 -9 L 0 0 L -1 2 L -2 3 M 0 -9 L 6 -12 L 8 -11 L 9 -9 L 9 -7 L 7 -5 L 3 -3 M 6 -11 L 8 -9 L 8 -7 M 4 -11 L 6 -10 L 7 -9 L 7 -6 L 5 -4 M 5 -4 L 8 -2 L 9 0 L 9 6 M 7 -2 L 8 0 L 8 5 M 5 -4 L 6 -3 L 7 -1 L 7 6 M -8 9 L -5 7 L -2 6 L 2 6 L 5 7 M -6 8 L -3 7 L 2 7 L 4 8 M -8 9 L -4 8 L 1 8 L 3 9 L 5 7 L 7 6 L 9 6 M 3 -3 L 3 6 M 3 0 L 7 0 M 3 3 L 7 3","-13 11 M -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 1 L -9 4 L -8 6 L -5 8 L -2 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 M -8 -7 L -9 -4 L -9 1 L -7 5 L -4 7 L -1 8 L 2 8 L 5 7 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 0 L -7 3 L -4 6 L -1 7 L 2 7 L 5 6 L 7 5 L 9 3 M -2 -8 L -2 4 M -1 -8 L -1 2 M 0 -9 L 0 1 L -1 3 L -2 4 M -2 -8 L 0 -9 L 3 -12 L 5 -11 L 7 -11 L 8 -12 M 2 -11 L 4 -10 L 6 -10 M 1 -10 L 3 -9 L 5 -9 L 7 -10 L 8 -12 M 5 -9 L 5 6","-11 12 M -9 -12 L 5 -12 L 7 -11 L 8 -9 L 8 6 M -7 -11 L 5 -11 L 7 -9 L 7 5 M -9 -12 L -8 -11 L -6 -10 L 5 -10 L 6 -9 L 6 6 M -3 -7 L -4 -6 L -5 -4 L -5 -3 L -7 -3 L -8 -2 L -8 0 L -7 -1 L -5 -1 L -5 4 M -4 -5 L -4 2 M -7 -2 L -4 -2 M -3 -7 L -3 1 L -4 3 L -5 4 M -9 9 L -6 7 L -3 6 L 1 6 L 4 7 M -7 8 L -4 7 L 1 7 L 3 8 M -9 9 L -5 8 L 0 8 L 2 9 L 4 7 L 6 6 L 8 6 M 0 -10 L 0 6 M 0 -5 L 2 -4 L 4 -4 L 6 -5 M 0 1 L 2 0 L 4 0 L 6 1","-11 11 M -9 -10 L -7 -12 L -5 -12 L -3 -11 L -1 -12 M -6 -11 L -4 -11 M -9 -10 L -7 -11 L -5 -10 L -3 -10 L -1 -12 M -4 -7 L -5 -6 L -6 -4 L -6 -3 L -8 -3 L -9 -2 L -9 0 L -8 -1 L -6 -1 L -6 5 M -5 -5 L -5 3 M -8 -2 L -5 -2 M -4 -7 L -4 2 L -5 4 L -6 5 M -1 -5 L 0 -8 L 1 -10 L 2 -11 L 4 -12 L 6 -12 L 9 -11 M 2 -10 L 4 -11 L 6 -11 L 8 -10 M 0 -8 L 1 -9 L 3 -10 L 5 -10 L 7 -9 L 9 -11 M -1 3 L 0 0 L 1 -2 L 2 -3 L 4 -3 L 6 -2 M 2 -2 L 4 -2 L 5 -1 M 0 0 L 1 -1 L 3 -1 L 4 0 L 6 -2 M -7 9 L -4 7 L 0 6 L 5 6 L 9 7 M -5 8 L -2 7 L 5 7 L 8 8 M -7 9 L -3 8 L 4 8 L 7 9 L 9 7 M -1 -5 L -1 6","-12 11 M -8 -10 L -6 -12 L -3 -12 L -1 -11 L 1 -12 M -5 -11 L -2 -11 M -8 -10 L -6 -11 L -4 -10 L -1 -10 L 1 -12 M -2 -7 L -3 -6 L -4 -4 L -4 -3 L -6 -3 L -7 -2 L -7 0 L -6 -1 L -4 -1 L -4 4 M -3 -5 L -3 2 M -6 -2 L -3 -2 M -2 -7 L -2 1 L -3 3 L -4 4 M 1 -8 L 1 7 L 0 8 L -1 8 L -5 6 L -7 6 L -9 7 L -11 9 M 2 -8 L 2 6 M 2 -2 L 6 -2 M -2 8 L -3 8 L -5 7 L -8 7 M 3 -9 L 3 -3 L 6 -3 M 6 -1 L 3 -1 L 3 5 L 2 7 L -2 9 L -4 9 L -6 8 L -8 8 L -11 9 M 1 -8 L 3 -9 L 6 -12 L 8 -11 L 10 -11 L 11 -12 M 5 -11 L 7 -10 L 9 -10 M 4 -10 L 6 -9 L 8 -9 L 10 -10 L 11 -12 M 6 -9 L 6 5","-13 12 M -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 3 9 L 6 8 L 8 6 L 9 4 L 9 1 L 8 -1 L 7 -2 L 5 -3 L 3 -3 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 7 6 L 8 5 L 8 1 L 7 -1 M 3 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 3 -3 M -2 -8 L -2 5 M -1 -8 L -1 3 M 0 -9 L 0 2 L -1 4 L -2 5 M -2 -8 L 0 -9 L 3 -12 L 5 -11 L 7 -11 L 8 -12 M 2 -11 L 4 -10 L 6 -10 M 1 -10 L 3 -9 L 5 -9 L 7 -10 L 8 -12 M 7 -10 L 3 -3 L 3 9 M 3 1 L 7 1 M 3 4 L 7 4","-12 12 M -10 -10 L -8 -12 L -5 -12 L -3 -11 L -1 -12 M -7 -11 L -4 -11 M -10 -10 L -8 -11 L -6 -10 L -3 -10 L -1 -12 M -5 -7 L -6 -6 L -7 -4 L -7 -3 L -9 -3 L -10 -2 L -10 0 L -9 -1 L -7 -1 L -7 5 M -6 -5 L -6 3 M -9 -2 L -6 -2 M -5 -7 L -5 2 L -6 4 L -7 5 M -8 9 L -5 7 L -2 6 L 1 6 L 3 7 M -6 8 L -3 7 L 0 7 L 2 8 M -8 9 L -4 8 L -1 8 L 1 9 L 3 7 M 0 -9 L -1 -8 L -2 -6 L -2 3 M -1 -7 L -1 1 M 0 -9 L 0 0 L -1 2 L -2 3 M 0 -9 L 2 -11 L 4 -12 L 6 -12 L 8 -11 M 5 -11 L 6 -11 L 7 -10 M 2 -11 L 4 -11 L 6 -9 L 8 -11 M 3 -3 L 5 -4 L 7 -6 L 8 -5 L 9 -2 L 9 2 L 8 6 L 6 9 M 6 -5 L 7 -4 L 8 -2 L 8 3 L 7 6 M 5 -4 L 6 -4 L 7 -2 L 7 3 L 6 9 M 3 -3 L 3 7 M 3 0 L 7 0 M 3 3 L 7 3","-9 10 M -6 -10 L -4 -12 L -1 -12 L 2 -11 L 4 -12 M -3 -11 L 1 -11 M -6 -10 L -4 -11 L -1 -10 L 2 -10 L 4 -12 M 1 -7 L 0 -6 L -1 -4 L -1 -3 L -3 -3 L -4 -2 L -4 0 L -3 -1 L -1 -1 L -1 4 M 0 -5 L 0 2 M -3 -2 L 0 -2 M 1 -7 L 1 1 L 0 3 L -1 4 M 7 -10 L 5 -8 L 4 -5 L 4 6 L 3 8 L 1 8 L -3 6 L -5 6 L -7 7 L -9 9 M 5 -7 L 5 5 M 0 8 L -1 8 L -3 7 L -6 7 M 7 -10 L 6 -8 L 6 4 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -7 8 L -9 9","-10 10 M -6 -10 L -4 -12 L -1 -12 L 2 -11 L 4 -12 M -3 -11 L 1 -11 M -6 -10 L -4 -11 L -1 -10 L 2 -10 L 4 -12 M 1 -7 L 0 -6 L -1 -4 L -1 -3 L -3 -3 L -4 -2 L -4 0 L -3 -1 L -1 -1 L -1 4 M 0 -5 L 0 2 M -3 -2 L 0 -2 M 1 -7 L 1 1 L 0 3 L -1 4 M 7 -10 L 5 -8 L 4 -5 L 4 6 L 3 8 M 5 -7 L 5 5 M 7 -10 L 6 -8 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -6 8 L -8 6 L -8 4 L -7 3 L -6 3 L -5 4 L -6 5 L -7 5 M -8 4 L -5 4","-12 12 M -10 -10 L -8 -12 L -5 -12 L -3 -11 L -1 -12 M -7 -11 L -4 -11 M -10 -10 L -8 -11 L -6 -10 L -3 -10 L -1 -12 M -5 -7 L -6 -6 L -7 -4 L -7 -3 L -9 -3 L -10 -2 L -10 0 L -9 -1 L -7 -1 L -7 5 M -6 -5 L -6 3 M -9 -2 L -6 -2 M -5 -7 L -5 2 L -6 4 L -7 5 M -8 9 L -5 7 L -2 6 L 1 6 L 3 7 M -6 8 L -4 7 L 0 7 L 2 8 M -8 9 L -4 8 L -1 8 L 1 9 L 3 7 M 0 -9 L -1 -8 L -2 -6 L -2 3 M -1 -7 L -1 1 M 0 -9 L 0 0 L -1 2 L -2 3 M 0 -9 L 2 -11 L 4 -12 L 6 -12 L 8 -11 M 5 -11 L 6 -11 L 7 -10 M 2 -11 L 4 -11 L 6 -9 L 8 -11 M 3 -3 L 6 -6 L 7 -5 L 9 -4 M 5 -5 L 7 -4 L 9 -4 M 9 -4 L 7 -1 L 5 1 L 3 3 M 5 1 L 7 2 L 8 6 L 9 8 L 10 8 M 7 4 L 8 8 M 5 1 L 6 2 L 7 8 L 8 9 L 9 9 L 10 8 M 3 -3 L 3 7","-11 11 M -9 -10 L -7 -12 L -4 -12 L -2 -11 L 0 -12 M -6 -11 L -3 -11 M -9 -10 L -7 -11 L -5 -10 L -2 -10 L 0 -12 M -4 -7 L -5 -6 L -6 -4 L -6 -3 L -8 -3 L -9 -2 L -9 0 L -8 -1 L -6 -1 L -6 5 M -5 -5 L -5 3 M -8 -2 L -5 -2 M -4 -7 L -4 2 L -5 4 L -6 5 M -7 9 L -4 7 L 0 6 L 5 6 L 9 7 M -5 8 L -2 7 L 5 7 L 8 8 M -7 9 L -3 8 L 4 8 L 7 9 L 9 7 M 1 -9 L 0 -8 L -1 -6 L -1 3 M 0 -7 L 0 1 M 1 -9 L 1 0 L 0 2 L -1 3 M 1 -9 L 3 -11 L 5 -12 L 7 -12 L 9 -11 M 6 -11 L 7 -11 L 8 -10 M 3 -11 L 5 -11 L 7 -9 L 9 -11 M 5 -11 L 5 6","-14 14 M -6 -8 L -7 -7 L -8 -5 L -8 -3 L -10 -3 L -11 -2 L -11 0 L -10 -1 L -8 -1 L -8 3 M -7 -6 L -7 1 M -10 -2 L -7 -2 M -6 -8 L -6 0 L -7 2 L -8 3 M -13 9 L -11 7 L -9 6 L -7 6 L -5 7 L -4 7 L -3 6 M -10 7 L -7 7 L -5 8 M -13 9 L -11 8 L -8 8 L -6 9 L -5 9 L -4 8 L -3 6 M -6 -8 L -2 -12 L 2 -8 L 2 5 L 3 7 L 4 7 M -2 -11 L 1 -8 L 1 6 L 0 7 L 1 8 L 2 7 L 1 6 M -2 -2 L 1 -2 M -4 -10 L -3 -10 L 0 -7 L 0 -3 L -3 -3 M -3 -1 L 0 -1 L 0 6 L -1 7 L 1 9 L 4 7 L 5 6 M 2 -8 L 6 -12 L 10 -8 L 10 5 L 11 7 L 12 7 M 6 -11 L 9 -8 L 9 6 L 11 8 M 6 -2 L 9 -2 M 4 -10 L 5 -10 L 8 -7 L 8 -3 L 5 -3 M 5 -1 L 8 -1 L 8 7 L 10 9 L 12 7 M -3 -10 L -3 6 M 5 -10 L 5 6","-13 12 M -11 -9 L -9 -11 L -7 -12 L -5 -12 L -3 -11 L -1 -8 L 4 3 L 6 6 L 7 7 M -5 -11 L -3 -9 L -2 -7 L 4 5 L 7 8 M -9 -11 L -7 -11 L -5 -10 L -3 -7 L 2 4 L 4 7 L 5 8 L 7 9 M 4 -10 L 6 -9 L 8 -9 L 10 -10 L 11 -12 M 5 -11 L 7 -10 L 9 -10 M 4 -10 L 6 -12 L 8 -11 L 10 -11 L 11 -12 M -7 -3 L -9 -3 L -10 -2 L -10 0 L -9 -1 L -7 -1 M -9 -2 L -7 -2 M -11 9 L -9 7 L -7 6 L -4 6 L -2 7 M -8 7 L -5 7 L -3 8 M -11 9 L -8 8 L -5 8 L -4 9 L -2 7 M -7 -11 L -7 6 M 7 -9 L 7 9 M 0 -6 L 1 -5 L 3 -4 L 5 -4 L 7 -5 M -7 2 L -5 1 L -1 1 L 1 2","-13 13 M -4 -12 L -6 -11 L -8 -9 L -9 -7 L -10 -4 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -4 L 9 -7 L 8 -9 L 6 -11 L 4 -12 L 3 -11 L 0 -9 L -3 -8 M -8 -8 L -9 -5 L -9 1 L -8 4 M -4 -12 L -6 -10 L -7 -8 L -8 -5 L -8 1 L -7 4 L -6 6 L -4 8 M 8 4 L 9 1 L 9 -5 L 7 -9 L 6 -10 M 4 8 L 6 6 L 7 4 L 8 1 L 8 -5 L 7 -7 L 5 -10 L 3 -11 M -3 -8 L -3 5 M -2 -8 L -2 3 M -1 -8 L -1 2 L -2 4 L -3 5 M 3 -11 L 3 8 M 3 -5 L 5 -4 L 6 -4 L 8 -5 M 3 1 L 5 0 L 6 0 L 8 1","-10 12 M -7 -12 L -6 -11 L -5 -9 L -5 -3 L -7 -3 L -8 -2 L -8 0 L -7 -1 L -5 -1 L -5 7 L -8 9 L -5 8 L -5 16 L -3 14 M -5 -10 L -4 -8 L -4 14 M -7 -2 L -4 -2 M -7 -12 L -5 -11 L -4 -10 L -3 -8 L -3 14 M -3 -7 L 0 -9 L 4 -12 L 8 -8 L 8 6 M 4 -11 L 7 -8 L 7 6 M 2 -10 L 3 -10 L 6 -7 L 6 7 M 0 6 L 3 6 L 6 7 M 1 7 L 3 7 L 5 8 M 0 8 L 2 8 L 4 9 L 6 7 L 8 6 M 0 -9 L 0 13 M 0 -5 L 2 -4 L 4 -4 L 6 -5 M 0 1 L 2 0 L 4 0 L 6 1","-13 13 M -4 -12 L -6 -11 L -8 -9 L -9 -7 L -10 -4 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -2 9 L 2 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -4 L 9 -7 L 8 -9 L 6 -11 L 4 -12 L 3 -11 L 0 -9 L -3 -8 M -8 -8 L -9 -5 L -9 1 L -8 4 M -4 -12 L -6 -10 L -7 -8 L -8 -5 L -8 1 L -7 4 L -6 6 L -4 8 M 8 4 L 9 1 L 9 -5 L 7 -9 L 6 -10 M 4 8 L 6 6 L 7 4 L 8 1 L 8 -5 L 7 -7 L 5 -10 L 3 -11 M -3 -8 L -3 5 M -2 -8 L -2 3 M -1 -8 L -1 2 L -2 4 L -3 5 M 3 -11 L 3 8 M 3 -5 L 5 -4 L 6 -4 L 8 -5 M 3 1 L 5 0 L 6 0 L 8 1 M -2 9 L -1 8 L 0 8 L 2 9 L 6 14 L 8 15 L 9 15 M 2 10 L 4 13 L 6 15 L 7 15 M 0 8 L 1 9 L 4 15 L 6 16 L 8 16 L 9 15","-12 12 M -10 -10 L -8 -12 L -5 -12 L -3 -11 L -1 -12 M -7 -11 L -4 -11 M -10 -10 L -8 -11 L -6 -10 L -3 -10 L -1 -12 M -5 -7 L -6 -6 L -7 -4 L -7 -3 L -9 -3 L -10 -2 L -10 0 L -9 -1 L -7 -1 L -7 5 M -6 -5 L -6 3 M -9 -2 L -6 -2 M -5 -7 L -5 2 L -6 4 L -7 5 M -8 9 L -5 7 L -2 6 L 0 6 L 3 7 M -6 8 L -4 7 L 0 7 L 2 8 M -8 9 L -4 8 L -1 8 L 1 9 L 3 7 M 0 -9 L -1 -8 L -2 -6 L -2 3 M -1 -7 L -1 1 M 0 -9 L 0 0 L -1 2 L -2 3 M 0 -9 L 3 -11 L 5 -12 L 7 -11 L 8 -9 L 8 -6 L 7 -4 L 6 -3 L 2 -1 L 0 0 M 5 -11 L 6 -11 L 7 -9 L 7 -5 L 6 -4 M 3 -11 L 5 -10 L 6 -8 L 6 -5 L 5 -3 L 2 -1 M 2 -1 L 4 0 L 5 1 L 8 6 L 9 7 L 10 7 M 5 2 L 7 6 L 9 8 M 2 -1 L 4 1 L 6 7 L 8 9 L 10 7","-11 12 M 3 -9 L 2 -10 L 0 -11 L -3 -12 M 4 -10 L 2 -11 M 5 -11 L 1 -12 L -3 -12 L -6 -11 L -7 -10 L -8 -8 L -7 -6 L -6 -5 L -3 -4 L 5 -4 L 7 -3 L 8 -2 L 8 0 L 7 3 M -7 -7 L -6 -6 L -3 -5 L 6 -5 L 8 -4 L 9 -3 L 9 -1 L 8 1 M -7 -10 L -7 -8 L -6 -7 L -3 -6 L 7 -6 L 9 -5 L 10 -3 L 10 -1 L 7 3 L 3 9 M -9 -3 L -8 -2 L -6 -1 L 3 -1 L 4 0 L 4 1 L 3 3 M -8 -1 L -6 0 L 2 0 L 3 1 M -9 -3 L -9 -2 L -8 0 L -6 1 L 1 1 L 3 2 L 3 3 M -9 9 L -6 7 L -2 6 L 1 6 L 4 7 M -7 8 L -4 7 L 0 7 L 3 8 M -9 9 L -5 8 L 0 8 L 3 9 M 5 -11 L 3 -9 L 1 -6 M 0 -4 L -2 -1 M -3 1 L -5 3 L -7 4 L -8 4 L -8 3 L -7 4","-13 11 M -8 -8 L -9 -6 L -10 -3 L -10 1 L -9 4 L -7 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 M -9 1 L -8 4 L -6 6 L -4 7 L -1 8 L 2 8 L 5 7 M -8 -8 L -9 -5 L -9 -1 L -8 2 L -6 5 L -4 6 L -1 7 L 2 7 L 5 6 L 7 5 L 9 3 M -10 -9 L -9 -11 L -7 -12 L -3 -12 L 3 -11 L 7 -11 L 9 -12 M -2 -11 L 2 -10 L 6 -10 M -10 -9 L -9 -10 L -7 -11 L -4 -11 L 2 -9 L 5 -9 L 7 -10 L 9 -12 M 1 -9 L 0 -8 L -2 -7 L -2 4 M -1 -7 L -1 2 M 0 -8 L 0 1 L -1 3 L -2 4 M 5 -9 L 5 6","-12 12 M -10 -10 L -8 -12 L -6 -12 L -3 -11 L -1 -12 M -7 -11 L -4 -11 M -10 -10 L -8 -11 L -5 -10 L -3 -10 L -1 -12 M -7 -8 L -8 -6 L -9 -3 L -9 1 L -8 4 L -7 6 L -5 8 L -2 9 L 1 9 L 4 8 L 6 7 L 8 9 L 10 7 M -8 1 L -7 4 L -4 7 L -1 8 L 2 8 M -7 -8 L -8 -4 L -8 -1 L -7 2 L -6 4 L -4 6 L -1 7 L 3 7 L 6 6 M 3 -9 L -1 -8 L -2 -6 L -2 4 M -1 -7 L -1 2 M 0 -8 L 0 1 L -1 3 L -2 4 M 3 -9 L 5 -10 L 7 -12 L 8 -11 L 10 -10 L 8 -9 L 8 5 L 9 7 L 10 7 M 7 -9 L 8 -10 L 7 -11 L 6 -10 L 7 -9 L 7 6 L 9 8 M 5 -10 L 6 -9 L 6 6 M 3 -9 L 3 7 M 3 -4 L 6 -4 M 3 0 L 6 0","-11 12 M -8 -12 L -7 -11 L -6 -9 L -6 -3 L -8 -3 L -9 -2 L -9 0 L -8 -1 L -6 -1 L -6 6 L -8 7 M -6 -10 L -5 -8 L -5 6 M -8 -2 L -5 -2 M -4 7 L -1 7 L 1 8 M -8 -12 L -6 -11 L -5 -10 L -4 -8 L -4 6 L 0 6 L 3 7 M -8 7 L -5 7 L -2 8 L 0 9 L 3 7 L 6 6 L 8 6 M 0 -8 L 3 -9 L 5 -10 L 7 -12 L 8 -11 L 10 -10 L 8 -9 L 8 6 M 7 -9 L 8 -10 L 7 -11 L 6 -10 L 7 -9 L 7 5 M 5 -10 L 6 -9 L 6 6 M 0 -8 L 0 6 M 0 -5 L 2 -4 L 4 -4 L 6 -5 M 0 1 L 2 0 L 4 0 L 6 1","-13 14 M -10 -12 L -9 -11 L -8 -9 L -8 -3 L -10 -3 L -11 -2 L -11 0 L -10 -1 L -8 -1 L -8 6 L -10 7 M -8 -10 L -7 -8 L -7 6 M -10 -2 L -7 -2 M -6 7 L -4 7 L -2 8 M -10 -12 L -8 -11 L -7 -10 L -6 -8 L -6 6 L -3 6 L -1 7 M -10 7 L -7 7 L -4 8 L -3 9 L -1 7 L 2 6 L 4 7 L 5 9 L 7 7 L 10 6 M -3 -10 L 0 -12 L 2 -10 L 2 6 L 5 6 L 7 7 M 0 -11 L 1 -10 L 1 6 M -3 -10 L -1 -10 L 0 -9 L 0 6 L -1 7 M 5 7 L 6 8 M 5 -10 L 8 -12 L 10 -10 L 10 6 M 8 -11 L 9 -10 L 9 6 M 5 -10 L 7 -10 L 8 -9 L 8 6 L 7 7 M -3 -10 L -3 6 M 5 -10 L 5 6 M -3 -4 L 0 -4 M -3 0 L 0 0 M 5 -4 L 8 -4 M 5 0 L 8 0","-11 11 M -10 -9 L -8 -11 L -6 -12 L -4 -12 L -3 -11 L 5 7 L 6 8 L 8 8 M -5 -11 L -4 -10 L 4 7 L 5 8 M -8 -11 L -6 -11 L -5 -10 L 3 8 L 4 9 L 6 9 L 8 8 L 10 6 M 5 -12 L 7 -11 L 9 -11 L 10 -12 M 5 -11 L 6 -10 L 8 -10 M 4 -10 L 5 -9 L 7 -9 L 9 -10 L 10 -12 M -10 9 L -9 7 L -7 6 L -5 6 L -4 7 M -8 7 L -6 7 L -5 8 M -10 9 L -9 8 L -7 8 L -5 9 M 5 -12 L 1 -3 M -1 0 L -5 9 M -6 -2 L -2 -2 M 1 -2 L 6 -2","-11 12 M -8 -12 L -7 -11 L -6 -9 L -6 -3 L -8 -3 L -9 -2 L -9 0 L -8 -1 L -6 -1 L -6 6 L -8 7 M -6 -10 L -5 -8 L -5 6 M -8 -2 L -5 -2 M -4 7 L -1 7 L 1 8 M -8 -12 L -6 -11 L -5 -10 L -4 -8 L -4 6 L 0 6 L 3 7 M -8 7 L -5 7 L -2 8 L 0 9 L 3 7 L 6 6 M 0 -8 L 3 -9 L 5 -10 L 7 -12 L 8 -11 L 10 -10 L 8 -9 L 8 12 L 7 14 L 5 16 L 3 15 L -1 14 L -6 14 M 7 -9 L 8 -10 L 7 -11 L 6 -10 L 7 -9 L 7 7 M 5 -10 L 6 -9 L 6 6 L 8 9 M 6 15 L 4 14 L 1 14 M 7 14 L 4 13 L -2 13 L -6 14 M 0 -8 L 0 6 M 0 -5 L 2 -4 L 4 -4 L 6 -5 M 0 1 L 2 0 L 4 0 L 6 1","-10 10 M 6 -11 L 5 -9 L 0 -3 L -3 1 L -5 5 L -8 9 M 4 -7 L -4 4 M 8 -12 L 5 -8 L 3 -4 L 0 0 L -5 6 L -6 8 M -8 -10 L -6 -12 L -3 -11 L 3 -11 L 8 -12 M -7 -11 L -3 -10 L 1 -10 L 5 -11 M -8 -10 L -4 -9 L 0 -9 L 4 -10 L 6 -11 M -6 8 L -4 7 L 0 6 L 4 6 L 8 7 M -5 8 L -1 7 L 3 7 L 7 8 M -8 9 L -3 8 L 3 8 L 6 9 L 8 7 M -5 -2 L -1 -2 M 2 -2 L 6 -2","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-11 11 M -11 16 L 11 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-8 9 M -2 0 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9 L 0 7 L 3 6 M -5 4 L -4 6 L -3 7 L -1 8 M -4 2 L -4 4 L -3 6 L -1 7 L 0 7 M -4 -2 L -2 -2 L 1 -3 L 3 -4 L 4 -5 L 6 -3 L 5 -2 L 5 6 L 6 7 L 7 7 M -3 -4 L -4 -3 L -1 -3 M 2 -3 L 5 -3 L 4 -4 L 4 7 L 5 8 M -5 -3 L -3 -5 L -2 -4 L 0 -3 L 3 -2 L 3 7 L 5 9 L 7 7 M -5 -3 L 0 2","-9 9 M -6 -10 L -5 -8 L -5 6 L -7 7 M -4 -8 L -5 -10 L -4 -11 L -4 6 L -1 8 M -6 -10 L -3 -12 L -3 6 L -1 7 L 0 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 3 7 L 5 7 M -3 -2 L 0 -3 L 2 -4 L 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 7 M 2 -4 L 4 -3 L 4 6 M 0 -3 L 1 -3 L 3 -2 L 3 7","-8 6 M -4 -3 L -4 6 L -6 7 L -5 7 L -3 8 L -2 9 M -3 -3 L -3 7 L -1 8 M -2 -3 L -2 6 L 0 7 L 1 7 L -1 8 L -2 9 M -4 -3 L 0 -4 L 2 -5 L 3 -4 L 5 -3 L 6 -3 M 1 -4 L 2 -3 L 4 -3 M -2 -3 L 0 -4 L 2 -2 L 4 -2 L 6 -3","-9 8 M 0 -5 L -2 -4 L -5 -3 L -5 6 L -7 7 M -4 -3 L -4 6 L -1 8 M 0 -5 L -3 -3 L -3 6 L -1 7 L 0 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 3 7 L 5 7 M -5 -10 L -2 -12 L -1 -9 L 5 -3 L 5 7 M -2 -9 L -4 -10 L -3 -11 L -2 -9 L 4 -3 L 4 6 M -5 -10 L 3 -2 L 3 7","-8 6 M -4 -3 L -4 6 L -6 7 L -5 7 L -3 8 L -2 9 M -3 -3 L -3 7 L -1 8 M -2 -3 L -2 6 L 0 7 L 1 7 L -1 8 L -2 9 M -4 -3 L 0 -4 L 2 -5 L 5 -1 L 3 0 L -2 3 M 1 -4 L 4 -1 M -2 -3 L 0 -4 L 3 0","-8 5 M -4 -10 L -4 6 L -6 7 L -5 7 L -3 8 L -2 9 M -3 -10 L -3 7 L -1 8 M -2 -10 L -2 6 L 0 7 L 1 7 L -1 8 L -2 9 M -4 -10 L -1 -11 L 1 -12 L 2 -11 L 4 -10 L 5 -10 M 0 -11 L 1 -10 L 3 -10 M -2 -10 L -1 -11 L 1 -9 L 3 -9 L 5 -10 M -7 -5 L -4 -5 M -2 -5 L 2 -5","-9 9 M -5 -3 L -5 6 L -7 7 L -6 7 L -4 8 L -3 9 L -2 8 L 0 7 L 3 6 M -4 -2 L -4 7 L -2 8 M -3 -3 L -3 6 L -1 7 L 0 7 M -5 -3 L -3 -3 L 0 -4 L 2 -5 L 3 -4 L 5 -3 L 7 -3 L 5 -2 L 5 10 L 4 13 L 2 15 L 0 16 L -1 15 L -3 14 L -5 14 M 1 -4 L 4 -2 L 4 10 M 1 15 L -1 14 L -2 14 M 0 -4 L 1 -3 L 3 -2 L 3 8 L 4 11 L 4 13 M 2 15 L 1 14 L -1 13 L -3 13 L -5 14","-9 9 M -6 -10 L -5 -8 L -5 6 L -7 7 L -6 7 L -4 8 L -3 9 M -4 -8 L -5 -10 L -4 -11 L -4 7 L -2 8 M -6 -10 L -3 -12 L -3 6 L -1 7 L -3 9 M -3 -2 L 0 -3 L 2 -4 L 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 7 L 3 9 L 2 11 M 2 -4 L 4 -3 L 4 7 L 3 9 M 0 -3 L 1 -3 L 3 -2 L 3 7 L 2 11 L 2 14 L 3 16 L 4 16 L 2 14","-5 5 M 0 -12 L -2 -10 L 0 -9 L 2 -10 L 0 -12 M 0 -11 L -1 -10 L 1 -10 L 0 -11 M 0 -5 L -1 -4 L -3 -3 L -1 -2 L -1 7 L 1 9 L 3 7 M 0 -2 L 1 -3 L 0 -4 L -1 -3 L 0 -2 L 0 7 L 1 8 M 0 -5 L 1 -4 L 3 -3 L 1 -2 L 1 6 L 2 7 L 3 7","-5 5 M 0 -12 L -2 -10 L 0 -9 L 2 -10 L 0 -12 M 0 -11 L -1 -10 L 1 -10 L 0 -11 M 0 -5 L -1 -4 L -3 -3 L -1 -2 L -1 7 L 1 9 L 2 11 M 0 -2 L 1 -3 L 0 -4 L -1 -3 L 0 -2 L 0 7 L 1 9 M 0 -5 L 1 -4 L 3 -3 L 1 -2 L 1 7 L 2 11 L 2 14 L 0 16 L -2 16 L -2 15 L 0 16","-9 8 M -6 -10 L -5 -8 L -5 6 L -7 7 L -6 7 L -4 8 L -3 9 M -4 -8 L -5 -10 L -4 -11 L -4 7 L -2 8 M -6 -10 L -3 -12 L -3 6 L -1 7 L -3 9 M -3 -2 L 0 -4 L 2 -5 L 4 -2 L 1 0 L -3 3 M 1 -4 L 3 -2 M 0 -4 L 2 -1 M 1 0 L 2 1 L 4 6 L 5 7 L 6 7 M 1 1 L 2 2 L 3 7 L 4 8 M 0 1 L 1 2 L 2 7 L 4 9 L 6 7","-5 5 M -2 -10 L -1 -8 L -1 6 L -3 7 L -2 7 L 0 8 L 1 9 M 0 -8 L -1 -10 L 0 -11 L 0 7 L 2 8 M -2 -10 L 1 -12 L 1 6 L 3 7 L 4 7 L 2 8 L 1 9","-13 13 M -11 -3 L -10 -3 L -9 -2 L -9 6 L -11 7 L -10 7 L -8 8 L -7 9 M -9 -4 L -8 -3 L -8 7 L -6 8 M -11 -3 L -9 -5 L -7 -3 L -7 6 L -5 7 L -7 9 M -7 -2 L -4 -3 L -2 -4 L -1 -5 L 1 -3 L 1 6 L 3 7 L 1 9 M -2 -4 L 0 -3 L 0 7 L 2 8 M -4 -3 L -3 -3 L -1 -2 L -1 6 L -2 7 L 0 8 L 1 9 M 1 -2 L 4 -3 L 6 -4 L 7 -5 L 8 -4 L 10 -3 L 11 -3 L 9 -2 L 9 6 L 10 7 L 11 7 M 6 -4 L 8 -3 L 8 7 L 9 8 M 4 -3 L 5 -3 L 7 -2 L 7 7 L 9 9 L 11 7","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -7 7 L -6 7 L -4 8 L -3 9 M -5 -4 L -4 -3 L -4 7 L -2 8 M -7 -3 L -5 -5 L -3 -3 L -3 6 L -1 7 L -3 9 M -3 -2 L 0 -3 L 2 -4 L 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 6 L 6 7 L 7 7 M 2 -4 L 4 -3 L 4 7 L 5 8 M 0 -3 L 1 -3 L 3 -2 L 3 7 L 5 9 L 7 7","-9 9 M -5 -3 L -5 6 L -7 7 M -4 -2 L -4 6 L -1 8 M -3 -3 L -3 6 L -1 7 L 0 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 3 7 L 5 7 M -5 -3 L -3 -3 L 0 -4 L 2 -5 L 3 -4 L 5 -3 L 7 -3 L 5 -2 L 5 7 M 1 -4 L 4 -2 L 4 6 M 0 -4 L 1 -3 L 3 -2 L 3 7","-9 9 M -6 -5 L -5 -3 L -5 6 L -7 7 L -5 7 L -5 16 M -5 -4 L -4 -3 L -4 15 L -3 14 L -4 12 M -4 7 L -3 7 L -1 8 M -6 -5 L -4 -4 L -3 -3 L -3 6 L -1 7 L 0 8 M -3 8 L -2 9 L 0 8 L 3 7 L 5 7 M -3 8 L -3 12 L -2 14 L -5 16 M -3 -2 L 0 -3 L 2 -4 L 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 7 M 2 -4 L 4 -3 L 4 6 M 0 -3 L 1 -3 L 3 -2 L 3 7","-9 9 M -5 -3 L -5 6 L -7 7 M -4 -2 L -4 7 L -2 8 M -3 -3 L -3 6 L -1 7 L 0 7 M -7 7 L -6 7 L -4 8 L -3 9 L -2 8 L 0 7 L 3 6 M -5 -3 L -3 -3 L 0 -4 L 2 -5 L 3 -4 L 5 -3 L 7 -3 L 5 -2 L 5 16 M 1 -4 L 4 -2 L 4 15 L 3 14 L 4 12 M 0 -4 L 1 -3 L 3 -2 L 3 12 L 2 14 L 5 16","-8 6 M -6 -3 L -5 -3 L -4 -2 L -4 6 L -6 7 L -5 7 L -3 8 L -2 9 M -5 -4 L -3 -3 L -3 7 L -1 8 M -6 -3 L -4 -5 L -2 -3 L -2 6 L 0 7 L 1 7 L -1 8 L -2 9 M -2 -3 L 2 -5 L 3 -4 L 5 -3 L 6 -3 M 1 -4 L 2 -3 L 4 -3 M 0 -4 L 2 -2 L 4 -2 L 6 -3","-8 8 M -5 -3 L -5 1 L -3 2 L 3 2 L 5 3 L 5 7 M -4 -3 L -4 1 M 4 3 L 4 7 M -2 -4 L -3 -3 L -3 1 L -1 2 M 1 2 L 3 3 L 3 7 L 2 8 M -5 -3 L -2 -4 L 0 -5 L 2 -4 L 4 -4 L 5 -5 M -1 -4 L 1 -4 M -2 -4 L 0 -3 L 2 -3 L 4 -4 M 5 7 L 2 8 L 0 9 L -2 8 L -4 8 L -6 9 M 1 8 L -1 8 M 2 8 L 0 7 L -3 7 L -6 9 M 5 -5 L 4 -3 L 2 0 L -3 5 L -6 9","-5 5 M -2 -10 L -1 -8 L -1 6 L -3 7 L -2 7 L 0 8 L 1 9 M 0 -8 L -1 -10 L 0 -11 L 0 7 L 2 8 M -2 -10 L 1 -12 L 1 6 L 3 7 L 4 7 L 2 8 L 1 9 M -4 -5 L -1 -5 M 1 -5 L 4 -5","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -7 7 M -6 -4 L -4 -3 L -4 7 L -2 8 M -7 -3 L -5 -5 L -3 -3 L -3 6 L -1 7 L 0 7 M -7 7 L -6 7 L -4 8 L -3 9 L -2 8 L 0 7 L 3 6 M 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 6 L 6 7 L 7 7 M 2 -4 L 4 -3 L 4 7 L 5 8 M 3 -5 L 1 -3 L 3 -2 L 3 7 L 5 9 L 7 7","-9 9 M -6 -5 L -5 -3 L -5 6 L -2 9 L 0 7 L 3 6 L 5 6 M -5 -4 L -4 -3 L -4 6 L -1 8 M -6 -5 L -4 -4 L -3 -3 L -3 5 L -2 6 L 0 7 M 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 6 M 2 -4 L 4 -3 L 4 5 M 3 -5 L 1 -3 L 3 -2 L 3 6","-13 13 M -10 -5 L -9 -3 L -9 6 L -6 9 L -4 7 L -1 6 M -9 -4 L -8 -3 L -8 6 L -5 8 M -10 -5 L -8 -4 L -7 -3 L -7 5 L -6 6 L -4 7 M -1 -5 L -3 -3 L -1 -2 L -1 6 L 2 9 L 4 7 L 7 6 L 9 6 M -2 -4 L 0 -3 L 0 6 L 3 8 M -1 -5 L 0 -4 L 2 -3 L 1 -2 L 1 5 L 2 6 L 4 7 M 7 -5 L 8 -4 L 10 -3 L 11 -3 L 9 -2 L 9 6 M 6 -4 L 8 -3 L 8 5 M 7 -5 L 5 -3 L 7 -2 L 7 6","-10 9 M -7 -3 L -6 -3 L -4 -2 L -3 -1 L 1 7 L 2 8 L 4 9 L 6 7 M -5 -4 L -3 -3 L 2 7 L 4 8 M -7 -3 L -5 -5 L -3 -4 L -2 -3 L 2 5 L 3 6 L 5 7 L 6 7 M 0 1 L 3 -5 L 4 -4 L 6 -4 L 7 -5 M 3 -4 L 4 -3 L 5 -3 M 2 -3 L 4 -2 L 6 -3 L 7 -5 M -1 3 L -4 9 L -5 8 L -7 8 L -8 9 M -4 8 L -5 7 L -6 7 M -3 7 L -5 6 L -7 7 L -8 9 M -5 2 L -2 2 M 1 2 L 4 2","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -7 7 M -6 -4 L -4 -3 L -4 7 L -2 8 M -7 -3 L -5 -5 L -3 -3 L -3 6 L -1 7 L 0 7 M -7 7 L -6 7 L -4 8 L -3 9 L -2 8 L 0 7 L 3 6 M 3 -5 L 4 -4 L 6 -3 L 7 -3 L 5 -2 L 5 10 L 4 13 L 2 15 L 0 16 L -1 15 L -3 14 L -5 14 M 2 -4 L 4 -3 L 4 10 M 1 15 L -1 14 L -2 14 M 3 -5 L 1 -3 L 3 -2 L 3 8 L 4 11 L 4 13 M 2 15 L 1 14 L -1 13 L -3 13 L -5 14","-9 9 M 6 -5 L -6 9 M -6 -3 L -4 -2 L -1 -2 L 2 -3 L 6 -5 M -5 -4 L -3 -3 L 1 -3 M -6 -3 L -4 -5 L -2 -4 L 2 -4 L 6 -5 M -6 9 L -2 7 L 1 6 L 4 6 L 6 7 M -1 7 L 3 7 L 5 8 M -6 9 L -2 8 L 2 8 L 4 9 L 6 7 M -4 2 L 4 2","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-6 6 M 0 -12 L -1 -11 L -3 -10 L -1 -9 L 0 2 M 0 -9 L 1 -10 L 0 -11 L -1 -10 L 0 -9 L 0 2 M 0 -12 L 1 -11 L 3 -10 L 1 -9 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 2 -12 L 4 -11 L 5 -9 L 5 -7 L 7 -8 L 6 -10 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M 6 -8 L 5 -10 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 7 L -6 5 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -5 4 L -5 6 L -4 8 L -2 9","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 7 -4 L 8 -3 L 9 -3 L 10 -4 M 6 -3 L 7 -2 L 9 -2 M 6 -2 L 7 -1 L 8 -1 L 9 -2 L 10 -4 M 7 -4 L 1 2 M 0 3 L -6 9 L -10 4 L -4 -2 M -3 -3 L 1 -7 L -3 -12 L -8 -6 L -2 0 L 2 6 L 4 8 L 6 9 L 8 9 L 9 8 L 10 6 M -6 8 L -9 4 M 0 -7 L -3 -11 M -7 -6 L -2 -1 L 2 5 L 4 7 L 6 8 L 9 8 M -5 8 L -9 3 M 0 -6 L -4 -11 M -7 -7 L -1 -1 L 3 5 L 4 6 L 6 7 L 9 7 L 10 6","-4 5 M 1 -12 L 0 -11 L 0 -5 M 1 -11 L 0 -5 M 1 -12 L 2 -11 L 0 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 M -1 -10 L -2 -7 L -3 -3 L -3 3 L -2 7 L -1 10 M 1 -14 L 0 -12 L -1 -9 L -2 -3 L -2 3 L -1 9 L 0 12 L 1 14","-7 7 M -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16 M 1 -10 L 2 -7 L 3 -3 L 3 3 L 2 7 L 1 10 M -1 -14 L 0 -12 L 1 -9 L 2 -3 L 2 3 L 1 9 L 0 12 L -1 14","-8 8 M 0 -12 L -1 -11 L 1 -1 L 0 0 M 0 -12 L 0 0 M 0 -12 L 1 -11 L -1 -1 L 0 0 M -5 -9 L -4 -9 L 4 -3 L 5 -3 M -5 -9 L 5 -3 M -5 -9 L -5 -8 L 5 -4 L 5 -3 M 5 -9 L 4 -9 L -4 -3 L -5 -3 M 5 -9 L -5 -3 M 5 -9 L 5 -8 L -5 -4 L -5 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-6 6 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-13 13 M -9 0 L 9 0","-6 6 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -3 -10 L -2 -9 L -1 -7 L -1 6 L -3 7 M -1 -9 L -2 -10 L -1 -11 L 0 -9 L 0 7 L 2 8 M -3 -10 L 0 -12 L 1 -10 L 1 6 L 3 7 L 4 7 M -3 7 L -2 7 L 0 8 L 1 9 L 2 8 L 4 7","-10 10 M -6 -10 L -4 -10 L -2 -11 L -1 -12 L 1 -11 L 4 -10 L 6 -10 M -2 -10 L 0 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -10 L 1 -11 M 4 -10 L 4 -2 M 5 -9 L 5 -3 M 6 -10 L 6 -2 L -1 -2 L -4 -1 L -6 1 L -7 4 L -7 9 M -7 9 L -3 7 L 1 6 L 4 6 L 8 7 M -4 8 L -1 7 L 4 7 L 7 8 M -7 9 L -2 8 L 3 8 L 6 9 L 8 7","-10 10 M -6 -10 L -5 -10 L -3 -11 L -2 -12 L 0 -11 L 4 -10 L 6 -10 M -3 -10 L -1 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -11 M 4 -10 L 4 -3 M 5 -9 L 5 -4 M 6 -10 L 6 -3 L 4 -3 L 1 -2 L -1 -1 M -1 -2 L 1 -1 L 4 0 L 6 0 L 6 7 M 5 1 L 5 6 M 4 0 L 4 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M 3 -12 L -7 -2 L -7 3 L 2 3 M 4 3 L 8 3 L 9 4 L 9 2 L 8 3 M -6 -2 L -6 2 M -5 -4 L -5 3 M 2 -11 L 2 6 L 0 7 M 3 -8 L 4 -10 L 3 -11 L 3 7 L 5 8 M 3 -12 L 5 -10 L 4 -8 L 4 6 L 6 7 L 7 7 M 0 7 L 1 7 L 3 8 L 4 9 L 5 8 L 7 7","-10 10 M -6 -12 L -6 -3 M -6 -12 L 6 -12 M -5 -11 L 4 -11 M -6 -10 L 3 -10 L 5 -11 L 6 -12 M 4 -6 L 3 -5 L 1 -4 L -3 -3 L -6 -3 M 1 -4 L 2 -4 L 4 -3 L 4 7 M 3 -5 L 5 -4 L 5 6 M 4 -6 L 5 -5 L 7 -4 L 8 -4 L 6 -3 L 6 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 0 -11 L 2 -12 L 3 -11 L 5 -10 L 6 -10 M 1 -11 L 3 -10 M 0 -11 L 2 -9 L 4 -9 L 6 -10 M -4 -2 L -3 -2 L 1 -3 L 3 -4 L 4 -5 M 1 -3 L 2 -3 L 4 -2 L 4 7 M 3 -4 L 5 -2 L 5 6 M 4 -5 L 5 -4 L 7 -3 L 8 -3 L 6 -2 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -7 -10 L -5 -12 L -2 -11 L 3 -11 L 8 -12 M -6 -11 L -3 -10 L 2 -10 L 5 -11 M -7 -10 L -3 -9 L 0 -9 L 4 -10 L 8 -12 M 8 -12 L 7 -10 L 5 -7 L 1 -3 L -1 0 L -2 3 L -2 6 L -1 9 M 0 -1 L -1 2 L -1 5 L 0 8 M 3 -5 L 1 -2 L 0 1 L 0 4 L 1 7 L -1 9","-10 10 M -6 -9 L -6 -3 M -5 -8 L -5 -4 M -4 -9 L -4 -3 M -6 -9 L -4 -9 L 1 -10 L 3 -11 L 4 -12 M 1 -10 L 2 -10 L 4 -9 L 4 -3 M 3 -11 L 5 -10 L 5 -4 M 4 -12 L 5 -11 L 7 -10 L 8 -10 L 6 -9 L 6 -3 M -6 -3 L -4 -3 L 4 0 L 6 0 M 6 -3 L 4 -3 L -4 0 L -6 0 M -6 0 L -6 6 L -8 7 M -5 1 L -5 6 L -2 8 M -4 0 L -4 6 L -2 7 L -1 8 M 4 0 L 4 7 M 5 1 L 5 6 M 6 0 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 -1 L -8 0 M -5 -9 L -5 0 L -3 1 M -4 -10 L -4 -1 L -2 0 L -1 0 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 0 L -7 0 L -5 1 L -4 2 L -3 1 L -1 0 L 3 -1 L 4 -1 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -8 L -5 -10 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -2 L 1 -1 M -5 -8 L -4 -10 M 4 -10 L 5 -9 L 5 -5 L 4 -4 M -6 -8 L -4 -7 L -4 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 4 -9 L 4 -5 L 3 -3 L 1 -1 M 0 -1 L 0 2 L 1 -1 L -1 -1 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-12 12 M -9 -10 L -8 -9 L -9 -8 L -10 -9 L -9 -11 L -7 -12 L -5 -12 L -3 -11 L -2 -10 L -1 -7 L -1 -3 L -2 0 L -4 2 L -6 3 L -9 4 M -3 -10 L -2 -7 L -2 -2 L -3 0 M -5 -12 L -4 -11 L -3 -8 L -3 -2 L -4 1 L -6 3 M -6 4 L -3 7 M -7 4 L -3 8 M -9 4 L -4 9 L 3 4 M 10 -11 L 9 -10 L 10 -10 L 10 -11 L 9 -12 L 7 -12 L 5 -11 L 4 -10 L 3 -8 L 3 7 L 5 9 L 9 5 M 5 -10 L 4 -8 L 4 6 L 6 8 M 7 -12 L 6 -11 L 5 -8 L 5 5 L 7 7","-13 13 M -11 -1 L -11 0 L -10 1 L -8 1 L -6 0 L -6 -3 L -7 -5 L -9 -8 L -9 -10 L -7 -12 M -7 -3 L -9 -7 M -8 1 L -7 0 L -7 -2 L -9 -5 L -10 -7 L -10 -9 L -9 -11 L -7 -12 L -4 -12 L -2 -11 L -1 -10 L 0 -8 L 0 0 L -1 3 L -3 5 M -2 -10 L -1 -8 L -1 2 M -4 -12 L -3 -11 L -2 -8 L -2 3 L -3 5 M 0 -9 L 1 -11 L 3 -12 L 5 -12 L 7 -11 L 8 -10 L 9 -8 L 10 -7 M 7 -10 L 8 -8 M 5 -12 L 6 -11 L 7 -8 L 8 -7 L 10 -7 M 10 -7 L 0 -2 M 7 -5 L 9 -3 L 10 0 L 10 3 L 9 6 L 7 8 L 4 9 L 1 9 L -2 8 L -8 5 L -9 5 L -10 6 M 6 -4 L 7 -4 L 9 -2 M 4 -4 L 7 -3 L 9 -1 L 10 1 M 2 8 L 0 8 L -6 5 L -7 5 M 8 7 L 6 8 L 3 8 L 0 7 L -4 5 L -7 4 L -9 4 L -10 6 L -10 8 L -9 9 L -8 8 L -9 7","-12 12 M 0 -10 L -2 -12 L -4 -12 L -6 -11 L -8 -8 L -9 -4 L -9 0 L -8 4 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 9 5 M -6 -10 L -7 -8 L -8 -5 L -8 0 L -7 4 L -5 7 L -2 8 M -4 -12 L -5 -11 L -6 -9 L -7 -5 L -7 -1 L -6 3 L -5 5 L -3 7 L 0 8 L 3 8 L 6 7 L 9 5 M 3 -12 L 0 -10 L -1 -9 L -2 -7 L -2 -6 L -1 -4 L 2 -2 L 3 0 L 3 2 M -1 -7 L -1 -6 L 3 -2 L 3 -1 M -1 -9 L -1 -8 L 0 -6 L 3 -4 L 4 -2 L 4 0 L 3 2 L 1 3 L 0 3 L -2 2 L -3 0 M 3 -12 L 4 -11 L 6 -10 L 8 -10 M 3 -11 L 4 -10 L 5 -10 M 2 -11 L 4 -9 L 6 -9 L 8 -10 L 9 -11","-13 13 M -10 -6 L -10 -7 L -9 -9 L -7 -11 L -4 -12 L 0 -12 L 3 -11 L 5 -10 L 7 -8 L 9 -5 L 10 -1 L 10 3 L 9 6 L 7 8 L 4 9 L 1 9 L -2 8 L -8 5 L -9 5 L -10 6 M -7 -10 L -5 -11 L 0 -11 L 3 -10 L 5 -9 L 7 -7 L 9 -4 M 2 8 L 0 8 L -6 5 L -7 5 M -10 -7 L -8 -9 L -5 -10 L 0 -10 L 3 -9 L 5 -8 L 7 -6 L 9 -3 L 10 0 M 8 7 L 6 8 L 3 8 L 0 7 L -4 5 L -7 4 L -9 4 L -10 6 L -10 8 L -9 9 L -8 8 L -9 7 M -2 -10 L -5 -7 L -6 -5 L -6 -3 L -4 1 L -4 3 M -5 -4 L -5 -3 L -4 -1 L -4 0 M -5 -7 L -5 -5 L -3 -1 L -3 1 L -4 3 L -5 4 L -7 4 L -8 3 L -8 2","-12 12 M 0 -10 L -2 -12 L -4 -12 L -6 -11 L -8 -8 L -9 -4 L -9 0 L -8 4 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 9 5 M -6 -10 L -7 -8 L -8 -5 L -8 0 L -7 4 L -5 7 L -2 8 M -4 -12 L -5 -11 L -6 -9 L -7 -5 L -7 -1 L -6 3 L -5 5 L -3 7 L 0 8 L 3 8 L 6 7 L 9 5 M 3 -12 L 0 -10 L -1 -9 L -2 -7 L -2 -6 L -1 -4 L 2 -2 L 3 0 L 3 2 M -1 -7 L -1 -6 L 3 -2 L 3 -1 M -1 -9 L -1 -8 L 0 -6 L 3 -4 L 4 -2 L 4 0 L 3 2 L 1 3 L 0 3 L -2 2 L -3 0 M 3 -12 L 4 -11 L 6 -10 L 8 -10 M 3 -11 L 4 -10 L 5 -10 M 2 -11 L 4 -9 L 6 -9 L 8 -10 L 9 -11 M 3 -4 L 7 -7 M 7 -7 L 8 -6 L 10 -6 M 6 -6 L 7 -5 L 8 -5 M 5 -5 L 6 -4 L 8 -4 L 10 -6","-12 12 M -5 -4 L -7 -5 L -8 -7 L -8 -9 L -7 -11 L -4 -12 L -1 -12 L 2 -11 L 6 -9 M -7 -10 L -5 -11 L 0 -11 L 3 -10 M -8 -7 L -7 -9 L -5 -10 L 0 -10 L 6 -9 L 8 -9 L 9 -10 L 9 -11 L 8 -12 L 7 -12 M 1 -10 L 0 -9 L -1 -7 L -1 -5 L 0 -3 L 4 1 L 5 4 L 5 7 L 4 10 L 3 11 L 1 12 M 2 -2 L 5 1 L 6 4 L 6 7 L 5 9 M -1 -5 L 1 -3 L 4 -1 L 6 1 L 7 4 L 7 7 L 6 9 L 4 11 L 1 12 L -3 12 L -6 11 L -7 10 L -8 8 L -8 5 L -6 2 L -6 0 L -7 -1 M -6 10 L -7 9 L -7 5 L -6 3 M -3 12 L -5 11 L -6 9 L -6 5 L -5 2 L -5 0 L -6 -1 L -8 -1 L -9 0 L -9 1 M 3 -2 L 7 -6 M 7 -6 L 8 -5 L 10 -5 M 6 -5 L 7 -4 L 8 -4 M 5 -4 L 6 -3 L 8 -3 L 10 -5","-13 13 M 3 -8 L 2 -10 L 1 -11 L -1 -12 L -4 -12 L -7 -11 L -9 -8 L -10 -4 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 9 5 L 10 2 L 10 -1 L 9 -4 L 7 -6 M -7 -10 L -8 -8 L -9 -5 L -9 0 L -8 3 L -7 5 M 8 5 L 9 3 L 9 -1 L 8 -4 L 7 -5 M -4 -12 L -6 -11 L -7 -9 L -8 -5 L -8 0 L -7 4 L -6 6 L -4 8 M 5 8 L 7 6 L 8 3 L 8 -1 L 7 -3 L 5 -5 M 3 -12 L 0 -10 L -2 -8 L -3 -6 L -3 -5 L -2 -3 L 1 -1 L 2 1 L 2 3 M -2 -6 L -2 -5 L 2 -1 L 2 0 M -2 -8 L -2 -7 L -1 -5 L 2 -3 L 3 -1 L 3 1 L 2 3 L 0 4 L -1 4 L -3 3 L -4 1 M 2 -3 L 7 -6 L 8 -8 M 10 -12 L 8 -8 M 7 -11 L 11 -9 M 10 -12 L 9 -11 L 7 -11 L 8 -10 L 8 -8 L 9 -9 L 11 -9 L 10 -10 L 10 -12","-12 13 M 0 -12 L -2 -11 L -4 -9 L -5 -7 L -5 -5 L -4 -3 L -2 -1 L -1 1 L -1 3 M -4 -6 L -4 -5 L -1 -1 L -1 0 M -4 -9 L -4 -7 L -3 -5 L -1 -3 L 0 -1 L 0 1 L -1 3 L -2 4 L -4 5 L -6 5 L -8 4 L -9 3 L -10 1 L -10 -1 L -9 -2 L -8 -1 L -9 0 M 0 -12 L 2 -10 L 4 -10 L 6 -11 M -1 -11 L 1 -10 M -2 -11 L -1 -10 L 1 -9 L 3 -9 L 6 -11 M 0 -2 L 7 -7 M 7 -7 L 9 -4 L 10 -1 L 10 2 L 9 5 L 7 7 L 4 8 L 0 9 M 6 -6 L 8 -4 L 9 -1 L 9 3 L 8 5 M 4 -5 L 5 -5 L 7 -3 L 8 0 L 8 4 L 7 6 L 6 7 L 4 8 M 4 8 L 2 8 L 0 7 L -2 7 L -4 8 L -5 10 L -4 12 L -2 13 L 0 13 L 2 12 M 1 8 L -1 8 M 0 9 L -2 8 L -4 8","-12 13 M -2 -2 L -4 -2 L -6 -3 L -7 -4 L -8 -6 L -8 -8 L -7 -10 L -6 -11 L -3 -12 L -1 -12 L 2 -11 L 5 -8 L 7 -7 M -6 -10 L -4 -11 L 0 -11 L 2 -10 L 3 -9 M -8 -8 L -7 -9 L -5 -10 L -1 -10 L 2 -9 L 4 -8 L 7 -7 L 9 -7 L 10 -8 L 10 -10 L 9 -11 L 7 -11 M -8 6 L -7 7 L -8 8 L -9 7 L -9 5 L -8 4 L -6 4 L -4 5 L -2 7 L 0 10 L 2 12 M -4 6 L -3 7 L -1 10 L 0 11 M -6 4 L -5 5 L -4 7 L -2 10 L -1 11 L 1 12 L 4 12 L 6 11 L 7 10 L 8 8 L 8 5 L 7 3 L 5 0 L 4 -2 L 4 -3 M 7 6 L 7 5 L 4 0 L 4 -1 M 6 11 L 7 9 L 7 7 L 6 5 L 4 2 L 3 0 L 3 -2 L 5 -4 L 7 -4 L 8 -3 L 8 -2","-12 13 M -2 -2 L -4 -2 L -6 -3 L -7 -4 L -8 -6 L -8 -8 L -7 -10 L -6 -11 L -3 -12 L -1 -12 L 2 -11 L 5 -8 L 7 -7 M -6 -10 L -4 -11 L 0 -11 L 2 -10 L 3 -9 M -8 -8 L -7 -9 L -5 -10 L -1 -10 L 2 -9 L 4 -8 L 7 -7 L 9 -7 L 10 -8 L 10 -10 L 9 -11 L 7 -11 M -8 6 L -7 7 L -8 8 L -9 7 L -9 5 L -8 4 L -6 4 L -4 5 L -2 7 L 0 10 L 2 12 M -4 6 L -3 7 L -1 10 L 0 11 M -6 4 L -5 5 L -4 7 L -2 10 L -1 11 L 1 12 L 4 12 L 6 11 L 7 10 L 8 8 L 8 5 L 7 3 L 5 0 L 4 -2 L 4 -3 M 7 6 L 7 5 L 4 0 L 4 -1 M 6 11 L 7 9 L 7 7 L 6 5 L 4 2 L 3 0 L 3 -2 L 5 -4 L 7 -4 L 8 -3 L 8 -2","-13 13 M 9 -7 L 8 -9 L 6 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -6 -4 L -5 -1 L -2 5 L -2 7 L -4 9 M -5 -4 L -5 -3 L -2 3 L -2 4 M -4 -10 L -5 -8 L -5 -5 L -4 -3 L -2 1 L -1 4 L -1 6 L -2 8 L -4 9 L -6 9 L -8 8 M -10 4 L -8 8 M -11 7 L -7 5 M -10 4 L -10 6 L -11 7 L -9 7 L -8 8 L -8 6 L -7 5 L -9 5 L -10 4 M -4 -3 L -4 -5 L -3 -7 L -1 -8 L 2 -8 L 4 -7 L 6 -5 L 7 -5 M 3 -7 L 5 -5 M 0 -8 L 2 -7 L 3 -6 L 4 -4 M 7 -5 L -2 -1 M 3 -3 L 7 6 L 8 7 L 9 7 M 2 -2 L 6 6 L 8 8 M 1 -2 L 5 7 L 7 9 L 10 6","-11 12 M 8 1 L 7 2 L 4 2 L 3 1 L 3 -1 L 4 -3 L 6 -6 L 7 -8 L 7 -10 M 4 -1 L 4 -2 L 7 -6 L 7 -7 M 5 2 L 4 1 L 4 0 L 5 -2 L 7 -4 L 8 -6 L 8 -8 L 7 -10 L 6 -11 L 3 -12 L -2 -12 L -5 -11 L -6 -10 L -7 -8 L -7 -6 L -6 -4 L -4 -1 L -3 1 L -3 2 L -4 4 M -6 -7 L -6 -6 L -3 -1 L -3 0 M -6 -10 L -6 -8 L -5 -6 L -3 -3 L -2 -1 L -2 1 L -3 3 L -5 5 L -8 7 M -5 5 L -3 5 L 0 7 L 3 8 L 6 8 L 8 7 M -4 6 L -3 6 L 1 8 L 2 8 M -8 7 L -6 6 L -5 6 L -1 8 L 2 9 L 4 9 L 7 8 L 8 7 L 9 5","-16 16 M -13 -1 L -13 0 L -12 1 L -10 1 L -8 0 L -8 -3 L -9 -5 L -11 -8 L -11 -10 L -9 -12 M -9 -3 L -11 -7 M -10 1 L -9 0 L -9 -2 L -11 -5 L -12 -7 L -12 -9 L -11 -11 L -9 -12 L -7 -12 L -5 -11 L -3 -9 L -2 -6 L -2 0 L -3 3 L -4 5 L -6 7 L -9 9 L -10 8 L -11 8 M -4 -9 L -3 -6 L -3 0 L -4 3 L -5 5 M -8 8 L -9 7 L -10 7 M -7 -12 L -5 -10 L -4 -7 L -4 0 L -5 4 L -6 6 L -7 7 L -8 6 L -9 6 L -12 9 M -4 -11 L -2 -12 L 0 -12 L 2 -11 L 4 -9 L 5 -6 L 5 0 L 4 3 L 3 5 L 1 7 L -1 9 L -2 8 L -3 8 M 3 -9 L 4 -6 L 4 0 L 3 4 M 0 8 L -1 7 L -2 7 M 0 -12 L 2 -10 L 3 -7 L 3 1 L 2 5 L 1 7 L 0 6 L -1 6 L -4 9 M 3 -10 L 4 -11 L 6 -12 L 8 -12 L 10 -11 L 11 -10 L 12 -8 L 13 -7 M 10 -10 L 11 -8 M 8 -12 L 9 -11 L 10 -8 L 11 -7 L 13 -7 M 13 -7 L 10 -5 L 9 -4 L 8 -1 L 8 2 L 9 6 L 11 9 L 14 6 M 10 -4 L 9 -2 L 9 2 L 10 5 L 12 8 M 13 -7 L 11 -5 L 10 -3 L 10 1 L 11 5 L 13 7","-14 14 M -11 -1 L -11 0 L -10 1 L -8 1 L -6 0 L -6 -3 L -7 -5 L -9 -8 L -9 -10 L -7 -12 M -7 -3 L -9 -7 M -8 1 L -7 0 L -7 -2 L -9 -5 L -10 -7 L -10 -9 L -9 -11 L -7 -12 L -4 -12 L -2 -11 L 0 -9 L 1 -6 L 1 0 L 0 3 L -1 5 L -3 7 L -6 9 L -7 8 L -9 8 L -11 9 M -1 -9 L 0 -7 L 0 0 L -1 3 L -2 5 L -3 6 M -5 8 L -7 7 L -9 7 M -4 -12 L -2 -10 L -1 -7 L -1 0 L -2 4 L -4 7 L -6 6 L -8 6 L -11 9 M 0 -10 L 1 -11 L 3 -12 L 5 -12 L 7 -11 L 8 -10 L 9 -8 L 10 -7 M 7 -10 L 8 -8 M 5 -12 L 6 -11 L 7 -8 L 8 -7 L 10 -7 M 10 -7 L 7 -5 L 6 -4 L 5 -1 L 5 2 L 6 6 L 8 9 L 11 6 M 7 -4 L 6 -2 L 6 2 L 7 5 L 9 8 M 10 -7 L 8 -5 L 7 -3 L 7 1 L 8 5 L 10 7","-14 14 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -7 -5 L -5 -1 L -5 1 M -6 -6 L -6 -5 L -5 -3 L -5 -2 M -6 -9 L -6 -7 L -4 -3 L -4 -1 L -5 1 L -6 2 L -8 2 L -9 1 L -9 0 M -2 -12 L -1 -11 L 5 -9 L 8 -7 L 9 -5 L 10 -2 L 10 1 L 9 4 L 8 6 L 6 8 L 3 9 L 0 9 L -3 8 L -9 5 L -10 5 L -11 6 M -2 -11 L -1 -10 L 5 -8 L 7 -7 L 8 -6 M -2 -12 L -2 -10 L -1 -9 L 5 -7 L 7 -6 L 9 -4 L 10 -2 M 1 8 L -1 8 L -7 5 L -8 5 M 7 7 L 5 8 L 2 8 L -1 7 L -5 5 L -8 4 L -10 4 L -11 6 L -11 8 L -10 9 L -9 8 L -10 7","-13 14 M -10 -1 L -10 0 L -9 1 L -7 1 L -5 0 L -5 -3 L -6 -5 L -8 -8 L -8 -10 L -6 -12 M -6 -3 L -8 -7 M -7 1 L -6 0 L -6 -2 L -8 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -3 -12 L -1 -11 L 0 -10 L 1 -8 L 1 3 M 1 5 L 1 10 L 0 12 L -2 13 L -5 13 L -6 12 L -6 10 L -5 9 L -4 10 L -5 11 M -1 -10 L 0 -8 L 0 10 L -1 12 M -3 -12 L -2 -11 L -1 -8 L -1 3 M -1 5 L -1 10 L -2 12 L -3 13 M 1 -8 L 6 -12 M 6 -12 L 8 -9 L 9 -7 L 10 -3 L 10 0 L 9 3 L 7 6 L 4 9 M 5 -11 L 8 -7 L 9 -4 L 9 -3 M 4 -10 L 6 -8 L 8 -5 L 9 -2 L 9 1 L 8 4 L 7 6 M 5 7 L 3 4 L 1 3 M -1 3 L -3 4 L -5 6 M 5 8 L 3 5 L 1 4 L -2 4 M 4 9 L 2 6 L 1 5 M -1 5 L -3 5 L -5 6","-14 14 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -7 -5 L -5 -1 L -5 1 M -6 -6 L -6 -5 L -5 -3 L -5 -2 M -6 -9 L -6 -7 L -4 -3 L -4 -1 L -5 1 L -6 2 L -8 2 L -9 1 L -9 0 M -2 -12 L -1 -11 L 5 -9 L 8 -7 L 9 -5 L 10 -2 L 10 1 L 9 4 L 8 6 M 6 8 L 3 9 L 0 9 L -3 8 L -9 5 L -10 5 L -11 6 M -2 -11 L -1 -10 L 5 -8 L 7 -7 L 8 -6 M -2 -12 L -2 -10 L -1 -9 L 5 -7 L 7 -6 L 9 -4 L 10 -2 M 1 8 L -1 8 L -7 5 L -8 5 M 6 8 L 2 8 L -1 7 L -5 5 L -8 4 L -10 4 L -11 6 L -11 8 L -10 9 L -9 8 L -10 7 M 2 6 L 4 4 L 6 4 L 10 8 L 11 8 M 5 5 L 6 5 L 9 8 M 3 5 L 4 5 L 8 9 L 10 9 L 12 7","-14 14 M -11 -1 L -11 0 L -10 1 L -8 1 L -6 0 L -6 -3 L -7 -5 L -9 -8 L -9 -10 L -7 -12 M -7 -3 L -9 -7 M -8 1 L -7 0 L -7 -2 L -9 -5 L -10 -7 L -10 -9 L -9 -11 L -7 -12 L -4 -12 L -2 -11 L -1 -10 L 0 -8 L 0 4 L -1 6 L -3 8 L -5 9 L -7 9 L -9 8 M -2 -10 L -1 -8 L -1 4 L -2 6 M -4 -12 L -3 -11 L -2 -8 L -2 4 L -3 7 L -5 9 M -11 4 L -9 8 M -12 7 L -8 5 M -11 4 L -11 6 L -12 7 L -10 7 L -9 8 L -9 6 L -8 5 L -10 5 L -11 4 M 0 -9 L 1 -11 L 3 -12 L 5 -12 L 7 -11 L 8 -10 L 9 -8 L 10 -7 M 7 -10 L 8 -8 M 5 -12 L 6 -11 L 7 -8 L 8 -7 L 10 -7 M 10 -7 L 0 -2 M 2 -3 L 6 7 L 8 9 L 11 6 M 3 -3 L 7 6 L 9 8 M 4 -4 L 8 6 L 9 7 L 10 7","-13 14 M 10 -10 L 9 -11 L 10 -12 L 11 -11 L 11 -9 L 10 -7 L 8 -7 L 4 -9 L 1 -10 L -3 -10 L -7 -9 L -9 -7 M 7 -8 L 4 -10 L 1 -11 L -3 -11 L -6 -10 M 11 -9 L 10 -8 L 8 -8 L 4 -11 L 1 -12 L -3 -12 L -6 -11 L -8 -9 L -9 -7 L -10 -4 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 3 9 L 6 8 L 8 7 L 10 5 L 11 2 L 11 -1 L 10 -3 L 8 -4 L 5 -4 L 3 -3 L 1 0 L -1 1 L -3 1 M -6 6 L -4 7 L -1 8 L 3 8 L 7 7 M -9 3 L -7 5 L -5 6 L -2 7 L 3 7 L 7 6 L 9 5 L 10 4 L 11 2 M 6 -3 L 5 -3 L 1 1 L 0 1 M 11 -1 L 9 -3 L 7 -3 L 5 -2 L 3 1 L 1 2 L -1 2 L -3 1 L -4 -1 L -4 -3 L -3 -5 L -1 -6","-12 13 M -6 -4 L -8 -5 L -9 -7 L -9 -9 L -8 -11 L -5 -12 L 0 -12 L 3 -11 L 7 -8 L 9 -8 L 10 -9 M -8 -10 L -6 -11 L 0 -11 L 3 -10 L 6 -8 M -9 -7 L -8 -9 L -6 -10 L 0 -10 L 3 -9 L 7 -7 L 9 -7 L 10 -9 L 10 -11 L 9 -12 L 8 -11 L 9 -10 M 3 -9 L 0 -6 L -1 -4 L -1 -2 L 1 2 L 1 4 M 0 -3 L 0 -2 L 1 0 L 1 1 M 0 -6 L 0 -4 L 2 0 L 2 2 L 1 4 L 0 5 L -2 5 L -3 4 L -3 2 M -8 7 L -7 8 L -8 9 L -9 8 L -9 6 L -8 4 L -6 4 L -3 5 L 1 7 L 4 8 L 7 8 L 9 7 M -6 5 L -5 5 L 1 8 L 3 8 M -9 6 L -8 5 L -7 5 L -5 6 L -1 8 L 2 9 L 5 9 L 8 8 L 10 6","-11 11 M -8 -10 L -7 -10 L -6 -9 L -6 5 L -8 6 M -7 -11 L -5 -10 L -5 6 L -2 8 M -9 -9 L -6 -12 L -4 -10 L -4 5 L -2 7 L 0 7 M -8 6 L -7 6 L -5 7 L -3 9 L 0 7 L 4 4 M 2 -10 L 3 -10 L 4 -9 L 4 7 L 6 9 L 9 6 M 3 -11 L 5 -10 L 5 7 L 7 8 M 1 -9 L 4 -12 L 7 -10 L 6 -9 L 6 6 L 7 7 L 8 7","-14 14 M -11 -1 L -11 0 L -10 1 L -8 1 L -6 0 L -6 -3 L -7 -5 L -9 -8 L -9 -10 L -7 -12 M -7 -3 L -9 -7 M -8 1 L -7 0 L -7 -2 L -9 -5 L -10 -7 L -10 -9 L -9 -11 L -7 -12 L -4 -12 L -2 -11 L -1 -10 L 0 -8 L 0 0 L -1 3 L -3 5 M -2 -10 L -1 -8 L -1 2 M -4 -12 L -3 -11 L -2 -8 L -2 3 L -3 5 M 0 -9 L 1 -11 L 3 -12 L 5 -12 L 7 -11 L 9 -8 L 10 -7 M 7 -10 L 8 -8 M 5 -12 L 6 -11 L 7 -8 L 8 -7 L 10 -7 M 8 -7 L 6 -7 L 5 -6 L 5 -4 L 6 -2 L 9 0 L 10 2 M 6 -3 L 9 -1 M 5 -5 L 6 -4 L 9 -2 L 10 0 L 10 4 L 9 6 L 7 8 L 5 9 L 1 9 L -2 8 L -8 5 L -9 5 L -10 6 M 2 8 L 0 8 L -6 5 L -7 5 M 8 7 L 6 8 L 3 8 L 0 7 L -4 5 L -7 4 L -9 4 L -10 6 L -10 8 L -9 9 L -8 8 L -9 7","-16 17 M -13 -1 L -13 0 L -12 1 L -10 1 L -8 0 L -8 -3 L -9 -5 L -11 -8 L -11 -10 L -9 -12 M -9 -3 L -11 -7 M -10 1 L -9 0 L -9 -2 L -11 -5 L -12 -7 L -12 -9 L -11 -11 L -9 -12 L -6 -12 L -4 -11 L -3 -10 L -2 -8 L -2 -4 L -3 -1 L -5 2 L -7 4 M -4 -10 L -3 -8 L -3 -3 L -4 0 M -6 -12 L -5 -11 L -4 -8 L -4 -3 L -5 1 L -7 4 M -4 -11 L -2 -12 L 1 -12 L 3 -11 M 5 -12 L 2 -11 L 1 -9 L 1 -5 L 2 -2 L 4 1 L 5 3 L 5 5 L 4 7 M 2 -5 L 2 -4 L 5 1 L 5 2 M 5 -12 L 3 -11 L 2 -9 L 2 -6 L 3 -4 L 5 -1 L 6 2 L 6 4 L 5 6 L 3 8 L 1 9 L -3 9 L -5 8 L -7 6 L -9 5 L -11 5 L -12 6 M -4 8 L -7 5 L -8 5 M -1 9 L -3 8 L -6 5 L -8 4 L -11 4 L -12 6 L -12 8 L -11 9 L -10 8 L -11 7 M 5 -12 L 8 -12 L 10 -11 L 12 -8 L 13 -7 M 10 -10 L 11 -8 M 8 -12 L 9 -11 L 10 -8 L 11 -7 L 13 -7 M 11 -7 L 9 -7 L 8 -6 L 8 -4 L 9 -2 L 12 0 L 13 2 M 9 -3 L 12 -1 M 8 -5 L 9 -4 L 12 -2 L 13 0 L 13 5 L 12 7 L 11 8 L 9 9 L 6 9 L 3 8 M 7 8 L 6 8 L 4 7 M 12 7 L 10 8 L 8 8 L 6 7 L 5 6","-12 12 M -7 -10 L -5 -10 L -3 -9 L -2 -8 L -1 -5 L -1 -3 M -1 -1 L -1 3 L -2 6 L -5 9 L -7 8 L -9 9 M -4 8 L -6 7 L -7 7 M -3 7 L -4 7 L -6 6 L -9 9 M -5 -11 L -2 -10 L -1 -9 L 0 -6 L 0 3 L 1 5 L 3 7 L 5 8 M -9 -9 L -4 -12 L -2 -11 L 0 -9 L 1 -6 L 1 -3 M 1 -1 L 1 2 L 2 5 L 3 6 L 5 7 L 7 7 M -1 3 L 0 6 L 2 8 L 4 9 L 9 6 M 1 -6 L 2 -9 L 5 -12 L 7 -11 L 9 -12 M 4 -11 L 6 -10 L 7 -10 M 3 -10 L 4 -10 L 6 -9 L 9 -12 M -7 1 L -5 -3 L -1 -3 M 1 -3 L 5 -3 L 7 -5 M -5 -2 L 5 -2 M -7 1 L -5 -1 L -1 -1 M 1 -1 L 5 -1 L 7 -5","-13 13 M -10 -1 L -10 0 L -9 1 L -7 1 L -5 0 L -5 -3 L -6 -5 L -8 -8 L -8 -10 L -6 -12 M -6 -3 L -8 -7 M -7 1 L -6 0 L -6 -2 L -8 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -3 -12 L -1 -11 L 0 -10 L 1 -8 L 1 -3 L 0 0 L -1 2 L -1 3 L 1 5 L 2 5 M -1 -10 L 0 -8 L 0 -2 L -1 1 L -2 3 L 1 6 M -3 -12 L -2 -11 L -1 -8 L -1 -2 L -2 2 L -3 4 L 0 7 L 3 4 M 1 -8 L 9 -12 M 7 -11 L 7 8 L 6 11 M 8 -11 L 8 6 L 7 9 M 9 -12 L 9 4 L 8 8 L 7 10 L 5 12 L 2 13 L -2 13 L -5 12 L -7 10 L -8 8 L -7 7 L -6 8 L -7 9","-12 12 M -4 -9 L -3 -11 L -1 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -5 L 5 -3 L 4 -2 L 2 -1 M -1 -1 L -3 -2 L -4 -4 M 4 -10 L 5 -9 L 5 -4 L 4 -3 M 2 -12 L 3 -11 L 4 -9 L 4 -4 L 3 -2 L 2 -1 M -5 3 L -4 1 L -3 0 L -1 -1 L 2 -1 L 5 0 L 7 2 L 8 4 L 8 8 L 7 10 L 5 12 L 2 13 L -2 13 L -4 12 L -7 8 L -8 7 M 6 2 L 7 4 L 7 8 L 6 10 M 2 -1 L 5 1 L 6 3 L 6 9 L 5 11 L 4 12 L 2 13 M -3 12 L -4 11 L -6 8 L -7 7 M 0 13 L -2 12 L -3 11 L -5 8 L -6 7 L -9 7 L -10 8 L -10 10 L -9 11 L -8 11","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-12 12 M -12 16 L 12 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-8 9 M 2 -5 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 4 L -4 7 L -3 9 L 3 6 M -4 4 L -3 7 L -2 8 M -1 -4 L -3 -2 L -4 1 L -4 3 L -3 6 L -1 8 M 0 -4 L 1 -3 L 3 -2 L 3 7 L 5 9 L 8 6 M 1 -4 L 4 -2 L 4 6 L 6 8 M 2 -5 L 3 -4 L 5 -3 L 6 -3 M 5 -2 L 6 -3 M 5 -2 L 5 6 L 6 7 L 7 7","-8 9 M -6 -10 L -5 -9 L -4 -7 M 2 -12 L -1 -11 L -3 -9 L -4 -7 L -4 6 L -5 7 M -2 -9 L -3 -7 L -3 6 L 0 8 M 2 -12 L 0 -11 L -1 -10 L -2 -7 L -2 6 L 0 7 L 1 8 M -5 7 L -4 7 L -2 8 L -1 9 L 2 8 M -2 -2 L 4 -5 L 5 -3 L 6 0 L 6 3 L 5 6 L 4 7 L 2 8 M 3 -4 L 4 -3 L 5 -1 M 2 -4 L 4 -2 L 5 1 L 5 3 L 4 6 L 2 8","-7 6 M 0 -4 L 2 -2 L 4 -3 L 2 -5 L 0 -4 L -3 -2 L -4 0 L -4 5 L -3 7 L -1 9 L 3 7 M 1 -4 L 3 -3 M -2 -2 L -3 0 L -3 5 L -2 7 L -1 8 M -1 -3 L -2 -1 L -2 4 L -1 6 L 1 8","-8 9 M -1 -12 L -4 -9 L -4 -7 L -3 -6 L 1 -4 L 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 M -3 -8 L -3 -7 L 1 -5 L 4 -3 L 5 -2 M -3 -10 L -3 -9 L -2 -8 L 3 -5 L 5 -3 L 6 0 L 6 3 L 5 6 L 2 8 L -1 9 M 0 -4 L -4 -2 L -4 6 L -5 7 M -3 -2 L -3 6 L 0 8 M -2 -3 L -2 6 L 0 7 L 1 8 M -5 7 L -4 7 L -2 8 L -1 9","-7 6 M -2 3 L 4 -1 L 1 -5 L -3 -2 L -4 0 L -4 5 L -3 7 L -1 9 L 3 7 M 3 -1 L 0 -4 M -2 -2 L -3 0 L -3 5 L -2 7 L -1 8 M 2 0 L 0 -3 L -1 -3 L -2 -1 L -2 4 L -1 6 L 1 8","-6 7 M 6 -12 L 5 -11 L 3 -11 L 1 -12 L -1 -12 L -2 -10 L -2 -5 L -3 -3 L -4 -2 M 4 -10 L 2 -10 L 0 -11 L -1 -11 M 6 -12 L 5 -10 L 4 -9 L 2 -9 L 0 -10 L -1 -10 L -2 -9 M -2 -7 L -1 -5 L 0 -4 L 2 -3 L 4 -3 L 4 -2 M -4 -2 L -2 -2 M 0 -2 L 4 -2 M -2 -2 L -2 2 L -1 14 M 1 -3 L -2 -3 L -1 -4 L -1 9 M 0 -2 L 0 2 L -1 14","-8 9 M 2 -5 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 4 L -4 7 L -3 9 L 3 6 M -4 5 L -3 7 L -2 8 M -1 -4 L -3 -2 L -4 1 L -4 3 L -3 6 L -1 8 M 0 -4 L 1 -3 L 3 -2 L 3 6 L 4 9 L 4 11 L 3 13 M 1 -4 L 4 -2 L 4 8 M 2 -5 L 3 -4 L 5 -3 L 6 -3 M 5 -2 L 6 -3 M 5 -2 L 5 10 L 4 12 L 3 13 L 1 14 L -2 14 L -4 13 L -5 12 L -5 11 L -4 11 L -4 12","-8 9 M -6 -10 L -5 -9 L -4 -7 M 2 -12 L -1 -11 L -3 -9 L -4 -7 L -4 6 L -5 7 M -2 -9 L -3 -7 L -3 7 L -2 8 M 2 -12 L 0 -11 L -1 -10 L -2 -7 L -2 6 L -1 7 L 0 7 M -5 7 L -3 8 L -2 9 L 1 6 M -2 -2 L 4 -5 L 5 -3 L 6 1 L 6 5 L 5 8 L 4 10 L 2 12 L -1 14 M 3 -4 L 4 -3 L 5 0 M 2 -4 L 4 -1 L 5 2 L 5 5 L 4 9 L 2 12","-5 5 M 0 -12 L -1 -11 L -1 -10 L 0 -9 L 1 -10 L 1 -11 L 0 -12 M -1 -11 L 1 -10 M -1 -10 L 1 -11 M -3 -3 L -2 -3 L -1 -2 L -1 7 L 1 9 L 4 6 M -2 -4 L 0 -3 L 0 6 L 2 8 M -4 -2 L -1 -5 L 0 -4 L 2 -3 M 1 -2 L 2 -3 M 1 -2 L 1 6 L 2 7 L 3 7","-5 5 M 0 -12 L -1 -11 L -1 -10 L 0 -9 L 1 -10 L 1 -11 L 0 -12 M -1 -11 L 1 -10 M -1 -10 L 1 -11 M -3 -3 L -2 -3 L -1 -2 L -1 9 L -2 12 L -3 13 L -5 14 M -2 -4 L 0 -3 L 0 9 L -1 11 M -4 -2 L -1 -5 L 0 -4 L 2 -3 M 1 -2 L 2 -3 M 1 -2 L 1 9 L 0 11 L -2 13 L -5 14 M 1 9 L 2 11 L 3 12","-7 7 M -4 -10 L -3 -9 L -2 -7 M 3 -12 L 1 -11 L -1 -9 L -2 -7 L -2 -5 L -3 -3 L -4 -2 M -2 -2 L -2 6 L -3 7 M 0 -9 L -1 -7 L -1 -5 M -1 -3 L -2 -3 L -1 -5 L -1 6 L 1 8 M 3 -12 L 1 -10 L 0 -7 L 0 -3 M 0 -2 L 0 6 L 1 7 L 2 7 M -3 7 L -1 8 L 0 9 L 3 6 M 0 -6 L 4 -9 L 5 -8 L 5 -6 L 3 -4 L 1 -3 M 3 -8 L 4 -7 L 4 -6 L 3 -4 M 0 -3 L 5 -3 L 5 -2 M -4 -2 L -2 -2 M 0 -2 L 5 -2","-5 5 M -3 -10 L -2 -9 L -1 -7 M 5 -12 L 2 -11 L 0 -9 L -1 -7 L -1 6 L -2 7 M 1 -9 L 0 -7 L 0 7 L 2 8 M 5 -12 L 3 -11 L 2 -10 L 1 -7 L 1 6 L 2 7 L 3 7 M -2 7 L 0 8 L 1 9 L 4 6","-13 13 M -11 -3 L -10 -3 L -9 -2 L -9 6 L -10 7 L -8 9 M -10 -4 L -8 -2 L -8 6 L -9 7 L -8 8 L -7 7 L -8 6 M -12 -2 L -9 -5 L -7 -3 L -7 6 L -6 7 L -8 9 M -4 -4 L -2 -3 L -1 -1 L -1 6 L -2 7 L 0 9 M -2 -4 L -1 -3 L 0 -1 L 0 6 L -1 7 L 0 8 L 1 7 L 0 6 M -7 -2 L -4 -4 L -2 -5 L 0 -4 L 1 -2 L 1 6 L 2 7 L 0 9 M 4 -4 L 5 -3 L 7 -2 L 7 7 L 9 9 L 12 6 M 5 -4 L 8 -2 L 8 6 L 10 8 M 1 -2 L 4 -4 L 6 -5 L 7 -4 L 9 -3 L 10 -3 M 9 -2 L 10 -3 M 9 -2 L 9 6 L 10 7 L 11 7","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -6 7 L -4 9 M -6 -4 L -4 -2 L -4 6 L -5 7 L -4 8 L -3 7 L -4 6 M -8 -2 L -5 -5 L -3 -3 L -3 6 L -2 7 L -4 9 M 0 -4 L 1 -3 L 3 -2 L 3 7 L 5 9 L 8 6 M 1 -4 L 4 -2 L 4 6 L 6 8 M -3 -2 L 0 -4 L 2 -5 L 3 -4 L 5 -3 L 6 -3 M 5 -2 L 6 -3 M 5 -2 L 5 6 L 6 7 L 7 7","-8 9 M -4 -2 L -4 6 L -5 7 M -3 -2 L -3 6 L 0 8 M -1 -3 L -2 -2 L -2 6 L 0 7 L 1 8 M -5 7 L -4 7 L -2 8 L -1 9 L 2 8 M -4 -2 L -1 -3 L 4 -5 L 5 -3 L 6 0 L 6 3 L 5 6 L 4 7 L 2 8 M 3 -4 L 4 -3 L 5 -1 M 2 -4 L 4 -2 L 5 1 L 5 3 L 4 6 L 2 8","-8 9 M -3 -8 L -5 -6 L -5 -4 L -4 -1 L -4 6 L -6 8 M -4 7 L -3 14 M -4 -5 L -4 -4 L -3 -1 L -3 9 M -4 -7 L -4 -6 L -3 -4 L -2 -1 L -2 6 L -1 6 L 1 7 L 2 8 M -2 7 L -3 14 M 1 8 L -1 7 M 2 8 L 0 9 L -2 7 M -4 7 L -6 8 M -2 -2 L 4 -5 L 5 -3 L 6 0 L 6 3 L 5 6 L 4 7 L 2 8 M 3 -4 L 4 -3 L 5 -1 M 2 -4 L 4 -2 L 5 1 L 5 3 L 4 6 L 2 8","-8 9 M 2 -5 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 4 L -4 7 L -3 9 L 3 6 M -4 5 L -3 7 L -2 8 M -1 -4 L -3 -2 L -4 1 L -4 3 L -3 6 L -1 8 M 0 -4 L 1 -3 L 3 -2 L 3 6 L 4 14 M 1 -4 L 4 -2 L 4 9 M 2 -5 L 3 -4 L 5 -3 L 6 -3 M 5 -2 L 6 -3 M 5 -2 L 5 6 L 4 14","-7 7 M -4 -3 L -3 -3 L -2 -2 L -2 6 L -3 7 M -3 -4 L -1 -2 L -1 7 L 1 8 M -5 -2 L -2 -5 L 0 -3 L 0 6 L 1 7 L 2 7 M -3 7 L -1 8 L 0 9 L 3 6 M 2 -4 L 3 -2 L 5 -3 L 4 -5 L 0 -3 M 3 -4 L 4 -3","-6 5 M 6 -12 L 5 -11 L 3 -11 L 1 -12 L -1 -12 L -2 -10 L -2 -5 L -3 -3 L -4 -2 M 4 -10 L 2 -10 L 0 -11 L -1 -11 M 6 -12 L 5 -10 L 4 -9 L 2 -9 L 0 -10 L -1 -10 L -2 -9 M -2 -7 L 0 -2 M -2 -2 L -2 2 L -1 14 M -1 -3 L -2 -3 L -1 -4 L -1 9 M 0 -2 L 0 2 L -1 14 M -4 -2 L -2 -2","-6 6 M 1 -9 L 0 -6 L -1 -4 L -2 -3 L -4 -2 M 1 -9 L 1 -3 L 4 -3 L 4 -2 M -4 -2 L -1 -2 M 1 -2 L 4 -2 M -1 -2 L -1 6 L -2 7 M 0 -3 L -1 -3 L 0 -5 L 0 6 L 2 8 M 1 -2 L 1 6 L 2 7 L 3 7 M -2 7 L 0 8 L 1 9 L 4 6","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -6 7 M -6 -4 L -4 -2 L -4 6 L -2 8 M -8 -2 L -5 -5 L -3 -3 L -3 6 L -1 7 L 0 8 M -6 7 L -5 7 L -3 8 L -2 9 L 0 8 L 3 6 M 4 -5 L 2 -3 L 3 -2 L 3 7 L 5 9 L 8 6 M 4 -2 L 5 -3 L 4 -4 L 3 -3 L 4 -2 L 4 6 L 6 8 M 4 -5 L 6 -3 L 5 -2 L 5 6 L 6 7 L 7 7","-8 9 M -3 -7 L -5 -5 L -5 -3 L -4 0 L -4 6 L -5 7 M -4 -4 L -4 -3 L -3 0 L -3 6 L 0 8 M -4 -6 L -4 -5 L -3 -3 L -2 0 L -2 6 L 0 7 L 1 8 M -5 7 L -4 7 L -2 8 L -1 9 L 2 8 M -2 -2 L 4 -5 L 5 -3 L 6 0 L 6 3 L 5 6 L 4 7 L 2 8 M 3 -4 L 4 -3 L 5 -1 M 2 -4 L 4 -2 L 5 1 L 5 3 L 4 6 L 2 8","-12 13 M -7 -7 L -9 -5 L -9 -3 L -8 0 L -8 6 L -9 7 L -7 9 M -8 -4 L -8 -3 L -7 0 L -7 6 L -8 7 L -7 8 L -6 7 L -7 6 M -8 -6 L -8 -5 L -7 -3 L -6 0 L -6 6 L -5 7 L -7 9 M -3 -4 L -1 -3 L 0 -1 L 0 6 L -1 7 M -1 -4 L 0 -3 L 1 -1 L 1 6 L 4 8 M -6 -2 L -3 -4 L -1 -5 L 1 -4 L 2 -2 L 2 6 L 4 7 L 5 8 M -1 7 L 0 7 L 2 8 L 3 9 L 6 8 M 2 -2 L 8 -5 L 9 -3 L 10 0 L 10 2 L 9 6 L 8 7 L 6 8 M 7 -4 L 8 -3 L 9 -1 M 6 -4 L 8 -2 L 9 1 L 9 3 L 8 6 L 6 8","-7 8 M -3 -3 L -2 -3 L -1 -2 L -1 6 L -2 6 L -4 7 L -5 9 L -5 11 L -4 13 L -2 14 L 1 14 L 4 13 L 4 12 L 3 12 L 3 13 M -2 -4 L 0 -2 L 0 6 L 3 8 M -4 -2 L -1 -5 L 1 -3 L 1 6 L 3 7 L 4 8 M 6 7 L 2 9 L 1 8 L -1 7 L -3 7 L -5 9 M 3 -4 L 4 -2 L 6 -3 L 5 -5 L 1 -3 M 4 -4 L 5 -3","-8 9 M -3 -7 L -5 -5 L -5 -3 L -4 0 L -4 6 L -5 7 M -4 -4 L -4 -3 L -3 0 L -3 7 L -1 8 M -4 -6 L -4 -5 L -3 -3 L -2 0 L -2 6 L -1 7 L 0 7 M -5 7 L -3 8 L -2 9 L 1 6 M -2 -2 L 4 -5 L 5 -3 L 6 1 L 6 5 L 5 8 L 4 10 L 2 12 L -1 14 M 3 -4 L 4 -3 L 5 0 M 2 -4 L 4 -1 L 5 2 L 5 5 L 4 9 L 2 12","-7 7 M -4 -2 L 1 -5 L 3 -4 L 4 -2 L 4 0 L 3 2 L -1 4 M 1 -4 L 3 -3 M 0 -4 L 2 -3 L 3 -1 L 3 0 L 2 2 L 1 3 M 1 3 L 3 5 L 4 7 L 4 11 L 3 13 L 1 14 L -1 14 L -3 13 L -4 11 L -4 9 L -3 7 L -1 6 L 5 4 M 0 4 L 2 5 L 3 7 M -1 4 L 2 6 L 3 8 L 3 11 L 2 13 L 1 14","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-6 6 M 0 -12 L -1 -11 L -3 -10 L -1 -9 L 0 2 M 0 -9 L 1 -10 L 0 -11 L -1 -10 L 0 -9 L 0 2 M 0 -12 L 1 -11 L 3 -10 L 1 -9 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 2 -12 L 4 -11 L 5 -9 L 5 -7 L 7 -8 L 6 -10 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M 6 -8 L 5 -10 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 7 L -6 5 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -5 4 L -5 6 L -4 8 L -2 9","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 7 -4 L 8 -3 L 9 -3 L 10 -4 M 6 -3 L 7 -2 L 9 -2 M 6 -2 L 7 -1 L 8 -1 L 9 -2 L 10 -4 M 7 -4 L 1 2 M 0 3 L -6 9 L -10 4 L -4 -2 M -3 -3 L 1 -7 L -3 -12 L -8 -6 L -2 0 L 2 6 L 4 8 L 6 9 L 8 9 L 9 8 L 10 6 M -6 8 L -9 4 M 0 -7 L -3 -11 M -7 -6 L -2 -1 L 2 5 L 4 7 L 6 8 L 9 8 M -5 8 L -9 3 M 0 -6 L -4 -11 M -7 -7 L -1 -1 L 3 5 L 4 6 L 6 7 L 9 7 L 10 6","-4 5 M 1 -12 L 0 -11 L 0 -5 M 1 -11 L 0 -5 M 1 -12 L 2 -11 L 0 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 M -1 -10 L -2 -7 L -3 -3 L -3 3 L -2 7 L -1 10 M 1 -14 L 0 -12 L -1 -9 L -2 -3 L -2 3 L -1 9 L 0 12 L 1 14","-7 7 M -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16 M 1 -10 L 2 -7 L 3 -3 L 3 3 L 2 7 L 1 10 M -1 -14 L 0 -12 L 1 -9 L 2 -3 L 2 3 L 1 9 L 0 12 L -1 14","-8 8 M 0 -12 L -1 -11 L 1 -1 L 0 0 M 0 -12 L 0 0 M 0 -12 L 1 -11 L -1 -1 L 0 0 M -5 -9 L -4 -9 L 4 -3 L 5 -3 M -5 -9 L 5 -3 M -5 -9 L -5 -8 L 5 -4 L 5 -3 M 5 -9 L 4 -9 L -4 -3 L -5 -3 M 5 -9 L -5 -3 M 5 -9 L 5 -8 L -5 -4 L -5 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-6 6 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-13 13 M -9 0 L 9 0","-6 6 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -3 -10 L -2 -9 L -1 -7 L -1 6 L -3 7 M -1 -9 L -2 -10 L -1 -11 L 0 -9 L 0 7 L 2 8 M -3 -10 L 0 -12 L 1 -10 L 1 6 L 3 7 L 4 7 M -3 7 L -2 7 L 0 8 L 1 9 L 2 8 L 4 7","-10 10 M -6 -10 L -4 -10 L -2 -11 L -1 -12 L 1 -11 L 4 -10 L 6 -10 M -2 -10 L 0 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -10 L 1 -11 M 4 -10 L 4 -2 M 5 -9 L 5 -3 M 6 -10 L 6 -2 L -1 -2 L -4 -1 L -6 1 L -7 4 L -7 9 M -7 9 L -3 7 L 1 6 L 4 6 L 8 7 M -4 8 L -1 7 L 4 7 L 7 8 M -7 9 L -2 8 L 3 8 L 6 9 L 8 7","-10 10 M -6 -10 L -5 -10 L -3 -11 L -2 -12 L 0 -11 L 4 -10 L 6 -10 M -3 -10 L -1 -11 M -6 -10 L -4 -9 L -2 -9 L 0 -11 M 4 -10 L 4 -3 M 5 -9 L 5 -4 M 6 -10 L 6 -3 L 4 -3 L 1 -2 L -1 -1 M -1 -2 L 1 -1 L 4 0 L 6 0 L 6 7 M 5 1 L 5 6 M 4 0 L 4 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M 3 -12 L -7 -2 L -7 3 L 2 3 M 4 3 L 8 3 L 9 4 L 9 2 L 8 3 M -6 -2 L -6 2 M -5 -4 L -5 3 M 2 -11 L 2 6 L 0 7 M 3 -8 L 4 -10 L 3 -11 L 3 7 L 5 8 M 3 -12 L 5 -10 L 4 -8 L 4 6 L 6 7 L 7 7 M 0 7 L 1 7 L 3 8 L 4 9 L 5 8 L 7 7","-10 10 M -6 -12 L -6 -3 M -6 -12 L 6 -12 M -5 -11 L 4 -11 M -6 -10 L 3 -10 L 5 -11 L 6 -12 M 4 -6 L 3 -5 L 1 -4 L -3 -3 L -6 -3 M 1 -4 L 2 -4 L 4 -3 L 4 7 M 3 -5 L 5 -4 L 5 6 M 4 -6 L 5 -5 L 7 -4 L 8 -4 L 6 -3 L 6 7 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 6 L -8 7 M -5 -9 L -5 6 L -2 8 M -4 -10 L -4 6 L -2 7 L -1 8 M -6 -10 L -4 -10 L 0 -11 L 2 -12 L 3 -11 L 5 -10 L 6 -10 M 1 -11 L 3 -10 M 0 -11 L 2 -9 L 4 -9 L 6 -10 M -4 -2 L -3 -2 L 1 -3 L 3 -4 L 4 -5 M 1 -3 L 2 -3 L 4 -2 L 4 7 M 3 -4 L 5 -2 L 5 6 M 4 -5 L 5 -4 L 7 -3 L 8 -3 L 6 -2 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -7 -10 L -5 -12 L -2 -11 L 3 -11 L 8 -12 M -6 -11 L -3 -10 L 2 -10 L 5 -11 M -7 -10 L -3 -9 L 0 -9 L 4 -10 L 8 -12 M 8 -12 L 7 -10 L 5 -7 L 1 -3 L -1 0 L -2 3 L -2 6 L -1 9 M 0 -1 L -1 2 L -1 5 L 0 8 M 3 -5 L 1 -2 L 0 1 L 0 4 L 1 7 L -1 9","-10 10 M -6 -9 L -6 -3 M -5 -8 L -5 -4 M -4 -9 L -4 -3 M -6 -9 L -4 -9 L 1 -10 L 3 -11 L 4 -12 M 1 -10 L 2 -10 L 4 -9 L 4 -3 M 3 -11 L 5 -10 L 5 -4 M 4 -12 L 5 -11 L 7 -10 L 8 -10 L 6 -9 L 6 -3 M -6 -3 L -4 -3 L 4 0 L 6 0 M 6 -3 L 4 -3 L -4 0 L -6 0 M -6 0 L -6 6 L -8 7 M -5 1 L -5 6 L -2 8 M -4 0 L -4 6 L -2 7 L -1 8 M 4 0 L 4 7 M 5 1 L 5 6 M 6 0 L 6 7 M -8 7 L -6 7 L -4 8 L -3 9 L -1 8 L 4 7 L 6 7","-10 10 M -6 -10 L -6 -1 L -8 0 M -5 -9 L -5 0 L -3 1 M -4 -10 L -4 -1 L -2 0 L -1 0 M -6 -10 L -4 -10 L 1 -11 L 3 -12 M 1 -11 L 2 -10 L 4 -9 L 4 7 M 2 -11 L 5 -9 L 5 6 M 3 -12 L 4 -11 L 6 -10 L 8 -10 L 6 -9 L 6 7 M -8 0 L -7 0 L -5 1 L -4 2 L -3 1 L -1 0 L 3 -1 L 4 -1 M -7 7 L -5 6 L -3 6 L -1 7 L 0 8 M -3 7 L -1 8 M -7 7 L -5 7 L -3 8 L -2 9 L 0 8 L 4 7 L 6 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-6 6 M 0 -5 L -2 -3 L 0 -2 L 2 -3 L 0 -5 M 0 -4 L -1 -3 L 1 -3 L 0 -4 M 0 12 L 0 10 L -2 8 L 0 6 L 1 8 L 1 10 L 0 12 L -2 13 M 0 7 L -1 8 L 0 9 L 0 7","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -8 L -5 -10 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -2 L 1 -1 M -5 -8 L -4 -10 M 4 -10 L 5 -9 L 5 -5 L 4 -4 M -6 -8 L -4 -7 L -4 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 4 -9 L 4 -5 L 3 -3 L 1 -1 M 0 -1 L 0 2 L 1 -1 L -1 -1 L 0 2 M 0 6 L -2 8 L 0 9 L 2 8 L 0 6 M 0 7 L -1 8 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-13 13 M -4 -10 L -6 -9 L -8 -7 L -9 -5 L -10 -2 L -10 1 L -9 3 L -7 4 M -8 -6 L -9 -3 L -9 1 L -8 3 M -4 -10 L -6 -8 L -7 -6 L -8 -3 L -8 0 L -7 4 L -7 6 L -8 8 L -10 9 M 4 -10 L 6 -10 L 6 7 L 4 7 M 7 -10 L 7 7 M 8 -11 L 8 8 M -10 -12 L -7 -11 L -1 -10 L 4 -10 L 8 -11 L 10 -12 M -8 -2 L 6 -2 M -10 9 L -7 8 L -1 7 L 4 7 L 8 8 L 10 9","-13 13 M -6 -11 L -6 8 M -5 -11 L -5 8 M -2 -12 L -4 -11 L -4 8 L -2 9 M -10 -8 L -8 -10 L -6 -11 L -2 -12 L 3 -12 L 6 -11 L 8 -9 L 8 -7 L 7 -5 M 6 -10 L 7 -9 L 7 -7 L 6 -5 M 3 -12 L 5 -11 L 6 -9 L 6 -7 L 5 -6 M -1 3 L -3 2 L -4 0 L -4 -2 L -3 -4 L -2 -5 L 1 -6 L 4 -6 L 7 -5 L 9 -3 L 10 -1 L 10 2 L 9 5 L 7 7 L 5 8 L 2 9 L -2 9 L -6 8 L -8 7 L -10 5 M 8 -3 L 9 -1 L 9 3 L 8 5 M 4 -6 L 7 -4 L 8 -1 L 8 3 L 7 6 L 5 8","-13 13 M 10 -12 L 9 -10 L 8 -8 L 6 -10 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 7 L 10 9 M 9 -10 L 8 -5 L 8 2 L 9 7 M 8 -7 L 7 -8 M 8 -4 L 7 -7 L 6 -9 L 4 -11 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 7 5 L 8 4 M 4 8 L 6 6 L 7 4 L 8 1","-13 13 M -7 -11 L -7 8 M -6 -11 L -6 8 M -4 -12 L -5 -11 L -5 8 L -4 9 M -10 -7 L -9 -9 L -7 -11 L -4 -12 L 1 -12 L 4 -11 L 6 -10 L 8 -8 L 9 -6 L 10 -3 L 10 0 L 9 3 L 8 5 L 6 7 L 4 8 L 1 9 L -4 9 L -7 8 L -9 6 L -10 4 M 8 -7 L 9 -4 L 9 1 L 8 4 M 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8","-13 13 M 10 -12 L 9 -10 L 8 -8 L 6 -10 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 7 L 10 9 M 9 -10 L 8 -5 L 8 2 L 9 7 M 8 -7 L 7 -8 M 8 -5 L 6 -9 L 4 -11 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 7 5 L 8 4 M 4 8 L 6 6 L 7 4 L 8 1 M -8 -2 L -7 -3 L -4 -3 L 3 -1 L 6 -1 L 8 -2 M -2 -2 L 0 -1 L 3 0 L 5 0 L 7 -1 M -5 -3 L 0 0 L 3 1 L 5 1 L 7 0 L 8 -2 M 8 -5 L 7 -6 L 6 -6 L 5 -5 L 6 -4 L 7 -5","-13 13 M -8 -10 L -8 8 M -5 -11 L -7 -10 L -7 7 M -3 -12 L -5 -11 L -6 -9 L -6 7 L -4 7 M -10 -8 L -8 -10 L -6 -11 L -3 -12 L 1 -12 L 4 -11 L 6 -10 L 7 -9 L 10 -12 M 10 -12 L 9 -10 L 8 -6 L 8 -3 L 9 1 L 10 3 M 8 -9 L 7 -7 M 4 -11 L 6 -9 L 7 -6 L 8 -3 M -6 -2 L -5 -3 L -3 -3 L 2 -2 L 5 -2 L 7 -3 M -1 -2 L 2 -1 L 4 -1 L 6 -2 M -4 -3 L 2 0 L 4 0 L 6 -1 L 7 -3 L 7 -6 L 6 -7 L 5 -7 L 4 -6 L 5 -5 L 6 -6 M -10 9 L -8 8 L -4 7 L 1 7 L 7 8 L 10 9","-13 13 M 10 -12 L 9 -10 L 8 -8 L 6 -10 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 4 8 L 6 7 L 7 6 L 8 4 L 9 7 L 10 9 M 9 -10 L 8 -5 L 8 2 L 9 7 M 8 -7 L 7 -8 M 8 -4 L 7 -7 L 6 -9 L 4 -11 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 6 6 L 7 4 L 7 0 M 4 8 L 5 7 L 6 4 L 6 -1 M -7 1 L -6 0 L -5 1 L -6 2 L -7 2 L -8 1 M -8 -2 L -7 -4 L -5 -5 L -3 -5 L 0 -4 L 3 -2 L 5 -1 M -7 -3 L -5 -4 L -3 -4 L 0 -3 L 2 -2 M -8 -2 L -6 -3 L -3 -3 L 3 -1 L 7 -1 L 8 -2","-13 13 M -8 -11 L -8 8 L -10 9 M -7 -10 L -7 8 M -4 -10 L -6 -10 L -6 8 M -10 -12 L -8 -11 L -4 -10 L 1 -10 L 7 -11 L 10 -12 M -6 -2 L -5 -4 L -3 -6 L 0 -7 L 4 -7 L 7 -6 L 9 -4 L 10 -1 L 10 2 L 9 3 L 7 4 M 8 -4 L 9 -2 L 9 1 L 8 3 M 4 -7 L 6 -6 L 7 -5 L 8 -3 L 8 1 L 7 4 L 7 6 L 8 8 L 10 9 M -10 9 L -6 8 L -2 8 L 3 9","-13 13 M -1 -9 L -1 7 M 0 -8 L 0 6 M 1 -9 L 1 7 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12 M -10 9 L -7 8 L -3 7 L 3 7 L 7 8 L 10 9","-13 13 M 2 -9 L 4 -9 L 4 6 L 3 8 L 1 9 M 5 -9 L 5 6 L 4 7 M 6 -10 L 6 7 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12 M -9 -3 L -10 -1 L -10 3 L -9 6 L -7 8 L -4 9 L 1 9 L 4 8 L 6 7 L 8 5 L 10 2 M -9 3 L -8 6 L -7 7 M -10 1 L -8 3 L -7 6 L -6 8 L -4 9","-13 13 M -8 -11 L -8 8 L -10 9 M -7 -10 L -7 8 M -4 -10 L -6 -10 L -6 8 M -10 -12 L -8 -11 L -4 -10 L 1 -10 L 7 -11 L 10 -12 M -6 -2 L -5 -4 L -3 -6 L 0 -7 L 3 -7 L 6 -6 L 7 -5 L 7 -3 L 6 -2 L 1 0 L -1 1 L -2 2 L -2 3 L -1 4 L 0 3 L -1 2 M 5 -6 L 6 -5 L 6 -3 L 5 -2 M 3 -7 L 5 -5 L 5 -3 L 4 -2 L 1 0 M 1 0 L 4 0 L 7 1 L 8 3 L 8 5 L 7 6 M 5 1 L 7 3 L 7 5 M 1 0 L 4 1 L 6 3 L 7 6 L 8 8 L 9 9 L 10 9 M -10 9 L -6 8 L -2 8 L 3 9","-13 13 M -8 -11 L -8 8 M -7 -10 L -7 7 M -4 -10 L -6 -10 L -6 7 L -4 7 M 10 -7 L 8 -4 L 7 -2 L 6 1 L 6 3 L 7 5 L 9 6 M 8 -3 L 7 0 L 7 3 L 8 5 M 10 -7 L 9 -5 L 8 -1 L 8 2 L 9 6 L 10 9 M -10 -12 L -8 -11 L -4 -10 L 1 -10 L 7 -11 L 10 -12 M -10 9 L -8 8 L -4 7 L 1 7 L 7 8 L 10 9","-13 13 M -1 -9 L -1 7 M 0 -8 L 0 6 M 1 -9 L 1 7 M -4 7 L -6 5 L -8 4 L -9 3 L -10 0 L -10 -5 L -9 -8 L -7 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 7 -10 L 9 -8 L 10 -5 L 10 0 L 9 3 L 8 4 L 6 5 L 4 7 M -8 3 L -9 0 L -9 -5 L -8 -8 M -6 5 L -7 3 L -8 0 L -8 -6 L -7 -9 L -5 -11 M 8 -8 L 9 -5 L 9 0 L 8 3 M 5 -11 L 7 -9 L 8 -6 L 8 0 L 7 3 L 6 5 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12 M -10 9 L -7 8 L -3 7 L 3 7 L 7 8 L 10 9","-13 13 M -8 -10 L -8 8 L -10 9 M -6 -10 L -7 -9 L -7 8 M -3 -12 L -5 -11 L -6 -9 L -6 8 M -10 -8 L -8 -10 L -6 -11 L -3 -12 L 1 -12 L 4 -11 L 6 -10 L 8 -8 L 9 -6 L 10 -3 L 10 1 L 9 3 L 7 4 M 8 -7 L 9 -4 L 9 0 L 8 3 M 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 0 L 7 4 L 7 6 L 8 8 L 9 9 L 10 9 M -10 9 L -6 8 L -2 8 L 3 9","-13 13 M -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -3 L 9 -6 L 8 -8 L 6 -10 L 4 -11 L 1 -12 L -1 -12 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 8 4 L 9 1 L 9 -4 L 8 -7 M 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11","-13 13 M -8 -9 L -8 8 M -5 -10 L -7 -8 L -7 7 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -6 7 L -4 7 M -10 -7 L -8 -9 L -4 -11 L -1 -12 L 2 -12 L 5 -11 L 7 -10 L 9 -8 L 10 -5 L 10 -3 L 9 0 L 7 2 L 4 3 L 0 3 L -3 2 L -5 0 L -6 -3 M 8 -8 L 9 -6 L 9 -2 L 8 0 M 5 -11 L 7 -9 L 8 -6 L 8 -2 L 7 1 L 4 3 M -10 9 L -8 8 L -4 7 L 1 7 L 7 8 L 10 9","-13 13 M -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -3 L 9 -6 L 8 -8 L 6 -10 L 4 -11 L 1 -12 L -1 -12 M -8 -7 L -9 -4 L -9 1 L -8 4 M -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 M 8 4 L 9 1 L 9 -4 L 8 -7 M 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 M -8 1 L -7 3 L -4 4 L 2 5 L 9 5 L 10 6 L 10 8 L 9 9 L 9 8 L 10 7 M -2 5 L 0 5 M -7 3 L -4 5 L -1 6 L 1 6 L 2 5","-13 13 M -8 -9 L -8 8 L -10 9 M -7 -9 L -7 8 M -6 -10 L -6 8 M -10 -7 L -8 -9 L -6 -10 L -4 -11 L -1 -12 L 3 -12 L 7 -11 L 9 -9 L 10 -7 L 10 -4 L 9 -2 L 8 -1 M 7 -10 L 8 -9 L 9 -7 L 9 -4 L 8 -2 M 3 -12 L 5 -11 L 7 -9 L 8 -7 L 8 -3 L 7 -1 M 6 0 L 3 1 L 0 1 L -2 0 L -2 -2 L 0 -3 L 3 -3 L 6 -2 L 8 0 L 10 3 L 10 5 L 9 6 L 8 6 M 6 -1 L 7 0 L 9 4 L 9 5 L 8 2 M 2 -3 L 4 -2 L 6 0 L 7 2 L 8 6 L 9 8 L 10 9 M -10 9 L -6 8 L -2 8 L 3 9","-13 13 M 2 -12 L 8 -11 L 10 -12 L 9 -10 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -8 -8 L -9 -5 L -9 -3 L -8 0 L -6 2 L -3 3 L 0 3 L 2 2 L 3 1 L 4 -1 L 4 -2 M 9 -11 L 8 -10 L 9 -8 M -8 -2 L -7 0 L -6 1 L -3 2 L 0 2 L 2 1 M -7 -9 L -8 -7 L -8 -4 L -7 -2 L -5 0 L -2 1 L 0 1 L 2 0 L 4 -2 L 5 -3 L 6 -3 M -6 -1 L -5 -1 L -4 -2 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 7 -2 L 8 0 L 8 3 L 7 6 L 5 8 M -2 -5 L 0 -6 L 3 -6 L 6 -5 L 8 -3 L 9 0 L 9 3 L 8 5 M -9 5 L -8 7 L -9 8 M -4 -2 L -4 -3 L -3 -5 L -2 -6 L 0 -7 L 3 -7 L 6 -6 L 9 -3 L 10 0 L 10 2 L 9 5 L 7 7 L 5 8 L 2 9 L -2 9 L -5 8 L -7 7 L -9 5 L -9 7 L -10 9 L -8 8 L -2 9","-13 13 M -1 -10 L -5 -10 L -7 -9 L -8 -8 L -9 -6 L -10 -3 L -10 1 L -9 4 L -8 6 L -7 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -4 L 9 -7 L 7 -9 L 5 -10 M 3 -10 L 2 -9 L 2 -7 L 3 -6 L 4 -7 L 3 -8 M -9 1 L -8 4 L -6 6 L -4 7 L -1 8 L 2 8 L 5 7 M -8 -8 L -9 -4 L -9 -1 L -8 2 L -6 5 L -4 6 L -1 7 L 2 7 L 5 6 L 7 5 L 9 2 L 10 0 M -10 -12 L -7 -9 M -7 -10 L -6 -11 M -9 -11 L -8 -11 L -7 -12 L -5 -11 L -1 -10 L 5 -10 L 8 -11 L 10 -12","-13 13 M -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 3 9 L 6 8 L 8 7 M -7 -8 L -8 -6 L -9 -3 L -9 1 L -8 4 M -7 -9 L -6 -8 L -6 -7 L -7 -5 L -8 -2 L -8 1 L -7 4 L -6 6 L -4 8 M 4 -10 L 6 -10 L 6 6 L 5 8 L 3 9 M 7 -10 L 7 6 L 6 7 M 8 -11 L 8 7 L 10 9 M -10 -12 L -7 -11 L -1 -10 L 4 -10 L 8 -11 L 10 -12","-13 13 M -10 -12 L 0 9 M -9 -11 L -8 -10 L -1 5 L 0 7 M -8 -11 L -7 -10 L 0 5 L 1 6 M 10 -12 L 0 9 M 5 -4 L 3 1 M 7 -6 L 3 -1 L 2 2 L 2 4 M -10 -12 L -8 -11 L -3 -10 L 3 -10 L 8 -11 L 10 -12","-13 13 M -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 1 9 L 4 8 L 6 7 L 8 5 L 9 3 L 10 0 L 10 -3 L 9 -6 L 8 -8 L 6 -10 M -8 -6 L -9 -3 L -9 0 L -8 3 L -7 5 M -8 -8 L -7 -7 L -7 -6 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 M 7 5 L 8 3 L 9 0 L 9 -3 L 8 -6 M 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -6 L 7 -7 L 8 -8 M -1 -9 L -1 9 M 0 -8 L 0 8 M 1 -9 L 1 9 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12","-13 13 M -10 -12 L 6 7 L 7 8 M -9 -11 L -7 -10 L 8 8 M -6 -10 L 10 9 M 10 -12 L 1 -2 M -1 0 L -8 8 M -2 1 L -5 3 L -6 5 M -1 0 L -5 2 L -6 3 L -7 5 L -7 7 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12 M -10 9 L -8 8 L -4 7 L 1 7 L 7 8 L 10 9","-13 13 M 6 -10 L 6 8 M 7 -10 L 7 7 M 8 -11 L 8 7 M -7 -10 L -9 -8 L -10 -5 L -10 -2 L -9 1 L -7 3 L -5 4 L -2 5 L 1 5 L 4 4 L 6 3 M -6 3 L -3 4 L 3 4 M -10 -2 L -9 0 L -7 2 L -4 3 L 2 3 L 4 4 M -10 -12 L -6 -10 L -2 -9 L 2 -9 L 6 -10 L 10 -12 M -10 5 L -8 7 L -6 8 L -2 9 L 2 9 L 6 8 L 10 6","-13 13 M -10 -12 L -9 -11 L -7 -10 L -4 -10 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 M 6 -11 L 7 -9 L 7 -7 L 6 -5 M 4 -12 L 5 -11 L 6 -9 L 6 -6 M 6 -4 L 2 -3 L 0 -3 L -2 -4 L -2 -6 L 0 -7 L 2 -7 L 6 -6 M 2 -7 L 4 -6 L 5 -5 L 4 -4 L 2 -3 M 7 -5 L 9 -3 L 10 0 L 10 2 L 9 5 L 7 7 L 5 8 L 2 9 L -2 9 L -5 8 L -7 7 L -9 5 L -10 2 L -10 0 L -9 -3 L -8 -4 L -6 -5 L -4 -5 L -2 -4 L -2 -2 L -3 -1 L -4 -2 L -3 -3 M 6 -5 L 8 -3 L 9 -1 L 9 3 L 8 5 M 6 -4 L 7 -3 L 8 -1 L 8 3 L 7 6 L 5 8","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-13 13 M -13 16 L 13 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-8 9 M -2 -1 L -5 2 L -5 6 L -2 9 L 2 7 M -4 2 L -4 6 L -2 8 M -3 0 L -3 5 L 0 8 M 0 1 L -5 -4 L -4 -5 L -3 -4 L -4 -3 M -3 -4 L 1 -4 L 3 -5 L 5 -3 L 5 6 L 6 7 M 3 -4 L 4 -3 L 4 6 L 3 7 L 4 8 L 5 7 L 4 6 M 1 -4 L 3 -2 L 3 6 L 2 7 L 4 9 L 6 7","-9 8 M -4 -10 L -6 -12 L -5 -8 L -5 6 L -2 9 L 3 7 L 5 6 M -4 -10 L -4 6 L -2 8 M -4 -10 L -2 -12 L -3 -8 L -3 5 L 0 8 M -3 -3 L 2 -5 L 5 -2 L 5 6 M 2 -4 L 4 -2 L 4 6 M 0 -4 L 3 -1 L 3 7","-7 5 M -4 -2 L -4 7 L -2 9 L 0 7 M -3 -2 L -3 7 L -2 8 M -2 -3 L -2 6 L -1 7 L 0 7 M -4 -2 L 2 -5 L 4 -3 L 2 -2 L 0 -4 M 1 -4 L 3 -3","-8 8 M 0 -5 L -5 -2 L -5 6 L -2 9 L 0 8 L 3 7 L 5 7 M -4 -2 L -4 6 L -2 8 M -3 -3 L -3 5 L 0 8 M -2 -9 L -2 -12 L -1 -9 L 5 -2 L 5 7 M -2 -9 L 4 -2 L 4 6 M -2 -9 L -5 -9 L -2 -8 L 3 -2 L 3 7","-7 6 M -4 -2 L -4 7 L -2 9 L 0 7 M -3 -2 L -3 7 L -2 8 M -2 -3 L -2 6 L -1 7 L 0 7 M -4 -2 L 2 -5 L 5 -1 L -2 3 M 1 -4 L 4 -1 M 0 -4 L 3 0","-7 5 M -3 -9 L -3 6 L -4 7 L -2 9 M -2 -9 L -2 6 L -3 7 L -2 8 L -1 7 L -2 6 M -1 -10 L -1 6 L 0 7 L -2 9 M -3 -9 L 3 -12 L 5 -10 L 3 -9 L 1 -11 M 2 -11 L 4 -10 M -6 -5 L -3 -5 M -1 -5 L 3 -5","-8 9 M -5 -2 L -5 6 L -2 9 L 3 7 M -4 -2 L -4 6 L -2 8 M -3 -3 L -3 5 L 0 8 M -5 -2 L -3 -3 L 2 -5 L 5 -2 L 5 11 L 4 13 L 3 14 L 1 15 L -1 15 L -3 14 L -5 15 L -3 16 L -1 15 M 2 -4 L 4 -2 L 4 11 L 3 13 M -2 15 L -4 15 M 0 -4 L 3 -1 L 3 12 L 2 14 L 1 15","-9 9 M -4 -10 L -6 -12 L -5 -8 L -5 6 L -6 7 L -4 9 M -4 -10 L -4 6 L -5 7 L -4 8 L -3 7 L -4 6 M -4 -10 L -2 -12 L -3 -8 L -3 6 L -2 7 L -4 9 M -3 -3 L 0 -4 L 2 -5 L 5 -2 L 5 7 L 2 11 L 2 14 L 3 16 L 4 16 L 2 14 M 2 -4 L 4 -2 L 4 7 L 3 9 M 0 -4 L 3 -1 L 3 8 L 2 11","-5 5 M 0 -12 L -2 -10 L 0 -8 L 2 -10 L 0 -12 M 0 -11 L -1 -10 L 0 -9 L 1 -10 L 0 -11 M 0 -5 L -2 -3 L -1 -2 L -1 6 L -2 7 L 0 9 M 0 -2 L 1 -3 L 0 -4 L -1 -3 L 0 -2 L 0 6 L -1 7 L 0 8 L 1 7 L 0 6 M 0 -5 L 2 -3 L 1 -2 L 1 6 L 2 7 L 0 9","-5 5 M 0 -12 L -2 -10 L 0 -8 L 2 -10 L 0 -12 M 0 -11 L -1 -10 L 0 -9 L 1 -10 L 0 -11 M 0 -5 L -2 -3 L -1 -2 L -1 7 L 2 11 M 0 -2 L 1 -3 L 0 -4 L -1 -3 L 0 -2 L 0 7 L 1 9 M 0 -5 L 2 -3 L 1 -2 L 1 8 L 2 11 L 2 14 L 0 16 L -2 15 L -2 16 L 0 16","-9 8 M -4 -10 L -6 -12 L -5 -8 L -5 6 L -6 7 L -4 9 M -4 -10 L -4 6 L -5 7 L -4 8 L -3 7 L -4 6 M -4 -10 L -2 -12 L -3 -8 L -3 6 L -2 7 L -4 9 M -3 -2 L 0 -4 L 2 -5 L 4 -2 L 1 0 L -3 3 M 1 -4 L 3 -2 M 0 -4 L 2 -1 M 0 1 L 1 2 L 2 7 L 4 9 L 6 7 M 1 1 L 2 3 L 3 7 L 4 8 M 1 0 L 2 1 L 4 6 L 5 7 L 6 7","-5 5 M 0 -10 L -2 -12 L -1 -8 L -1 6 L -2 7 L 0 9 M 0 -10 L 0 6 L -1 7 L 0 8 L 1 7 L 0 6 M 0 -10 L 2 -12 L 1 -8 L 1 6 L 2 7 L 0 9","-13 13 M -11 -3 L -10 -3 L -9 -2 L -9 6 L -10 7 L -8 9 M -9 -4 L -8 -3 L -8 6 L -9 7 L -8 8 L -7 7 L -8 6 M -11 -3 L -9 -5 L -7 -3 L -7 6 L -6 7 L -8 9 M -7 -3 L -4 -4 L -2 -5 L 1 -3 L 1 6 L 2 7 L 0 9 M -2 -4 L 0 -3 L 0 6 L -1 7 L 0 8 L 1 7 L 0 6 M -4 -4 L -1 -2 L -1 6 L -2 7 L 0 9 M 1 -3 L 4 -4 L 6 -5 L 9 -3 L 9 6 L 10 7 L 8 9 M 6 -4 L 8 -3 L 8 6 L 7 7 L 8 8 L 9 7 L 8 6 M 4 -4 L 7 -2 L 7 6 L 6 7 L 8 9","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 6 L -6 7 L -4 9 M -5 -4 L -4 -3 L -4 6 L -5 7 L -4 8 L -3 7 L -4 6 M -7 -3 L -5 -5 L -3 -3 L -3 6 L -2 7 L -4 9 M -3 -3 L 0 -4 L 2 -5 L 5 -3 L 5 6 L 6 7 L 4 9 M 2 -4 L 4 -3 L 4 6 L 3 7 L 4 8 L 5 7 L 4 6 M 0 -4 L 3 -2 L 3 6 L 2 7 L 4 9","-8 8 M -5 -2 L -5 6 L -2 9 L 3 7 L 5 6 M -4 -2 L -4 6 L -2 8 M -3 -3 L -3 5 L 0 8 M -5 -2 L -3 -3 L 2 -5 L 5 -2 L 5 6 M 2 -4 L 4 -2 L 4 6 M 0 -4 L 3 -1 L 3 7","-9 8 M -6 -5 L -5 -3 L -5 6 L -7 7 L -5 7 L -5 13 L -6 16 L -4 14 M -4 -3 L -4 14 M -6 -5 L -4 -4 L -3 -3 L -3 6 L -1 7 L 0 8 M -4 7 L -3 7 L -1 8 M -3 8 L -2 9 L 3 7 L 5 6 M -3 8 L -3 13 L -2 16 L -4 14 M -3 -3 L 0 -4 L 2 -5 L 5 -2 L 5 6 M 2 -4 L 4 -2 L 4 6 M 0 -4 L 3 -1 L 3 7","-8 9 M -5 -2 L -5 6 L -2 9 L 3 7 M -4 -2 L -4 6 L -2 8 M -3 -3 L -3 5 L 0 8 M -5 -2 L -3 -3 L 2 -5 L 5 -2 L 5 13 L 6 16 L 4 14 M 2 -4 L 4 -2 L 4 14 M 0 -4 L 3 -1 L 3 13 L 2 16 L 4 14","-7 6 M -5 -3 L -4 -3 L -3 -2 L -3 6 L -4 7 L -2 9 M -3 -4 L -2 -3 L -2 6 L -3 7 L -2 8 L -1 7 L -2 6 M -5 -3 L -3 -5 L -1 -3 L -1 6 L 0 7 L -2 9 M -1 -3 L 3 -5 L 5 -3 L 3 -2 L 1 -4 M 2 -4 L 4 -3","-8 8 M -5 -2 L -5 1 L -3 3 L 3 0 L 5 2 L 5 6 M -4 -2 L -4 1 L -3 2 M -3 -3 L -3 1 L -2 2 M 3 1 L 4 2 L 4 6 M 2 1 L 3 2 L 3 7 M -5 -2 L 1 -5 L 4 -4 L 2 -3 L -1 -4 M 0 -4 L 3 -4 M 5 6 L -1 9 L -5 7 L -3 6 L 1 8 M -3 7 L -1 8","-5 5 M 0 -10 L -2 -12 L -1 -8 L -1 6 L -2 7 L 0 9 M 0 -10 L 0 6 L -1 7 L 0 8 L 1 7 L 0 6 M 0 -10 L 2 -12 L 1 -8 L 1 6 L 2 7 L 0 9 M -4 -5 L -1 -5 M 1 -5 L 4 -5","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 7 L -2 9 L 3 7 M -5 -4 L -4 -3 L -4 7 L -2 8 M -7 -3 L -5 -5 L -3 -3 L -3 6 L 0 8 M 4 -5 L 6 -3 L 5 -2 L 5 6 L 6 7 L 7 7 M 4 -2 L 5 -3 L 4 -4 L 3 -3 L 4 -2 L 4 7 L 5 8 M 4 -5 L 2 -3 L 3 -2 L 3 7 L 5 9 L 7 7","-9 9 M -6 -5 L -5 -3 L -5 6 L -1 9 L 1 7 L 5 5 M -5 -4 L -4 -3 L -4 6 L -1 8 M -6 -5 L -4 -4 L -3 -3 L -3 5 L 0 7 L 1 7 M 4 -5 L 6 -3 L 5 -2 L 5 5 M 4 -2 L 5 -3 L 4 -4 L 3 -3 L 4 -2 L 4 5 M 4 -5 L 2 -3 L 3 -2 L 3 6","-13 13 M -10 -5 L -9 -3 L -9 6 L -5 9 L -3 7 L -1 6 M -9 -4 L -8 -3 L -8 6 L -5 8 M -10 -5 L -8 -4 L -7 -3 L -7 5 L -4 7 L -3 7 M 0 -5 L -2 -3 L -1 -2 L -1 6 L 3 9 L 5 7 L 9 5 M 0 -2 L 1 -3 L 0 -4 L -1 -3 L 0 -2 L 0 6 L 3 8 M 0 -5 L 2 -3 L 1 -2 L 1 5 L 4 7 L 5 7 M 8 -5 L 10 -3 L 9 -2 L 9 5 M 8 -2 L 9 -3 L 8 -4 L 7 -3 L 8 -2 L 8 5 M 8 -5 L 6 -3 L 7 -2 L 7 6","-9 9 M -6 -3 L -4 -2 L 3 8 L 4 9 L 6 7 M -5 -4 L -3 -3 L 3 7 L 5 8 M -6 -3 L -4 -5 L -3 -4 L 4 6 L 6 7 M 6 -5 L 4 -5 L 4 -3 L 6 -3 L 6 -5 L 4 -3 L 1 1 M -1 3 L -4 7 L -6 9 L -4 9 L -4 7 L -6 7 L -6 9 M -4 2 L -1 2 M 1 2 L 4 2","-9 9 M -7 -3 L -6 -3 L -5 -2 L -5 7 L -2 9 L 3 7 M -5 -4 L -4 -3 L -4 7 L -2 8 M -7 -3 L -5 -5 L -3 -3 L -3 6 L 0 8 M 4 -5 L 6 -3 L 5 -2 L 5 11 L 4 13 L 3 14 L 1 15 L -1 15 L -3 14 L -5 15 L -3 16 L -1 15 M 4 -2 L 5 -3 L 4 -4 L 3 -3 L 4 -2 L 4 12 L 3 13 M -2 15 L -4 15 M 4 -5 L 2 -3 L 3 -2 L 3 12 L 2 14 L 1 15","-6 9 M 0 -4 L -3 -2 L -3 -3 L 0 -4 L 2 -5 L 5 -3 L 5 1 L 0 3 M 2 -4 L 4 -3 L 4 1 M 0 -4 L 3 -2 L 3 1 L 2 2 M 0 3 L 5 5 L 5 11 L 4 13 L 3 14 L 1 15 L -1 15 L -3 14 L -5 15 L -3 16 L -1 15 M 4 5 L 4 12 L 3 13 M -2 15 L -4 15 M 2 4 L 3 5 L 3 12 L 2 14 L 1 15","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 5 M 0 -12 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -4 -12 L -4 -5 M 4 -12 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -6 9 L -8 8 L -9 7 L -10 5 L -10 3 L -9 1 L -8 0 L -1 -4 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 7 9 L 9 9 L 10 8 L 10 7","-5 5 M 0 -10 L -1 -11 L 0 -12 L 1 -11 L 1 -9 L 0 -7 L -1 -6","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9 M -5 2 L 5 2","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 M -7 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -7 9","-10 10 M -7 -12 L 7 9 M -7 9 L 7 -12","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9 M -8 9 L 8 9","-10 9 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2 M -6 9 L 7 9","-10 10 M 0 -12 L 0 9 M -2 -7 L -5 -6 L -6 -5 L -7 -3 L -7 0 L -6 2 L -5 3 L -2 4 L 2 4 L 5 3 L 6 2 L 7 0 L 7 -3 L 6 -5 L 5 -6 L 2 -7 L -2 -7","-10 7 M -6 -12 L -6 9 M -6 -12 L 6 -12","-11 11 M -7 -12 L -7 9 M 7 -12 L 7 9 M -7 -2 L 7 -2","-4 4 M 0 -12 L 0 9","-2 3 M 0 -1 L 0 0 L 1 0 L 1 -1 L 0 -1","-11 10 M -7 -12 L -7 9 M 7 -12 L -7 2 M -2 -3 L 7 9","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9","-12 12 M -8 -12 L -8 9 M -8 -12 L 0 9 M 8 -12 L 0 9 M 8 -12 L 8 9","-11 11 M -7 -12 L -7 9 M -7 -12 L 7 9 M 7 -12 L 7 9","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12","-11 11 M -7 -12 L -7 9 M 7 -12 L 7 9 M -7 -12 L 7 -12","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M -3 -2 L 3 -2","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 L -7 -1","-9 9 M -7 -12 L 0 -2 L -7 9 M -7 -12 L 7 -12 M -7 9 L 7 9","-8 8 M 0 -12 L 0 9 M -7 -12 L 7 -12","-9 9 M -7 -7 L -7 -9 L -6 -11 L -5 -12 L -3 -12 L -2 -11 L -1 -9 L 0 -5 L 0 9 M 7 -7 L 7 -9 L 6 -11 L 5 -12 L 3 -12 L 2 -11 L 1 -9 L 0 -5","-7 7 M -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -4 L 1 -4 L 3 -5 L 4 -7 L 4 -9 L 3 -11 L 1 -12 L -1 -12","-10 10 M -7 9 L -3 9 L -6 2 L -7 -2 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 -2 L 6 2 L 3 9 L 7 9","-9 9 M -7 -12 L 7 -12 M -3 -2 L 3 -2 M -7 9 L 7 9","-11 11 M 0 -12 L 0 9 M -9 -6 L -8 -6 L -7 -5 L -6 -1 L -5 1 L -4 2 L -1 3 L 1 3 L 4 2 L 5 1 L 6 -1 L 7 -5 L 8 -6 L 9 -6","-10 10 M 7 -12 L -7 9 M -7 -12 L 7 -12 M -7 9 L 7 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-8 8 M 0 -14 L -8 0 M 0 -14 L 8 0","-9 9 M -9 16 L 9 16","-4 4 M 1 -7 L -1 -5 L -1 -3 L 0 -2 L 1 -3 L 0 -4 L -1 -3","-10 11 M -1 -5 L -3 -4 L -5 -2 L -6 0 L -7 3 L -7 6 L -6 8 L -4 9 L -2 9 L 0 8 L 3 5 L 5 2 L 7 -2 L 8 -5 M -1 -5 L 1 -5 L 2 -4 L 3 -2 L 5 6 L 6 8 L 7 9 L 8 9","-9 10 M 3 -12 L 1 -11 L -1 -9 L -3 -5 L -4 -2 L -5 2 L -6 8 L -7 16 M 3 -12 L 5 -12 L 7 -10 L 7 -7 L 6 -5 L 5 -4 L 3 -3 L 0 -3 M 0 -3 L 2 -2 L 4 0 L 5 2 L 5 5 L 4 7 L 3 8 L 1 9 L -1 9 L -3 8 L -4 7 L -5 4","-9 9 M -7 -5 L -5 -5 L -3 -3 L 3 14 L 5 16 L 7 16 M 8 -5 L 7 -3 L 5 0 L -5 11 L -7 14 L -8 16","-9 9 M 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 2 8 L 4 6 L 5 3 L 5 0 L 4 -3 L 2 -5 L 0 -7 L -1 -9 L -1 -11 L 0 -12 L 2 -12 L 4 -11 L 6 -9","-8 8 M 5 -3 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -3 -2 L -2 0 L 1 1 M 1 1 L -3 2 L -5 4 L -5 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6","-11 11 M -3 -4 L -5 -3 L -7 -1 L -8 2 L -8 5 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 5 6 L 7 3 L 8 0 L 8 -3 L 6 -5 L 4 -5 L 2 -3 L 0 1 L -2 6 L -5 16","-9 10 M -8 -2 L -6 -4 L -4 -5 L -3 -5 L -1 -4 L 0 -3 L 1 0 L 1 4 L 0 9 M 8 -5 L 7 -2 L 6 0 L 0 9 L -2 13 L -3 16","-10 10 M -9 -1 L -8 -3 L -6 -5 L -4 -5 L -3 -4 L -3 -2 L -4 2 L -6 9 M -4 2 L -2 -2 L 0 -4 L 2 -5 L 4 -5 L 6 -3 L 6 0 L 5 5 L 2 16","-6 5 M 0 -5 L -2 2 L -3 6 L -3 8 L -2 9 L 0 9 L 2 7 L 3 5","-11 11 M -7 -7 L 7 7 M 7 -7 L -7 7","-9 9 M -3 -5 L -7 9 M 7 -4 L 6 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 L -4 1 M -4 1 L -2 2 L -1 3 L 1 8 L 2 9 L 3 9 L 4 8","-8 8 M -7 -12 L -5 -12 L -3 -11 L -2 -10 L 6 9 M 0 -5 L -6 9","-10 11 M -3 -5 L -9 16 M -4 -1 L -5 4 L -5 7 L -3 9 L -1 9 L 1 8 L 3 6 L 5 2 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5","-9 9 M -6 -5 L -3 -5 L -4 1 L -5 6 L -6 9 M 7 -5 L 6 -2 L 5 0 L 3 3 L 0 6 L -3 8 L -6 9","-8 9 M 0 -5 L -2 -4 L -4 -2 L -5 1 L -5 4 L -4 7 L -3 8 L -1 9 L 1 9 L 3 8 L 5 6 L 6 3 L 6 0 L 5 -3 L 4 -4 L 2 -5 L 0 -5","-11 11 M -2 -5 L -6 9 M 3 -5 L 4 1 L 5 6 L 6 9 M -9 -2 L -7 -4 L -4 -5 L 9 -5","-11 10 M -10 -1 L -9 -3 L -7 -5 L -5 -5 L -4 -4 L -4 -2 L -5 3 L -5 6 L -4 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 3 L 5 0 L 6 -5 L 6 -8 L 5 -11 L 3 -12 L 1 -12 L 0 -10 L 0 -8 L 1 -5 L 3 -2 L 5 0 L 8 2","-9 9 M -5 1 L -5 4 L -4 7 L -3 8 L -1 9 L 1 9 L 3 8 L 5 6 L 6 3 L 6 0 L 5 -3 L 4 -4 L 2 -5 L 0 -5 L -2 -4 L -4 -2 L -5 1 L -9 16","-9 11 M 9 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 2 8 L 4 6 L 5 3 L 5 0 L 4 -3 L 3 -4 L 1 -5","-10 10 M 1 -5 L -2 9 M -8 -2 L -6 -4 L -3 -5 L 8 -5","-10 10 M -9 -1 L -8 -3 L -6 -5 L -4 -5 L -3 -4 L -3 -2 L -5 4 L -5 7 L -3 9 L -1 9 L 2 8 L 4 6 L 6 2 L 7 -2 L 7 -5","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 0 L 9 0 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-12 11 M -4 -5 L -6 -4 L -8 -1 L -9 2 L -9 5 L -8 8 L -7 9 L -5 9 L -3 8 L -1 5 M 0 1 L -1 5 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 L 8 2 L 8 -1 L 7 -4 L 6 -5","-8 8 M 2 -12 L 0 -11 L -1 -10 L -1 -9 L 0 -8 L 3 -7 L 6 -7 M 3 -7 L 0 -6 L -2 -5 L -3 -3 L -3 -1 L -1 1 L 2 2 L 4 2 M 2 2 L -2 3 L -4 4 L -5 6 L -5 8 L -3 10 L 1 12 L 2 13 L 2 15 L 0 16 L -2 16","-12 11 M 4 -12 L -4 16 M -11 -1 L -10 -3 L -8 -5 L -6 -5 L -5 -4 L -5 -2 L -6 3 L -6 6 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 8 -2 L 9 -5","-8 7 M 2 -12 L 0 -11 L -1 -10 L -1 -9 L 0 -8 L 3 -7 L 6 -7 M 6 -7 L 2 -5 L -1 -3 L -4 0 L -5 3 L -5 5 L -4 7 L -2 9 L 1 11 L 2 13 L 2 15 L 1 16 L -1 16 L -2 14","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 9 L -11 8 L -10 7 L -9 8 L -9 10 L -10 12 L -11 13","-14 13 M -10 -3 L -8 -1 L 8 -2 L 10 -1 M -9 -2 L 10 -1","-14 13 M -5 -10 L -7 -11 L -7 10 M -6 -10 L -6 10 M -6 8 L 6 8 M -6 -10 L 6 -10 M 5 -10 L 6 -11 L 8 -10 L 7 -8 M 6 -10 L 6 10 M 7 -10 L 7 10 M -6 -1 L 6 -1","-14 13 M -7 -11 L -9 -12 L -9 9 M -8 -11 L -8 9 M -8 -11 L 7 -11 M 6 -11 L 7 -12 L 9 -11 L 8 -9 M 7 -11 L 7 5 L 6 7 L 5 8 M 7 6 L 7 7 L 6 8 M 8 -11 L 8 7 L 7 9 L 6 9 L 5 8 L 3 7 M -1 -11 L -1 -2 M 0 -11 L 0 -2 M -8 -2 L 7 -2","-14 13 M -6 -12 L -8 -7 L -11 -1 M -6 -12 L -5 -11 L -6 -9 L -9 -4 L -11 -1 M -6 -9 L 10 -9 L 8 -11 L 6 -9 M 6 -9 L 9 -10 M 0 -9 L 0 10 M 1 -9 L 1 10 M -4 -3 L -6 -4 L -6 3 M -5 -3 L -5 3 M -5 -3 L 9 -3 L 7 -5 L 5 -3 M 5 -3 L 8 -4 M -11 3 L 11 3 L 9 1 L 7 3 M 7 3 L 10 2","-14 13 M -1 -12 L -1 -6 L -2 -1 L -4 3 L -6 6 L -8 8 L -11 10 M 0 -11 L 0 -7 M -1 -12 L 1 -11 L 0 -4 L -1 0 L -3 4 L -6 7 L -9 9 L -11 10 M -11 -5 L 11 -5 L 9 -7 L 7 -5 M 7 -5 L 10 -6 M 1 -5 L 2 -1 L 3 2 L 4 4 L 6 7 L 9 10 L 11 9 M 7 7 L 9 9 M 1 -5 L 2 -2 L 4 2 L 6 5 L 8 7 L 11 9","-14 13 M -8 -11 L -10 -12 L -10 10 M -9 -11 L -9 10 M -9 -11 L 9 -11 M 8 -11 L 9 -12 L 11 -11 L 10 -9 M 9 -11 L 9 10 M 10 -11 L 10 10 M -7 -7 L 7 -7 L 6 -8 L 5 -7 M -1 -7 L -1 5 M 0 -7 L 0 5 M -6 -1 L 6 -1 L 5 -2 L 4 -1 M -7 5 L 7 5 L 6 4 L 5 5 M 2 1 L 3 3 L 4 3 L 4 2 L 2 1 M -9 9 L 9 9","-14 13 M -1 -12 L -1 -6 L -2 -1 L -4 3 L -6 6 L -8 8 L -11 10 M 0 -11 L 0 -7 M -1 -12 L 1 -11 L 0 -4 L -1 0 L -3 4 L -6 7 L -9 9 L -11 10 M 1 -11 L 1 -7 L 2 -1 L 3 2 L 4 4 L 6 7 L 9 10 L 11 9 M 7 7 L 9 9 M 1 -7 L 2 -2 L 4 2 L 6 5 L 8 7 L 11 9","-14 13 M 0 -11 L 1 -12 L -1 -12 L -1 10 M 0 -11 L 0 10 M -11 -10 L 11 -10 L 9 -11 L 8 -10 M -6 -6 L -8 -7 L -8 3 M -7 -6 L -7 3 M -7 -6 L 6 -6 M 5 -6 L 6 -7 L 8 -6 L 7 -4 M 6 -6 L 6 3 M 7 -6 L 7 3 M -7 -2 L 6 -2 M -7 2 L 6 2 M -1 2 L -4 5 L -7 7 L -11 9 M -1 4 L -4 6 L -6 7 L -11 9 M 0 2 L 3 6 L 6 8 L 9 9 L 11 8 M 0 2 L 3 5 L 7 7 L 11 8","-14 13 M 0 -10 L 1 -11 L -1 -12 L -1 11 M 0 -11 L 0 11 M -8 -6 L -10 -7 L -10 4 M -9 -6 L -9 4 M -9 -6 L 8 -6 M 7 -6 L 8 -7 L 10 -6 L 9 -4 M 8 -6 L 8 4 M 9 -6 L 9 4 M -9 3 L 8 3","-14 13 M 0 -10 L 1 -11 L -1 -12 L -1 10 M 0 -11 L 0 10 M -11 -6 L 11 -6 L 9 -8 L 7 -6 M 7 -6 L 10 -7 M -2 -6 L -5 0 L -8 4 L -11 7 M -1 -5 L -3 -1 L -5 2 L -7 4 L -11 7 M 1 -5 L 2 -2 L 3 0 L 6 4 L 9 7 L 11 6 M 7 4 L 9 6 M 1 -5 L 2 -3 L 5 1 L 8 4 L 11 6 M -5 5 L 4 5 L 3 4 L 2 5","-14 13 M 0 -11 L 0 -12 L 1 -13 L -1 -13 L -1 -11 M -11 -11 L 11 -11 L 9 -13 L 7 -11 M 7 -11 L 10 -12 M -6 -7 L -8 -8 L -8 0 M -7 -7 L -7 0 M -7 -7 L 6 -7 M 5 -7 L 6 -8 L 8 -7 L 7 -5 M 6 -7 L 6 0 M 7 -7 L 7 0 M -7 -1 L 6 -1 M -1 -1 L -1 6 L -2 8 L -3 9 M -1 7 L -1 8 L -2 9 M 0 -1 L 0 8 L -1 10 L -2 10 L -3 9 L -5 8 M -6 2 L -11 7 M -6 2 L -5 3 L -11 7 M 4 2 L 6 4 L 8 7 L 9 7 L 9 6 L 8 5 L 4 2","-14 13 M -1 -12 L -1 9 M -1 -12 L 0 -12 L 0 9 M -8 -10 L -8 -2 M -8 -10 L -7 -10 L -7 -2 M -7 -3 L 6 -3 M 6 -10 L 6 -2 M 6 -10 L 7 -10 L 7 -2 M -10 1 L -10 10 M -10 1 L -9 1 L -9 10 M -9 9 L 8 9 M 8 1 L 8 10 M 8 1 L 9 1 L 9 10","-14 13 M -10 -12 L -10 5 M -10 -12 L -9 -11 L -9 5 M -9 -11 L -5 -11 M -5 -11 L -5 4 M -6 -11 L -5 -12 L -4 -11 L -4 4 M -9 -4 L -5 -4 M -9 3 L -5 3 M 3 -11 L 4 -12 L 2 -12 L 2 -4 M 3 -11 L 3 -4 M -2 -8 L 9 -8 L 7 -9 L 6 -8 M -4 -4 L 11 -4 L 9 -5 L 8 -4 M 5 -4 L 5 8 L 4 9 M 6 -4 L 6 8 L 5 10 L 4 9 L 2 8 M -4 0 L 11 0 L 9 -1 L 8 0 M -1 2 L 0 3 L 1 5 L 2 5 L 2 4 L 1 3 L -1 2","-14 13 M 0 -10 L 1 -11 L -1 -12 L -1 9 M 0 -11 L 0 9 M 0 -2 L 9 -2 L 7 -4 L 5 -2 M 5 -2 L 8 -3 M -11 9 L 11 9 L 9 7 L 7 9 M 7 9 L 10 8","-14 13 M -1 -11 L 0 -12 L -2 -12 L -2 -5 M -1 -11 L -1 -5 M -9 -9 L 3 -9 L 2 -10 L 1 -9 M -11 -5 L 11 -5 L 9 -6 L 8 -5 M 6 -11 L 3 -7 L 0 -4 L -4 -1 L -9 2 M 6 -11 L 7 -10 L 2 -5 L -2 -2 L -5 0 L -9 2 L -11 3 M -3 -1 L -5 -2 L -5 10 M -4 -1 L -4 10 M -4 -1 L 6 -1 M 5 -1 L 6 -2 L 8 -1 L 7 1 M 6 -1 L 6 10 M 7 -1 L 7 10 M -4 4 L 6 4 M -4 9 L 6 9","-14 13 M 0 -10 L 1 -11 L -1 -12 L -1 10 M 0 -11 L 0 10 M -11 -3 L 11 -3 L 9 -5 L 7 -3 M 7 -3 L 10 -4","-14 13 M -11 -1 L 11 -1 L 8 -3 L 6 -1 M 6 -1 L 9 -2","-14 13 M -8 -8 L 8 -8 L 6 -10 L 4 -8 M 4 -8 L 7 -9 M -11 6 L 11 6 L 9 4 L 7 6 M 7 6 L 10 5","-14 13 M -9 -9 L 9 -9 L 7 -11 L 5 -9 M 5 -9 L 8 -10 M -7 -1 L 7 -1 L 5 -3 L 3 -1 M 3 -1 L 6 -2 M -11 8 L 11 8 L 9 6 L 7 8 M 7 8 L 10 7","-14 13 M -8 -9 L -10 -10 L -10 8 M -9 -9 L -9 8 M -9 -9 L 9 -9 M 8 -9 L 9 -10 L 11 -9 L 10 -7 M 9 -9 L 9 8 M 10 -9 L 10 8 M -3 -9 L -3 -4 L -4 0 L -5 2 M -2 -9 L -2 -4 L -3 -1 L -5 2 L -7 4 M 1 -9 L 1 1 L 2 2 L 6 2 L 7 1 L 6 0 M 2 -9 L 2 0 L 3 1 L 5 1 L 6 0 L 6 -2 M -9 7 L 9 7","-14 13 M -9 -10 L 8 -10 L 6 -12 L 4 -10 M 4 -10 L 7 -11 M -1 -10 L -4 8 M 0 -10 L -3 8 M -9 -2 L 4 -2 M 3 -2 L 4 -3 L 6 -2 L 5 0 M 4 -2 L 3 8 M 5 -2 L 5 0 L 4 8 M -11 8 L 11 8 L 9 6 L 7 8 M 7 8 L 10 7","-14 13 M 0 -9 L 1 -10 L -1 -11 L -1 -5 M 0 -10 L 0 -5 M -11 -5 L 11 -5 L 9 -7 L 7 -5 M 7 -5 L 10 -6 M -5 -1 L -6 2 L -8 6 L -10 9 M -4 0 L -5 2 M -5 -1 L -3 0 L -5 3 L -8 7 L -10 9 M 2 -1 L 4 1 L 7 5 L 8 7 L 9 8 L 10 7 L 9 5 L 6 2 L 2 -1 M 7 4 L 9 7","-14 13 M -5 -9 L -4 -10 L -6 -11 L -6 8 L -5 9 L 7 9 L 8 8 L 7 6 M 6 8 L 7 8 L 7 7 M -5 -10 L -5 7 L -4 8 L 5 8 L 7 6 L 8 3 M -10 -1 L -4 -2 L 1 -3 L 9 -5 L 6 -6 L 5 -4 M 5 -4 L 7 -5","-14 13 M 0 -6 L -1 -3 L -3 2 L -5 5 L -8 8 L -11 10 M 0 -6 L 1 -5 L 0 -2 L -2 2 L -4 5 L -6 7 L -9 9 L -11 10 M -6 -11 L 3 -11 M 0 -11 L 1 -12 L 3 -11 L 1 -9 M 1 -11 L 1 -6 L 2 -1 L 3 2 L 4 4 L 6 7 L 9 10 L 11 9 M 7 7 L 9 9 M 1 -6 L 2 -2 L 4 2 L 6 5 L 8 7 L 11 9","-14 13 M -4 -9 L -3 -10 L -5 -11 L -5 0 L -6 4 L -8 7 L -11 10 M -4 -10 L -4 0 L -5 4 L -7 7 L -11 10 M -11 -4 L 2 -5 M 1 -5 L 2 -6 L 4 -5 L 3 -3 M 2 -5 L 2 8 L 3 9 L 10 9 L 11 8 L 10 6 M 9 8 L 10 8 L 10 7 M 3 -5 L 3 7 L 4 8 L 8 8 L 10 6 L 11 3","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -3 -12 L -1 -11 L -1 -9 L -2 -6 M -2 -11 L -2 0 L -1 4 M -7 -7 L -5 -6 L 0 -6 L 3 -7 L 6 -8 M 3 -7 L 4 -8 L 6 -8 M 1 1 L 2 -3 L 3 -2 L 1 1 L -2 5 L -4 7 L -6 8 L -8 8 L -9 7 L -9 4 L -8 2 L -6 0 L -4 -1 L -1 -2 L 4 -2 L 7 -1 L 9 1 L 10 3 L 10 5 L 9 7 L 7 9 L 4 10 L 2 10 M -7 8 L -8 7 L -8 4 L -7 2 L -5 0 L -1 -2 M 4 -2 L 6 -1 L 8 1 L 9 3 L 9 5 L 8 7 L 6 9 L 4 10","-14 13 M -10 -10 L -8 -9 L -8 -7 L -9 -5 L -10 -2 M -9 -9 L -9 -7 L -10 -2 L -10 1 L -9 4 L -8 5 L -6 6 L -5 6 L -5 4 L -4 1 L -3 -1 M -8 5 L -6 5 L -5 4 M 5 -6 L 7 -5 L 9 -2 L 10 1 L 10 3 L 8 2 L 5 3 M 9 -2 L 9 1 L 8 2","-14 13 M -4 -13 L -3 -12 L -1 -11 L 3 -11 M -3 -12 L 2 -12 L 3 -11 L -1 -9 M -7 -5 L -7 -4 L -6 -3 L -5 -3 L -2 -5 L 0 -6 M -7 -4 L -6 -4 L -3 -5 L 0 -6 L 2 -6 L 5 -5 L 6 -3 L 6 0 L 5 3 L 3 6 L 0 8 L -4 10 M 2 -6 L 4 -5 L 5 -3 L 5 0 L 4 3 L 3 5 L 1 7 L -2 9","-14 13 M -4 -13 L -3 -12 L -1 -11 L 3 -11 M -3 -12 L 2 -12 L 3 -11 L -1 -9 M -8 -5 L -8 -4 L -7 -3 L -5 -3 L 0 -5 L 3 -6 L 4 -5 M -8 -4 L -5 -4 L 0 -5 M 4 -5 L 0 -2 L -6 4 L -10 8 M 3 -6 L 0 -2 M -10 8 L -10 7 L -6 4 L -4 3 L 0 3 L 1 4 L 2 8 L 3 9 L 10 9 M 5 9 L 8 8 L 9 8 L 10 9","-14 13 M -5 -12 L -3 -11 L -3 -9 L -4 -5 M -4 -11 L -4 9 M -4 2 L -3 8 L -4 9 L -5 7 L -7 6 L -10 5 M -10 -5 L -8 -4 L -4 -4 L -1 -5 L 1 -6 M -4 -4 L -2 -5 L -1 -6 L 1 -6 M -10 6 L -6 3 L -1 0 L 2 -1 L 4 -1 L 7 0 L 8 2 L 8 4 L 7 6 L 5 7 L 3 7 L 1 6 L 0 4 M -10 6 L -10 5 L -6 3 M 4 -1 L 6 0 L 7 2 L 7 4 L 6 6 L 5 7 M 5 -7 L 7 -7 L 9 -6 L 10 -5 L 8 -5 L 7 -4 M 7 -7 L 8 -6 L 8 -5","-14 13 M -4 -12 L -2 -11 L -2 -9 L -3 -6 L -5 -1 L -10 9 M -3 -11 L -3 -9 L -5 -3 L -7 2 L -10 9 M -10 -5 L -10 -4 L -9 -3 L -8 -3 L -5 -5 L -3 -6 L 0 -6 L 2 -5 L 3 -3 L 3 0 L 2 4 L 0 8 L -1 9 L -2 9 L -2 8 L -3 7 L -5 6 M -10 -4 L -8 -4 L -5 -5 M 0 -6 L 2 -4 L 2 0 L 1 4 L 0 6 L -2 8 M 5 -5 L 7 -4 L 9 -2 L 10 0 L 10 2 L 9 1 L 7 2 M 8 -3 L 9 -1 L 9 1","-14 13 M -4 -12 L -1 -11 L 0 -7 L 1 -4 L 3 0 L 6 4 L 5 5 M -2 -11 L 0 -7 M 3 0 L 5 5 M -6 -7 L -4 -6 L -1 -6 L 2 -7 M -9 -3 L -8 -2 L -5 -1 L 0 -1 L 5 -2 L 8 -3 M 5 -2 L 7 -4 L 8 -3 M 5 5 L 3 4 L 0 3 L -4 3 L -6 4 L -7 5 L -7 7 L -6 8 L -4 9 L -1 10 L 5 10 M -4 9 L 4 9 L 5 10","-14 13 M 2 -12 L 4 -11 L 4 -10 L -4 -2 M 3 -11 L 2 -9 L -4 -2 L -4 -1 L 1 4 L 3 7 L 4 9 M -4 -1 L 4 6 L 5 8 L 4 9","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 L -4 -3 L -3 -4 L -1 -5 L 8 -5 L 10 -6 M -9 3 L -8 5 L -7 6 M 4 -5 L 8 -6 L 9 -7 L 10 -6 M 4 -12 L 6 -11 L 6 3 L 5 6 L 3 8 L 0 10 M 5 -11 L 6 -8 M 6 0 L 5 4 L 3 7 L 0 10","-14 13 M -5 -10 L -3 -9 L 0 -8 L 2 -8 L 5 -9 M -3 -9 L 2 -9 L 4 -10 L 5 -9 L -1 -6 M -7 1 L -7 3 L -6 5 L -4 6 L 0 7 L 7 7 M -4 6 L 6 6 L 7 7","-14 13 M -8 -7 L -7 -6 L -4 -5 L 0 -5 L 4 -6 L 6 -7 L 7 -8 M 4 -6 L 6 -8 L 7 -8 M -2 -12 L -1 -9 L 0 -7 L 3 -3 L 5 0 L 6 2 M -2 -12 L -1 -12 L -1 -10 L 0 -7 M 3 -3 L 6 0 L 7 2 L 7 3 M 7 3 L 6 2 L 4 1 L 0 0 L -4 0 L -7 1 L -8 3 L -8 5 L -7 7 L -5 8 L -1 9 L 3 9 M -8 5 L -7 6 L -5 7 L -1 8 L 4 8 L 3 9","-14 13 M -6 -12 L -4 -11 L -5 1 L -5 6 L -4 8 M -5 -11 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 L 1 9 L 4 8 L 7 6 L 9 4","-14 13 M 0 -12 L 2 -11 L 2 1 L 1 3 L -1 4 L -3 3 L -4 1 L -4 0 L -3 -2 L -1 -3 L 1 -2 L 2 0 L 2 4 L 1 8 L -1 10 M 1 -11 L 2 -6 M 2 4 L 1 7 L -1 10 M -10 -8 L -8 -6 L -5 -6 L 4 -7 L 8 -8 L 10 -7 M -9 -7 L -5 -6 M 4 -7 L 10 -7","-14 13 M -6 -8 L -4 -7 L -4 4 L -3 6 L -2 7 L 0 8 L 8 8 M -5 -7 L -4 -4 M 3 8 L 6 7 L 7 7 L 8 8 M 3 -11 L 5 -10 L 5 2 L 4 4 L 3 3 L 0 1 M 4 -10 L 5 -7 M 5 -1 L 4 2 L 3 3 M -10 -4 L -8 -2 L -6 -2 L -2 -3 L 3 -4 L 7 -5 L 9 -5 L 10 -4 M -9 -3 L -2 -3 M 3 -4 L 10 -4","-14 13 M -4 -11 L -2 -9 L 4 -11 M -3 -10 L -1 -10 L 4 -11 L 5 -10 M 5 -10 L 2 -7 L -4 -3 L -8 -1 M 4 -11 L 3 -9 L 1 -7 L -4 -3 M -9 -2 L -7 0 L -3 -2 M -8 -1 L -3 -2 L 3 -3 L 8 -4 L 9 -3 M 3 -3 L 9 -3 M 3 -3 L 1 -2 L -1 0 L -2 2 L -2 4 L -1 6 L 0 7 L 3 8 L 7 8 M -1 6 L 2 7 L 6 7 L 7 8","-14 13 M -4 -12 L -2 -11 L -2 -9 L -4 -3 L -5 0 L -7 5 L -9 9 M -3 -11 L -3 -8 L -4 -3 M -5 0 L -7 4 L -9 7 L -9 9 M -10 -5 L -8 -4 L -5 -4 L -2 -5 L 0 -6 L 2 -7 M 0 -6 L 1 -8 L 2 -7 M 3 -4 L 6 -4 L 9 -3 L 7 -3 L 5 -2 M 6 -4 L 7 -3 M 0 4 L 1 6 L 2 7 L 4 8 L 9 8 M 1 6 L 3 7 L 10 7 L 9 8","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 -4 L -6 0 L -6 2 L -5 3 M -4 -11 L -4 -7 M -5 -4 L -5 3 M -10 -8 L -7 -7 L -3 -7 L 1 -8 L 3 -9 M 1 -8 L 2 -10 L 3 -9 M -5 3 L -3 1 L 0 -1 L 3 -2 L 6 -2 L 8 -1 L 9 1 L 9 4 L 8 6 L 5 8 L 1 9 L -3 9 M 6 -2 L 7 -1 L 8 1 L 8 4 L 7 6 L 5 8","-14 13 M -10 -6 L -8 -4 L -3 -6 L 1 -7 L 5 -7 L 8 -6 L 9 -5 L 10 -3 L 10 0 L 9 2 L 8 3 L 6 4 L 3 5 L -2 6 M -9 -5 L -3 -6 M 5 -7 L 7 -6 L 8 -5 L 9 -3 L 9 0 L 8 2 L 6 4","-14 13 M -10 -9 L -8 -7 L -6 -7 L -2 -8 L 3 -9 L 7 -10 L 9 -10 L 10 -9 M -9 -8 L -2 -8 M 3 -9 L 10 -9 M 7 -9 L 4 -8 L 1 -6 L -1 -3 L -2 0 L -2 3 L -1 6 L 0 7 L 2 8 L 6 9 L 9 9 M -2 3 L -1 5 L 0 6 L 2 7 L 6 8 L 8 8 L 9 9","-14 13 M -3 -12 L -1 -11 L -1 -1 M -2 -11 L -2 -5 L -1 -1 M 5 -4 L 7 -2 L -1 -1 M 6 -3 L 2 -2 L -1 -1 L -4 0 L -6 1 L -7 3 L -7 5 L -6 7 L -5 8 L -3 9 L 8 9 M -6 7 L -4 8 L 7 8 L 8 9","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 -3 L -6 0 L -8 5 L -10 9 M -4 -11 L -4 -8 L -5 -3 M -6 0 L -8 4 L -10 7 L -10 9 M -10 -5 L -8 -4 L -6 -4 L -3 -5 L -1 -6 L 1 -7 M -1 -6 L 0 -8 L 1 -7 M 4 -8 L 6 -8 L 8 -7 L 10 -5 L 8 -6 L 6 -5 M 6 -8 L 8 -6 M 4 -3 L 4 -1 L 5 2 L 6 4 L 6 6 L 5 8 L 3 9 L 0 9 L -2 8 L -3 6 L -2 4 L 0 3 L 3 3 L 5 4 L 9 8 M 4 -1 L 5 4 L 5 7 L 3 9 M 7 6 L 8 8 L 9 8","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M 1 -8 L 4 -9 L 7 -9 L 9 -8 L 7 -8 L 5 -7 M 5 -9 L 7 -8 M -1 1 L -1 3 L 0 5 L 3 6 L 10 6 M -1 3 L 0 4 L 3 5 L 8 5 L 10 6","-14 13 M -8 -9 L -6 -8 L -6 -3 M -7 -8 L -6 -3 L -5 0 L -4 2 L -3 4 L -2 5 M -4 2 L -2 4 L -2 5 M 0 -11 L 2 -10 L 2 -8 L 0 -3 L -3 4 L -5 7 L -7 8 L -9 8 L -10 6 L -10 4 L -9 1 L -8 -1 L -6 -3 L -3 -5 L 1 -6 L 4 -6 L 7 -5 L 9 -3 L 10 0 L 10 3 L 9 6 L 8 7 L 6 8 L 4 8 L 2 7 L 1 6 L 1 5 L 2 4 L 4 3 L 6 3 L 8 4 L 10 7 M 1 -10 L 1 -8 L 0 -3 M -8 8 L -9 6 L -9 3 L -8 0 L -6 -3 M 4 -6 L 6 -5 L 8 -3 L 9 0 L 9 4 L 8 7 M 8 4 L 10 6 L 10 7","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 9 M -4 -11 L -4 9 L -5 9 L -6 6 L -7 5 L -9 4 M -10 -6 L -9 -4 L -7 -4 L -3 -6 L -3 -5 M -9 -5 L -6 -5 L -4 -6 L -5 -2 M -3 -5 L -5 -2 L -7 1 L -10 5 L -10 4 L -7 1 L -3 -3 L 1 -6 L 4 -7 L 6 -7 L 8 -6 L 9 -4 L 9 3 L 8 6 L 7 7 L 5 8 L 3 8 L 1 7 L 0 6 L 0 5 L 1 4 L 3 3 L 5 3 L 7 4 L 8 5 L 10 8 M 6 -7 L 7 -6 L 8 -4 L 8 4 L 7 7 M 8 5 L 10 7 L 10 8","-14 13 M -1 -10 L 0 -8 L 0 -5 L -1 -2 L -2 0 L -4 3 L -6 5 L -8 5 L -9 4 L -10 2 L -10 -1 L -9 -4 L -7 -7 L -4 -9 L -1 -10 L 2 -10 L 5 -9 L 7 -8 L 9 -6 L 10 -3 L 10 0 L 9 3 L 7 5 L 5 6 L 2 7 L -1 7 M -4 3 L -6 4 L -8 4 L -9 2 L -9 -1 L -8 -5 L -7 -7 M 5 -9 L 8 -6 L 9 -3 L 9 0 L 8 3 L 5 6","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M 3 -11 L 5 -10 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -10 L 5 -6 M 4 3 L 9 6 L 9 7 M -2 -5 L 0 -4 L 3 -4 L 7 -5 L 9 -6 M 7 -5 L 8 -7 L 9 -6","-14 13 M -9 -11 L -8 -10 L -6 -9 L -2 -9 M -8 -10 L -3 -10 L -2 -9 M -2 -9 L -5 -7 L -7 -5 L -9 -2 L -10 1 L -10 4 L -9 6 L -8 7 L -6 8 L -3 8 L 0 7 L 2 5 L 3 3 L 4 -1 L 4 -5 L 3 -9 L 4 -9 M -10 3 L -9 5 L -8 6 L -6 7 L -3 7 L 0 6 L 2 4 L 3 2 L 4 -1 M 4 -9 L 6 -6 L 7 -4 L 9 -1 L 10 1 L 8 1 L 7 2 M 3 -9 L 6 -6 M 7 -4 L 8 -1 L 8 1","-14 13 M -2 -12 L -1 -9 L 1 -8 M -1 -10 L 1 -9 L 2 -9 L 2 -8 M 2 -8 L -1 -7 L -3 -6 L -4 -5 L -4 -3 L -3 -2 L -1 -1 L 1 0 L 2 1 L 3 3 L 3 5 L 2 7 L 0 8 L -2 8 L -5 7 L -7 5 M -1 -1 L 1 1 L 2 3 L 2 5 L 1 7 L 0 8 M -9 7 L -6 4 L -3 2 L 0 1 M 4 1 L 7 2 L 9 4 L 10 6 L 7 6 M -9 7 L -9 6 L -6 4 M 4 1 L 6 2 L 8 4 L 9 6","-14 13 M -10 -2 L -8 0 L -5 -5 M -9 -1 L -4 -6 L -2 -6 L 3 -1 L 7 2 L 9 3 L 10 4 L 8 4 L 6 5 M 3 -1 L 8 4","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M -1 -11 L 1 -10 L 3 -10 L 7 -11 M 3 -10 L 5 -11 L 6 -12 L 7 -11 M -2 -3 L 1 -2 L 4 -2 L 7 -3 L 9 -4 M 7 -3 L 8 -5 L 9 -4 M 3 -10 L 5 -9 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -9 L 5 -5 M 4 3 L 9 6 L 9 7","-14 13 M 0 -12 L 2 -11 L 2 6 L 1 8 L -1 9 L -3 9 L -5 8 L -6 6 L -5 4 L -3 3 L -1 3 L 2 4 L 5 6 L 8 9 M 1 -11 L 2 -7 M 5 6 L 8 8 L 8 9 M -10 -9 L -8 -7 L 1 -7 L 8 -8 M -9 -8 L -4 -7 M 1 -7 L 5 -8 L 7 -9 L 8 -8 M -5 -2 L -3 -1 L 1 -1 L 5 -2 L 7 -3 M 5 -2 L 6 -4 L 7 -3","-14 13 M -8 -11 L -7 -9 L -5 -9 L -2 -10 L 1 -12 L 2 -11 M -7 -10 L -2 -10 M 2 -11 L -2 -5 L -5 1 L -7 4 L -8 5 L -9 5 L -10 4 L -10 2 L -9 0 L -7 -1 L -2 -1 L 2 0 L 5 1 L 10 3 M 1 -12 L -2 -5 M 2 0 L 10 2 L 10 3 M 4 -5 L 6 -4 L 6 2 L 5 5 L 3 7 L 1 8 L -2 9 M 5 -4 L 5 3 L 4 6","-14 13 M -5 -12 L -3 -11 L -3 -9 L -4 -5 L -5 -1 L -6 2 L -7 4 L -8 5 L -9 5 L -10 4 L -10 2 L -9 0 L -8 -1 L -7 -1 L -6 0 L -6 5 L -5 8 L -3 9 L 3 9 L 6 8 L 7 7 L 6 6 L 5 4 L 4 0 L 4 -4 L 5 -5 L 7 -5 L 9 -4 L 10 -2 L 10 -1 L 9 -1 L 7 0 M -4 -11 L -4 -5 M -6 5 L -5 7 L -3 8 L 3 8 L 5 7 L 6 6 M 7 -5 L 9 -3 L 9 -1 M -10 -7 L -7 -6 L -4 -6 L -1 -7 L 1 -8 M -1 -7 L 0 -9 L 1 -8","-14 13 M -8 -9 L -6 -8 L -6 -3 M -7 -8 L -6 -3 L -5 0 L -4 2 L -3 4 L -2 5 M -4 2 L -2 4 L -2 5 M 1 -11 L 3 -10 L 3 -8 L 2 -5 L 0 0 L -1 2 L -3 5 L -5 7 L -7 8 L -9 8 L -10 7 L -10 5 L -9 2 L -8 0 L -6 -2 L -3 -4 L 0 -5 L 4 -5 L 7 -4 L 9 -2 L 10 1 L 10 4 L 9 6 L 8 7 L 6 8 L 3 9 L 0 9 M 2 -10 L 2 -8 L 0 0 M -8 8 L -9 7 L -9 4 L -8 1 L -6 -2 M 4 -5 L 6 -4 L 8 -2 L 9 1 L 9 4 L 8 6 L 6 8","-14 13 M -3 -12 L -1 -11 L -1 -9 L -2 -5 M -2 -11 L -2 2 L -1 6 L 0 8 L 2 9 L 6 9 L 8 8 L 9 6 L 9 4 L 8 2 L 6 0 L 3 -2 M 5 9 L 7 8 L 8 6 L 8 3 L 7 1 M -6 -8 L -5 -7 L -2 -6 L 1 -6 M -5 -7 L 0 -7 L 1 -6 M 1 -6 L -2 -5 L -4 -4 L -6 -2 L -7 0 L -6 2 L -4 3 L 1 3","-14 13 M -2 -12 L 0 -11 L 1 -10 L 2 -8 L 1 -7 L -2 -8 L -5 -8 L -6 -7 L -6 -5 L -3 2 L -1 9 M 0 -11 L 1 -9 L 1 -7 M -3 2 L 0 8 L -1 9 M -10 -4 L -9 -2 L -7 -2 L -4 -4 L 0 -6 L 4 -7 L 7 -7 L 9 -6 L 10 -4 L 10 -2 L 9 0 L 8 1 L 5 2 L 2 2 L -1 1 M -9 -3 L -7 -3 L -4 -4 M 7 -7 L 8 -6 L 9 -4 L 9 -2 L 8 0 L 7 1 L 5 2","-14 13 M -10 -10 L -8 -9 L -8 -7 L -9 -5 L -10 -2 M -9 -9 L -9 -7 L -10 -2 L -10 1 L -9 4 L -8 5 L -6 6 L -5 6 L -5 4 L -4 1 L -3 -1 M -8 5 L -6 5 L -5 4 M 5 -6 L 7 -5 L 9 -2 L 10 1 L 10 3 L 8 2 L 5 3 M 9 -2 L 9 1 L 8 2","-14 13 M -10 -8 L -8 -7 L -8 -5 L -9 -2 M -9 -7 L -9 2 L -8 5 L -7 6 L -7 4 L -6 1 L -5 -1 L -3 -4 L 0 -7 L 3 -8 L 6 -8 L 8 -7 L 9 -6 L 10 -4 L 10 -1 L 9 1 L 7 3 L 5 4 L 2 4 L 0 3 L -2 1 L -3 -2 L -3 -5 L -2 -9 L -1 -11 L 1 -12 L 3 -12 L 4 -10 L 4 1 L 3 5 L 2 7 L -1 9 M -9 2 L -8 4 L -7 4 M 8 -7 L 9 -4 L 9 -1 L 8 2 M 4 1 L 3 4 L 2 6 L -1 9","-14 13 M -4 -13 L -3 -12 L -1 -11 L 3 -11 M -3 -12 L 2 -12 L 3 -11 L -1 -9 M -8 -5 L -8 -4 L -7 -3 L -5 -3 L 0 -5 L 3 -6 L 4 -5 M -8 -4 L -5 -4 L 0 -5 M 4 -5 L 0 -2 L -6 4 L -10 8 M 3 -6 L 0 -2 M -10 8 L -10 7 L -6 4 L -4 3 L 0 3 L 1 4 L 2 8 L 3 9 L 10 9 M 5 9 L 8 8 L 9 8 L 10 9","-14 13 M -2 -12 L 1 -11 L 1 -9 L 0 -4 M 0 -11 L 0 6 L -1 8 L -3 9 L -6 9 L -8 8 L -9 7 L -9 5 L -8 4 L -6 3 L -3 3 L 0 4 L 4 6 L 9 9 M 0 4 L 5 6 L 9 8 L 9 9 M 0 -4 L 3 -4 L 7 -5 M 3 -4 L 5 -5 L 6 -6 L 7 -5","-14 13 M -3 -12 L -1 -12 L 1 -11 L 2 -10 L 0 -10 L -2 -9 M -1 -12 L 0 -11 L 0 -10 M -6 -6 L -7 -4 L -8 0 L -8 2 L -7 3 L -3 0 L -1 -1 L 2 -2 L 5 -2 L 7 -1 L 8 1 L 8 3 L 7 5 L 5 7 L 3 8 L -1 9 L -3 9 M -8 0 L -7 2 L -6 2 M 5 -2 L 6 -1 L 7 1 L 7 3 L 6 5 L 4 7 L 2 8 L -1 9","-14 13 M -6 -12 L -4 -11 L -4 -9 L -6 -5 M -5 -11 L -5 -9 L -6 -5 L -6 -2 L -5 1 L -4 2 L -4 0 L -3 -3 L -1 -6 L 1 -8 L 3 -9 L 5 -9 L 6 -8 L 7 -5 L 7 0 L 6 4 L 4 7 L 1 9 L -3 10 M -6 -2 L -5 0 L -4 0 M 4 -9 L 5 -8 L 6 -5 L 6 0 L 5 4 L 3 7 L 1 9","-14 13 M -5 -11 L -4 -9 L -2 -9 L 3 -11 M -4 -10 L -2 -10 L 3 -11 L 4 -10 M 4 -10 L 0 -7 L -4 -2 L -7 3 M 3 -11 L 0 -7 M -4 -2 L -7 1 L -7 3 M -7 3 L -5 1 L -2 -1 L 1 -2 L 4 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 4 9 L 1 9 L -1 8 L -2 7 L -2 6 L -1 5 L 1 5 L 3 6 L 5 8 M 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 4 9","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 9 M -4 -11 L -4 9 L -5 9 L -6 6 L -7 5 L -9 4 M -10 -6 L -9 -4 L -7 -4 L -3 -6 L -3 -5 M -9 -5 L -6 -5 L -3 -6 L -5 -2 M -3 -5 L -5 -2 L -7 1 L -10 5 L -10 4 L -7 1 L -3 -3 L 1 -6 L 4 -7 L 5 -7 L 7 -6 L 8 -4 L 7 6 L 7 8 L 8 9 M 5 -7 L 6 -6 L 7 -4 L 6 6 L 6 8 L 7 9 L 8 9 L 9 8 L 10 6","-14 13 M -5 -11 L -4 -9 L -2 -9 L 3 -11 M -4 -10 L -2 -10 L 3 -11 L 4 -10 M 4 -10 L 0 -7 L -4 -2 L -7 3 M 3 -11 L 0 -7 M -4 -2 L -7 1 L -7 3 M -7 3 L -5 1 L -2 -1 L 1 -2 L 4 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 5 9 L 2 10 L 0 10 M 4 -2 L 6 0 L 7 2 L 7 5 L 6 8","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 9 M -4 -11 L -4 9 L -5 9 L -6 6 L -7 5 L -9 4 M -10 -6 L -9 -4 L -7 -4 L -3 -6 L -3 -5 M -9 -5 L -6 -5 L -3 -6 L -5 -2 M -3 -5 L -5 -2 L -7 1 L -10 5 L -10 4 L -7 1 L -4 -2 L -1 -4 L 2 -5 L 5 -5 L 7 -4 L 8 -3 L 9 -1 L 9 2 L 8 5 L 6 7 L 4 8 L 1 9 M 5 -5 L 7 -3 L 8 -1 L 8 2 L 7 5 L 4 8","-14 13 M -5 -11 L -4 -9 L -2 -9 L 2 -11 M -4 -10 L -2 -10 L 2 -11 L 3 -10 M 3 -10 L -1 -4 L -5 4 L -6 6 L -7 7 L -9 7 L -10 5 L -10 3 L -9 1 L -7 -1 L -4 -3 L 0 -4 L 4 -4 L 7 -3 L 9 -1 L 10 1 L 10 4 L 9 6 L 7 8 L 5 9 L 2 9 L 0 8 L -1 7 L -1 6 L 0 5 L 2 5 L 4 6 L 6 8 M 2 -11 L -1 -4 M -8 7 L -9 5 L -9 3 L -8 0 M 8 -2 L 9 1 L 9 4 L 8 7","-14 13 M -4 -13 L -3 -12 L -1 -11 L 3 -11 M -3 -12 L 2 -12 L 3 -11 L -1 -9 M -7 -5 L -7 -4 L -6 -3 L -5 -3 L -2 -5 L 0 -6 M -7 -4 L -6 -4 L -3 -5 L 0 -6 L 2 -6 L 5 -5 L 6 -3 L 6 0 L 5 3 L 3 6 L 0 8 L -4 10 M 2 -6 L 4 -5 L 5 -3 L 5 0 L 4 3 L 3 5 L 1 7 L -2 9","-14 13 M -6 -11 L -5 -9 L -3 -9 L -1 -10 L 2 -12 M -5 -10 L -3 -10 L 0 -11 L 2 -12 L 3 -11 M 3 -11 L -1 -8 L -5 -4 L -8 0 M 2 -12 L -1 -8 M -8 0 L -8 -2 L -5 -4 L -3 -5 L 0 -6 L 3 -6 L 5 -5 L 6 -3 L 6 -1 L 5 1 L 3 2 L -2 2 L -3 1 L -3 -1 L -2 -2 L 0 -2 L 1 -1 L 1 1 L 0 2 L -4 3 L -7 5 L -10 8 L -10 9 L -9 9 L -7 5 M -8 7 L -6 6 L -4 6 L -2 8 L -1 8 L 1 6 L 4 5 L 7 5 L 9 6 L 10 7 L 10 9 L 9 8 L 7 8 M -4 6 L -2 7 L -1 7 L 1 6 M 7 5 L 9 7 L 9 8","-14 13 M -2 -12 L 0 -11 L 0 -10 L -2 -6 L -4 -3 L -7 0 L -10 3 M -1 -11 L -1 -10 L -2 -6 M -7 0 L -10 2 L -10 3 M -5 -2 L -3 -3 L -1 -3 L 1 -2 L 2 0 L 2 4 M -1 -3 L 0 -2 L 1 0 L 1 5 L 2 4 M -9 -8 L -6 -7 L -3 -7 L 1 -8 L 5 -9 M 1 -8 L 4 -10 L 5 -9 M 9 -3 L 10 -1 L 2 0 L -1 1 L -3 2 L -4 4 L -4 6 L -3 8 L -1 9 L 7 9 M 9 -2 L 2 0 M 2 9 L 6 8 L 7 9","-14 13 M -1 -12 L 1 -10 L -2 -5 L -6 2 L -10 9 M 0 -11 L -2 -5 M -10 9 L -10 7 L -6 2 L -4 0 L -2 -1 L 0 -1 L 2 0 L 2 5 L 3 7 L 4 8 M 0 -1 L 1 0 L 1 5 L 2 7 L 4 8 L 6 8 L 8 7 L 9 5 L 10 2","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -4 -12 L -2 -11 L -2 -9 L -3 -6 L -5 -1 L -10 9 M -3 -11 L -3 -9 L -5 -3 L -7 2 L -10 9 M -10 -5 L -10 -4 L -9 -3 L -8 -3 L -5 -5 L -3 -6 L 0 -6 L 2 -5 L 3 -3 L 3 0 L 2 4 L 0 8 L -1 9 L -2 9 L -2 8 L -3 7 L -5 6 M -10 -4 L -8 -4 L -5 -5 M 0 -6 L 2 -4 L 2 0 L 1 4 L 0 6 L -2 8 M 5 -5 L 7 -4 L 9 -2 L 10 0 L 10 2 L 9 1 L 7 2 M 8 -3 L 9 -1 L 9 1 M 5 -10 L 7 -8 M 7 -12 L 9 -10","-14 13 M -4 -12 L -1 -11 L 0 -7 L 1 -4 L 3 0 L 6 4 L 5 5 M -2 -11 L 0 -7 M 3 0 L 5 5 M -6 -7 L -4 -6 L -1 -6 L 2 -7 M -9 -3 L -8 -2 L -5 -1 L 0 -1 L 5 -2 L 8 -3 M 5 -2 L 7 -4 L 8 -3 M 5 5 L 3 4 L 0 3 L -4 3 L -6 4 L -7 5 L -7 7 L -6 8 L -4 9 L -1 10 L 5 10 M -4 9 L 4 9 L 5 10 M 5 -10 L 7 -8 M 7 -12 L 9 -10","-14 13 M 2 -12 L 4 -11 L 4 -10 L -4 -2 M 3 -11 L 2 -9 L -4 -2 L -4 -1 L 1 4 L 3 7 L 4 9 M -4 -1 L 4 6 L 5 8 L 4 9 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 L -4 -3 L -3 -4 L -1 -5 L 8 -5 L 10 -6 M -9 3 L -8 5 L -7 6 M 4 -5 L 8 -6 L 9 -7 L 10 -6 M 4 -12 L 6 -11 L 6 3 L 5 6 L 3 8 L 0 10 M 5 -11 L 6 -8 M 6 0 L 5 4 L 3 7 L 0 10 M 9 -11 L 11 -9 M 11 -13 L 13 -11","-14 13 M -5 -10 L -3 -9 L 0 -8 L 2 -8 L 5 -9 M -3 -9 L 2 -9 L 4 -10 L 5 -9 L -1 -6 M -7 1 L -7 3 L -6 5 L -4 6 L 0 7 L 7 7 M -4 6 L 6 6 L 7 7 M 7 -11 L 9 -9 M 9 -13 L 11 -11","-14 13 M -8 -7 L -7 -6 L -4 -5 L 0 -5 L 4 -6 L 6 -7 L 7 -8 M 4 -6 L 6 -8 L 7 -8 M -2 -12 L -1 -9 L 0 -7 L 3 -3 L 5 0 L 6 2 M -2 -12 L -1 -12 L -1 -10 L 0 -7 M 3 -3 L 6 0 L 7 2 L 7 3 M 7 3 L 6 2 L 4 1 L 0 0 L -4 0 L -7 1 L -8 3 L -8 5 L -7 7 L -5 8 L -1 9 L 3 9 M -8 5 L -7 6 L -5 7 L -1 8 L 4 8 L 3 9 M 8 -11 L 10 -9 M 10 -13 L 12 -11","-14 13 M -6 -12 L -4 -11 L -5 1 L -5 6 L -4 8 M -5 -11 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 L 1 9 L 4 8 L 7 6 L 9 4 M 1 -10 L 3 -8 M 3 -12 L 5 -10","-14 13 M 0 -12 L 2 -11 L 2 1 L 1 3 L -1 4 L -3 3 L -4 1 L -4 0 L -3 -2 L -1 -3 L 1 -2 L 2 0 L 2 4 L 1 8 L -1 10 M 1 -11 L 2 -6 M 2 4 L 1 7 L -1 10 M -10 -8 L -8 -6 L -5 -6 L 4 -7 L 8 -8 L 10 -7 M -9 -7 L -5 -6 M 4 -7 L 10 -7 M 6 -12 L 8 -10 M 9 -13 L 11 -11","-14 13 M -6 -8 L -4 -7 L -4 4 L -3 6 L -2 7 L 0 8 L 8 8 M -5 -7 L -4 -4 M 3 8 L 6 7 L 7 7 L 8 8 M 3 -11 L 5 -10 L 5 2 L 4 4 L 3 3 L 0 1 M 4 -10 L 5 -7 M 5 -1 L 4 2 L 3 3 M -10 -4 L -8 -2 L -6 -2 L -2 -3 L 3 -4 L 7 -5 L 9 -5 L 10 -4 M -9 -3 L -2 -3 M 3 -4 L 10 -4 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -4 -11 L -2 -9 L 4 -11 M -3 -10 L -1 -10 L 4 -11 L 5 -10 M 5 -10 L 2 -7 L -4 -3 L -8 -1 M 4 -11 L 3 -9 L 1 -7 L -4 -3 M -9 -2 L -7 0 L -3 -2 M -8 -1 L -3 -2 L 3 -3 L 8 -4 L 9 -3 M 3 -3 L 9 -3 M 3 -3 L 1 -2 L -1 0 L -2 2 L -2 4 L -1 6 L 0 7 L 3 8 L 7 8 M -1 6 L 2 7 L 6 7 L 7 8 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -4 -12 L -2 -11 L -2 -9 L -4 -3 L -5 0 L -7 5 L -9 9 M -3 -11 L -3 -8 L -4 -3 M -5 0 L -7 4 L -9 7 L -9 9 M -10 -5 L -8 -4 L -5 -4 L -2 -5 L 0 -6 L 2 -7 M 0 -6 L 1 -8 L 2 -7 M 3 -4 L 6 -4 L 9 -3 L 7 -3 L 5 -2 M 6 -4 L 7 -3 M 0 4 L 1 6 L 2 7 L 4 8 L 9 8 M 1 6 L 3 7 L 10 7 L 9 8 M 6 -10 L 8 -8 M 8 -12 L 10 -10","-14 13 M -5 -12 L -3 -11 L -3 -9 L -5 -4 L -6 0 L -6 2 L -5 3 M -4 -11 L -4 -7 M -5 -4 L -5 3 M -10 -8 L -7 -7 L -3 -7 L 1 -8 L 3 -9 M 1 -8 L 2 -10 L 3 -9 M -5 3 L -3 1 L 0 -1 L 3 -2 L 6 -2 L 8 -1 L 9 1 L 9 4 L 8 6 L 5 8 L 1 9 L -3 9 M 6 -2 L 7 -1 L 8 1 L 8 4 L 7 6 L 5 8 M 6 -9 L 8 -7 M 8 -11 L 10 -9","-14 13 M -10 -6 L -8 -4 L -3 -6 L 1 -7 L 5 -7 L 8 -6 L 9 -5 L 10 -3 L 10 0 L 9 2 L 8 3 L 6 4 L 3 5 L -2 6 M -9 -5 L -3 -6 M 5 -7 L 7 -6 L 8 -5 L 9 -3 L 9 0 L 8 2 L 6 4 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -10 -9 L -8 -7 L -6 -7 L -2 -8 L 3 -9 L 7 -10 L 9 -10 L 10 -9 M -9 -8 L -2 -8 M 3 -9 L 10 -9 M 7 -9 L 4 -8 L 1 -6 L -1 -3 L -2 0 L -2 3 L -1 6 L 0 7 L 2 8 L 6 9 L 9 9 M -2 3 L -1 5 L 0 6 L 2 7 L 6 8 L 8 8 L 9 9 M 8 -5 L 10 -3 M 10 -7 L 12 -5","-14 13 M -3 -12 L -1 -11 L -1 -1 M -2 -11 L -2 -5 L -1 -1 M 5 -4 L 7 -2 L -1 -1 M 6 -3 L 2 -2 L -1 -1 L -4 0 L -6 1 L -7 3 L -7 5 L -6 7 L -5 8 L -3 9 L 8 9 M -6 7 L -4 8 L 7 8 L 8 9 M 6 -9 L 8 -7 M 8 -11 L 10 -9","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M 3 -11 L 5 -10 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -10 L 5 -6 M 4 3 L 9 6 L 9 7 M -2 -5 L 0 -4 L 3 -4 L 7 -5 L 9 -6 M 7 -5 L 8 -7 L 9 -6 M 8 -11 L 10 -9 M 10 -13 L 12 -11","-14 13 M -9 -11 L -8 -10 L -6 -9 L -2 -9 M -8 -10 L -3 -10 L -2 -9 M -2 -9 L -5 -7 L -7 -5 L -9 -2 L -10 1 L -10 4 L -9 6 L -8 7 L -6 8 L -3 8 L 0 7 L 2 5 L 3 3 L 4 -1 L 4 -5 L 3 -9 L 4 -9 M -10 3 L -9 5 L -8 6 L -6 7 L -3 7 L 0 6 L 2 4 L 3 2 L 4 -1 M 4 -9 L 6 -6 L 7 -4 L 9 -1 L 10 1 L 8 1 L 7 2 M 3 -9 L 6 -6 M 7 -4 L 8 -1 L 8 1 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -2 -12 L -1 -9 L 1 -8 M -1 -10 L 1 -9 L 2 -9 L 2 -8 M 2 -8 L -1 -7 L -3 -6 L -4 -5 L -4 -3 L -3 -2 L -1 -1 L 1 0 L 2 1 L 3 3 L 3 5 L 2 7 L 0 8 L -2 8 L -5 7 L -7 5 M -1 -1 L 1 1 L 2 3 L 2 5 L 1 7 L 0 8 M -9 7 L -6 4 L -3 2 L 0 1 M 4 1 L 7 2 L 9 4 L 10 6 L 7 6 M -9 7 L -9 6 L -6 4 M 4 1 L 6 2 L 8 4 L 9 6 M 5 -9 L 7 -7 M 7 -11 L 9 -9","-14 13 M -10 -2 L -8 0 L -5 -5 M -9 -1 L -4 -6 L -2 -6 L 3 -1 L 7 2 L 9 3 L 10 4 L 8 4 L 6 5 M 3 -1 L 8 4 M 3 -8 L 5 -6 M 5 -10 L 7 -8","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M -1 -11 L 1 -10 L 3 -10 L 7 -11 M 3 -10 L 5 -11 L 6 -12 L 7 -11 M -2 -3 L 1 -2 L 4 -2 L 7 -3 L 9 -4 M 7 -3 L 8 -5 L 9 -4 M 3 -10 L 5 -9 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -9 L 5 -5 M 4 3 L 9 6 L 9 7 M 9 -9 L 11 -7 M 11 -11 L 13 -9","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M 3 -11 L 5 -10 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -10 L 5 -6 M 4 3 L 9 6 L 9 7 M -2 -5 L 0 -4 L 3 -4 L 7 -5 L 9 -6 M 7 -5 L 8 -7 L 9 -6 M 9 -13 L 8 -12 L 8 -10 L 9 -9 L 11 -9 L 12 -10 L 12 -12 L 11 -13 L 9 -13","-14 13 M -9 -11 L -8 -10 L -6 -9 L -2 -9 M -8 -10 L -3 -10 L -2 -9 M -2 -9 L -5 -7 L -7 -5 L -9 -2 L -10 1 L -10 4 L -9 6 L -8 7 L -6 8 L -3 8 L 0 7 L 2 5 L 3 3 L 4 -1 L 4 -5 L 3 -9 L 4 -9 M -10 3 L -9 5 L -8 6 L -6 7 L -3 7 L 0 6 L 2 4 L 3 2 L 4 -1 M 4 -9 L 6 -6 L 7 -4 L 9 -1 L 10 1 L 8 1 L 7 2 M 3 -9 L 6 -6 M 7 -4 L 8 -1 L 8 1 M 8 -12 L 7 -11 L 7 -9 L 8 -8 L 10 -8 L 11 -9 L 11 -11 L 10 -12 L 8 -12","-14 13 M -2 -12 L -1 -9 L 1 -8 M -1 -10 L 1 -9 L 2 -9 L 2 -8 M 2 -8 L -1 -7 L -3 -6 L -4 -5 L -4 -3 L -3 -2 L -1 -1 L 1 0 L 2 1 L 3 3 L 3 5 L 2 7 L 0 8 L -2 8 L -5 7 L -7 5 M -1 -1 L 1 1 L 2 3 L 2 5 L 1 7 L 0 8 M -9 7 L -6 4 L -3 2 L 0 1 M 4 1 L 7 2 L 9 4 L 10 6 L 7 6 M -9 7 L -9 6 L -6 4 M 4 1 L 6 2 L 8 4 L 9 6 M 6 -11 L 5 -10 L 5 -8 L 6 -7 L 8 -7 L 9 -8 L 9 -10 L 8 -11 L 6 -11","-14 13 M -10 -2 L -8 0 L -5 -5 M -9 -1 L -4 -6 L -2 -6 L 3 -1 L 7 2 L 9 3 L 10 4 L 8 4 L 6 5 M 3 -1 L 8 4 M 4 -10 L 3 -9 L 3 -7 L 4 -6 L 6 -6 L 7 -7 L 7 -9 L 6 -10 L 4 -10","-14 13 M -9 -11 L -7 -10 L -7 -8 L -8 -5 L -9 -1 M -8 -10 L -8 -7 L -9 -1 L -9 3 L -8 7 L -7 8 L -7 6 L -6 2 L -5 -1 M -9 3 L -8 5 L -7 6 M -1 -11 L 1 -10 L 3 -10 L 7 -11 M 3 -10 L 5 -11 L 6 -12 L 7 -11 M -2 -3 L 1 -2 L 4 -2 L 7 -3 L 9 -4 M 7 -3 L 8 -5 L 9 -4 M 3 -10 L 5 -9 L 5 5 L 4 7 L 2 8 L 0 8 L -2 7 L -3 5 L -2 3 L 0 2 L 2 2 L 4 3 L 9 7 M 4 -9 L 5 -5 M 4 3 L 9 6 L 9 7 M 10 -11 L 9 -10 L 9 -8 L 10 -7 L 12 -7 L 13 -8 L 13 -10 L 12 -11 L 10 -11","-14 13 M -10 -11 L -8 -9 L -3 -10 L 3 -11 L 8 -12 L 10 -10 M -9 -10 L -3 -10 M 3 -11 L 9 -11 M 10 -10 L 8 -8 L 4 -5 L 2 -4 M 9 -11 L 7 -8 L 5 -6 L 2 -4 M 0 -5 L 2 -4 L 2 -1 L 1 3 L -1 6 L -4 9 M 0 -5 L 1 -4 L 1 -1 L 0 3 L -2 7","-14 13 M 4 -12 L 6 -10 L 3 -6 L 0 -3 L -4 0 L -7 2 M 5 -11 L 4 -9 L 1 -5 L -3 -1 L -7 2 M 1 -4 L 3 -3 L 3 10 M 1 -4 L 2 -3 L 2 9 L 3 10","-14 13 M -2 -12 L 1 -11 L 1 -9 L 0 -7 M -2 -12 L 0 -11 L 0 -7 M -8 -9 L -7 -7 L -7 -2 L -6 -1 M -6 -7 L -6 -1 M -6 -7 L 7 -7 M -8 -9 L -7 -8 L -3 -7 M 3 -7 L 6 -8 L 8 -6 M 8 -6 L 5 0 L 3 3 L 0 6 L -3 8 L -5 9 M 7 -7 L 6 -4 L 4 0 L 1 4 L -2 7 L -5 9","-14 13 M -1 -7 L 1 -6 L 1 5 M 0 -6 L 0 5 M -8 -8 L -6 -6 L 6 -8 L 8 -7 M -7 -7 L 8 -7 M -10 4 L -8 6 L 8 4 L 10 5 M -9 5 L 10 5","-14 13 M 0 -12 L 3 -11 L 3 7 L 2 9 L 1 7 L -2 5 M 2 -11 L 2 6 L 1 7 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -6 M -9 -5 L -3 -5 M 3 -6 L 10 -6 M 1 -5 L -2 -1 L -6 3 L -10 6 M 2 -5 L 0 -2 L -3 1 L -7 4 L -10 6","-14 13 M -1 -12 L 1 -10 L 0 -7 L -2 -2 L -4 2 L -6 5 L -9 9 M 0 -11 L -1 -8 L -3 -2 L -5 3 L -9 9 M -9 -6 L -7 -4 L -3 -5 L 2 -6 L 6 -7 L 8 -5 M -8 -5 L -3 -5 M 2 -6 L 7 -6 M 8 -5 L 7 1 L 6 5 L 5 7 L 3 9 L 2 8 L 0 7 M 7 -6 L 6 1 L 5 5 L 4 7 L 2 8","-14 13 M -3 -12 L 0 -11 L 2 9 M -1 -11 L 1 10 L 2 9 M -8 -6 L -6 -4 L -2 -5 L 2 -6 L 6 -7 L 8 -6 M -7 -5 L -2 -5 M 2 -6 L 8 -6 M -10 0 L -8 2 L -3 1 L 3 0 L 8 -1 L 10 0 M -9 1 L -3 1 M 3 0 L 10 0","-14 13 M -2 -12 L 0 -10 L -2 -6 L -4 -3 L -6 -1 L -8 1 M -1 -11 L -2 -8 L -4 -4 L -6 -1 M -1 -8 L 1 -7 L 6 -7 M 3 -7 L 5 -8 L 7 -6 M 7 -6 L 4 0 L 1 4 L -2 7 L -5 9 L -7 10 M 6 -7 L 5 -4 L 3 0 L 0 4 L -3 7 L -7 10","-14 13 M -4 -12 L -2 -10 L -4 -6 L -6 -3 L -8 -1 L -10 1 M -3 -11 L -4 -8 L -6 -4 L -8 -1 M -6 -3 L 2 -4 L 7 -5 L 10 -4 M 2 -4 L 10 -4 M 0 -3 L 2 -1 L 0 3 L -2 6 L -4 8 L -6 10 M 1 -2 L 0 1 L -2 5 L -4 8","-14 13 M -8 -8 L -6 -6 L 6 -8 L 8 -6 M -7 -7 L 7 -7 M 8 -6 L 7 -3 L 6 4 M 7 -7 L 6 -2 L 6 4 M -8 4 L -6 6 L 6 4 L 7 5 M -7 5 L 7 5","-14 13 M -6 -9 L -4 -8 L -4 -3 L -5 1 M -5 -8 L -5 1 M 2 -12 L 5 -11 L 5 -6 L 4 -1 L 3 2 L 2 4 L 0 7 L -2 9 M 4 -11 L 4 -6 L 3 -1 L 2 3 L 0 7 M -10 -5 L -8 -3 L -3 -4 L 3 -5 L 8 -6 L 10 -5 M -9 -4 L -3 -4 M 3 -5 L 10 -5","-14 13 M -5 -11 L -3 -10 L -2 -9 L -2 -8 L -3 -8 L -4 -10 L -5 -11 M -10 -4 L -8 -3 L -7 -2 L -7 -1 L -8 -1 L -9 -3 L -10 -4 M -9 6 L -7 8 L -3 6 L 1 3 L 5 -1 L 9 -6 M -8 7 L -3 5 L 1 2 L 9 -6","-14 13 M -8 -9 L -6 -7 L 5 -9 L 7 -7 M -7 -8 L 6 -8 M 7 -7 L 5 -4 L 2 -1 L -1 1 L -3 2 L -8 4 M 6 -8 L 5 -6 L 3 -3 L 1 -1 L -2 1 L -8 4 M 3 1 L 6 3 L 8 5 L 8 6 L 7 6 L 6 4 L 3 1","-14 13 M -3 -11 L 0 -10 L 0 -8 L -1 1 M -1 -10 L -1 4 L 0 6 L 2 7 L 9 7 M 4 7 L 8 6 L 9 7 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -3 M -9 -3 L -3 -3 M 3 -4 L 9 -4 M 10 -3 L 7 -1 L 4 2 M 9 -4 L 4 2","-14 13 M -9 -9 L -7 -8 L -6 -7 L -6 -6 L -7 -6 L -8 -8 L -9 -9 M 6 -10 L 8 -8 L 5 -2 L 2 2 L -1 5 L -4 7 L -6 8 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -6 8","-14 13 M -2 -12 L 0 -10 L -2 -6 L -4 -3 L -6 -1 L -8 1 M -1 -11 L -2 -8 L -4 -4 L -6 -1 M -1 -8 L 1 -7 L 6 -7 M 3 -7 L 5 -8 L 7 -6 M 7 -6 L 4 0 L 1 4 L -2 7 L -5 9 L -7 10 M 6 -7 L 5 -4 L 3 0 L 0 4 L -3 7 L -7 10 M -4 -2 L -1 -1 L 1 0 L 4 2 L 5 3 L 5 4 L 4 4 L 3 2 L 1 0","-14 13 M -6 -8 L -3 -8 L 2 -9 L 6 -11 M -3 -8 L 0 -9 L 2 -10 L 5 -12 L 6 -11 M -2 -8 L 1 -7 L 1 -1 L 0 3 L -2 6 L -5 9 M 0 -7 L 0 -1 L -1 3 L -3 7 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -4 M -9 -3 L -3 -3 M 3 -4 L 10 -4","-14 13 M -9 -9 L -7 -8 L -6 -7 L -6 -6 L -7 -6 L -8 -8 L -9 -9 M -3 -10 L -1 -9 L 0 -8 L 0 -7 L -1 -7 L -2 -9 L -3 -10 M 6 -10 L 8 -8 L 5 -2 L 2 2 L -1 5 L -4 7 L -6 8 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -6 8","-14 13 M -6 -12 L -4 -10 L 5 -12 L 6 -11 M -5 -11 L 6 -11 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -4 M -9 -3 L -3 -3 M 3 -4 L 10 -4 M 1 -3 L 1 -1 L 0 3 L -2 6 L -5 9 M 0 -3 L 0 -1 L -1 3 L -3 7","-14 13 M -5 -12 L -2 -11 L -2 10 M -3 -11 L -3 9 L -2 10 M -2 -3 L 0 -3 L 3 -2 L 4 -1 L 4 0 L 3 0 L 2 -2 L 0 -3","-14 13 M -1 -12 L 2 -11 L 2 -6 L 1 -1 L 0 2 L -1 4 L -3 7 L -5 9 M 1 -11 L 1 -6 L 0 -1 L -1 3 L -3 7 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -4 M -9 -3 L -3 -3 M 3 -4 L 10 -4","-14 13 M -6 -8 L -4 -6 L 5 -8 L 6 -7 M -5 -7 L 6 -7 M -10 4 L -8 6 L 8 4 L 10 5 M -9 5 L 10 5","-14 13 M -8 -9 L -6 -7 L 5 -9 L 7 -7 M -7 -8 L 6 -8 M 7 -7 L 5 -4 L 2 -1 L -1 1 L -3 2 L -8 4 M 6 -8 L 5 -6 L 3 -3 L 1 -1 L -2 1 L -8 4 M -5 -3 L -2 -2 L 2 0 L 5 2 L 7 4 L 7 5 L 6 5 L 4 2 L 2 0","-14 13 M -2 -12 L 0 -11 L 1 -10 L 1 -9 L 0 -9 L -1 -11 L -2 -12 M -8 -6 L -6 -4 L 4 -6 L 6 -4 M -7 -5 L 5 -5 M 6 -4 L 2 0 L -1 2 L -3 3 L -8 5 M 5 -5 L 3 -2 L 1 0 L -2 2 L -8 5 M 0 1 L 1 2 L 1 10 M 0 1 L 0 9 L 1 10 M 6 3 L 8 4 L 9 5 L 9 6 L 8 6 L 7 4 L 6 3","-14 13 M 5 -10 L 7 -8 L 4 -2 L 1 2 L -2 5 L -5 7 L -7 8 M 6 -9 L 5 -6 L 2 -1 L -1 3 L -4 6 L -7 8","-14 13 M -6 -6 L -4 -4 L -5 -1 L -6 1 L -8 3 L -10 4 M -5 -5 L -6 -1 L -8 3 M 4 -6 L 6 -4 L 8 -1 L 10 3 L 10 4 L 9 4 L 8 0 L 6 -4","-14 13 M -9 -11 L -6 -10 L -6 -8 L -7 1 M -7 -10 L -7 4 L -6 6 L -4 7 L 7 7 M -1 7 L 3 6 L 5 6 L 7 7 M -7 0 L -3 -1 L 0 -2 L 6 -4 M 0 -2 L 4 -4 L 6 -4","-14 13 M -8 -11 L -6 -9 L 7 -9 M -7 -10 L -3 -9 M 3 -9 L 6 -10 L 8 -8 M 8 -8 L 5 -2 L 3 1 L 0 4 L -3 6 L -5 7 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -5 7","-14 13 M -11 -1 L -9 1 L -2 -6 L -1 -6 L 9 4 M -10 0 L -8 -1 L -4 -4 M 3 -2 L 8 2 L 10 3 L 9 4","-14 13 M -2 -12 L 1 -11 L 1 7 L 0 9 L -1 7 L -3 6 M 0 -11 L 0 6 L -1 7 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -6 M -9 -5 L -3 -5 M 3 -6 L 10 -6 M -6 1 L -8 4 L -9 5 L -10 5 L -10 4 L -8 3 L -6 1 M 6 1 L 9 3 L 10 4 L 10 5 L 9 5 L 8 3 L 6 1","-14 13 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -5 M -9 -5 L -3 -5 M 3 -6 L 9 -6 M 10 -5 L 8 -3 L 4 0 L 2 1 M 9 -6 L 7 -3 L 5 -1 L 2 1 M -1 0 L 2 2 L 4 4 L 4 5 L 3 5 L 2 3 L -1 0","-14 13 M -2 -12 L 1 -11 L 3 -10 L 4 -9 L 4 -8 L 3 -8 L 1 -10 L -2 -12 M -5 -5 L -2 -4 L 0 -3 L 1 -2 L 1 -1 L 0 -1 L -2 -3 L -5 -5 M -4 4 L 1 6 L 3 7 L 4 8 L 4 9 L 3 9 L 1 7 L -2 5 L -4 4","-14 13 M 0 -9 L 2 -7 L -5 5 M 1 -8 L -5 5 M -10 5 L -8 7 L 7 4 M -9 6 L 7 4 M 4 1 L 6 3 L 8 6 L 9 6 L 9 5 L 7 3 L 4 1","-14 13 M 5 -10 L 7 -8 L 4 -2 L 1 2 L -2 5 L -5 7 L -7 8 M 6 -9 L 5 -6 L 3 -2 L 0 2 L -3 5 L -7 8 M -3 -4 L 1 -2 L 4 0 L 6 2 L 6 3 L 5 3 L 4 1 L 2 -1 L -1 -3","-14 13 M -6 -9 L -4 -7 L 5 -9 L 6 -8 M -5 -8 L 6 -8 M -1 -7 L -1 4 L 0 6 L 2 7 L 9 7 M 0 -7 L 0 -5 L -1 1 M 4 7 L 8 6 L 9 7 M -10 -1 L -8 1 L -3 0 L 3 -1 L 8 -2 L 10 -1 M -9 0 L -3 0 M 3 -1 L 10 -1","-14 13 M -3 -12 L 0 -11 L 2 9 M -1 -11 L 1 10 L 2 9 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -5 M -9 -5 L -3 -5 M 3 -6 L 9 -6 M 10 -5 L 7 -2 L 4 0 L 2 1 M 9 -6 L 8 -4 L 5 -1 L 2 1","-14 13 M 4 -12 L 6 -10 L 3 -6 L 0 -3 L -4 0 L -7 2 M 5 -11 L 4 -9 L 1 -5 L -3 -1 L -7 2 M 1 -4 L 3 -3 L 3 10 M 1 -4 L 2 -3 L 2 9 L 3 10","-14 13 M -9 -8 L -7 -6 L 3 -8 L 5 -6 M -8 -7 L 4 -7 M 5 -6 L 4 -3 L 3 4 M 4 -7 L 3 -2 L 3 4 M -10 4 L -8 6 L 8 4 L 10 5 M -9 5 L 10 5","-14 13 M -1 -7 L 1 -6 L 1 5 M 0 -6 L 0 5 M -8 -8 L -6 -6 L 6 -8 L 8 -7 M -7 -7 L 8 -7 M -10 4 L -8 6 L 8 4 L 10 5 M -9 5 L 10 5","-14 13 M -8 -8 L -6 -6 L 6 -8 L 8 -6 M -7 -7 L 7 -7 M -8 -2 L -6 0 L 6 -2 M -7 -1 L 6 -1 M -8 4 L -6 6 L 6 4 M -7 5 L 6 5 M 8 -6 L 7 -3 L 7 7 M 7 -7 L 6 -2 L 6 7","-14 13 M -6 -12 L -4 -10 L 5 -12 L 6 -11 M -5 -11 L 6 -11 M -8 -5 L -6 -3 L 5 -5 L 7 -3 M -7 -4 L 6 -4 M 7 -3 L 5 0 L 2 3 L -1 5 L -3 6 L -6 7 M 6 -4 L 5 -2 L 3 1 L 1 3 L -2 5 L -6 7","-14 13 M -6 -9 L -4 -8 L -4 -3 L -5 1 M -5 -8 L -5 1 M 2 -12 L 5 -11 L 5 -1 L 4 3 L 2 6 L -1 9 M 4 -11 L 4 -1 L 3 3 L 2 5 L -1 9","-14 13 M -6 -8 L -4 -7 L -4 -4 L -5 0 L -7 3 L -10 6 M -5 -7 L -5 -4 L -6 0 L -7 2 L -10 6 M -1 -9 L 1 -8 L 1 -5 L 0 1 M 0 -8 L 0 4 L 1 6 L 3 6 L 5 5 L 7 3 L 10 -1 M 0 4 L 1 5 L 4 5 L 6 4","-14 13 M -10 -9 L -7 -8 L -7 -5 L -8 1 M -8 -8 L -8 4 L -7 6 L -4 6 L -1 5 L 2 3 L 6 0 L 9 -3 M -8 4 L -7 5 L -4 5 L -1 4 L 3 2 L 6 0","-14 13 M -8 -8 L -6 -6 L 6 -8 L 8 -6 M -7 -7 L 7 -7 M -7 -7 L -7 8 M -6 -6 L -6 6 L -7 8 M 8 -6 L 7 -3 L 6 4 M 7 -7 L 6 -2 L 6 4 M -6 6 L 6 4 L 7 5 M -6 5 L 7 5","-14 13 M -8 -11 L -7 -9 L -7 -4 L -6 -3 M -6 -9 L -6 -3 M -6 -9 L 7 -9 M -8 -11 L -7 -10 L -3 -9 M 3 -9 L 6 -10 L 8 -8 M 8 -8 L 5 -2 L 3 1 L 0 4 L -3 6 L -5 7 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -5 7","-14 13 M -1 -12 L 2 -11 L 2 9 M 1 -11 L 1 10 L 2 9 M -8 -8 L -6 -6 L 6 -8 L 8 -7 M -7 -7 L 8 -7 M -6 -6 L -4 -5 L -4 0 M -5 -5 L -5 0 M -10 -1 L -8 1 L 8 -1 L 10 0 M -9 0 L 10 0","-14 13 M -2 -12 L 1 -11 L 1 -9 L 0 -7 M -2 -12 L 0 -11 L 0 -7 M -8 -9 L -7 -7 L -7 -2 L -6 -1 M -6 -7 L -6 -1 M -6 -7 L 7 -7 M -8 -9 L -7 -8 L -3 -7 M 3 -7 L 6 -8 L 8 -6 M 8 -6 L 5 0 L 3 3 L 0 6 L -3 8 L -5 9 M 7 -7 L 6 -4 L 4 0 L 1 4 L -2 7 L -5 9","-14 13 M -8 -8 L -6 -6 L 6 -8 L 8 -6 M -7 -7 L 7 -7 M 8 -6 L 5 -4 L 1 -1 M 7 -7 L 1 -1 M -1 -2 L 1 -1 L 1 5 M 0 -1 L 0 5 M -10 4 L -8 6 L 8 4 L 10 5 M -9 5 L 10 5","-14 13 M -8 -11 L -6 -9 L 7 -9 M -7 -10 L -3 -9 M 3 -9 L 6 -10 L 8 -8 M -6 -4 L -4 -2 L 4 -3 M -5 -3 L 4 -3 M 8 -8 L 5 -2 L 3 1 L 0 4 L -3 6 L -5 7 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -5 7","-14 13 M -8 -10 L -6 -9 L -5 -8 L -5 -7 L -6 -7 L -7 -9 L -8 -10 M -9 5 L -7 7 L -3 5 L 1 2 L 5 -2 L 9 -7 M -8 6 L -3 4 L 1 1 L 9 -7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -10 7 L -11 8 L -10 9 L -9 8 L -10 7","-14 13 M -1 -12 L 1 -10 L 0 -7 L -2 -2 L -4 2 L -6 5 L -9 9 M 0 -11 L -1 -8 L -3 -2 L -5 3 L -9 9 M -9 -6 L -7 -4 L -3 -5 L 2 -6 L 6 -7 L 8 -5 M -8 -5 L -3 -5 M 2 -6 L 7 -6 M 8 -5 L 7 1 L 6 5 L 5 7 L 3 9 L 2 8 L 0 7 M 7 -6 L 6 1 L 5 5 L 4 7 L 2 8 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -3 -12 L 0 -11 L 2 9 M -1 -11 L 1 10 L 2 9 M -8 -6 L -6 -4 L -2 -5 L 2 -6 L 6 -7 L 8 -6 M -7 -5 L -2 -5 M 2 -6 L 8 -6 M -10 0 L -8 2 L -3 1 L 3 0 L 8 -1 L 10 0 M -9 1 L -3 1 M 3 0 L 10 0 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -2 -12 L 0 -10 L -2 -6 L -4 -3 L -6 -1 L -8 1 M -1 -11 L -2 -8 L -4 -4 L -6 -1 M -1 -8 L 1 -7 L 6 -7 M 3 -7 L 5 -8 L 7 -6 M 7 -6 L 4 0 L 1 4 L -2 7 L -5 9 L -7 10 M 6 -7 L 5 -4 L 3 0 L 0 4 L -3 7 L -7 10 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -4 -12 L -2 -10 L -4 -6 L -6 -3 L -8 -1 L -10 1 M -3 -11 L -4 -8 L -6 -4 L -8 -1 M -6 -3 L 2 -4 L 7 -5 L 10 -4 M 2 -4 L 10 -4 M 0 -3 L 2 -1 L 0 3 L -2 6 L -4 8 L -6 10 M 1 -2 L 0 1 L -2 5 L -4 8 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -8 -8 L -6 -6 L 6 -8 L 8 -6 M -7 -7 L 7 -7 M 8 -6 L 7 -3 L 6 4 M 7 -7 L 6 -2 L 6 4 M -8 4 L -6 6 L 6 4 L 7 5 M -7 5 L 7 5 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -6 -9 L -4 -8 L -4 -3 L -5 1 M -5 -8 L -5 1 M 2 -12 L 5 -11 L 5 -6 L 4 -1 L 3 2 L 2 4 L 0 7 L -2 9 M 4 -11 L 4 -6 L 3 -1 L 2 3 L 0 7 M -10 -5 L -8 -3 L -3 -4 L 3 -5 L 8 -6 L 10 -5 M -9 -4 L -3 -4 M 3 -5 L 10 -5 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -5 -11 L -3 -10 L -2 -9 L -2 -8 L -3 -8 L -4 -10 L -5 -11 M -10 -4 L -8 -3 L -7 -2 L -7 -1 L -8 -1 L -9 -3 L -10 -4 M -9 6 L -7 8 L -3 6 L 1 3 L 5 -1 L 9 -6 M -8 7 L -3 5 L 1 2 L 9 -6 M 4 -10 L 6 -8 M 6 -12 L 8 -10","-14 13 M -8 -9 L -6 -7 L 5 -9 L 7 -7 M -7 -8 L 6 -8 M 7 -7 L 5 -4 L 2 -1 L -1 1 L -3 2 L -8 4 M 6 -8 L 5 -6 L 3 -3 L 1 -1 L -2 1 L -8 4 M 3 1 L 6 3 L 8 5 L 8 6 L 7 6 L 6 4 L 3 1 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -3 -11 L 0 -10 L 0 -8 L -1 1 M -1 -10 L -1 4 L 0 6 L 2 7 L 9 7 M 4 7 L 8 6 L 9 7 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -3 M -9 -3 L -3 -3 M 3 -4 L 9 -4 M 10 -3 L 7 -1 L 4 2 M 9 -4 L 4 2 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -9 -9 L -7 -8 L -6 -7 L -6 -6 L -7 -6 L -8 -8 L -9 -9 M 6 -10 L 8 -8 L 5 -2 L 2 2 L -1 5 L -4 7 L -6 8 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -6 8 M 9 -11 L 11 -9 M 11 -13 L 13 -11","-14 13 M -2 -12 L 0 -10 L -2 -6 L -4 -3 L -6 -1 L -8 1 M -1 -11 L -2 -8 L -4 -4 L -6 -1 M -1 -8 L 1 -7 L 6 -7 M 3 -7 L 5 -8 L 7 -6 M 7 -6 L 4 0 L 1 4 L -2 7 L -5 9 L -7 10 M 6 -7 L 5 -4 L 3 0 L 0 4 L -3 7 L -7 10 M -4 -2 L -1 -1 L 1 0 L 4 2 L 5 3 L 5 4 L 4 4 L 3 2 L 1 0 M 7 -10 L 9 -8 M 9 -12 L 11 -10","-14 13 M -6 -8 L -3 -8 L 2 -9 L 6 -11 M -3 -8 L 0 -9 L 2 -10 L 5 -12 L 6 -11 M -2 -8 L 1 -7 L 1 -1 L 0 3 L -2 6 L -5 9 M 0 -7 L 0 -1 L -1 3 L -3 7 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -4 M -9 -3 L -3 -3 M 3 -4 L 10 -4 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -9 -9 L -7 -8 L -6 -7 L -6 -6 L -7 -6 L -8 -8 L -9 -9 M -3 -10 L -1 -9 L 0 -8 L 0 -7 L -1 -7 L -2 -9 L -3 -10 M 6 -10 L 8 -8 L 5 -2 L 2 2 L -1 5 L -4 7 L -6 8 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -6 8 M 9 -11 L 11 -9 M 11 -13 L 13 -11","-14 13 M -6 -12 L -4 -10 L 5 -12 L 6 -11 M -5 -11 L 6 -11 M -10 -4 L -8 -2 L -3 -3 L 3 -4 L 8 -5 L 10 -4 M -9 -3 L -3 -3 M 3 -4 L 10 -4 M 1 -3 L 1 -1 L 0 3 L -2 6 L -5 9 M 0 -3 L 0 -1 L -1 3 L -3 7 M 8 -10 L 10 -8 M 10 -12 L 12 -10","-14 13 M -5 -12 L -2 -11 L -2 10 M -3 -11 L -3 9 L -2 10 M -2 -3 L 0 -3 L 3 -2 L 4 -1 L 4 0 L 3 0 L 2 -2 L 0 -3 M 2 -10 L 4 -8 M 4 -12 L 6 -10","-14 13 M -6 -6 L -4 -4 L -5 -1 L -6 1 L -8 3 L -10 4 M -5 -5 L -6 -1 L -8 3 M 4 -6 L 6 -4 L 8 -1 L 10 3 L 10 4 L 9 4 L 8 0 L 6 -4 M 7 -9 L 9 -7 M 9 -11 L 11 -9","-14 13 M -9 -11 L -6 -10 L -6 -8 L -7 1 M -7 -10 L -7 4 L -6 6 L -4 7 L 7 7 M -1 7 L 3 6 L 5 6 L 7 7 M -7 0 L -3 -1 L 0 -2 L 6 -4 M 0 -2 L 4 -4 L 6 -4 M 7 -9 L 9 -7 M 9 -11 L 11 -9","-14 13 M -8 -11 L -6 -9 L 7 -9 M -7 -10 L -3 -9 M 3 -9 L 6 -10 L 8 -8 M 8 -8 L 5 -2 L 3 1 L 0 4 L -3 6 L -5 7 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -5 7 M 9 -11 L 11 -9 M 11 -13 L 13 -11","-14 13 M -11 -1 L -9 1 L -2 -6 L -1 -6 L 9 4 M -10 0 L -8 -1 L -4 -4 M 3 -2 L 8 2 L 10 3 L 9 4 M 4 -8 L 6 -6 M 6 -10 L 8 -8","-14 13 M -2 -12 L 1 -11 L 1 7 L 0 9 L -1 7 L -3 6 M 0 -11 L 0 6 L -1 7 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -6 M -9 -5 L -3 -5 M 3 -6 L 10 -6 M -6 1 L -8 4 L -9 5 L -10 5 L -10 4 L -8 3 L -6 1 M 6 1 L 9 3 L 10 4 L 10 5 L 9 5 L 8 3 L 6 1 M 8 -11 L 10 -9 M 10 -13 L 12 -11","-14 13 M -6 -6 L -4 -4 L -5 -1 L -6 1 L -8 3 L -10 4 M -5 -5 L -6 -1 L -8 3 M 4 -6 L 6 -4 L 8 -1 L 10 3 L 10 4 L 9 4 L 8 0 L 6 -4 M 8 -11 L 7 -10 L 7 -8 L 8 -7 L 10 -7 L 11 -8 L 11 -10 L 10 -11 L 8 -11","-14 13 M -9 -11 L -6 -10 L -6 -8 L -7 1 M -7 -10 L -7 4 L -6 6 L -4 7 L 7 7 M -1 7 L 3 6 L 5 6 L 7 7 M -7 0 L -3 -1 L 0 -2 L 6 -4 M 0 -2 L 4 -4 L 6 -4 M 8 -11 L 7 -10 L 7 -8 L 8 -7 L 10 -7 L 11 -8 L 11 -10 L 10 -11 L 8 -11","-14 13 M -8 -11 L -6 -9 L 7 -9 M -7 -10 L -3 -9 M 3 -9 L 6 -10 L 8 -8 M 8 -8 L 5 -2 L 3 1 L 0 4 L -3 6 L -5 7 M 7 -9 L 6 -6 L 4 -2 L 1 2 L -2 5 L -5 7 M 10 -13 L 9 -12 L 9 -10 L 10 -9 L 12 -9 L 13 -10 L 13 -12 L 12 -13 L 10 -13","-14 13 M -11 -1 L -9 1 L -2 -6 L -1 -6 L 9 4 M -10 0 L -8 -1 L -4 -4 M 3 -2 L 8 2 L 10 3 L 9 4 M 5 -10 L 4 -9 L 4 -7 L 5 -6 L 7 -6 L 8 -7 L 8 -9 L 7 -10 L 5 -10","-14 13 M -2 -12 L 1 -11 L 1 7 L 0 9 L -1 7 L -3 6 M 0 -11 L 0 6 L -1 7 M -10 -6 L -8 -4 L -3 -5 L 3 -6 L 8 -7 L 10 -6 M -9 -5 L -3 -5 M 3 -6 L 10 -6 M -6 1 L -8 4 L -9 5 L -10 5 L -10 4 L -8 3 L -6 1 M 6 1 L 9 3 L 10 4 L 10 5 L 9 5 L 8 3 L 6 1 M 9 -13 L 8 -12 L 8 -10 L 9 -9 L 11 -9 L 12 -10 L 12 -12 L 11 -13 L 9 -13"},
        new string[] {"-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-7 7 M -1 -7 L -4 -6 L -6 -4 L -7 -1 L -7 1 L -6 4 L -4 6 L -1 7 L 1 7 L 4 6 L 6 4 L 7 1 L 7 -1 L 6 -4 L 4 -6 L 1 -7 L -1 -7","-6 6 M -6 -6 L -6 6 L 6 6 L 6 -6 L -6 -6","-7 7 M 0 -8 L -7 4 L 7 4 L 0 -8","-6 6 M 0 -10 L -6 0 L 0 10 L 6 0 L 0 -10","-8 8 M 0 -9 L -2 -3 L -8 -3 L -3 1 L -5 7 L 0 3 L 5 7 L 3 1 L 8 -3 L 2 -3 L 0 -9","-6 6 M -2 -6 L -2 -2 L -6 -2 L -6 2 L -2 2 L -2 6 L 2 6 L 2 2 L 6 2 L 6 -2 L 2 -2 L 2 -6 L -2 -6","-7 7 M 0 -7 L 0 7 M -7 0 L 7 0","-5 5 M -5 -5 L 5 5 M 5 -5 L -5 5","-5 5 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-4 4 M -1 -4 L -3 -3 L -4 -1 L -4 1 L -3 3 L -1 4 L 1 4 L 3 3 L 4 1 L 4 -1 L 3 -3 L 1 -4 L -1 -4 M -3 -1 L -3 1 M -2 -2 L -2 2 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -2 L 2 2 M 3 -1 L 3 1","-4 4 M -4 -4 L -4 4 L 4 4 L 4 -4 L -4 -4 M -3 -3 L -3 3 M -2 -3 L -2 3 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -3 L 2 3 M 3 -3 L 3 3","-5 5 M 0 -6 L -5 3 L 5 3 L 0 -6 M 0 -3 L -3 2 M 0 -3 L 3 2 M 0 0 L -1 2 M 0 0 L 1 2","-6 3 M -6 0 L 3 5 L 3 -5 L -6 0 M -3 0 L 2 3 M -3 0 L 2 -3 M 0 0 L 2 1 M 0 0 L 2 -1","-5 5 M 0 6 L 5 -3 L -5 -3 L 0 6 M 0 3 L 3 -2 M 0 3 L -3 -2 M 0 0 L 1 -2 M 0 0 L -1 -2","-3 6 M 6 0 L -3 -5 L -3 5 L 6 0 M 3 0 L -2 -3 M 3 0 L -2 3 M 0 0 L -2 -1 M 0 0 L -2 1","-14 14 M -14 0 L 14 0 M -14 0 L 0 16 M 14 0 L 0 16","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-7 7 M -1 -7 L -4 -6 L -6 -4 L -7 -1 L -7 1 L -6 4 L -4 6 L -1 7 L 1 7 L 4 6 L 6 4 L 7 1 L 7 -1 L 6 -4 L 4 -6 L 1 -7 L -1 -7","-6 6 M -6 -6 L -6 6 L 6 6 L 6 -6 L -6 -6","-7 7 M 0 -8 L -7 4 L 7 4 L 0 -8","-6 6 M 0 -10 L -6 0 L 0 10 L 6 0 L 0 -10","-8 8 M 0 -9 L -2 -3 L -8 -3 L -3 1 L -5 7 L 0 3 L 5 7 L 3 1 L 8 -3 L 2 -3 L 0 -9","-6 6 M -2 -6 L -2 -2 L -6 -2 L -6 2 L -2 2 L -2 6 L 2 6 L 2 2 L 6 2 L 6 -2 L 2 -2 L 2 -6 L -2 -6","-7 7 M 0 -7 L 0 7 M -7 0 L 7 0","-5 5 M -5 -5 L 5 5 M 5 -5 L -5 5","-5 5 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-4 4 M -1 -4 L -3 -3 L -4 -1 L -4 1 L -3 3 L -1 4 L 1 4 L 3 3 L 4 1 L 4 -1 L 3 -3 L 1 -4 L -1 -4 M -3 -1 L -3 1 M -2 -2 L -2 2 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -2 L 2 2 M 3 -1 L 3 1","-4 4 M -4 -4 L -4 4 L 4 4 L 4 -4 L -4 -4 M -3 -3 L -3 3 M -2 -3 L -2 3 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -3 L 2 3 M 3 -3 L 3 3","-5 5 M 0 -6 L -5 3 L 5 3 L 0 -6 M 0 -3 L -3 2 M 0 -3 L 3 2 M 0 0 L -1 2 M 0 0 L 1 2","-6 3 M -6 0 L 3 5 L 3 -5 L -6 0 M -3 0 L 2 3 M -3 0 L 2 -3 M 0 0 L 2 1 M 0 0 L 2 -1","-5 5 M 0 6 L 5 -3 L -5 -3 L 0 6 M 0 3 L 3 -2 M 0 3 L -3 -2 M 0 0 L 1 -2 M 0 0 L -1 -2","-3 6 M 6 0 L -3 -5 L -3 5 L 6 0 M 3 0 L -2 -3 M 3 0 L -2 3 M 0 0 L -2 -1 M 0 0 L -2 1","-14 14 M -14 0 L 14 0 M -14 0 L 0 16 M 14 0 L 0 16","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8","-8 8"},
        new string[] {"-8 8","-12 12 M 0 -8 L 0 9 M -8 0 L 8 0 M -8 9 L 8 9","-12 12 M 0 -8 L 0 9 M -8 -8 L 8 -8 M -8 0 L 8 0","-11 11 M -7 -7 L 7 7 M 7 -7 L -7 7","-2 3 M 0 -1 L 0 0 L 1 0 L 1 -1 L 0 -1","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 12 M 8 -12 L -8 -5 L 8 2 M -8 4 L 8 4 M -8 9 L 8 9","-12 12 M -8 -12 L 8 -5 L -8 2 M -8 4 L 8 4 M -8 9 L 8 9","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16 M 2 -14 L 0 -10 L -1 -7 L -2 -2 L -2 2 L -1 7 L 0 10 L 2 14","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 M -2 -14 L 0 -10 L 1 -7 L 2 -2 L 2 2 L 1 7 L 0 10 L -2 14","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-17 17 M -10 -16 L -10 16 M -9 -16 L -9 16 M 9 -16 L 9 16 M 10 -16 L 10 16 M -14 -16 L 14 -16 M -14 16 L -5 16 M 5 16 L 14 16","-16 15 M -11 -16 L -1 -2 L -12 16 M -12 -16 L -2 -2 M -13 -16 L -2 -1 M -13 -16 L 10 -16 L 12 -9 L 9 -16 M -11 15 L 10 15 M -12 16 L 10 16 L 12 9 L 9 16","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-13 13 M 7 -9 L -7 9 M -9 -3 L 9 -3 M -9 3 L 9 3","-13 13 M -9 -5 L 9 -5 M -9 0 L 9 0 M -9 5 L 9 5","-9 10 M 6 -5 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-10 9 M -6 -12 L -6 9 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M 6 -12 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 9 M -6 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-5 7 M 5 -12 L 3 -12 L 1 -11 L 0 -8 L 0 9 M -3 -5 L 4 -5","-9 10 M 6 -5 L 6 11 L 5 14 L 4 15 L 2 16 L -1 16 L -3 15 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M -5 -12 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-4 4 M -1 -12 L 0 -11 L 1 -12 L 0 -13 L -1 -12 M 0 -5 L 0 9","-5 5 M 0 -12 L 1 -11 L 2 -12 L 1 -13 L 0 -12 M 1 -5 L 1 12 L 0 15 L -2 16 L -4 16","-9 8 M -5 -12 L -5 9 M 5 -5 L -5 5 M -1 1 L 6 9","-4 4 M 0 -12 L 0 9","-15 15 M -11 -5 L -11 9 M -11 -1 L -8 -4 L -6 -5 L -3 -5 L -1 -4 L 0 -1 L 0 9 M 0 -1 L 3 -4 L 5 -5 L 8 -5 L 10 -4 L 11 -1 L 11 9","-9 10 M -5 -5 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-9 10 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 2 -5 L -1 -5","-10 9 M -6 -5 L -6 16 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 10 M 6 -5 L 6 16 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-7 6 M -3 -5 L -3 9 M -3 1 L -2 -2 L 0 -4 L 2 -5 L 5 -5","-8 9 M 6 -2 L 5 -4 L 2 -5 L -1 -5 L -4 -4 L -5 -2 L -4 0 L -2 1 L 3 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -1 9 L -4 8 L -5 6","-5 7 M 0 -12 L 0 5 L 1 8 L 3 9 L 5 9 M -3 -5 L 4 -5","-9 10 M -5 -5 L -5 5 L -4 8 L -2 9 L 1 9 L 3 8 L 6 5 M 6 -5 L 6 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9","-11 11 M -8 -5 L -4 9 M 0 -5 L -4 9 M 0 -5 L 4 9 M 8 -5 L 4 9","-8 9 M -5 -5 L 6 9 M 6 -5 L -5 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9 L -2 13 L -4 15 L -6 16 L -7 16","-8 9 M 6 -5 L -5 9 M -5 -5 L 6 -5 M -5 9 L 6 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-12 13 M 9 5 L 7 5 L 5 4 L 3 2 L 0 -2 L -1 -3 L -3 -4 L -5 -4 L -7 -3 L -8 -1 L -8 1 L -7 3 L -5 4 L -3 4 L -1 3 L 0 2 L 3 -2 L 5 -4 L 7 -5 L 9 -5","-12 13 M 10 1 L 9 3 L 7 4 L 5 4 L 3 3 L 2 2 L -1 -2 L -2 -3 L -4 -4 L -6 -4 L -8 -3 L -9 -1 L -9 1 L -8 3 L -6 4 L -4 4 L -2 3 L -1 2 L 2 -2 L 3 -3 L 5 -4 L 7 -4 L 9 -3 L 10 -1 L 10 1","-7 7 M -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -4 L 1 -4 L 3 -5 L 4 -7 L 4 -9 L 3 -11 L 1 -12 L -1 -12","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-13 9 M -10 -5 L -6 -5 L 0 7 M -7 -5 L 0 9 M 9 -16 L 0 9","-17 16 M -14 -5 L -9 -5 L 0 7 M -10 -4 L 0 9 M 16 -24 L 0 9","-12 12 M 8 -8 L 1 -8 L -3 -7 L -5 -6 L -7 -4 L -8 -1 L -8 1 L -7 4 L -5 6 L -3 7 L 1 8 L 8 8","-12 12 M -8 -8 L -8 -1 L -7 3 L -6 5 L -4 7 L -1 8 L 1 8 L 4 7 L 6 5 L 7 3 L 8 -1 L 8 -8","-12 12 M -8 -8 L -1 -8 L 3 -7 L 5 -6 L 7 -4 L 8 -1 L 8 1 L 7 4 L 5 6 L 3 7 L -1 8 L -8 8","-12 12 M -8 8 L -8 1 L -7 -3 L -6 -5 L -4 -7 L -1 -8 L 1 -8 L 4 -7 L 6 -5 L 7 -3 L 8 1 L 8 8","-12 12 M 8 -8 L 1 -8 L -3 -7 L -5 -6 L -7 -4 L -8 -1 L -8 1 L -7 4 L -5 6 L -3 7 L 1 8 L 8 8 M -8 0 L 4 0","-13 13 M 6 -2 L 9 0 L 6 2 M 3 -5 L 8 0 L 3 5 M -9 0 L 8 0","-8 8 M -2 -6 L 0 -9 L 2 -6 M -5 -3 L 0 -8 L 5 -3 M 0 -8 L 0 9","-13 13 M -6 -2 L -9 0 L -6 2 M -3 -5 L -8 0 L -3 5 M -8 0 L 9 0","-8 8 M -2 6 L 0 9 L 2 6 M -5 3 L 0 8 L 5 3 M 0 -9 L 0 8","-9 10 M 6 0 L 5 -3 L 4 -4 L 2 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 L 6 3 L 7 -2 L 7 -7 L 6 -10 L 5 -11 L 3 -12 L 0 -12 L -2 -11 L -3 -10 L -3 -9 L -2 -9 L -2 -10 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 6 L 5 3 L 6 -2 L 6 -7 L 5 -10 L 3 -12","-10 10 M -8 -12 L 0 9 M -7 -12 L 0 7 M 8 -12 L 0 9 M -8 -12 L 8 -12 M -7 -11 L 7 -11","-17 16 M -14 -5 L -9 -5 L 0 7 M -10 -4 L 0 9 M 16 -24 L 0 9","-12 12 M 9 -15 L 8 -14 L 9 -13 L 10 -14 L 10 -15 L 9 -16 L 7 -16 L 5 -15 L 3 -13 L 2 -11 L 1 -8 L 0 -4 L -2 8 L -3 12 L -4 14 M 4 -14 L 3 -12 L 2 -8 L 0 4 L -1 8 L -2 11 L -3 13 L -5 15 L -7 16 L -9 16 L -10 15 L -10 14 L -9 13 L -8 14 L -9 15","-15 15 M 11 -36 L 10 -36 L 9 -35 L 9 -34 L 10 -33 L 11 -33 L 12 -34 L 12 -36 L 11 -38 L 9 -39 L 7 -39 L 5 -38 L 3 -36 L 2 -34 L 1 -31 L 0 -24 L -1 -8 L -1 24 L -2 33 L -3 36 M 10 -35 L 10 -34 L 11 -34 L 11 -35 L 10 -35 M 0 -24 L 0 24 M 3 -36 L 2 -33 L 1 -24 L 1 8 L 0 24 L -1 31 L -2 34 L -3 36 L -5 38 L -7 39 L -9 39 L -11 38 L -12 36 L -12 34 L -11 33 L -10 33 L -9 34 L -9 35 L -10 36 L -11 36 M -11 34 L -11 35 L -10 35 L -10 34 L -11 34","-9 9 M 6 -39 L 3 -33 L 0 -26 L -2 -21 L -3 -17 L -4 -12 L -5 -4 L -5 4 L -4 12 L -3 17 L -2 21 L 0 26 L 3 33 L 6 39 M 3 -33 L 1 -28 L -1 -22 L -2 -18 L -3 -12 L -4 -4 L -4 4 L -3 12 L -2 18 L -1 22 L 1 28 L 3 33","-9 9 M -6 -39 L -3 -33 L 0 -26 L 2 -21 L 3 -17 L 4 -12 L 5 -4 L 5 4 L 4 12 L 3 17 L 2 21 L 0 26 L -3 33 L -6 39 M -3 -33 L -1 -28 L 1 -22 L 2 -18 L 3 -12 L 4 -4 L 4 4 L 3 12 L 2 18 L 1 22 L -1 28 L -3 33","-9 9 M -5 -39 L -5 0 L -5 39 M -4 -39 L -4 0 L -4 39 M -5 -39 L 6 -39 M -5 39 L 6 39","-9 9 M 4 -39 L 4 0 L 4 39 M 5 -39 L 5 0 L 5 39 M -6 -39 L 5 -39 M -6 39 L 5 39","-9 10 M 6 -12 L 6 9 M -7 -12 L 6 -12 M -2 -2 L 6 -2 M -7 9 L 6 9","-10 10 M -7 -9 L -6 -7 L 6 5 L 7 7 L 7 9 M -6 -6 L 6 6 M -7 -9 L -7 -7 L -6 -5 L 6 7 L 7 9 M -2 -2 L -6 2 L -7 4 L -7 6 L -6 8 L -7 9 M -7 4 L -5 8 M -6 2 L -6 4 L -5 6 L -5 8 L -7 9 M 1 1 L 6 -4 M 4 -9 L 4 -6 L 5 -4 L 7 -4 L 7 -6 L 5 -7 L 4 -9 M 4 -9 L 5 -6 L 7 -4","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 0 L 9 0 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-7 7 M -3 -16 L -3 16 M 3 -16 L 3 16","-12 12 M 0 -16 L 0 9 M -9 9 L 9 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-12 12 M 9 -16 L -9 9 L 9 9","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 7 L -10 8 L -9 9 L -8 8 L -9 7 M 9 7 L 8 8 L 9 9 L 10 8 L 9 7","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3"},
        new string[] {"-8 8","-12 12 M 0 -8 L 0 9 M -8 0 L 8 0 M -8 9 L 8 9","-12 12 M 0 -8 L 0 9 M -8 -8 L 8 -8 M -8 0 L 8 0","-11 11 M -7 -7 L 7 7 M 7 -7 L -7 7","-2 3 M 0 -1 L 0 0 L 1 0 L 1 -1 L 0 -1","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 12 M 8 -12 L -8 -5 L 8 2 M -8 4 L 8 4 M -8 9 L 8 9","-12 12 M -8 -12 L 8 -5 L -8 2 M -8 4 L 8 4 M -8 9 L 8 9","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16 M 2 -14 L 0 -10 L -1 -7 L -2 -2 L -2 2 L -1 7 L 0 10 L 2 14","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 M -2 -14 L 0 -10 L 1 -7 L 2 -2 L 2 2 L 1 7 L 0 10 L -2 14","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-5 5 M 1 8 L 0 9 L -1 8 L 0 7 L 1 8 L 1 10 L 0 12 L -1 13","-13 13 M -9 0 L 9 0","-5 5 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-17 17 M -10 -16 L -10 16 M -9 -16 L -9 16 M 9 -16 L 9 16 M 10 -16 L 10 16 M -14 -16 L 14 -16 M -14 16 L -5 16 M 5 16 L 14 16","-16 15 M -11 -16 L -1 -2 L -12 16 M -12 -16 L -2 -2 M -13 -16 L -2 -1 M -13 -16 L 10 -16 L 12 -9 L 9 -16 M -11 15 L 10 15 M -12 16 L 10 16 L 12 9 L 9 16","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-13 13 M 7 -9 L -7 9 M -9 -3 L 9 -3 M -9 3 L 9 3","-13 13 M -9 -5 L 9 -5 M -9 0 L 9 0 M -9 5 L 9 5","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9 M -5 2 L 5 2","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 M -7 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -7 9","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4","-11 10 M -7 -12 L -7 9 M -7 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 0 9 L -7 9","-10 9 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2 M -6 9 L 7 9","-10 8 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4 L 8 1 M 3 1 L 8 1","-11 11 M -7 -12 L -7 9 M 7 -12 L 7 9 M -7 -2 L 7 -2","-4 4 M 0 -12 L 0 9","-8 8 M 4 -12 L 4 4 L 3 7 L 2 8 L 0 9 L -2 9 L -4 8 L -5 7 L -6 4 L -6 2","-11 10 M -7 -12 L -7 9 M 7 -12 L -7 2 M -2 -3 L 7 9","-10 7 M -6 -12 L -6 9 M -6 9 L 6 9","-12 12 M -8 -12 L -8 9 M -8 -12 L 0 9 M 8 -12 L 0 9 M 8 -12 L 8 9","-11 11 M -7 -12 L -7 9 M -7 -12 L 7 9 M 7 -12 L 7 9","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 L -7 -1","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M 1 5 L 7 11","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 L -7 -2 M 0 -2 L 7 9","-10 10 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-8 8 M 0 -12 L 0 9 M -7 -12 L 7 -12","-11 11 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -12","-9 9 M -8 -12 L 0 9 M 8 -12 L 0 9","-12 12 M -10 -12 L -5 9 M 0 -12 L -5 9 M 0 -12 L 5 9 M 10 -12 L 5 9","-10 10 M -7 -12 L 7 9 M 7 -12 L -7 9","-9 9 M -8 -12 L 0 -2 L 0 9 M 8 -12 L 0 -2","-10 10 M 7 -12 L -7 9 M -7 -12 L 7 -12 M -7 9 L 7 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-12 13 M 9 5 L 7 5 L 5 4 L 3 2 L 0 -2 L -1 -3 L -3 -4 L -5 -4 L -7 -3 L -8 -1 L -8 1 L -7 3 L -5 4 L -3 4 L -1 3 L 0 2 L 3 -2 L 5 -4 L 7 -5 L 9 -5","-12 13 M 10 1 L 9 3 L 7 4 L 5 4 L 3 3 L 2 2 L -1 -2 L -2 -3 L -4 -4 L -6 -4 L -8 -3 L -9 -1 L -9 1 L -8 3 L -6 4 L -4 4 L -2 3 L -1 2 L 2 -2 L 3 -3 L 5 -4 L 7 -4 L 9 -3 L 10 -1 L 10 1","-7 7 M -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -4 L 1 -4 L 3 -5 L 4 -7 L 4 -9 L 3 -11 L 1 -12 L -1 -12","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 9 M -10 -5 L -6 -5 L 0 7 M -7 -5 L 0 9 M 9 -16 L 0 9","-17 16 M -14 -5 L -9 -5 L 0 7 M -10 -4 L 0 9 M 16 -24 L 0 9","-12 12 M 8 -8 L 1 -8 L -3 -7 L -5 -6 L -7 -4 L -8 -1 L -8 1 L -7 4 L -5 6 L -3 7 L 1 8 L 8 8","-12 12 M -8 -8 L -8 -1 L -7 3 L -6 5 L -4 7 L -1 8 L 1 8 L 4 7 L 6 5 L 7 3 L 8 -1 L 8 -8","-12 12 M -8 -8 L -1 -8 L 3 -7 L 5 -6 L 7 -4 L 8 -1 L 8 1 L 7 4 L 5 6 L 3 7 L -1 8 L -8 8","-12 12 M -8 8 L -8 1 L -7 -3 L -6 -5 L -4 -7 L -1 -8 L 1 -8 L 4 -7 L 6 -5 L 7 -3 L 8 1 L 8 8","-12 12 M 8 -8 L 1 -8 L -3 -7 L -5 -6 L -7 -4 L -8 -1 L -8 1 L -7 4 L -5 6 L -3 7 L 1 8 L 8 8 M -8 0 L 4 0","-13 13 M 6 -2 L 9 0 L 6 2 M 3 -5 L 8 0 L 3 5 M -9 0 L 8 0","-8 8 M -2 -6 L 0 -9 L 2 -6 M -5 -3 L 0 -8 L 5 -3 M 0 -8 L 0 9","-13 13 M -6 -2 L -9 0 L -6 2 M -3 -5 L -8 0 L -3 5 M -8 0 L 9 0","-8 8 M -2 6 L 0 9 L 2 6 M -5 3 L 0 8 L 5 3 M 0 -9 L 0 8","-9 10 M 6 0 L 5 -3 L 4 -4 L 2 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 L 6 3 L 7 -2 L 7 -7 L 6 -10 L 5 -11 L 3 -12 L 0 -12 L -2 -11 L -3 -10 L -3 -9 L -2 -9 L -2 -10 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 6 L 5 3 L 6 -2 L 6 -7 L 5 -10 L 3 -12","-10 10 M -8 -12 L 0 9 M -7 -12 L 0 7 M 8 -12 L 0 9 M -8 -12 L 8 -12 M -7 -11 L 7 -11","-17 16 M -14 -5 L -9 -5 L 0 7 M -10 -4 L 0 9 M 16 -24 L 0 9","-12 12 M 9 -15 L 8 -14 L 9 -13 L 10 -14 L 10 -15 L 9 -16 L 7 -16 L 5 -15 L 3 -13 L 2 -11 L 1 -8 L 0 -4 L -2 8 L -3 12 L -4 14 M 4 -14 L 3 -12 L 2 -8 L 0 4 L -1 8 L -2 11 L -3 13 L -5 15 L -7 16 L -9 16 L -10 15 L -10 14 L -9 13 L -8 14 L -9 15","-15 15 M 11 -36 L 10 -36 L 9 -35 L 9 -34 L 10 -33 L 11 -33 L 12 -34 L 12 -36 L 11 -38 L 9 -39 L 7 -39 L 5 -38 L 3 -36 L 2 -34 L 1 -31 L 0 -24 L -1 -8 L -1 24 L -2 33 L -3 36 M 10 -35 L 10 -34 L 11 -34 L 11 -35 L 10 -35 M 0 -24 L 0 24 M 3 -36 L 2 -33 L 1 -24 L 1 8 L 0 24 L -1 31 L -2 34 L -3 36 L -5 38 L -7 39 L -9 39 L -11 38 L -12 36 L -12 34 L -11 33 L -10 33 L -9 34 L -9 35 L -10 36 L -11 36 M -11 34 L -11 35 L -10 35 L -10 34 L -11 34","-9 9 M 6 -39 L 3 -33 L 0 -26 L -2 -21 L -3 -17 L -4 -12 L -5 -4 L -5 4 L -4 12 L -3 17 L -2 21 L 0 26 L 3 33 L 6 39 M 3 -33 L 1 -28 L -1 -22 L -2 -18 L -3 -12 L -4 -4 L -4 4 L -3 12 L -2 18 L -1 22 L 1 28 L 3 33","-9 9 M -6 -39 L -3 -33 L 0 -26 L 2 -21 L 3 -17 L 4 -12 L 5 -4 L 5 4 L 4 12 L 3 17 L 2 21 L 0 26 L -3 33 L -6 39 M -3 -33 L -1 -28 L 1 -22 L 2 -18 L 3 -12 L 4 -4 L 4 4 L 3 12 L 2 18 L 1 22 L -1 28 L -3 33","-9 9 M -5 -39 L -5 0 L -5 39 M -4 -39 L -4 0 L -4 39 M -5 -39 L 6 -39 M -5 39 L 6 39","-9 9 M 4 -39 L 4 0 L 4 39 M 5 -39 L 5 0 L 5 39 M -6 -39 L 5 -39 M -6 39 L 5 39","-9 10 M 6 -12 L 6 9 M -7 -12 L 6 -12 M -2 -2 L 6 -2 M -7 9 L 6 9","-10 10 M -7 -9 L -6 -7 L 6 5 L 7 7 L 7 9 M -6 -6 L 6 6 M -7 -9 L -7 -7 L -6 -5 L 6 7 L 7 9 M -2 -2 L -6 2 L -7 4 L -7 6 L -6 8 L -7 9 M -7 4 L -5 8 M -6 2 L -6 4 L -5 6 L -5 8 L -7 9 M 1 1 L 6 -4 M 4 -9 L 4 -6 L 5 -4 L 7 -4 L 7 -6 L 5 -7 L 4 -9 M 4 -9 L 5 -6 L 7 -4","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 0 L 9 0 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-7 7 M -3 -16 L -3 16 M 3 -16 L 3 16","-12 12 M 0 -16 L 0 9 M -9 9 L 9 9","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-12 12 M 9 -16 L -9 9 L 9 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 7 L -10 8 L -9 9 L -8 8 L -9 7 M 9 7 L 8 8 L 9 9 L 10 8 L 9 7","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3"},
        new string[] {"-8 8","-2 1 M 1 0 L 0 1 L -1 1 L -2 0 L -2 -1 L -1 -2 L 0 -2 L 1 -1 L 1 1 L 0 3 L -1 4 M -1 -1 L -1 0 L 0 0 L 0 -1 L -1 -1","-2 2 M -1 -2 L -2 -1 L -2 1 L -1 2 L 1 2 L 2 1 L 2 -1 L 1 -2 L -1 -2 M 0 -1 L -1 0 L 0 1 L 1 0 L 0 -1","-4 4 M -2 -3 L 2 3 M 2 -3 L -2 3 M -4 0 L 4 0","-5 5 M 0 -7 L -1 -5 L -3 -2 L -5 0 M 0 -7 L 1 -5 L 3 -2 L 5 0 M 0 -5 L -3 -1 M 0 -5 L 3 -1 M 0 -3 L -2 -1 M 0 -3 L 2 -1 M -1 -1 L 1 -1 M -5 0 L 5 0","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-5 5 M -5 0 L -5 -1 L -4 -3 L -3 -4 L -1 -5 L 1 -5 L 3 -4 L 4 -3 L 5 -1 L 5 0 M -2 -4 L 2 -4 M -3 -3 L 3 -3 M -4 -2 L 4 -2 M -4 -1 L 4 -1 M -5 0 L 5 0","-6 0 M -6 -12 L -6 0 L 0 0 L -6 -12 M -6 -9 L -2 -1 M -6 -6 L -3 0 M -6 -3 L -5 -1","-5 5 M 0 -7 L -1 -5 L -3 -2 L -5 0 M 0 -7 L 1 -5 L 3 -2 L 5 0","-5 5 M 5 0 L 5 -1 L 4 -3 L 3 -4 L 1 -5 L -1 -5 L -3 -4 L -4 -3 L -5 -1 L -5 0","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-11 11 M 11 0 L 11 -2 L 10 -5 L 8 -8 L 5 -10 L 2 -11 L -2 -11 L -5 -10 L -8 -8 L -10 -5 L -11 -2 L -11 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9","-10 10 M -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 3 -1 L -7 9 L 7 9","-10 10 M -5 -12 L 6 -12 L 0 -4 L 3 -4 L 5 -3 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 3 -12 L -7 2 L 8 2 M 3 -12 L 3 9","-10 10 M 5 -12 L -5 -12 L -6 -3 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5","-10 10 M 6 -9 L 5 -11 L 2 -12 L 0 -12 L -3 -11 L -5 -8 L -6 -3 L -6 2 L -5 6 L -3 8 L 0 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2","-10 10 M 7 -12 L -3 9 M -7 -12 L 7 -12","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 1 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 2 L -6 0 L -4 -2 L -1 -3 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 2 -12 L -2 -12","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -5 L 6 0 L 5 5 L 3 8 L 0 9 L -2 9 L -5 8 L -6 6","-5 5 M -5 0 L -5 1 L -4 3 L -3 4 L -1 5 L 1 5 L 3 4 L 4 3 L 5 1 L 5 0","-6 6 M -6 -2 L -4 0 L -1 1 L 1 1 L 4 0 L 6 -2","0 3 M 0 3 L 2 2 L 3 0 L 2 -2 L 0 -3","0 4 M 0 0 L 3 -2 L 4 -4 L 4 -6 L 3 -7 L 2 -7","-4 0 M 0 0 L -3 -2 L -4 -4 L -4 -6 L -3 -7 L -2 -7","-9 9 M -5 -8 L -4 -7 L -5 -6 L -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 1 -12 L 3 -11 L 4 -10 L 5 -8 L 5 -6 L 4 -4 L 2 -2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-9 9 M 0 -12 L -8 9 M 0 -12 L 8 9 M -5 2 L 5 2","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 M -7 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -7 9","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4","-11 10 M -7 -12 L -7 9 M -7 -12 L 0 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 0 9 L -7 9","-10 9 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2 M -6 9 L 7 9","-10 8 M -6 -12 L -6 9 M -6 -12 L 7 -12 M -6 -2 L 2 -2","-10 11 M 8 -7 L 7 -9 L 5 -11 L 3 -12 L -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 L 3 9 L 5 8 L 7 6 L 8 4 L 8 1 M 3 1 L 8 1","-11 11 M -7 -12 L -7 9 M 7 -12 L 7 9 M -7 -2 L 7 -2","-4 4 M 0 -12 L 0 9","-8 8 M 4 -12 L 4 4 L 3 7 L 2 8 L 0 9 L -2 9 L -4 8 L -5 7 L -6 4 L -6 2","-11 10 M -7 -12 L -7 9 M 7 -12 L -7 2 M -2 -3 L 7 9","-10 7 M -6 -12 L -6 9 M -6 9 L 6 9","-12 12 M -8 -12 L -8 9 M -8 -12 L 0 9 M 8 -12 L 0 9 M 8 -12 L 8 9","-11 11 M -7 -12 L -7 9 M -7 -12 L 7 9 M 7 -12 L 7 9","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 2 -1 L -7 -1","-11 11 M -2 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -2 9 L 2 9 L 4 8 L 6 6 L 7 4 L 8 1 L 8 -4 L 7 -7 L 6 -9 L 4 -11 L 2 -12 L -2 -12 M 1 5 L 7 11","-11 10 M -7 -12 L -7 9 M -7 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 2 -2 L -7 -2 M 0 -2 L 7 9","-10 10 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-8 8 M 0 -12 L 0 9 M -7 -12 L 7 -12","-11 11 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -12","-9 9 M -8 -12 L 0 9 M 8 -12 L 0 9","-12 12 M -10 -12 L -5 9 M 0 -12 L -5 9 M 0 -12 L 5 9 M 10 -12 L 5 9","-10 10 M -7 -12 L 7 9 M 7 -12 L -7 9","-9 9 M -8 -12 L 0 -2 L 0 9 M 8 -12 L 0 -2","-10 10 M 7 -12 L -7 9 M -7 -12 L 7 -12 M -7 9 L 7 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-10 10 M 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 1 L 7 3 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6","-11 11 M 0 0 L 2 3 L 3 4 L 5 5 L 7 5 L 9 4 L 10 3 L 11 1 L 11 -1 L 10 -3 L 9 -4 L 7 -5 L 5 -5 L 3 -4 L 2 -3 L -2 3 L -3 4 L -5 5 L -7 5 L -9 4 L -10 3 L -11 1 L -11 -1 L -10 -3 L -9 -4 L -7 -5 L -5 -5 L -3 -4 L -2 -3 L 0 0","-11 11 M -9 5 L -10 4 L -11 2 L -11 -1 L -10 -3 L -9 -4 L -7 -5 L -5 -5 L -3 -4 L -2 -3 L 2 3 L 3 4 L 5 5 L 7 5 L 9 4 L 10 3 L 11 1 L 11 -2 L 10 -4 L 9 -5","-9 10 M 6 -5 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-10 9 M -6 -12 L -6 9 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M 6 -12 L 6 9 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 9 M -6 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-5 7 M 5 -12 L 3 -12 L 1 -11 L 0 -8 L 0 9 M -3 -5 L 4 -5","-9 10 M 6 -5 L 6 11 L 5 14 L 4 15 L 2 16 L -1 16 L -3 15 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-9 10 M -5 -12 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-4 4 M -1 -12 L 0 -11 L 1 -12 L 0 -13 L -1 -12 M 0 -5 L 0 9","-5 5 M 0 -12 L 1 -11 L 2 -12 L 1 -13 L 0 -12 M 1 -5 L 1 12 L 0 15 L -2 16 L -4 16","-9 8 M -5 -12 L -5 9 M 5 -5 L -5 5 M -1 1 L 6 9","-4 4 M 0 -12 L 0 9","-15 15 M -11 -5 L -11 9 M -11 -1 L -8 -4 L -6 -5 L -3 -5 L -1 -4 L 0 -1 L 0 9 M 0 -1 L 3 -4 L 5 -5 L 8 -5 L 10 -4 L 11 -1 L 11 9","-9 10 M -5 -5 L -5 9 M -5 -1 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -1 L 6 9","-9 10 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 2 -5 L -1 -5","-10 9 M -6 -5 L -6 16 M -6 -2 L -4 -4 L -2 -5 L 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 L -2 9 L -4 8 L -6 6","-9 10 M 6 -5 L 6 16 M 6 -2 L 4 -4 L 2 -5 L -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 L 2 9 L 4 8 L 6 6","-7 6 M -3 -5 L -3 9 M -3 1 L -2 -2 L 0 -4 L 2 -5 L 5 -5","-8 9 M 6 -2 L 5 -4 L 2 -5 L -1 -5 L -4 -4 L -5 -2 L -4 0 L -2 1 L 3 2 L 5 3 L 6 5 L 6 6 L 5 8 L 2 9 L -1 9 L -4 8 L -5 6","-5 7 M 0 -12 L 0 5 L 1 8 L 3 9 L 5 9 M -3 -5 L 4 -5","-9 10 M -5 -5 L -5 5 L -4 8 L -2 9 L 1 9 L 3 8 L 6 5 M 6 -5 L 6 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9","-11 11 M -8 -5 L -4 9 M 0 -5 L -4 9 M 0 -5 L 4 9 M 8 -5 L 4 9","-8 9 M -5 -5 L 6 9 M 6 -5 L -5 9","-8 8 M -6 -5 L 0 9 M 6 -5 L 0 9 L -2 13 L -4 15 L -6 16 L -7 16","-8 9 M 6 -5 L -5 9 M -5 -5 L 6 -5 M -5 9 L 6 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-10 10 M -7 -12 L -7 9 M -10 -12 L 9 -12 L -1 -2 L 9 8 M 8 4 L 9 7 L 10 9 M 8 4 L 8 7 M 5 7 L 8 7 M 5 7 L 8 8 L 10 9","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-7 7 M 3 -17 L 0 -16 L -2 -15 L -4 -13 L -6 -10 L -7 -6 L -7 0 L -6 3 L -4 5 L -1 6 L 1 6 L 4 5 L 6 3 L 7 0 M -7 -2 L -6 -5 L -4 -7 L -1 -8 L 1 -8 L 4 -7 L 6 -5 L 7 -2 L 7 4 L 6 8 L 4 11 L 2 13 L 0 14 L -3 15","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3"},
        new string[] {"-8 8","-5 5 M 0 -12 L -1 -10 L 0 2 L 1 -10 L 0 -12 M 0 -10 L 0 -4 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -5 -2 L -1 0 L 2 2 L 4 4 L 5 7 L 5 9 L 4 11 L 3 12 M -5 -1 L 1 2 M -5 0 L -2 1 L 2 3 L 4 5 L 5 7","-8 8 M 5 -7 L 4 -5 L 2 -3 L -2 -1 L -5 0 M 1 -2 L -5 1 M 3 -12 L 4 -11 L 5 -9 L 5 -7 L 4 -4 L 2 -2 L -1 0 L -5 2","-10 10 M 1 -5 L -3 -4 L -6 -2 L -7 0 L -7 2 L -6 4 L -4 5 L -1 5 L 3 4 L 6 2 L 7 0 L 7 -2 L 6 -4 L 4 -5 L 1 -5 M 6 -4 L 1 -5 M 4 -5 L -1 -4 L -6 -2 M -3 -4 L -7 0 M -6 4 L -1 5 M -4 5 L 1 4 L 6 2 M 3 4 L 7 0","-10 10 M 1 -5 L -3 -4 L -6 -2 L -7 0 L -7 2 L -6 4 L -4 5 L -1 5 L 3 4 L 6 2 L 7 0 L 7 -2 L 6 -4 L 4 -5 L 1 -5 M 6 -4 L 1 -5 M 4 -5 L -1 -4 L -6 -2 M -3 -4 L -7 0 M -6 4 L -1 5 M -4 5 L 1 4 L 6 2 M 3 4 L 7 0","-8 9 M 1 -5 L -2 -4 L -4 -2 L -5 0 L -5 2 L -4 4 L -2 5 L 0 5 L 3 4 L 5 2 L 6 0 L 6 -2 L 5 -4 L 3 -5 L 1 -5 M -3 -2 L 3 -5 M -4 0 L 4 -4 M -5 2 L 5 -3 M -4 3 L 6 -2 M -3 4 L 5 0 M -2 5 L 4 2","-8 8 M -3 -11 L -3 12 M 3 -12 L 3 11 M -5 -4 L 5 -6 M -5 -3 L 5 -5 M -5 5 L 5 3 M -5 6 L 5 4","-8 8 M -4 -12 L -4 6 M 4 -6 L 4 12 M -4 -4 L 4 -6 M -4 -3 L 4 -5 M -4 5 L 4 3 M -4 6 L 4 4","-8 8 M -4 -16 L -4 5 M -4 -4 L -1 -6 L 2 -6 L 4 -5 L 5 -3 L 5 -1 L 4 1 L 1 3 L -1 4 L -4 5 M -4 -4 L -1 -5 L 2 -5 L 4 -4 M 3 -5 L 4 -3 L 4 -1 L 3 1 L 1 3","-13 13 M -10 -9 L -10 -6 M 10 -9 L 10 -6 M -10 -9 L 10 -9 M -10 -8 L 10 -8 M -10 -7 L 10 -7 M -10 -6 L 10 -6","-8 8 M -5 -4 L -5 -1 M 5 -4 L 5 -1 M -5 -4 L 5 -4 M -5 -3 L 5 -3 M -5 -2 L 5 -2 M -5 -1 L 5 -1","-8 8 M -5 -6 L 5 6 M -5 -6 L -3 -4 L -1 -3 L 2 -3 L 4 -4 L 5 -5 L 5 -7 L 3 -7 L 3 -5 L 2 -3 M -3 -4 L 2 -3 M -1 -3 L 5 -5 M 4 -7 L 4 -4 M 3 -6 L 5 -6 M 5 6 L 3 4 L 1 3 L -2 3 L -4 4 L -5 5 L -5 7 L -3 7 L -3 5 L -2 3 M 3 4 L -2 3 M 1 3 L -5 5 M -4 4 L -4 7 M -5 6 L -3 6","-8 8 M -2 -3 L -3 -5 L -3 -7 L -5 -7 L -5 -5 L -4 -4 L -2 -3 L 1 -3 L 3 -4 L 5 -6 M -4 -7 L -4 -4 M -5 -6 L -3 -6 M -5 -5 L 1 -3 M -2 -3 L 3 -4 M 5 -6 L 5 7","-8 8 M -1 -15 L 4 -5 L 0 2 L 0 3 M 3 -6 L -1 1 M 2 -9 L 2 -7 L -2 0 L 0 3 L 3 7 M 5 10 L 3 7 L 1 6 L -1 6 L -3 7 L -4 9 L -4 11 L -3 13 L 0 15 M 5 10 L 3 8 L 1 7 L -3 7 L -3 11 L -2 13 L 0 15 M 1 6 L -2 8 L -4 11","-13 20 M -4 1 L -3 3 L -1 4 L 1 4 L 3 3 L 4 1 L 4 -1 L 3 -3 L 1 -4 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 4 L -4 7 L -2 9 L 1 10 L 4 10 L 7 9 L 9 7 L 10 5 L 11 2 L 11 -2 L 10 -5 L 8 -8 L 6 -9 L 3 -10 L 0 -10 L -3 -9 L -5 -8 L -7 -6 L -9 -3 L -10 1 L -10 6 L -9 11 L -7 15 L -5 17 L -2 19 L 2 20 L 7 20 L 11 19 L 14 17 L 16 15 M -7 -6 L -8 -4 L -9 0 L -9 6 L -8 10 L -6 14 L -4 16 L -1 18 L 3 19 L 7 19 L 11 18 L 13 17 L 16 15 M -2 -3 L 2 -3 M -3 -2 L 3 -2 M -4 -1 L 4 -1 M -4 0 L 4 0 M -4 1 L 4 1 M -3 2 L 3 2 M -2 3 L 2 3 M 15 -6 L 15 -4 L 17 -4 L 17 -6 L 15 -6 M 16 -6 L 16 -4 M 15 -5 L 17 -5 M 15 4 L 15 6 L 17 6 L 17 4 L 15 4 M 16 4 L 16 6 M 15 5 L 17 5","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -4 -10 L -5 -8 L -6 -3 L -6 0 L -5 5 L -4 7 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 5 L 6 0 L 6 -3 L 5 -8 L 4 -10 L 3 -11 L 1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9 M 0 -11 L 0 9 M -4 9 L 5 9","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 3 -2 L -2 0 L -4 1 L -6 3 L -7 6 L -7 9 M 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 2 -2 L -2 0 M -7 7 L -6 6 L -4 6 L 1 8 L 4 8 L 6 7 L 7 6 M -4 6 L 1 9 L 5 9 L 6 8 L 7 6 L 7 4","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 L -1 -3 M 2 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -4 L 2 -3 M 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 5 -1 L 6 2 L 6 5 L 5 7 L 4 8 L 2 9","-10 10 M 2 -10 L 2 9 M 3 -12 L 3 9 M 3 -12 L -8 3 L 8 3 M -1 9 L 6 9","-10 10 M -5 -12 L -7 -2 M -7 -2 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -5 -12 L 5 -12 M -5 -11 L 0 -11 L 5 -12","-10 10 M 5 -9 L 4 -8 L 5 -7 L 6 -8 L 6 -9 L 5 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2 M 0 -12 L -2 -11 L -4 -9 L -5 -7 L -6 -3 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 2 L 5 -1 L 3 -3 L 1 -4","-10 10 M -7 -12 L -7 -6 M -7 -8 L -6 -10 L -4 -12 L -2 -12 L 3 -9 L 5 -9 L 6 -10 L 7 -12 M -6 -10 L -4 -11 L -2 -11 L 3 -9 M 7 -12 L 7 -9 L 6 -6 L 2 -1 L 1 1 L 0 4 L 0 9 M 6 -6 L 1 -1 L 0 1 L -1 4 L -1 9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 6 -6 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -2 -12 L -4 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -3 M 2 -3 L 4 -4 L 5 -6 L 5 -9 L 4 -11 L 2 -12 M -2 -3 L -5 -2 L -6 -1 L -7 1 L -7 5 L -6 7 L -5 8 L -2 9 L 2 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 2 -3 M -2 -3 L -4 -2 L -5 -1 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 M 2 9 L 4 8 L 5 7 L 6 5 L 6 1 L 5 -1 L 4 -2 L 2 -3","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 0 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 5 L -5 6 M -1 1 L -3 0 L -5 -2 L -6 -5 L -6 -6 L -5 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 5 -9 L 6 -6 L 6 0 L 5 4 L 4 6 L 2 8 L 0 9","-5 5 M 0 -5 L -1 -4 L 0 -3 L 1 -4 L 0 -5 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-17 12 M -11 20 L -10 20 L -9 19 L -9 18 L -10 17 L -11 17 L -12 18 L -12 20 L -11 22 L -9 23 L -7 23 L -4 22 L -2 20 L -1 18 L 0 14 L 0 3 L -1 -23 L -1 -30 L 0 -35 L 1 -37 L 3 -38 L 4 -38 L 6 -37 L 7 -35 L 7 -31 L 6 -28 L 5 -26 L 3 -23 L -2 -19 L -8 -15 L -10 -13 L -12 -10 L -13 -8 L -14 -4 L -14 0 L -13 4 L -11 7 L -8 9 L -4 10 L 0 10 L 4 9 L 6 8 L 8 5 L 9 2 L 9 -2 L 8 -5 L 7 -7 L 5 -9 L 2 -10 L -2 -10 L -5 -9 L -7 -7 L -8 -4 L -8 0 L -7 3 L -5 5 M -11 18 L -11 19 L -10 19 L -10 18 L -11 18 M 3 -23 L -1 -19 L -6 -15 L -9 -12 L -11 -9 L -12 -7 L -13 -4 L -13 0 L -12 4 L -11 6 L -8 9 M 0 10 L 3 9 L 5 8 L 7 5 L 8 2 L 8 -2 L 7 -5 L 6 -7 L 4 -9 L 2 -10","-13 20 M -4 1 L -3 3 L -1 4 L 1 4 L 3 3 L 4 1 L 4 -1 L 3 -3 L 1 -4 L -1 -4 L -3 -3 L -4 -2 L -5 1 L -5 4 L -4 7 L -2 9 L 1 10 L 4 10 L 7 9 L 9 7 L 10 5 L 11 2 L 11 -2 L 10 -5 L 8 -8 L 6 -9 L 3 -10 L 0 -10 L -3 -9 L -5 -8 L -7 -6 L -9 -3 L -10 1 L -10 6 L -9 11 L -7 15 L -5 17 L -2 19 L 2 20 L 7 20 L 11 19 L 14 17 L 16 15 M -7 -6 L -8 -4 L -9 0 L -9 6 L -8 10 L -6 14 L -4 16 L -1 18 L 3 19 L 7 19 L 11 18 L 13 17 L 16 15 M -2 -3 L 2 -3 M -3 -2 L 3 -2 M -4 -1 L 4 -1 M -4 0 L 4 0 M -4 1 L 4 1 M -3 2 L 3 2 M -2 3 L 2 3 M 15 -6 L 15 -4 L 17 -4 L 17 -6 L 15 -6 M 16 -6 L 16 -4 M 15 -5 L 17 -5 M 15 4 L 15 6 L 17 6 L 17 4 L 15 4 M 16 4 L 16 6 M 15 5 L 17 5","-9 24 M -4 -1 L -3 -3 L -1 -4 L 1 -4 L 3 -3 L 4 -1 L 4 1 L 3 3 L 1 4 L -1 4 L -3 3 L -4 2 L -5 -1 L -5 -4 L -4 -7 L -2 -9 L 1 -10 L 5 -10 L 9 -9 L 12 -7 L 14 -4 L 15 0 L 15 5 L 14 9 L 13 11 L 11 14 L 8 17 L 4 20 L -1 23 L -5 25 M 5 -10 L 8 -9 L 11 -7 L 13 -4 L 14 0 L 14 5 L 13 9 L 12 11 L 10 14 L 7 17 L 2 21 L -1 23 M -2 -3 L 2 -3 M -3 -2 L 3 -2 M -4 -1 L 4 -1 M -4 0 L 4 0 M -4 1 L 4 1 M -3 2 L 3 2 M -2 3 L 2 3 M 19 -6 L 19 -4 L 21 -4 L 21 -6 L 19 -6 M 20 -6 L 20 -4 M 19 -5 L 21 -5 M 19 4 L 19 6 L 21 6 L 21 4 L 19 4 M 20 4 L 20 6 M 19 5 L 21 5","-14 14 M -10 -18 L -10 18 M -5 -18 L -5 18 M 5 -18 L 5 18 M 10 -18 L 10 18 M -5 -5 L 5 -7 M -5 -4 L 5 -6 M -5 -3 L 5 -5 M -5 5 L 5 3 M -5 6 L 5 4 M -5 7 L 5 5","-14 14 M -10 -20 L -10 20 M -9 -20 L -9 20 M -5 -20 L -5 20 M -1 -16 L 1 -16 L 1 -14 L -1 -14 L -1 -17 L 0 -19 L 2 -20 L 5 -20 L 7 -19 L 9 -17 L 10 -14 L 10 -9 L 9 -6 L 7 -4 L 5 -3 L 3 -3 L 1 -4 L 0 -6 L -1 -4 L -3 -1 L -4 0 L -3 1 L -1 4 L 0 6 L 1 4 L 3 3 L 5 3 L 7 4 L 9 6 L 10 9 L 10 14 L 9 17 L 7 19 L 5 20 L 2 20 L 0 19 L -1 17 L -1 14 L 1 14 L 1 16 L -1 16 M 0 -16 L 0 -14 M -1 -15 L 1 -15 M 7 -19 L 8 -17 L 9 -14 L 9 -9 L 8 -6 L 7 -4 M 0 -6 L 0 -4 L -2 -1 L -4 0 L -2 1 L 0 4 L 0 6 M 7 4 L 8 6 L 9 9 L 9 14 L 8 17 L 7 19 M 0 14 L 0 16 M -1 15 L 1 15","-8 8 M -5 -4 L -5 -1 M 5 -4 L 5 -1 M -5 -4 L 5 -4 M -5 -3 L 5 -3 M -5 -2 L 5 -2 M -5 -1 L 5 -1","-10 10 M 3 -12 L -10 9 M 3 -12 L 4 9 M 2 -10 L 3 9 M -6 3 L 3 3 M -12 9 L -6 9 M 0 9 L 6 9","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 L 4 -2 M -5 -2 L 4 -2 L 6 -1 L 7 1 L 7 3 L 6 6 L 4 8 L 0 9 L -12 9 M 4 -2 L 5 -1 L 6 1 L 6 3 L 5 6 L 3 8 L 0 9","-10 11 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 L 6 4 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -7 L 8 -3 L 7 1 L 5 5 L 3 7 L 1 8 L -3 9 L -12 9 M 3 -12 L 5 -11 L 6 -10 L 7 -7 L 7 -3 L 6 1 L 4 5 L 2 7 L 0 8 L -3 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M 2 -6 L 0 2 M -6 -12 L 9 -12 L 8 -6 L 8 -12 M -5 -2 L 1 -2 M -12 9 L 3 9 L 5 4 L 2 9","-12 10 M -3 -12 L -9 9 M -2 -12 L -8 9 M 2 -6 L 0 2 M -6 -12 L 9 -12 L 8 -6 L 8 -12 M -5 -2 L 1 -2 M -12 9 L -5 9","-10 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 0 9 L 3 8 L 5 6 L 7 2 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9 M 0 9 L 2 8 L 4 6 L 6 2 M 3 2 L 10 2","-13 13 M -4 -12 L -10 9 M -3 -12 L -9 9 M 9 -12 L 3 9 M 10 -12 L 4 9 M -7 -12 L 0 -12 M 6 -12 L 13 -12 M -6 -2 L 6 -2 M -13 9 L -6 9 M 0 9 L 7 9","-6 7 M 3 -12 L -3 9 M 4 -12 L -2 9 M 0 -12 L 7 -12 M -6 9 L 1 9","-9 9 M 6 -12 L 1 5 L 0 7 L -1 8 L -3 9 L -5 9 L -7 8 L -8 6 L -8 4 L -7 3 L -6 4 L -7 5 M 5 -12 L 0 5 L -1 7 L -3 9 M 2 -12 L 9 -12","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M 11 -12 L -6 1 M 1 -3 L 5 9 M 0 -3 L 4 9 M -6 -12 L 1 -12 M 7 -12 L 13 -12 M -12 9 L -5 9 M 1 9 L 7 9","-10 10 M -1 -12 L -7 9 M 0 -12 L -6 9 M -4 -12 L 3 -12 M -10 9 L 5 9 L 7 3 L 4 9","-13 14 M -4 -12 L -10 9 M -4 -12 L -3 9 M -3 -12 L -2 7 M 10 -12 L -3 9 M 10 -12 L 4 9 M 11 -12 L 5 9 M -7 -12 L -3 -12 M 10 -12 L 14 -12 M -13 9 L -7 9 M 1 9 L 8 9","-12 13 M -3 -12 L -9 9 M -3 -12 L 4 6 M -3 -9 L 4 9 M 10 -12 L 4 9 M -6 -12 L -3 -12 M 7 -12 L 13 -12 M -12 9 L -6 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -4 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 6 -12 L 9 -11 L 10 -9 L 10 -7 L 9 -4 L 7 -2 L 3 -1 L -5 -1 M 6 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 6 -2 L 3 -1 M -12 9 L -5 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -4 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12 M -6 7 L -6 6 L -5 4 L -3 3 L -2 3 L 0 4 L 1 6 L 1 13 L 2 14 L 4 14 L 5 12 L 5 11 M 1 6 L 2 12 L 3 13 L 4 13 L 5 12","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 L -5 -2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 L 4 -2 M 0 -2 L 2 -1 L 3 0 L 4 8 L 5 9 L 7 9 L 8 7 L 8 6 M 3 0 L 5 7 L 6 8 L 7 8 L 8 7 M -12 9 L -5 9","-11 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -7 L -4 -5 L -3 -4 L 4 0 L 6 2 M -5 -7 L -3 -5 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -9 9 L -8 7 L -7 7","-10 11 M 3 -12 L -3 9 M 4 -12 L -2 9 M -3 -12 L -6 -6 L -4 -12 L 11 -12 L 10 -6 L 10 -12 M -6 9 L 1 9","-12 13 M -4 -12 L -7 -1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 3 8 L 5 6 L 6 3 L 10 -12 M -3 -12 L -6 -1 L -7 3 L -7 6 L -6 8 L -4 9 M -7 -12 L 0 -12 M 7 -12 L 13 -12","-10 10 M -4 -12 L -3 9 M -3 -12 L -2 7 M 10 -12 L -3 9 M -6 -12 L 0 -12 M 6 -12 L 12 -12","-13 13 M -5 -12 L -7 9 M -4 -12 L -6 7 M 3 -12 L -7 9 M 3 -12 L 1 9 M 4 -12 L 2 7 M 11 -12 L 1 9 M -8 -12 L -1 -12 M 8 -12 L 14 -12","-11 11 M -4 -12 L 3 9 M -3 -12 L 4 9 M 10 -12 L -10 9 M -6 -12 L 0 -12 M 6 -12 L 12 -12 M -12 9 L -6 9 M 0 9 L 6 9","-10 11 M -4 -12 L 0 -2 L -3 9 M -3 -12 L 1 -2 L -2 9 M 11 -12 L 1 -2 M -6 -12 L 0 -12 M 7 -12 L 13 -12 M -6 9 L 1 9","-11 11 M 9 -12 L -10 9 M 10 -12 L -9 9 M -3 -12 L -6 -6 L -4 -12 L 10 -12 M -10 9 L 4 9 L 6 3 L 3 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-6 6 M 2 -12 L -3 -6 M 2 -12 L 3 -11 L -3 -6","-13 13 M -9 0 L 9 0","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-10 11 M 6 -5 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8","-10 9 M -2 -12 L -6 1 L -6 4 L -5 7 L -4 8 M -1 -12 L -5 1 M -5 1 L -4 -2 L -2 -4 L 0 -5 L 2 -5 L 4 -4 L 5 -3 L 6 -1 L 6 2 L 5 5 L 3 8 L 0 9 L -2 9 L -4 8 L -5 5 L -5 1 M 4 -4 L 5 -2 L 5 2 L 4 5 L 2 8 L 0 9 M -5 -12 L -1 -12","-9 9 M 5 -2 L 5 -1 L 6 -1 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8","-10 11 M 8 -12 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 9 -12 L 5 2 L 4 6 L 4 8 L 5 9 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8 M 5 -12 L 9 -12","-9 9 M -5 4 L -1 3 L 2 2 L 5 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8","-7 8 M 8 -11 L 7 -10 L 8 -9 L 9 -10 L 9 -11 L 8 -12 L 6 -12 L 4 -11 L 3 -10 L 2 -8 L 1 -5 L -2 9 L -3 13 L -4 15 M 6 -12 L 4 -10 L 3 -8 L 2 -4 L 0 5 L -1 9 L -2 12 L -3 14 L -4 15 L -6 16 L -8 16 L -9 15 L -9 14 L -8 13 L -7 14 L -8 15 M -3 -5 L 7 -5","-10 10 M 7 -5 L 3 9 L 2 12 L 0 15 L -3 16 L -6 16 L -8 15 L -9 14 L -9 13 L -8 12 L -7 13 L -8 14 M 6 -5 L 2 9 L 1 12 L -1 15 L -3 16 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8","-10 11 M -2 -12 L -8 9 M -1 -12 L -7 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 6 -1 L 4 5 L 4 8 L 5 9 M 3 -5 L 5 -3 L 5 -1 L 3 5 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M -5 -12 L -1 -12","-6 7 M 3 -12 L 2 -11 L 3 -10 L 4 -11 L 3 -12 M -5 -1 L -4 -3 L -2 -5 L 1 -5 L 2 -4 L 2 -1 L 0 5 L 0 8 L 1 9 M 0 -5 L 1 -4 L 1 -1 L -1 5 L -1 8 L 0 9 L 3 9 L 5 7 L 6 5","-6 7 M 4 -12 L 3 -11 L 4 -10 L 5 -11 L 4 -12 M -4 -1 L -3 -3 L -1 -5 L 2 -5 L 3 -4 L 3 -1 L 0 9 L -1 12 L -2 14 L -3 15 L -5 16 L -7 16 L -8 15 L -8 14 L -7 13 L -6 14 L -7 15 M 1 -5 L 2 -4 L 2 -1 L -1 9 L -2 12 L -3 14 L -5 16","-10 10 M -2 -12 L -8 9 M -1 -12 L -7 9 M 6 -4 L 5 -3 L 6 -2 L 7 -3 L 7 -4 L 6 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 L -5 1 M -3 1 L -1 2 L 1 8 L 2 9 M -3 1 L -2 2 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 M -5 -12 L -1 -12","-5 7 M 3 -12 L -1 2 L -2 6 L -2 8 L -1 9 L 2 9 L 4 7 L 5 5 M 4 -12 L 0 2 L -1 6 L -1 8 L 0 9 M 0 -12 L 4 -12","-17 16 M -16 -1 L -15 -3 L -13 -5 L -10 -5 L -9 -4 L -9 -2 L -10 2 L -12 9 M -11 -5 L -10 -4 L -10 -2 L -11 2 L -13 9 M -10 2 L -8 -2 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -3 L 1 -1 L -2 9 M -2 -5 L 0 -3 L 0 -1 L -3 9 M 0 2 L 2 -2 L 4 -4 L 6 -5 L 8 -5 L 10 -4 L 11 -3 L 11 -1 L 9 5 L 9 8 L 10 9 M 8 -5 L 10 -3 L 10 -1 L 8 5 L 8 8 L 9 9 L 12 9 L 14 7 L 15 5","-12 11 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -2 L -5 2 L -7 9 M -6 -5 L -5 -4 L -5 -2 L -6 2 L -8 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 6 -1 L 4 5 L 4 8 L 5 9 M 3 -5 L 5 -3 L 5 -1 L 3 5 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5","-9 9 M 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 L 6 2 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L 0 -5 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 5 L 5 2 L 5 -2 L 4 -4","-11 10 M -10 -1 L -9 -3 L -7 -5 L -4 -5 L -3 -4 L -3 -2 L -4 2 L -8 16 M -5 -5 L -4 -4 L -4 -2 L -5 2 L -9 16 M -4 2 L -3 -1 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 7 -1 L 7 2 L 6 5 L 4 8 L 1 9 L -1 9 L -3 8 L -4 5 L -4 2 M 5 -4 L 6 -2 L 6 2 L 5 5 L 3 8 L 1 9 M -12 16 L -5 16","-10 10 M 6 -5 L 0 16 M 7 -5 L 1 16 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8 M -3 16 L 4 16","-9 8 M -8 -1 L -7 -3 L -5 -5 L -2 -5 L -1 -4 L -1 -2 L -2 2 L -4 9 M -3 -5 L -2 -4 L -2 -2 L -3 2 L -5 9 M -2 2 L 0 -2 L 2 -4 L 4 -5 L 6 -5 L 7 -4 L 7 -3 L 6 -2 L 5 -3 L 6 -4","-8 9 M 6 -3 L 6 -2 L 7 -2 L 7 -3 L 6 -4 L 3 -5 L 0 -5 L -3 -4 L -4 -3 L -4 -1 L -3 0 L 4 4 L 5 5 M -4 -2 L -3 -1 L 4 3 L 5 4 L 5 7 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -6 6 L -5 6 L -5 7","-7 7 M 2 -12 L -2 2 L -3 6 L -3 8 L -2 9 L 1 9 L 3 7 L 4 5 M 3 -12 L -1 2 L -2 6 L -2 8 L -1 9 M -4 -5 L 5 -5","-12 11 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -1 L -6 5 L -6 7 L -4 9 M -6 -5 L -5 -4 L -5 -1 L -7 5 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 2 M 6 -5 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9","-10 10 M -9 -1 L -8 -3 L -6 -5 L -3 -5 L -2 -4 L -2 -1 L -4 5 L -4 7 L -2 9 M -4 -5 L -3 -4 L -3 -1 L -5 5 L -5 7 L -4 8 L -2 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 -1 L 7 -5 L 6 -5 L 7 -3","-15 14 M -14 -1 L -13 -3 L -11 -5 L -8 -5 L -7 -4 L -7 -1 L -9 5 L -9 7 L -7 9 M -9 -5 L -8 -4 L -8 -1 L -10 5 L -10 7 L -9 8 L -7 9 L -5 9 L -3 8 L -1 6 L 0 4 M 2 -5 L 0 4 L 0 7 L 1 8 L 3 9 L 5 9 L 7 8 L 9 6 L 10 4 L 11 0 L 11 -5 L 10 -5 L 11 -3 M 3 -5 L 1 4 L 1 7 L 3 9","-10 10 M -7 -1 L -5 -4 L -3 -5 L 0 -5 L 1 -3 L 1 0 M -1 -5 L 0 -3 L 0 0 L -1 4 L -2 6 L -4 8 L -6 9 L -7 9 L -8 8 L -8 7 L -7 6 L -6 7 L -7 8 M -1 4 L -1 7 L 0 9 L 3 9 L 5 8 L 7 5 M 7 -4 L 6 -3 L 7 -2 L 8 -3 L 8 -4 L 7 -5 L 6 -5 L 4 -4 L 2 -2 L 1 0 L 0 4 L 0 7 L 1 9","-11 10 M -10 -1 L -9 -3 L -7 -5 L -4 -5 L -3 -4 L -3 -1 L -5 5 L -5 7 L -3 9 M -5 -5 L -4 -4 L -4 -1 L -6 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 6 L 5 2 M 8 -5 L 4 9 L 3 12 L 1 15 L -2 16 L -5 16 L -7 15 L -8 14 L -8 13 L -7 12 L -6 13 L -7 14 M 7 -5 L 3 9 L 2 12 L 0 15 L -2 16","-10 10 M 7 -5 L 6 -3 L 4 -1 L -4 5 L -6 7 L -7 9 M -6 -1 L -5 -3 L -3 -5 L 0 -5 L 4 -3 M -5 -3 L -3 -4 L 0 -4 L 4 -3 L 6 -3 M -6 7 L -4 7 L 0 8 L 3 8 L 5 7 M -4 7 L 0 9 L 3 9 L 5 7 L 6 5","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 6 M 3 -12 L 2 -11 L 0 1 M 3 -11 L 0 1 M 3 -12 L 4 -11 L 0 1 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-9 9 M -2 -12 L -4 -5 M -1 -12 L -4 -5 M 7 -12 L 5 -5 M 8 -12 L 5 -5","-10 11 M 1 -12 L -6 16 M 7 -12 L 0 16 M -6 -1 L 8 -1 M -7 5 L 7 5","-10 11 M 2 -16 L -6 13 M 7 -16 L -1 13 M 8 -8 L 7 -7 L 8 -6 L 9 -7 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -7 L -4 -5 L -3 -4 L 4 0 L 6 2 M -5 -7 L -3 -5 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -4 L 9 -3 L 10 -2 L 11 -3 L 11 -4 L 10 -5 L 9 -5 L 7 -4 L 5 -2 L 0 6 L -2 8 L -4 9 L -7 9 L -10 8 L -11 6 L -11 4 L -10 2 L -9 1 L -7 0 L -2 -2 L 0 -3 L 2 -5 L 3 -7 L 3 -9 L 2 -11 L 0 -12 L -2 -11 L -3 -9 L -3 -6 L -2 0 L -1 3 L 1 6 L 3 8 L 5 9 L 7 9 L 8 7 L 8 6 M -7 9 L -9 8 L -10 6 L -10 4 L -9 2 L -8 1 L -2 -2 M -3 -6 L -2 -1 L -1 2 L 1 5 L 3 7 L 5 8 L 7 8 L 8 7","-5 6 M 3 -10 L 2 -11 L 3 -12 L 4 -11 L 4 -10 L 3 -8 L 1 -6","-7 8 M 8 -16 L 4 -13 L 1 -10 L -1 -7 L -3 -3 L -4 2 L -4 6 L -3 11 L -2 14 L -1 16 M 4 -13 L 1 -9 L -1 -5 L -2 -2 L -3 3 L -3 8 L -2 13 L -1 16","-8 7 M 1 -16 L 2 -14 L 3 -11 L 4 -6 L 4 -2 L 3 3 L 1 7 L -1 10 L -4 13 L -8 16 M 1 -16 L 2 -13 L 3 -8 L 3 -3 L 2 2 L 1 5 L -1 9 L -4 13","-8 9 M 2 -12 L 2 0 M -3 -9 L 7 -3 M 7 -9 L -3 -3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-5 6 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-13 13 M -9 0 L 9 0","-5 6 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-11 11 M 13 -16 L -13 16","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -3 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12","-10 11 M 2 -8 L -3 9 M 4 -12 L -2 9 M 4 -12 L 1 -9 L -2 -7 L -4 -6 M 3 -9 L -1 -7 L -4 -6","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 5 -3 L 2 -1 L -2 1 L -5 3 L -7 5 L -9 9 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 L -2 1 M -8 7 L -7 6 L -5 6 L 0 8 L 3 8 L 5 7 L 6 5 M -5 6 L 0 9 L 3 9 L 5 8 L 6 5","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 4 -3 L 1 -2 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 M -1 -2 L 1 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -2 L 3 -1 L 4 0 L 5 2 L 5 5 L 4 7 L 3 8 L 1 9","-10 11 M 6 -11 L 0 9 M 7 -12 L 1 9 M 7 -12 L -8 3 L 8 3","-10 11 M -1 -12 L -6 -2 M -1 -12 L 9 -12 M -1 -11 L 4 -11 L 9 -12 M -6 -2 L -5 -3 L -2 -4 L 1 -4 L 4 -3 L 5 -2 L 6 0 L 6 3 L 5 6 L 3 8 L 0 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -4 L 3 -3 L 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 L 0 9","-10 11 M 7 -9 L 6 -8 L 7 -7 L 8 -8 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 5 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 L -1 -3 L -3 -2 L -5 0 L -6 2 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 6 L -5 8 M 0 9 L 2 8 L 4 6 L 5 4 L 5 0 L 4 -2","-10 11 M -4 -12 L -6 -6 M 9 -12 L 8 -9 L 6 -6 L 1 0 L -1 3 L -2 5 L -3 9 M 6 -6 L 0 0 L -2 3 L -3 5 L -4 9 M -5 -9 L -2 -12 L 0 -12 L 5 -9 M -4 -10 L -2 -11 L 0 -11 L 5 -9 L 7 -9 L 8 -10 L 9 -12","-10 11 M 1 -12 L -2 -11 L -3 -10 L -4 -8 L -4 -5 L -3 -3 L -1 -2 L 2 -2 L 6 -3 L 7 -4 L 8 -6 L 8 -9 L 7 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -2 -10 L -3 -8 L -3 -5 L -2 -3 L -1 -2 M 2 -2 L 5 -3 L 6 -4 L 7 -6 L 7 -9 L 6 -11 L 4 -12 M -1 -2 L -5 -1 L -7 1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 4 8 L 5 7 L 6 5 L 6 2 L 5 0 L 4 -1 L 2 -2 M -1 -2 L -4 -1 L -6 1 L -7 3 L -7 6 L -6 8 L -4 9 M 0 9 L 3 8 L 4 7 L 5 5 L 5 1 L 4 -1","-10 11 M 7 -5 L 6 -3 L 4 -1 L 2 0 L -1 0 L -3 -1 L -4 -2 L -5 -4 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 4 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -4 L 7 0 L 6 3 L 4 6 L 2 8 L -1 9 L -4 9 L -6 8 L -7 6 L -7 5 L -6 4 L -5 5 L -6 6 M -3 -1 L -4 -3 L -4 -7 L -3 -9 L -1 -11 L 1 -12 M 6 -11 L 7 -9 L 7 -4 L 6 0 L 5 3 L 3 6 L 1 8 L -1 9","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 7 L -3 8 L -2 9 L -1 8","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 1 -2 L -1 -1 L -1 1 L 0 2 L 2 2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-13 10 M 6 -12 L 4 -10 L 2 -7 L -1 -2 L -3 1 L -6 5 L -9 8 L -11 9 L -13 9 L -14 8 L -14 6 L -13 5 L -12 6 L -13 7 M 6 -12 L 5 -8 L 3 2 L 2 9 M 6 -12 L 3 9 M 2 9 L 2 7 L 1 4 L 0 2 L -2 0 L -4 -1 L -6 -1 L -7 0 L -7 2 L -6 5 L -3 8 L 0 9 L 4 9 L 6 8","-12 12 M 3 -11 L 2 -10 L 1 -8 L -1 -3 L -3 3 L -4 5 L -6 8 L -8 9 M 2 -10 L 1 -7 L -1 1 L -2 4 L -3 6 L -5 8 L -8 9 L -10 9 L -11 8 L -11 6 L -10 5 L -9 6 L -10 7 M -3 -6 L -4 -4 L -5 -3 L -7 -3 L -8 -4 L -8 -6 L -7 -8 L -5 -10 L -3 -11 L 0 -12 L 6 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 6 -4 L 2 -3 L 0 -3 M 6 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 M 2 -3 L 5 -2 L 6 -1 L 7 1 L 7 4 L 6 7 L 5 8 L 3 9 L 1 9 L 0 8 L 0 6 L 1 3 M 2 -3 L 4 -2 L 5 -1 L 6 1 L 6 4 L 5 7 L 3 9","-10 11 M -7 -10 L -8 -8 L -8 -6 L -7 -4 L -4 -3 L -1 -3 L 3 -4 L 5 -5 L 7 -7 L 8 -9 L 8 -11 L 7 -12 L 5 -12 L 2 -11 L -1 -8 L -3 -5 L -5 -1 L -6 3 L -6 6 L -5 8 L -2 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 2 L 5 0 L 3 0 L 1 1 L 0 3 M 5 -12 L 3 -11 L 0 -8 L -2 -5 L -4 -1 L -5 3 L -5 6 L -4 8 L -2 9","-12 11 M 3 -11 L 2 -10 L 1 -8 L -1 -3 L -3 3 L -4 5 L -6 8 L -8 9 M 2 -10 L 1 -7 L -1 1 L -2 4 L -3 6 L -5 8 L -8 9 L -10 9 L -11 8 L -11 6 L -10 5 L -8 5 L -6 6 L -4 8 L -2 9 L 1 9 L 3 8 L 5 6 L 7 2 L 8 -3 L 8 -6 L 7 -9 L 5 -11 L 3 -12 L -2 -12 L -5 -11 L -7 -9 L -8 -7 L -8 -5 L -7 -4 L -5 -4 L -4 -5 L -3 -7","-9 10 M 5 -9 L 4 -8 L 4 -6 L 5 -5 L 7 -5 L 8 -7 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L 0 -11 L -1 -10 L -2 -8 L -2 -6 L -1 -4 L 1 -3 M 2 -12 L 0 -10 L -1 -8 L -1 -5 L 1 -3 M 1 -3 L -1 -3 L -4 -2 L -6 0 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 2 L 5 0 L 3 0 L 1 1 L 0 3 M -1 -3 L -3 -2 L -5 0 L -6 2 L -6 6 L -5 8","-11 10 M 5 -10 L 4 -8 L 2 -3 L 0 3 L -1 5 L -3 8 L -5 9 M -1 -6 L -2 -4 L -4 -3 L -6 -3 L -7 -5 L -7 -7 L -6 -9 L -4 -11 L -1 -12 L 9 -12 L 6 -11 L 5 -10 L 4 -7 L 2 1 L 1 4 L 0 6 L -2 8 L -5 9 L -7 9 L -9 8 L -10 7 L -10 6 L -9 5 L -8 6 L -9 7 M 1 -12 L 5 -11 L 6 -11 M -3 1 L -2 0 L 0 -1 L 4 -1 L 6 -2 L 8 -5 L 6 2","-11 11 M -8 -9 L -9 -7 L -9 -5 L -8 -3 L -6 -2 L -3 -2 L 0 -3 L 2 -4 L 5 -7 L 6 -10 L 6 -11 L 5 -12 L 4 -12 L 2 -11 L 0 -9 L -1 -7 L -2 -4 L -2 -1 L -1 1 L 1 2 L 3 2 L 5 1 L 7 -1 L 8 -3 M 5 -12 L 3 -11 L 1 -9 L 0 -7 L -1 -4 L -1 0 L 1 2 M 8 -3 L 7 1 L 5 5 L 3 7 L 1 8 L -3 9 L -6 9 L -8 8 L -9 6 L -9 5 L -8 4 L -7 5 L -8 6 M 7 1 L 5 4 L 3 6 L 0 8 L -3 9","-12 12 M -6 -6 L -7 -7 L -7 -9 L -6 -11 L -3 -12 L 0 -12 L -3 -1 L -5 5 L -6 7 L -7 8 L -9 9 L -11 9 L -12 8 L -12 6 L -11 5 L -10 6 L -11 7 M 0 -12 L -3 -3 L -4 0 L -6 5 L -7 7 L -9 9 M -8 2 L -7 1 L -5 0 L 4 -3 L 6 -4 L 9 -6 L 11 -8 L 12 -10 L 12 -11 L 11 -12 L 10 -12 L 8 -11 L 6 -8 L 5 -6 L 3 0 L 2 4 L 2 7 L 4 9 L 5 9 L 7 8 L 9 6 M 10 -12 L 8 -10 L 6 -6 L 4 0 L 3 4 L 3 7 L 4 9","-9 7 M 5 -10 L 3 -7 L 1 -2 L -1 3 L -2 5 L -4 8 L -6 9 M 7 -6 L 5 -4 L 2 -3 L -1 -3 L -3 -4 L -4 -6 L -4 -8 L -3 -10 L -1 -11 L 3 -12 L 7 -12 L 5 -10 L 4 -8 L 2 -2 L 0 4 L -1 6 L -3 8 L -6 9 L -8 9 L -9 8 L -9 6 L -8 5 L -7 6 L -8 7","-9 8 M 7 -12 L 5 -10 L 3 -7 L 1 -2 L -2 7 L -4 11 M 7 -5 L 5 -3 L 2 -2 L -1 -2 L -3 -3 L -4 -5 L -4 -7 L -3 -9 L -1 -11 L 3 -12 L 7 -12 L 5 -9 L 4 -7 L 1 2 L -1 6 L -2 8 L -4 11 L -5 12 L -7 13 L -8 12 L -8 10 L -7 8 L -5 6 L -3 5 L 0 4 L 4 3","-12 12 M -6 -6 L -7 -7 L -7 -9 L -5 -11 L -2 -12 L 0 -12 L -3 -1 L -5 5 L -6 7 L -7 8 L -9 9 L -11 9 L -12 8 L -12 6 L -11 5 L -10 6 L -11 7 M 0 -12 L -3 -3 L -4 0 L -6 5 L -7 7 L -9 9 M 8 -11 L 5 -7 L 3 -5 L 1 -4 L -2 -3 M 11 -11 L 10 -10 L 11 -9 L 12 -10 L 12 -11 L 11 -12 L 10 -12 L 8 -11 L 5 -6 L 4 -5 L 2 -4 L -2 -3 M -2 -3 L 1 -2 L 2 0 L 3 7 L 4 9 M -2 -3 L 0 -2 L 1 0 L 2 7 L 4 9 L 5 9 L 7 8 L 9 6","-9 9 M -5 -9 L -6 -7 L -6 -5 L -5 -3 L -3 -2 L 0 -2 L 3 -3 L 5 -4 L 8 -7 L 9 -10 L 9 -11 L 8 -12 L 7 -12 L 5 -11 L 4 -10 L 2 -7 L -2 3 L -3 5 L -5 8 L -7 9 M 4 -10 L 2 -6 L 0 1 L -1 4 L -2 6 L -4 8 L -7 9 L -9 9 L -10 8 L -10 6 L -9 5 L -7 5 L -5 6 L -2 8 L 0 9 L 3 9 L 5 8 L 7 6","-14 14 M 0 -12 L -4 -3 L -7 3 L -9 6 L -11 8 L -13 9 L -15 9 L -16 8 L -16 6 L -15 5 L -14 6 L -15 7 M 0 -12 L -2 -5 L -3 -1 L -4 4 L -4 8 L -2 9 M 0 -12 L -1 -8 L -2 -3 L -3 4 L -3 8 L -2 9 M 9 -12 L 5 -3 L 0 6 L -2 9 M 9 -12 L 7 -5 L 6 -1 L 5 4 L 5 8 L 7 9 L 8 9 L 10 8 L 12 6 M 9 -12 L 8 -8 L 7 -3 L 6 4 L 6 8 L 7 9","-11 12 M 0 -12 L -1 -8 L -3 -2 L -5 3 L -6 5 L -8 8 L -10 9 L -12 9 L -13 8 L -13 6 L -12 5 L -11 6 L -12 7 M 0 -12 L 0 -7 L 1 4 L 2 9 M 0 -12 L 1 -7 L 2 4 L 2 9 M 14 -11 L 13 -10 L 14 -9 L 15 -10 L 15 -11 L 14 -12 L 12 -12 L 10 -11 L 8 -8 L 7 -6 L 5 -1 L 3 5 L 2 9","-10 11 M 1 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -4 L -7 0 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 1 L 8 -3 L 8 -7 L 7 -10 L 6 -11 L 5 -11 L 3 -10 L 1 -8 L -1 -4 L -2 1 L -2 4 M -1 -11 L -3 -8 L -5 -4 L -6 0 L -6 4 L -5 7 L -3 9","-12 11 M 3 -11 L 2 -10 L 1 -8 L -1 -3 L -3 3 L -4 5 L -6 8 L -8 9 M 2 -10 L 1 -7 L -1 1 L -2 4 L -3 6 L -5 8 L -8 9 L -10 9 L -11 8 L -11 6 L -10 5 L -9 6 L -10 7 M -3 -6 L -4 -4 L -5 -3 L -7 -3 L -8 -4 L -8 -6 L -7 -8 L -5 -10 L -3 -11 L 0 -12 L 4 -12 L 7 -11 L 8 -10 L 9 -8 L 9 -5 L 8 -3 L 7 -2 L 4 -1 L 2 -1 L 0 -2 M 4 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 4 -1","-10 11 M 3 -8 L 3 -6 L 2 -4 L 1 -3 L -1 -2 L -3 -2 L -4 -4 L -4 -6 L -3 -9 L -1 -11 L 2 -12 L 5 -12 L 7 -11 L 8 -9 L 8 -5 L 7 -2 L 5 1 L 1 5 L -2 7 L -4 8 L -7 9 L -9 9 L -10 8 L -10 6 L -9 5 L -7 5 L -5 6 L -2 8 L 1 9 L 4 9 L 6 8 L 8 6 M 5 -12 L 6 -11 L 7 -9 L 7 -5 L 6 -2 L 4 1 L 1 4 L -3 7 L -7 9","-12 12 M 3 -11 L 2 -10 L 1 -8 L -1 -3 L -3 3 L -4 5 L -6 8 L -8 9 M 2 -10 L 1 -7 L -1 1 L -2 4 L -3 6 L -5 8 L -8 9 L -10 9 L -11 8 L -11 6 L -10 5 L -9 6 L -10 7 M -3 -6 L -4 -4 L -5 -3 L -7 -3 L -8 -4 L -8 -6 L -7 -8 L -5 -10 L -3 -11 L 0 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 4 -3 L 0 -3 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 M 0 -3 L 3 -2 L 4 0 L 5 7 L 6 9 M 0 -3 L 2 -2 L 3 0 L 4 7 L 6 9 L 7 9 L 9 8 L 11 6","-10 10 M -4 -9 L -5 -7 L -5 -5 L -4 -3 L -2 -2 L 1 -2 L 4 -3 L 6 -4 L 9 -7 L 10 -10 L 10 -11 L 9 -12 L 8 -12 L 6 -11 L 5 -10 L 4 -8 L 3 -5 L 1 2 L 0 5 L -2 8 L -4 9 M 4 -8 L 3 -4 L 2 3 L 1 6 L -1 8 L -4 9 L -7 9 L -9 8 L -10 6 L -10 5 L -9 4 L -8 5 L -9 6","-9 9 M 7 -10 L 6 -8 L 4 -3 L 2 3 L 1 5 L -1 8 L -3 9 M 1 -6 L 0 -4 L -2 -3 L -4 -3 L -5 -5 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 10 -12 L 8 -11 L 7 -10 L 6 -7 L 4 1 L 3 4 L 2 6 L 0 8 L -3 9 L -5 9 L -7 8 L -8 7 L -8 6 L -7 5 L -6 6 L -7 7 M 3 -12 L 7 -11 L 8 -11","-11 11 M -10 -8 L -8 -11 L -6 -12 L -5 -12 L -3 -10 L -3 -7 L -4 -4 L -7 4 L -7 7 L -6 9 M -5 -12 L -4 -10 L -4 -7 L -7 1 L -8 4 L -8 7 L -6 9 L -4 9 L -2 8 L 1 5 L 3 2 L 4 0 M 8 -12 L 4 0 L 3 4 L 3 7 L 5 9 L 6 9 L 8 8 L 10 6 M 9 -12 L 5 0 L 4 4 L 4 7 L 5 9","-11 10 M -10 -8 L -8 -11 L -6 -12 L -5 -12 L -3 -10 L -3 -7 L -4 -3 L -6 4 L -6 7 L -5 9 M -5 -12 L -4 -10 L -4 -7 L -6 0 L -7 4 L -7 7 L -5 9 L -4 9 L -1 8 L 2 5 L 4 2 L 6 -2 L 7 -5 L 8 -9 L 8 -11 L 7 -12 L 6 -12 L 5 -11 L 4 -9 L 4 -6 L 5 -4 L 7 -2 L 9 -1 L 11 -1","-12 11 M -9 -6 L -10 -6 L -11 -7 L -11 -9 L -10 -11 L -8 -12 L -4 -12 L -5 -10 L -6 -6 L -7 3 L -8 9 M -6 -6 L -6 3 L -7 9 M 4 -12 L 2 -10 L 0 -6 L -3 3 L -5 7 L -7 9 M 4 -12 L 3 -10 L 2 -6 L 1 3 L 0 9 M 2 -6 L 2 3 L 1 9 M 14 -12 L 12 -11 L 10 -9 L 8 -6 L 5 3 L 3 7 L 1 9","-10 10 M -2 -7 L -3 -6 L -5 -6 L -6 -7 L -6 -9 L -5 -11 L -3 -12 L -1 -12 L 1 -11 L 2 -9 L 2 -6 L 1 -2 L -1 3 L -3 6 L -5 8 L -8 9 L -10 9 L -11 8 L -11 6 L -10 5 L -9 6 L -10 7 M -1 -12 L 0 -11 L 1 -9 L 1 -6 L 0 -2 L -2 3 L -4 6 L -6 8 L -8 9 M 11 -11 L 10 -10 L 11 -9 L 12 -10 L 12 -11 L 11 -12 L 9 -12 L 7 -11 L 5 -9 L 3 -6 L 1 -2 L 0 3 L 0 6 L 1 8 L 2 9 L 3 9 L 5 8 L 7 6","-11 11 M -8 -8 L -6 -11 L -4 -12 L -3 -12 L -1 -11 L -1 -9 L -3 -3 L -3 0 L -2 2 M -3 -12 L -2 -11 L -2 -9 L -4 -3 L -4 0 L -2 2 L 0 2 L 3 1 L 5 -1 L 7 -4 L 8 -6 M 10 -12 L 8 -6 L 5 2 L 3 6 M 11 -12 L 9 -6 L 7 -1 L 5 3 L 3 6 L 1 8 L -2 9 L -6 9 L -8 8 L -9 6 L -9 5 L -8 4 L -7 5 L -8 6","-11 10 M 8 -10 L 7 -8 L 5 -3 L 4 0 L 3 2 L 1 5 L -1 7 L -3 8 L -6 9 M 1 -6 L 0 -4 L -2 -3 L -4 -3 L -5 -5 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 11 -12 L 9 -11 L 8 -10 L 7 -7 L 6 -3 L 4 3 L 2 6 L -1 8 L -6 9 L -10 9 L -11 8 L -11 6 L -10 5 L -8 5 L -6 6 L -3 8 L -1 9 L 2 9 L 5 8 L 7 6 M 4 -12 L 8 -11 L 9 -11","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-8 8 M -2 -6 L 0 -9 L 2 -6 M -5 -3 L 0 -8 L 5 -3 M 0 -8 L 0 9","-8 8 M -8 11 L 8 11","-5 6 M 4 -12 L 2 -10 L 1 -8 L 1 -7 L 2 -6 L 3 -7 L 2 -8","-7 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 M -2 0 L -4 2 L -5 4 L -5 7 L -3 9 M 4 0 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4 M 5 0 L 3 6 L 3 8 L 4 9","-6 8 M -6 4 L -4 1 L -2 -3 M 1 -12 L -5 6 L -5 8 L -3 9 L -2 9 L 0 8 L 2 6 L 3 3 L 3 0 L 4 4 L 5 5 L 6 5 L 8 4 M 2 -12 L -4 6 L -4 8 L -3 9","-6 6 M 2 1 L 1 2 L 2 2 L 2 1 L 1 0 L -1 0 L -3 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9 L 1 9 L 4 7 L 6 4 M -1 0 L -3 2 L -4 4 L -4 7 L -2 9","-7 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 M -2 0 L -4 2 L -5 4 L -5 7 L -3 9 M 8 -12 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4 M 9 -12 L 3 6 L 3 8 L 4 9","-6 6 M -3 7 L -1 6 L 0 5 L 1 3 L 1 1 L 0 0 L -1 0 L -3 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9 L 1 9 L 4 7 L 6 4 M -1 0 L -3 2 L -4 4 L -4 7 L -2 9","-3 6 M 0 0 L 3 -3 L 5 -6 L 6 -9 L 6 -11 L 5 -12 L 3 -11 L 2 -9 L -7 18 L -7 20 L -6 21 L -4 20 L -3 17 L -2 8 L -1 9 L 1 9 L 3 8 L 4 7 L 6 4 M 2 -9 L 1 -4 L 0 0 L -3 9 L -5 14 L -7 18","-7 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 M -2 0 L -4 2 L -5 4 L -5 7 L -3 9 M 4 0 L -2 18 M 5 0 L 2 9 L 0 14 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -3 13 L 0 11 L 4 9 L 7 7 L 9 4","-6 9 M -6 4 L -4 1 L -2 -3 M 1 -12 L -6 9 M 2 -12 L -5 9 M -3 3 L -1 1 L 1 0 L 2 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 M 2 0 L 3 1 L 3 3 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4","-4 4 M 1 -6 L 0 -5 L 1 -4 L 2 -5 L 1 -6 M -1 0 L -3 6 L -3 8 L -1 9 L 1 8 L 2 7 L 4 4 M 0 0 L -2 6 L -2 8 L -1 9","-4 4 M 1 -6 L 0 -5 L 1 -4 L 2 -5 L 1 -6 M -1 0 L -7 18 M 0 0 L -3 9 L -5 14 L -7 18 L -8 20 L -10 21 L -11 20 L -11 18 L -10 15 L -8 13 L -5 11 L -1 9 L 2 7 L 4 4","-6 8 M -6 4 L -4 1 L -2 -3 M 1 -12 L -6 9 M 2 -12 L -5 9 M 3 0 L 3 1 L 4 1 L 3 0 L 2 0 L 0 2 L -3 3 M -3 3 L 0 4 L 1 8 L 2 9 M -3 3 L -1 4 L 0 8 L 2 9 L 3 9 L 6 7 L 8 4","-4 4 M -4 4 L -2 1 L 0 -3 M 3 -12 L -3 6 L -3 8 L -1 9 L 1 8 L 2 7 L 4 4 M 4 -12 L -2 6 L -2 8 L -1 9","-13 12 M -13 4 L -11 1 L -9 0 L -7 1 L -7 3 L -9 9 M -9 0 L -8 1 L -8 3 L -10 9 M -7 3 L -5 1 L -3 0 L -2 0 L 0 1 L 0 3 L -2 9 M -2 0 L -1 1 L -1 3 L -3 9 M 0 3 L 2 1 L 4 0 L 5 0 L 7 1 L 7 3 L 6 6 L 6 8 L 7 9 M 5 0 L 6 1 L 6 3 L 5 6 L 5 8 L 7 9 L 9 8 L 10 7 L 12 4","-9 9 M -9 4 L -7 1 L -5 0 L -3 1 L -3 3 L -5 9 M -5 0 L -4 1 L -4 3 L -6 9 M -3 3 L -1 1 L 1 0 L 2 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 M 2 0 L 3 1 L 3 3 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4","-7 7 M 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 L 3 5 L 3 3 L 2 1 L 0 0 L -1 1 L -1 3 L 0 5 L 2 6 L 4 6 L 6 5 L 7 4 M -2 0 L -4 2 L -5 4 L -5 7 L -3 9","-6 9 M -6 4 L -4 1 L -2 -3 M -1 -6 L -10 21 M 0 -6 L -9 21 M -3 3 L -1 1 L 1 0 L 2 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 M 2 0 L 3 1 L 3 3 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4","-7 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 M -2 0 L -4 2 L -5 4 L -5 7 L -3 9 M 4 0 L -2 18 L -2 20 L -1 21 L 1 20 L 2 17 L 2 9 L 4 9 L 7 7 L 9 4 M 5 0 L 2 9 L 0 14 L -2 18","-6 8 M -6 4 L -4 1 L -2 0 L 0 1 L 0 3 L -2 9 M -2 0 L -1 1 L -1 3 L -3 9 M 0 3 L 2 1 L 4 0 L 5 0 L 4 3 M 4 0 L 4 3 L 5 5 L 6 5 L 8 4","-4 8 M -4 4 L -2 1 L -1 -1 L -1 1 L 2 3 L 3 5 L 3 7 L 2 8 L 0 9 M -1 1 L 1 3 L 2 5 L 2 7 L 0 9 M -4 8 L -2 9 L 3 9 L 6 7 L 8 4","-4 4 M -4 4 L -2 1 L 0 -3 M 3 -12 L -3 6 L -3 8 L -1 9 L 1 8 L 2 7 L 4 4 M 4 -12 L -2 6 L -2 8 L -1 9 M -2 -4 L 4 -4","-7 9 M -4 0 L -6 6 L -6 8 L -4 9 L -3 9 L -1 8 L 1 6 L 3 3 M -3 0 L -5 6 L -5 8 L -4 9 M 4 0 L 2 6 L 2 8 L 4 9 L 6 8 L 7 7 L 9 4 M 5 0 L 3 6 L 3 8 L 4 9","-7 8 M -4 0 L -5 2 L -6 5 L -6 8 L -4 9 L -3 9 L 0 8 L 2 6 L 3 3 L 3 0 M -3 0 L -4 2 L -5 5 L -5 8 L -4 9 M 3 0 L 4 4 L 5 5 L 6 5 L 8 4","-10 11 M -6 0 L -8 2 L -9 5 L -9 8 L -7 9 L -6 9 L -4 8 L -2 6 M -5 0 L -7 2 L -8 5 L -8 8 L -7 9 M 0 0 L -2 6 L -2 8 L 0 9 L 1 9 L 3 8 L 5 6 L 6 3 L 6 0 M 1 0 L -1 6 L -1 8 L 0 9 M 6 0 L 7 4 L 8 5 L 9 5 L 11 4","-8 8 M -8 4 L -6 1 L -4 0 L -2 0 L -1 1 L -1 3 L -2 6 L -3 8 L -5 9 L -6 9 L -7 8 L -7 7 L -6 7 L -7 8 M 5 1 L 4 2 L 5 2 L 5 1 L 4 0 L 3 0 L 1 1 L 0 3 L -1 6 L -1 8 L 0 9 L 3 9 L 6 7 L 8 4 M -1 1 L 0 3 M 1 1 L -1 3 M -2 6 L -1 8 M -1 6 L -3 8","-7 9 M -4 0 L -6 6 L -6 8 L -4 9 L -3 9 L -1 8 L 1 6 L 3 3 M -3 0 L -5 6 L -5 8 L -4 9 M 4 0 L -2 18 M 5 0 L 2 9 L 0 14 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -3 13 L 0 11 L 4 9 L 7 7 L 9 4","-6 7 M -6 4 L -4 1 L -2 0 L 0 0 L 2 1 L 2 4 L 1 6 L -2 8 L -4 9 M 0 0 L 1 1 L 1 4 L 0 6 L -2 8 M -4 9 L -2 10 L -1 12 L -1 15 L -2 18 L -4 20 L -6 21 L -7 20 L -7 18 L -6 15 L -3 12 L 0 10 L 4 7 L 7 4 M -4 9 L -3 10 L -2 12 L -2 15 L -3 18 L -4 20","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-7 8 M 1 -12 L -1 -11 L -2 -9 L -2 -7 L -1 -5 L 1 -4 L 3 -4 L 5 -5 L 6 -7 L 6 -9 L 5 -11 L 3 -12 L 1 -12"},
        new string[] {"-8 8","-5 6 M 3 -12 L 2 -11 L 0 1 M 3 -11 L 0 1 M 3 -12 L 4 -11 L 0 1 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-9 9 M -2 -12 L -4 -5 M -1 -12 L -4 -5 M 7 -12 L 5 -5 M 8 -12 L 5 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 11 M 2 -16 L -6 13 M 7 -16 L -1 13 M 8 -8 L 7 -7 L 8 -6 L 9 -7 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -7 L -4 -5 L -3 -4 L 4 0 L 6 2 M -5 -7 L -3 -5 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -4 L 9 -3 L 10 -2 L 11 -3 L 11 -4 L 10 -5 L 9 -5 L 7 -4 L 5 -2 L 0 6 L -2 8 L -4 9 L -7 9 L -10 8 L -11 6 L -11 4 L -10 2 L -9 1 L -7 0 L -2 -2 L 0 -3 L 2 -5 L 3 -7 L 3 -9 L 2 -11 L 0 -12 L -2 -11 L -3 -9 L -3 -6 L -2 0 L -1 3 L 1 6 L 3 8 L 5 9 L 7 9 L 8 7 L 8 6 M -7 9 L -9 8 L -10 6 L -10 4 L -9 2 L -8 1 L -2 -2 M -3 -6 L -2 -1 L -1 2 L 1 5 L 3 7 L 5 8 L 7 8 L 8 7","-5 6 M 3 -10 L 2 -11 L 3 -12 L 4 -11 L 4 -10 L 3 -8 L 1 -6","-7 8 M 8 -16 L 4 -13 L 1 -10 L -1 -7 L -3 -3 L -4 2 L -4 6 L -3 11 L -2 14 L -1 16 M 4 -13 L 1 -9 L -1 -5 L -2 -2 L -3 3 L -3 8 L -2 13 L -1 16","-8 7 M 1 -16 L 2 -14 L 3 -11 L 4 -6 L 4 -2 L 3 3 L 1 7 L -1 10 L -4 13 L -8 16 M 1 -16 L 2 -13 L 3 -8 L 3 -3 L 2 2 L 1 5 L -1 9 L -4 13","-8 9 M 2 -12 L 2 0 M -3 -9 L 7 -3 M 7 -9 L -3 -3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-5 6 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-13 13 M -9 0 L 9 0","-5 5 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-11 11 M 13 -16 L -13 16","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -3 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12","-10 11 M 2 -8 L -3 9 M 4 -12 L -2 9 M 4 -12 L 1 -9 L -2 -7 L -4 -6 M 3 -9 L -1 -7 L -4 -6","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 5 -3 L 2 -1 L -2 1 L -5 3 L -7 5 L -9 9 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 L -2 1 M -8 7 L -7 6 L -5 6 L 0 8 L 3 8 L 5 7 L 6 5 M -5 6 L 0 9 L 3 9 L 5 8 L 6 5","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 4 -3 L 1 -2 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 M -1 -2 L 1 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -2 L 3 -1 L 4 0 L 5 2 L 5 5 L 4 7 L 3 8 L 1 9","-10 11 M 6 -11 L 0 9 M 7 -12 L 1 9 M 7 -12 L -8 3 L 8 3","-10 11 M -1 -12 L -6 -2 M -1 -12 L 9 -12 M -1 -11 L 4 -11 L 9 -12 M -6 -2 L -5 -3 L -2 -4 L 1 -4 L 4 -3 L 5 -2 L 6 0 L 6 3 L 5 6 L 3 8 L 0 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -4 L 3 -3 L 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 L 0 9","-10 11 M 7 -9 L 6 -8 L 7 -7 L 8 -8 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 5 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 L -1 -3 L -3 -2 L -5 0 L -6 2 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 6 L -5 8 M 0 9 L 2 8 L 4 6 L 5 4 L 5 0 L 4 -2","-10 11 M -4 -12 L -6 -6 M 9 -12 L 8 -9 L 6 -6 L 1 0 L -1 3 L -2 5 L -3 9 M 6 -6 L 0 0 L -2 3 L -3 5 L -4 9 M -5 -9 L -2 -12 L 0 -12 L 5 -9 M -4 -10 L -2 -11 L 0 -11 L 5 -9 L 7 -9 L 8 -10 L 9 -12","-10 11 M 1 -12 L -2 -11 L -3 -10 L -4 -8 L -4 -5 L -3 -3 L -1 -2 L 2 -2 L 6 -3 L 7 -4 L 8 -6 L 8 -9 L 7 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -2 -10 L -3 -8 L -3 -5 L -2 -3 L -1 -2 M 2 -2 L 5 -3 L 6 -4 L 7 -6 L 7 -9 L 6 -11 L 4 -12 M -1 -2 L -5 -1 L -7 1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 4 8 L 5 7 L 6 5 L 6 2 L 5 0 L 4 -1 L 2 -2 M -1 -2 L -4 -1 L -6 1 L -7 3 L -7 6 L -6 8 L -4 9 M 0 9 L 3 8 L 4 7 L 5 5 L 5 1 L 4 -1","-10 11 M 7 -5 L 6 -3 L 4 -1 L 2 0 L -1 0 L -3 -1 L -4 -2 L -5 -4 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 4 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -4 L 7 0 L 6 3 L 4 6 L 2 8 L -1 9 L -4 9 L -6 8 L -7 6 L -7 5 L -6 4 L -5 5 L -6 6 M -3 -1 L -4 -3 L -4 -7 L -3 -9 L -1 -11 L 1 -12 M 6 -11 L 7 -9 L 7 -4 L 6 0 L 5 3 L 3 6 L 1 8 L -1 9","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 7 L -3 8 L -2 9 L -1 8","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 1 -2 L -1 -1 L -1 1 L 0 2 L 2 2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-11 9 M -11 9 L -9 8 L -6 5 L -3 1 L 1 -6 L 4 -12 L 4 9 L 3 6 L 1 3 L -1 1 L -4 -1 L -6 -1 L -7 0 L -7 2 L -6 4 L -4 6 L -1 8 L 2 9 L 7 9","-12 11 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 6 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 5 -3 L 2 -2 M 1 -2 L 2 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9 L 0 9 L -2 8 L -3 6","-10 10 M 2 -6 L 2 -5 L 3 -4 L 5 -4 L 7 -5 L 8 -7 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -4 L -7 0 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 5 4","-11 12 M 2 -12 L 0 -11 L -1 -9 L -2 -5 L -3 1 L -4 4 L -5 6 L -7 8 L -9 9 L -11 9 L -12 8 L -12 6 L -11 5 L -9 5 L -7 6 L -5 8 L -2 9 L 1 9 L 4 8 L 6 6 L 8 2 L 9 -3 L 9 -7 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L 0 -10 L 0 -8 L 1 -5 L 3 -2 L 5 0 L 8 2 L 10 3","-10 10 M 4 -8 L 4 -7 L 5 -6 L 7 -6 L 8 -7 L 8 -9 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -4 -9 L -4 -6 L -3 -4 L -2 -3 L 1 -2 L -2 -2 L -5 -1 L -6 0 L -7 2 L -7 5 L -6 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 6 L 7 4","-10 10 M 0 -6 L -2 -6 L -4 -7 L -5 -9 L -4 -11 L -1 -12 L 2 -12 L 6 -11 L 9 -11 L 11 -12 M 6 -11 L 4 -4 L 2 2 L 0 6 L -2 8 L -4 9 L -6 9 L -8 8 L -9 6 L -9 4 L -8 3 L -6 3 L -4 4 M -1 -2 L 8 -2","-11 12 M -11 9 L -9 8 L -5 4 L -2 -1 L -1 -4 L 0 -8 L 0 -11 L -1 -12 L -2 -12 L -3 -11 L -4 -9 L -4 -6 L -3 -4 L -1 -3 L 3 -3 L 6 -4 L 7 -5 L 8 -7 L 8 -1 L 7 4 L 6 6 L 4 8 L 1 9 L -3 9 L -6 8 L -8 6 L -9 4 L -9 2","-12 12 M -5 -5 L -7 -6 L -8 -8 L -8 -9 L -7 -11 L -5 -12 L -4 -12 L -2 -11 L -1 -9 L -1 -7 L -2 -3 L -4 3 L -6 7 L -8 9 L -10 9 L -11 8 L -11 6 M -5 0 L 4 -3 L 6 -4 L 9 -6 L 11 -8 L 12 -10 L 12 -11 L 11 -12 L 10 -12 L 8 -10 L 6 -6 L 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-9 8 M 5 4 L 3 2 L 1 -1 L 0 -3 L -1 -6 L -1 -9 L 0 -11 L 1 -12 L 3 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -1 L 2 4 L 1 6 L -1 8 L -3 9 L -5 9 L -7 8 L -8 6 L -8 4 L -7 3 L -5 3 L -3 4","-8 7 M 2 12 L 0 9 L -2 4 L -3 -2 L -3 -8 L -2 -11 L 0 -12 L 2 -12 L 3 -11 L 4 -8 L 4 -5 L 3 0 L 0 9 L -2 15 L -3 18 L -4 20 L -6 21 L -7 20 L -7 18 L -6 15 L -4 12 L -2 10 L 1 8 L 5 6","-12 12 M -5 -5 L -7 -6 L -8 -8 L -8 -9 L -7 -11 L -5 -12 L -4 -12 L -2 -11 L -1 -9 L -1 -7 L -2 -3 L -4 3 L -6 7 L -8 9 L -10 9 L -11 8 L -11 6 M 12 -9 L 12 -11 L 11 -12 L 10 -12 L 8 -11 L 6 -9 L 4 -6 L 2 -4 L 0 -3 L -2 -3 M 0 -3 L 1 -1 L 1 6 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-9 10 M -5 0 L -3 0 L 1 -1 L 4 -3 L 6 -5 L 7 -7 L 7 -10 L 6 -12 L 4 -12 L 3 -11 L 2 -9 L 1 -4 L 0 1 L -1 4 L -2 6 L -4 8 L -6 9 L -8 9 L -9 8 L -9 6 L -8 5 L -6 5 L -4 6 L -1 8 L 2 9 L 4 9 L 7 8 L 9 6","-18 15 M -13 -5 L -15 -6 L -16 -8 L -16 -9 L -15 -11 L -13 -12 L -12 -12 L -10 -11 L -9 -9 L -9 -7 L -10 -2 L -11 2 L -13 9 M -11 2 L -8 -6 L -6 -10 L -5 -11 L -3 -12 L -2 -12 L 0 -11 L 1 -9 L 1 -7 L 0 -2 L -1 2 L -3 9 M -1 2 L 2 -6 L 4 -10 L 5 -11 L 7 -12 L 8 -12 L 10 -11 L 11 -9 L 11 -7 L 10 -2 L 8 5 L 8 8 L 9 9 L 10 9 L 12 8 L 13 7 L 15 4","-13 11 M -8 -5 L -10 -6 L -11 -8 L -11 -9 L -10 -11 L -8 -12 L -7 -12 L -5 -11 L -4 -9 L -4 -7 L -5 -2 L -6 2 L -8 9 M -6 2 L -3 -6 L -1 -10 L 0 -11 L 2 -12 L 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -2 L 4 5 L 4 8 L 5 9 L 6 9 L 8 8 L 9 7 L 11 4","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -4 L -7 0 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 1 L 8 -3 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 L 0 -10 L 0 -7 L 1 -4 L 3 -1 L 5 1 L 8 3 L 10 4","-12 13 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 8 -12 L 10 -11 L 11 -10 L 12 -8 L 12 -5 L 11 -3 L 10 -2 L 8 -1 L 5 -1 L 3 -2 L 2 -3","-10 12 M 3 -6 L 2 -4 L 1 -3 L -1 -2 L -3 -2 L -4 -4 L -4 -6 L -3 -9 L -1 -11 L 2 -12 L 5 -12 L 7 -11 L 8 -9 L 8 -5 L 7 -2 L 5 1 L 1 5 L -2 7 L -4 8 L -7 9 L -9 9 L -10 8 L -10 6 L -9 5 L -7 5 L -5 6 L -2 8 L 1 9 L 4 9 L 7 8 L 9 6","-12 13 M 1 -10 L 2 -9 L 2 -6 L 1 -2 L 0 1 L -1 3 L -3 6 L -5 8 L -7 9 L -8 9 L -9 8 L -9 5 L -8 0 L -7 -3 L -6 -5 L -4 -8 L -2 -10 L 0 -11 L 3 -12 L 7 -12 L 9 -11 L 10 -10 L 11 -8 L 11 -5 L 10 -3 L 9 -2 L 7 -1 L 4 -1 L 1 -2 L 2 -1 L 3 1 L 3 6 L 4 8 L 6 9 L 8 8 L 9 7 L 11 4","-10 10 M -10 9 L -8 8 L -6 6 L -3 2 L -1 -1 L 1 -5 L 2 -8 L 2 -11 L 1 -12 L 0 -12 L -1 -11 L -2 -9 L -2 -7 L -1 -5 L 1 -3 L 4 -1 L 6 1 L 7 3 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6 L -8 4 L -8 2","-10 9 M 0 -6 L -2 -6 L -4 -7 L -5 -9 L -4 -11 L -1 -12 L 2 -12 L 6 -11 L 9 -11 L 11 -12 M 6 -11 L 4 -4 L 2 2 L 0 6 L -2 8 L -4 9 L -6 9 L -8 8 L -9 6 L -9 4 L -8 3 L -6 3 L -4 4","-13 11 M -8 -5 L -10 -6 L -11 -8 L -11 -9 L -10 -11 L -8 -12 L -7 -12 L -5 -11 L -4 -9 L -4 -7 L -5 -3 L -6 0 L -7 4 L -7 6 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 3 3 L 6 -5 L 8 -12 M 6 -5 L 5 -1 L 4 5 L 4 8 L 5 9 L 6 9 L 8 8 L 9 7 L 11 4","-12 11 M -7 -5 L -9 -6 L -10 -8 L -10 -9 L -9 -11 L -7 -12 L -6 -12 L -4 -11 L -3 -9 L -3 -7 L -4 -3 L -5 0 L -6 4 L -6 7 L -5 9 L -3 9 L -1 8 L 2 5 L 4 2 L 6 -2 L 7 -5 L 8 -9 L 8 -11 L 7 -12 L 6 -12 L 5 -11 L 4 -9 L 4 -7 L 5 -4 L 7 -2 L 9 -1","-15 13 M -10 -5 L -12 -6 L -13 -8 L -13 -9 L -12 -11 L -10 -12 L -9 -12 L -7 -11 L -6 -9 L -6 -6 L -7 9 M 3 -12 L -7 9 M 3 -12 L 1 9 M 15 -12 L 13 -11 L 10 -8 L 7 -4 L 4 2 L 1 9","-12 12 M -4 -6 L -6 -6 L -7 -7 L -7 -9 L -6 -11 L -4 -12 L -2 -12 L 0 -11 L 1 -9 L 1 -6 L -1 3 L -1 6 L 0 8 L 2 9 L 4 9 L 6 8 L 7 6 L 7 4 L 6 3 L 4 3 M 11 -9 L 11 -11 L 10 -12 L 8 -12 L 6 -11 L 4 -9 L 2 -6 L -2 3 L -4 6 L -6 8 L -8 9 L -10 9 L -11 8 L -11 6","-12 11 M -7 -5 L -9 -6 L -10 -8 L -10 -9 L -9 -11 L -7 -12 L -6 -12 L -4 -11 L -3 -9 L -3 -7 L -4 -3 L -5 0 L -6 4 L -6 6 L -5 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 3 L 5 1 L 7 -5 M 9 -12 L 7 -5 L 4 5 L 2 11 L 0 16 L -2 20 L -4 21 L -5 20 L -5 18 L -4 15 L -2 12 L 1 9 L 4 7 L 9 4","-10 11 M 3 -6 L 2 -4 L 1 -3 L -1 -2 L -3 -2 L -4 -4 L -4 -6 L -3 -9 L -1 -11 L 2 -12 L 5 -12 L 7 -11 L 8 -9 L 8 -5 L 7 -2 L 5 2 L 2 5 L -2 8 L -4 9 L -7 9 L -8 8 L -8 6 L -7 5 L -4 5 L -2 6 L -1 7 L 0 9 L 0 12 L -1 15 L -2 17 L -4 20 L -6 21 L -7 20 L -7 18 L -6 15 L -4 12 L -1 9 L 2 7 L 8 4","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-8 8 M -2 -6 L 0 -9 L 2 -6 M -5 -3 L 0 -8 L 5 -3 M 0 -8 L 0 9","-8 8 M -8 11 L 8 11","-5 6 M 4 -12 L 2 -10 L 1 -8 L 1 -7 L 2 -6 L 3 -7 L 2 -8","-6 10 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 L 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-5 9 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 2 L -4 8 L -3 9 L -2 9 L 0 8 L 2 6 L 3 3 L 3 0 L 4 4 L 5 5 L 7 5 L 9 4","-5 6 M 2 2 L 2 1 L 1 0 L -1 0 L -3 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9 L 1 9 L 4 7 L 6 4","-6 10 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 6 L 8 -12 M 4 0 L 3 5 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-4 6 M -3 7 L -1 6 L 0 5 L 1 3 L 1 1 L 0 0 L -1 0 L -3 1 L -4 3 L -4 6 L -3 8 L -1 9 L 1 9 L 3 8 L 4 7 L 6 4","-3 5 M -3 4 L 1 -1 L 3 -4 L 4 -6 L 5 -9 L 5 -11 L 4 -12 L 2 -11 L 1 -9 L -1 -1 L -4 8 L -7 15 L -8 18 L -8 20 L -7 21 L -5 20 L -4 17 L -3 8 L -2 9 L 0 9 L 2 8 L 3 7 L 5 4","-6 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 M 4 0 L 2 7 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 3 9 L 6 7 L 9 4","-5 10 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 1 L -5 9 M -5 9 L -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-2 5 M 1 -5 L 1 -4 L 2 -4 L 2 -5 L 1 -5 M -2 4 L 0 0 L -2 6 L -2 8 L -1 9 L 0 9 L 2 8 L 3 7 L 5 4","-2 5 M 1 -5 L 1 -4 L 2 -4 L 2 -5 L 1 -5 M -2 4 L 0 0 L -6 18 L -7 20 L -9 21 L -10 20 L -10 18 L -9 15 L -6 12 L -3 10 L -1 9 L 2 7 L 5 4","-5 9 M -5 4 L -3 1 L 0 -4 L 1 -6 L 2 -9 L 2 -11 L 1 -12 L -1 -11 L -2 -9 L -3 -5 L -4 1 L -5 9 M -5 9 L -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 2 4 L -1 4 M -1 4 L 1 5 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-3 5 M -3 4 L -1 1 L 2 -4 L 3 -6 L 4 -9 L 4 -11 L 3 -12 L 1 -11 L 0 -9 L -1 -5 L -2 2 L -2 8 L -1 9 L 0 9 L 2 8 L 3 7 L 5 4","-13 12 M -13 4 L -11 1 L -9 0 L -8 1 L -8 2 L -9 6 L -10 9 M -9 6 L -8 4 L -6 1 L -4 0 L -2 0 L -1 1 L -1 2 L -2 6 L -3 9 M -2 6 L -1 4 L 1 1 L 3 0 L 5 0 L 6 1 L 6 3 L 5 6 L 5 8 L 6 9 L 7 9 L 9 8 L 10 7 L 12 4","-8 10 M -8 4 L -6 1 L -4 0 L -3 1 L -3 2 L -4 6 L -5 9 M -4 6 L -3 4 L -1 1 L 1 0 L 3 0 L 4 1 L 4 3 L 3 6 L 3 8 L 4 9 L 5 9 L 7 8 L 8 7 L 10 4","-6 8 M 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 L 3 5 L 3 3 L 2 1 L 0 0 L -1 1 L -1 3 L 0 5 L 2 6 L 5 6 L 7 5 L 8 4","-7 8 M -7 4 L -5 1 L -4 -1 L -5 3 L -11 21 M -5 3 L -4 1 L -2 0 L 0 0 L 2 1 L 3 3 L 3 5 L 2 7 L 1 8 L -1 9 M -5 8 L -3 9 L 0 9 L 3 8 L 5 7 L 8 4","-6 9 M 3 3 L 2 1 L 0 0 L -2 0 L -4 1 L -5 2 L -6 4 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 M 4 0 L 3 3 L 1 8 L -2 15 L -3 18 L -3 20 L -2 21 L 0 20 L 1 17 L 1 10 L 3 9 L 6 7 L 9 4","-5 8 M -5 4 L -3 1 L -2 -1 L -2 1 L 1 1 L 2 2 L 2 4 L 1 7 L 1 8 L 2 9 L 3 9 L 5 8 L 6 7 L 8 4","-4 7 M -4 4 L -2 1 L -1 -1 L -1 1 L 1 4 L 2 6 L 2 8 L 0 9 M -4 8 L -2 9 L 2 9 L 4 8 L 5 7 L 7 4","-3 6 M -3 4 L -1 1 L 1 -3 M 4 -12 L -2 6 L -2 8 L -1 9 L 1 9 L 3 8 L 4 7 L 6 4 M -2 -4 L 5 -4","-6 9 M -6 4 L -4 0 L -6 6 L -6 8 L -5 9 L -3 9 L -1 8 L 1 6 L 3 3 M 4 0 L 2 6 L 2 8 L 3 9 L 4 9 L 6 8 L 7 7 L 9 4","-6 9 M -6 4 L -4 0 L -5 5 L -5 8 L -4 9 L -3 9 L 0 8 L 2 6 L 3 3 L 3 0 M 3 0 L 4 4 L 5 5 L 7 5 L 9 4","-9 12 M -6 0 L -8 2 L -9 5 L -9 7 L -8 9 L -6 9 L -4 8 L -2 6 M 0 0 L -2 6 L -2 8 L -1 9 L 1 9 L 3 8 L 5 6 L 6 3 L 6 0 M 6 0 L 7 4 L 8 5 L 10 5 L 12 4","-8 8 M -8 4 L -6 1 L -4 0 L -2 0 L -1 1 L -1 8 L 0 9 L 3 9 L 6 7 L 8 4 M 5 1 L 4 0 L 2 0 L 1 1 L -3 8 L -4 9 L -6 9 L -7 8","-6 9 M -6 4 L -4 0 L -6 6 L -6 8 L -5 9 L -3 9 L -1 8 L 1 6 L 3 3 M 4 0 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 3 9 L 6 7 L 9 4","-6 8 M -6 4 L -4 1 L -2 0 L 0 0 L 2 2 L 2 4 L 1 6 L -1 8 L -4 9 L -2 10 L -1 12 L -1 15 L -2 18 L -3 20 L -5 21 L -6 20 L -6 18 L -5 15 L -2 12 L 1 10 L 5 7 L 8 4","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-7 7 M -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -4 L 1 -4 L 3 -5 L 4 -7 L 4 -9 L 3 -11 L 1 -12 L -1 -12"},
        new string[] {"-8 8","-14 14 M -14 0 L 14 0","-14 14 M -14 14 L 14 -14","0 0 M 0 -20 L 0 20","-14 14 M -14 -14 L 14 14","-14 14 M -14 0 L 14 0","-12 12 M -12 7 L 12 -7","-7 7 M -7 12 L 7 -12","0 0 M 0 -14 L 0 14","-7 7 M -7 -12 L 7 12","-12 12 M -12 -7 L 12 7","-7 7 M -7 0 L 7 0","-5 5 M -5 5 L 5 -5","0 0 M 0 -7 L 0 7","-5 5 M -5 -5 L 5 5","-11 0 M 0 -11 L -2 -11 L -5 -10 L -8 -8 L -10 -5 L -11 -2 L -11 0","-11 0 M -11 0 L -11 2 L -10 5 L -8 8 L -5 10 L -2 11 L 0 11","0 11 M 0 11 L 2 11 L 5 10 L 8 8 L 10 5 L 11 2 L 11 0","0 11 M 11 0 L 11 -2 L 10 -5 L 8 -8 L 5 -10 L 2 -11 L 0 -11","-14 14 M -14 -3 L -11 -1 L -7 1 L -2 2 L 2 2 L 7 1 L 11 -1 L 14 -3","-2 3 M 3 -14 L 1 -11 L -1 -7 L -2 -2 L -2 2 L -1 7 L 1 11 L 3 14","-3 2 M -3 -14 L -1 -11 L 1 -7 L 2 -2 L 2 2 L 1 7 L -1 11 L -3 14","-14 14 M -14 3 L -11 1 L -7 -1 L -2 -2 L 2 -2 L 7 -1 L 11 1 L 14 3","-7 7 M 0 -8 L 7 -4 L -7 4 L 0 8","-8 8 M -8 0 L -4 -7 L 4 7 L 8 0","-7 7 M -7 4 L -7 -4 L 7 4 L 7 -4","-8 8 M -6 6 L -8 -2 L 8 2 L 6 -6","-8 8 M -8 11 L -6 11 L -3 10 L -1 9 L 2 6 L 3 4 L 4 1 L 4 -3 L 3 -6 L 2 -8 L 1 -9 L -1 -9 L -2 -8 L -3 -6 L -4 -3 L -4 1 L -3 4 L -2 6 L 1 9 L 3 10 L 6 11 L 8 11","-9 11 M 11 8 L 11 6 L 10 3 L 9 1 L 6 -2 L 4 -3 L 1 -4 L -3 -4 L -6 -3 L -8 -2 L -9 -1 L -9 1 L -8 2 L -6 3 L -3 4 L 1 4 L 4 3 L 6 2 L 9 -1 L 10 -3 L 11 -6 L 11 -8","-8 8 M 8 -11 L 6 -11 L 3 -10 L 1 -9 L -2 -6 L -3 -4 L -4 -1 L -4 3 L -3 6 L -2 8 L -1 9 L 1 9 L 2 8 L 3 6 L 4 3 L 4 -1 L 3 -4 L 2 -6 L -1 -9 L -3 -10 L -6 -11 L -8 -11","-11 9 M -11 -8 L -11 -6 L -10 -3 L -9 -1 L -6 2 L -4 3 L -1 4 L 3 4 L 6 3 L 8 2 L 9 1 L 9 -1 L 8 -2 L 6 -3 L 3 -4 L -1 -4 L -4 -3 L -6 -2 L -9 1 L -10 3 L -11 6 L -11 8","-13 9 M -13 -2 L -12 0 L -10 2 L -8 3 L -5 4 L -1 4 L 3 3 L 6 1 L 8 -2 L 9 -4 L 8 -6 L 5 -6 L 1 -5 L -1 -4 L -4 -2 L -6 1 L -7 4 L -7 7 L -6 10 L -5 12","-13 7 M -13 2 L -10 4 L -7 5 L -2 5 L 1 4 L 4 2 L 6 -1 L 7 -4 L 7 -6 L 6 -7 L 4 -7 L 1 -6 L -2 -4 L -4 -1 L -5 2 L -5 7 L -4 10 L -2 13","-3 3 M -1 -3 L -3 -1 L -3 1 L -1 3 L 1 3 L 3 1 L 3 -1 L 1 -3 L -1 -3 M -1 -2 L -2 -1 L -2 1 L -1 2 L 1 2 L 2 1 L 2 -1 L 1 -2 L -1 -2 M 0 -1 L -1 0 L 0 1 L 1 0 L 0 -1","0 5 M 0 -5 L 1 -5 L 3 -4 L 4 -3 L 5 -1 L 5 1 L 4 3 L 3 4 L 1 5 L 0 5","-14 14 M -14 0 L -8 0 M -3 0 L 3 0 M 8 0 L 14 0","-14 14 M -14 3 L -14 -3 L 14 -3 L 14 3","-8 8 M 0 -14 L -8 0 M 0 -14 L 8 0","-14 14 M -14 0 L 14 0 M -8 7 L 8 7 M -2 14 L 2 14","-14 14 M -14 0 L 14 0 M -14 0 L 0 16 M 14 0 L 0 16","-7 7 M -1 -7 L -4 -6 L -6 -4 L -7 -1 L -7 1 L -6 4 L -4 6 L -1 7 L 1 7 L 4 6 L 6 4 L 7 1 L 7 -1 L 6 -4 L 4 -6 L 1 -7 L -1 -7","-6 6 M -6 -6 L -6 6 L 6 6 L 6 -6 L -6 -6","-7 7 M 0 -8 L -7 4 L 7 4 L 0 -8","-6 6 M 0 -10 L -6 0 L 0 10 L 6 0 L 0 -10","-8 8 M 0 -9 L -2 -3 L -8 -3 L -3 1 L -5 7 L 0 3 L 5 7 L 3 1 L 8 -3 L 2 -3 L 0 -9","-7 7 M 0 -7 L 0 7 M -7 0 L 7 0","-5 5 M -5 -5 L 5 5 M 5 -5 L -5 5","-5 5 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-4 4 M -1 -4 L -3 -3 L -4 -1 L -4 1 L -3 3 L -1 4 L 1 4 L 3 3 L 4 1 L 4 -1 L 3 -3 L 1 -4 L -1 -4 M -3 -1 L -3 1 M -2 -2 L -2 2 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -2 L 2 2 M 3 -1 L 3 1","-4 4 M -4 -4 L -4 4 L 4 4 L 4 -4 L -4 -4 M -3 -3 L -3 3 M -2 -3 L -2 3 M -1 -3 L -1 3 M 0 -3 L 0 3 M 1 -3 L 1 3 M 2 -3 L 2 3 M 3 -3 L 3 3","-5 5 M 0 -6 L -5 3 L 5 3 L 0 -6 M 0 -3 L -3 2 M 0 -3 L 3 2 M 0 0 L -1 2 M 0 0 L 1 2","-6 3 M -6 0 L 3 5 L 3 -5 L -6 0 M -3 0 L 2 3 M -3 0 L 2 -3 M 0 0 L 2 1 M 0 0 L 2 -1","-5 5 M 0 6 L 5 -3 L -5 -3 L 0 6 M 0 3 L 3 -2 M 0 3 L -3 -2 M 0 0 L 1 -2 M 0 0 L -1 -2","-3 6 M 6 0 L -3 -5 L -3 5 L 6 0 M 3 0 L -2 -3 M 3 0 L -2 3 M 0 0 L -2 -1 M 0 0 L -2 1","0 7 M 0 -7 L 0 7 M 0 -7 L 7 -4 L 0 -1 M 1 -5 L 4 -4 L 1 -3","-9 9 M 0 -11 L 0 4 M -5 -8 L 5 -2 M 5 -8 L -5 -2 M -9 4 L -6 10 M 9 4 L 6 10 M -9 4 L 9 4 M -6 10 L 6 10","-5 5 M 0 -6 L 0 6 M -3 -3 L 3 -3 M -5 3 L -3 5 L -1 6 L 1 6 L 3 5 L 5 3","-6 6 M 0 -6 L 0 6 M -6 -1 L -5 -3 L 5 -3 L 6 -1 M -2 5 L 2 5","-7 7 M -5 -4 L 5 6 M 5 -4 L -5 6 M -3 -6 L -6 -3 L -7 -1 M 3 -6 L 6 -3 L 7 -1","-9 9 M -4 -9 L -9 9 M 4 -9 L 9 9 M -5 -5 L 9 9 M 5 -5 L -9 9 M -4 -9 L 4 -9 M -5 -5 L 5 -5","-7 7 M -7 -12 L 7 12","-11 9 M -5 -8 L 1 4 M -7 -2 L 1 -6 M -11 10 L 9 10 L 9 0 L -11 10","-6 6 M -2 -6 L -2 -2 L -6 -2 L -6 2 L -2 2 L -2 6 L 2 6 L 2 2 L 6 2 L 6 -2 L 2 -2 L 2 -6 L -2 -6","-7 7 M 7 -2 L 6 -4 L 4 -6 L 1 -7 L -1 -7 L -4 -6 L -6 -4 L -7 -1 L -7 1 L -6 4 L -4 6 L -1 7 L 1 7 L 4 6 L 6 4 L 7 2 M 7 -2 L 5 -4 L 3 -5 L 1 -5 L -1 -4 L -2 -3 L -3 -1 L -3 1 L -2 3 L -1 4 L 1 5 L 3 5 L 5 4 L 7 2","-7 7 M 0 -8 L -7 4 L 7 4 L 0 -8 M 0 8 L 7 -4 L -7 -4 L 0 8","-11 11 M -2 -9 L -2 -11 L -1 -12 L 1 -12 L 2 -11 L 2 -9 M -11 8 L -10 6 L -8 4 L -7 2 L -6 -2 L -6 -7 L -5 -8 L -3 -9 L 3 -9 L 5 -8 L 6 -7 L 6 -2 L 7 2 L 8 4 L 10 6 L 11 8 M -11 8 L 11 8 M -1 8 L -2 9 L -1 10 L 1 10 L 2 9 L 1 8","-8 8 M 0 -5 L 0 1 M 0 1 L -1 10 M 0 1 L 1 10 M -1 10 L 1 10 M 0 -5 L -1 -8 L -2 -10 L -4 -11 M -1 -8 L -4 -11 M 0 -5 L 1 -8 L 2 -10 L 4 -11 M 1 -8 L 4 -11 M 0 -5 L -4 -7 L -6 -7 L -8 -5 M -2 -6 L -6 -6 L -8 -5 M 0 -5 L 4 -7 L 6 -7 L 8 -5 M 2 -6 L 6 -6 L 8 -5 M 0 -5 L -2 -4 L -3 -3 L -3 0 M 0 -5 L -2 -3 L -3 0 M 0 -5 L 2 -4 L 3 -3 L 3 0 M 0 -5 L 2 -3 L 3 0","-8 8 M 0 -9 L 0 -7 M 0 -4 L 0 -2 M 0 1 L 0 3 M 0 7 L -1 10 M 0 7 L 1 10 M -1 10 L 1 10 M 0 -11 L -1 -9 L -2 -8 M 0 -11 L 1 -9 L 2 -8 M -2 -8 L 0 -9 L 2 -8 M 0 -7 L -2 -4 L -4 -3 L -5 -4 M 0 -7 L 2 -4 L 4 -3 L 5 -4 M -4 -3 L -2 -3 L 0 -4 L 2 -3 L 4 -3 M 0 -2 L -2 1 L -4 2 L -6 2 L -7 0 L -7 1 L -6 2 M 0 -2 L 2 1 L 4 2 L 6 2 L 7 0 L 7 1 L 6 2 M -4 2 L -2 2 L 0 1 L 2 2 L 4 2 M 0 3 L -2 6 L -3 7 L -5 8 L -6 8 L -7 7 L -8 5 L -8 7 L -6 8 M 0 3 L 2 6 L 3 7 L 5 8 L 6 8 L 7 7 L 8 5 L 8 7 L 6 8 M -5 8 L -3 8 L 0 7 L 3 8 L 5 8","-8 8 M 0 7 L -1 10 M 0 7 L 1 10 M -1 10 L 1 10 M 0 7 L 3 8 L 6 8 L 8 6 L 8 3 L 7 2 L 5 2 L 7 0 L 8 -3 L 7 -5 L 5 -6 L 3 -5 L 4 -8 L 3 -10 L 1 -11 L -1 -11 L -3 -10 L -4 -8 L -3 -5 L -5 -6 L -7 -5 L -8 -3 L -7 0 L -5 2 L -7 2 L -8 3 L -8 6 L -6 8 L -3 8 L 0 7","-8 8 M 0 7 L -1 10 M 0 7 L 1 10 M -1 10 L 1 10 M 0 7 L 4 6 L 4 4 L 6 3 L 6 0 L 8 -1 L 8 -6 L 7 -9 L 6 -10 L 4 -10 L 2 -11 L -2 -11 L -4 -10 L -6 -10 L -7 -9 L -8 -6 L -8 -1 L -6 0 L -6 3 L -4 4 L -4 6 L 0 7","-9 9 M -9 -2 L -7 0 M -6 -7 L -4 -2 M 0 -11 L 0 -3 M 6 -7 L 4 -2 M 9 -2 L 7 0","-11 11 M -9 -9 L -8 -7 L -7 -3 L -7 3 L -8 7 L -9 9 M 9 -9 L 8 -7 L 7 -3 L 7 3 L 8 7 L 9 9 M -9 -9 L -7 -8 L -3 -7 L 3 -7 L 7 -8 L 9 -9 M -9 9 L -7 8 L -3 7 L 3 7 L 7 8 L 9 9","-12 12 M 0 0 L 0 9 L -1 10 M 0 4 L -1 10 M 0 -9 L -1 -10 L -3 -10 L -4 -9 L -4 -7 L -3 -4 L 0 0 M 0 -9 L 1 -10 L 3 -10 L 4 -9 L 4 -7 L 3 -4 L 0 0 M 0 0 L -4 -3 L -6 -4 L -8 -4 L -9 -3 L -9 -1 L -8 0 M 0 0 L 4 -3 L 6 -4 L 8 -4 L 9 -3 L 9 -1 L 8 0 M 0 0 L -4 3 L -6 4 L -8 4 L -9 3 L -9 1 L -8 0 M 0 0 L 4 3 L 6 4 L 8 4 L 9 3 L 9 1 L 8 0","-8 8 M 3 -9 L 2 -8 L 3 -7 L 4 -8 L 4 -9 L 3 -11 L 1 -12 L -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -3 L 4 0 M -3 -5 L 2 -2 L 4 0 L 5 2 L 5 4 L 4 6 L 2 8 M -2 -4 L -4 -2 L -5 0 L -5 2 L -4 4 L -2 6 L 3 9 M -4 4 L 1 7 L 3 9 L 4 11 L 4 13 L 3 15 L 1 16 L -1 16 L -3 15 L -4 13 L -4 12 L -3 11 L -2 12 L -3 13","-8 8 M 0 -12 L -1 -10 L 0 -8 L 1 -10 L 0 -12 M 0 -12 L 0 16 M 0 -1 L -1 2 L 0 16 L 1 2 L 0 -1 M -6 -5 L -4 -4 L -2 -5 L -4 -6 L -6 -5 M -6 -5 L 6 -5 M 2 -5 L 4 -4 L 6 -5 L 4 -6 L 2 -5","-8 8 M 0 -12 L -1 -10 L 0 -8 L 1 -10 L 0 -12 M 0 -12 L 0 2 M 0 -2 L -1 0 L 1 4 L 0 6 L -1 4 L 1 0 L 0 -2 M 0 2 L 0 16 M 0 12 L -1 14 L 0 16 L 1 14 L 0 12 M -6 -5 L -4 -4 L -2 -5 L -4 -6 L -6 -5 M -6 -5 L 6 -5 M 2 -5 L 4 -4 L 6 -5 L 4 -6 L 2 -5 M -6 9 L -4 10 L -2 9 L -4 8 L -6 9 M -6 9 L 6 9 M 2 9 L 4 10 L 6 9 L 4 8 L 2 9","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 7 L -10 8 L -9 9 L -8 8 L -9 7 M 9 7 L 8 8 L 9 9 L 10 8 L 9 7","-12 12 M 0 -10 L -4 -6 L -7 -2 L -8 1 L -8 3 L -7 5 L -5 6 L -3 6 L -1 5 L 0 3 M 0 -10 L 4 -6 L 7 -2 L 8 1 L 8 3 L 7 5 L 5 6 L 3 6 L 1 5 L 0 3 M 0 3 L -1 7 L -2 10 M 0 3 L 1 7 L 2 10 M -2 10 L 2 10","-12 12 M 0 -4 L -1 -7 L -2 -9 L -4 -10 L -5 -10 L -7 -9 L -8 -7 L -8 -3 L -7 0 L -6 2 L -4 5 L 0 10 M 0 -4 L 1 -7 L 2 -9 L 4 -10 L 5 -10 L 7 -9 L 8 -7 L 8 -3 L 7 0 L 6 2 L 4 5 L 0 10","-12 12 M 0 -11 L -2 -8 L -6 -3 L -9 0 M 0 -11 L 2 -8 L 6 -3 L 9 0 M -9 0 L -6 3 L -2 8 L 0 11 M 9 0 L 6 3 L 2 8 L 0 11","-12 12 M 0 2 L 2 5 L 4 6 L 6 6 L 8 5 L 9 3 L 9 1 L 8 -1 L 6 -2 L 4 -2 L 1 -1 M 1 -1 L 3 -3 L 4 -5 L 4 -7 L 3 -9 L 1 -10 L -1 -10 L -3 -9 L -4 -7 L -4 -5 L -3 -3 L -1 -1 M -1 -1 L -4 -2 L -6 -2 L -8 -1 L -9 1 L -9 3 L -8 5 L -6 6 L -4 6 L -2 5 L 0 2 M 0 2 L -1 7 L -2 10 M 0 2 L 1 7 L 2 10 M -2 10 L 2 10","-9 9 M 4 -39 L 1 -37 L -1 -35 L -2 -33 L -3 -30 L -3 -26 L -2 -22 L 2 -14 L 3 -11 L 3 -8 L 2 -5 L 0 -2 M 1 -37 L -1 -34 L -2 -30 L -2 -26 L -1 -23 L 3 -15 L 4 -11 L 4 -8 L 3 -5 L 0 -2 L -4 0 L 0 2 L 3 5 L 4 8 L 4 11 L 3 15 L -1 23 L -2 26 L -2 30 L -1 34 L 1 37 M 0 2 L 2 5 L 3 8 L 3 11 L 2 14 L -2 22 L -3 26 L -3 30 L -2 33 L -1 35 L 1 37 L 4 39","-9 9 M -4 -39 L -1 -37 L 1 -35 L 2 -33 L 3 -30 L 3 -26 L 2 -22 L -2 -14 L -3 -11 L -3 -8 L -2 -5 L 0 -2 M -1 -37 L 1 -34 L 2 -30 L 2 -26 L 1 -23 L -3 -15 L -4 -11 L -4 -8 L -3 -5 L 0 -2 L 4 0 L 0 2 L -3 5 L -4 8 L -4 11 L -3 15 L 1 23 L 2 26 L 2 30 L 1 34 L -1 37 M 0 2 L -2 5 L -3 8 L -3 11 L -2 14 L 2 22 L 3 26 L 3 30 L 2 33 L 1 35 L -1 37 L -4 39","-9 9 M 4 -36 L 1 -33 L -1 -30 L -3 -26 L -4 -21 L -4 -15 L -3 -9 L -2 -5 L 1 6 L 2 10 L 3 16 L 3 21 L 2 26 L 1 29 L -1 33 M 1 -33 L -1 -29 L -2 -26 L -3 -21 L -3 -16 L -2 -10 L -1 -6 L 2 5 L 3 9 L 4 15 L 4 21 L 3 26 L 1 30 L -1 33 L -4 36","-9 9 M -4 -36 L -1 -33 L 1 -30 L 3 -26 L 4 -21 L 4 -15 L 3 -9 L 2 -5 L -1 6 L -2 10 L -3 16 L -3 21 L -2 26 L -1 29 L 1 33 M -1 -33 L 1 -29 L 2 -26 L 3 -21 L 3 -16 L 2 -10 L 1 -6 L -2 5 L -3 9 L -4 15 L -4 21 L -3 26 L -1 30 L 1 33 L 4 36","-27 8 M -24 0 L -17 0 L 0 29 M -18 0 L -1 29 M -19 0 L 0 32 M 8 -48 L 4 -8 L 0 32","-9 9 M 2 -5 L 4 -4 L 6 -2 L 6 -3 L 5 -4 L 2 -5 L -1 -5 L -4 -4 L -5 -3 L -6 -1 L -6 1 L -5 3 L -3 5 L 1 8 M -1 -5 L -3 -4 L -4 -3 L -5 -1 L -5 1 L -4 3 L 1 8 L 2 10 L 2 12 L 1 13 L -1 13","-11 11 M -6 -5 L -7 -4 L -8 -2 L -8 0 L -7 3 L -3 7 L -2 9 M -8 0 L -7 2 L -3 6 L -2 9 L -2 11 L -3 14 L -5 16 L -6 16 L -7 15 L -8 13 L -8 10 L -7 6 L -5 2 L -3 -1 L 0 -4 L 2 -5 L 4 -5 L 7 -4 L 8 -2 L 8 2 L 7 6 L 5 8 L 3 9 L 2 9 L 1 8 L 1 6 L 2 5 L 3 6 L 2 7 M 4 -5 L 6 -4 L 7 -2 L 7 2 L 6 6 L 5 8","-13 13 M 7 -11 L 6 -10 L 7 -9 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -4 -7 L -5 -4 L -6 0 L -8 9 L -9 13 L -10 15 M 2 -12 L 0 -11 L -2 -9 L -3 -7 L -4 -4 L -6 5 L -7 9 L -8 12 L -9 14 L -10 15 L -12 16 L -14 16 L -15 15 L -15 14 L -14 13 L -13 14 L -14 15 M 13 -11 L 12 -10 L 13 -9 L 14 -10 L 14 -11 L 13 -12 L 11 -12 L 9 -11 L 8 -10 L 7 -8 L 6 -5 L 3 9 L 2 13 L 1 15 M 11 -12 L 9 -10 L 8 -8 L 7 -4 L 5 5 L 4 9 L 3 12 L 2 14 L 1 15 L -1 16 L -3 16 L -4 15 L -4 14 L -3 13 L -2 14 L -3 15 M -9 -5 L 12 -5","-12 12 M 9 -11 L 8 -10 L 9 -9 L 10 -10 L 9 -11 L 6 -12 L 3 -12 L 0 -11 L -2 -9 L -3 -7 L -4 -4 L -5 0 L -7 9 L -8 13 L -9 15 M 3 -12 L 1 -11 L -1 -9 L -2 -7 L -3 -4 L -5 5 L -6 9 L -7 12 L -8 14 L -9 15 L -11 16 L -13 16 L -14 15 L -14 14 L -13 13 L -12 14 L -13 15 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9 L 8 9 L 10 7 L 11 5 M 8 -5 L 6 2 L 5 6 L 5 8 L 6 9 M -8 -5 L 8 -5","-12 12 M 7 -11 L 6 -10 L 7 -9 L 8 -10 L 8 -11 L 6 -12 M 10 -12 L 3 -12 L 0 -11 L -2 -9 L -3 -7 L -4 -4 L -5 0 L -7 9 L -8 13 L -9 15 M 3 -12 L 1 -11 L -1 -9 L -2 -7 L -3 -4 L -5 5 L -6 9 L -7 12 L -8 14 L -9 15 L -11 16 L -13 16 L -14 15 L -14 14 L -13 13 L -12 14 L -13 15 M 9 -12 L 5 2 L 4 6 L 4 8 L 5 9 L 8 9 L 10 7 L 11 5 M 10 -12 L 6 2 L 5 6 L 5 8 L 6 9 M -8 -5 L 7 -5","-18 17 M 2 -11 L 1 -10 L 2 -9 L 3 -10 L 2 -11 L 0 -12 L -3 -12 L -6 -11 L -8 -9 L -9 -7 L -10 -4 L -11 0 L -13 9 L -14 13 L -15 15 M -3 -12 L -5 -11 L -7 -9 L -8 -7 L -9 -4 L -11 5 L -12 9 L -13 12 L -14 14 L -15 15 L -17 16 L -19 16 L -20 15 L -20 14 L -19 13 L -18 14 L -19 15 M 14 -11 L 13 -10 L 14 -9 L 15 -10 L 14 -11 L 11 -12 L 8 -12 L 5 -11 L 3 -9 L 2 -7 L 1 -4 L 0 0 L -2 9 L -3 13 L -4 15 M 8 -12 L 6 -11 L 4 -9 L 3 -7 L 2 -4 L 0 5 L -1 9 L -2 12 L -3 14 L -4 15 L -6 16 L -8 16 L -9 15 L -9 14 L -8 13 L -7 14 L -8 15 M 12 -5 L 10 2 L 9 6 L 9 8 L 10 9 L 13 9 L 15 7 L 16 5 M 13 -5 L 11 2 L 10 6 L 10 8 L 11 9 M -14 -5 L 13 -5","-18 17 M 2 -11 L 1 -10 L 2 -9 L 3 -10 L 2 -11 L 0 -12 L -3 -12 L -6 -11 L -8 -9 L -9 -7 L -10 -4 L -11 0 L -13 9 L -14 13 L -15 15 M -3 -12 L -5 -11 L -7 -9 L -8 -7 L -9 -4 L -11 5 L -12 9 L -13 12 L -14 14 L -15 15 L -17 16 L -19 16 L -20 15 L -20 14 L -19 13 L -18 14 L -19 15 M 12 -11 L 11 -10 L 12 -9 L 13 -10 L 13 -11 L 11 -12 M 15 -12 L 8 -12 L 5 -11 L 3 -9 L 2 -7 L 1 -4 L 0 0 L -2 9 L -3 13 L -4 15 M 8 -12 L 6 -11 L 4 -9 L 3 -7 L 2 -4 L 0 5 L -1 9 L -2 12 L -3 14 L -4 15 L -6 16 L -8 16 L -9 15 L -9 14 L -8 13 L -7 14 L -8 15 M 14 -12 L 10 2 L 9 6 L 9 8 L 10 9 L 13 9 L 15 7 L 16 5 M 15 -12 L 11 2 L 10 6 L 10 8 L 11 9 M -14 -5 L 12 -5","-6 7 M -5 -1 L -4 -3 L -2 -5 L 1 -5 L 2 -4 L 2 -1 L 0 5 L 0 8 L 1 9 M 0 -5 L 1 -4 L 1 -1 L -1 5 L -1 8 L 0 9 L 3 9 L 5 7 L 6 5","-6 6 M 0 -6 L -4 5 L 6 -2 L -6 -2 L 4 5 L 0 -6 M 0 0 L 0 -6 M 0 0 L -6 -2 M 0 0 L -4 5 M 0 0 L 4 5 M 0 0 L 6 -2","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3"},
        new string[] {"-8 8","-5 5 M 0 -12 L -1 -10 L 0 2 L 1 -10 L 0 -12 M 0 -10 L 0 -4 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-8 8 M -4 -12 L -5 -5 M -3 -12 L -5 -5 M 4 -12 L 3 -5 M 5 -12 L 3 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 6 -9 L 5 -8 L 6 -7 L 7 -8 L 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 7 2 M -7 -7 L -5 -5 L -3 -4 L 3 -2 L 5 -1 L 6 0 L 7 2 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6 L -7 5 L -6 4 L -5 5 L -6 6","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 13 M 9 -4 L 8 -3 L 9 -2 L 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -5 9 L -8 8 L -9 6 L -9 3 L -8 1 L -2 -3 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 8 9 L 9 9 L 10 8 L 10 7 M -5 9 L -7 8 L -8 6 L -8 3 L -7 1 L -5 -1 M -5 -7 L -4 -5 L 4 6 L 6 8 L 8 9","-4 4 M 0 -12 L -1 -5 M 1 -12 L -1 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 L 4 16 M 3 -16 L 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 L -3 16 M -4 -16 L -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -4 -10 L -5 -8 L -6 -3 L -6 0 L -5 5 L -4 7 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 5 L 6 0 L 6 -3 L 5 -8 L 4 -10 L 3 -11 L 1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9 M 0 -11 L 0 9 M -4 9 L 5 9","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 3 -2 L -2 0 L -4 1 L -6 3 L -7 6 L -7 9 M 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 2 -2 L -2 0 M -7 7 L -6 6 L -4 6 L 1 8 L 4 8 L 6 7 L 7 6 M -4 6 L 1 9 L 5 9 L 6 8 L 7 6 L 7 4","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 L -1 -3 M 2 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -4 L 2 -3 M 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 5 -1 L 6 2 L 6 5 L 5 7 L 4 8 L 2 9","-10 10 M 2 -10 L 2 9 M 3 -12 L 3 9 M 3 -12 L -8 3 L 8 3 M -1 9 L 6 9","-10 10 M -5 -12 L -7 -2 M -7 -2 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -5 -12 L 5 -12 M -5 -11 L 0 -11 L 5 -12","-10 10 M 5 -9 L 4 -8 L 5 -7 L 6 -8 L 6 -9 L 5 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2 M 0 -12 L -2 -11 L -4 -9 L -5 -7 L -6 -3 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 2 L 5 -1 L 3 -3 L 1 -4","-10 10 M -7 -12 L -7 -6 M -7 -8 L -6 -10 L -4 -12 L -2 -12 L 3 -9 L 5 -9 L 6 -10 L 7 -12 M -6 -10 L -4 -11 L -2 -11 L 3 -9 M 7 -12 L 7 -9 L 6 -6 L 2 -1 L 1 1 L 0 4 L 0 9 M 6 -6 L 1 -1 L 0 1 L -1 4 L -1 9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 6 -6 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -2 -12 L -4 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -3 M 2 -3 L 4 -4 L 5 -6 L 5 -9 L 4 -11 L 2 -12 M -2 -3 L -5 -2 L -6 -1 L -7 1 L -7 5 L -6 7 L -5 8 L -2 9 L 2 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 2 -3 M -2 -3 L -4 -2 L -5 -1 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 M 2 9 L 4 8 L 5 7 L 6 5 L 6 1 L 5 -1 L 4 -2 L 2 -3","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 0 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 5 L -5 6 M -1 1 L -3 0 L -5 -2 L -6 -5 L -6 -6 L -5 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 5 -9 L 6 -6 L 6 0 L 5 4 L 4 6 L 2 8 L 0 9","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -5 -8 L -4 -7 L -5 -6 L -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 1 -12 L 3 -11 L 4 -10 L 5 -8 L 5 -6 L 4 -4 L 2 -2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 0 -12 L -7 9 M 0 -12 L 7 9 M 0 -9 L 6 9 M -5 3 L 4 3 M -9 9 L -3 9 M 3 9 L 9 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 3 -2 M -5 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 3 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9","-10 10 M -7 -12 L 6 9 M -6 -12 L 7 9 M 7 -12 L -7 9 M -9 -12 L -3 -12 M 3 -12 L 9 -12 M -9 9 L -3 9 M 3 9 L 9 9","-10 10 M 0 -12 L -8 9 M 0 -12 L 8 9 M 0 -9 L 7 9 M -7 8 L 7 8 M -8 9 L 8 9","-11 10 M -6 -12 L -6 9 M -5 -12 L -5 9 M 1 -6 L 1 2 M -9 -12 L 7 -12 L 7 -6 L 6 -12 M -5 -2 L 1 -2 M -9 9 L 7 9 L 7 3 L 6 9","-10 11 M 0 -12 L 0 9 M 1 -12 L 1 9 M -2 -7 L -5 -6 L -6 -5 L -7 -3 L -7 0 L -6 2 L -5 3 L -2 4 L 3 4 L 6 3 L 7 2 L 8 0 L 8 -3 L 7 -5 L 6 -6 L 3 -7 L -2 -7 M -2 -7 L -4 -6 L -5 -5 L -6 -3 L -6 0 L -5 2 L -4 3 L -2 4 M 3 4 L 5 3 L 6 2 L 7 0 L 7 -3 L 6 -5 L 5 -6 L 3 -7 M -3 -12 L 4 -12 M -3 9 L 4 9","-9 9 M -4 -12 L -4 9 M -3 -12 L -3 9 M -7 -12 L 8 -12 L 8 -6 L 7 -12 M -7 9 L 0 9","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M -6 -2 L 6 -2 M -10 9 L -3 9 M 3 9 L 10 9","-5 6 M 0 -12 L 0 9 M 1 -12 L 1 9 M -3 -12 L 4 -12 M -3 9 L 4 9","-2 3 M 0 -1 L 0 0 L 1 0 L 1 -1 L 0 -1","-12 10 M -7 -12 L -7 9 M -6 -12 L -6 9 M 7 -12 L -6 1 M -1 -3 L 7 9 M -2 -3 L 6 9 M -10 -12 L -3 -12 M 3 -12 L 9 -12 M -10 9 L -3 9 M 3 9 L 9 9","-10 10 M 0 -12 L -7 9 M 0 -12 L 7 9 M 0 -9 L 6 9 M -9 9 L -3 9 M 3 9 L 9 9","-12 13 M -7 -12 L -7 9 M -6 -12 L 0 6 M -7 -12 L 0 9 M 7 -12 L 0 9 M 7 -12 L 7 9 M 8 -12 L 8 9 M -10 -12 L -6 -12 M 7 -12 L 11 -12 M -10 9 L -4 9 M 4 9 L 11 9","-11 12 M -6 -12 L -6 9 M -5 -12 L 7 7 M -5 -10 L 7 9 M 7 -12 L 7 9 M -9 -12 L -5 -12 M 4 -12 L 10 -12 M -9 9 L -3 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 0 L -6 4 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 4 L 7 0 L 7 -3 L 6 -7 L 5 -9 L 3 -11 L 1 -12","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L 10 -12 M -10 9 L -3 9 M 3 9 L 10 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 0 L -6 4 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 4 L 7 0 L 7 -3 L 6 -7 L 5 -9 L 3 -11 L 1 -12 M -3 -5 L -3 2 M 3 -5 L 3 2 M -3 -2 L 3 -2 M -3 -1 L 3 -1","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -5 -1 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 3 -1 M -9 9 L -2 9","-10 11 M -7 -12 L 0 -2 L -8 9 M -8 -12 L -1 -2 M -8 -12 L 7 -12 L 8 -6 L 6 -12 M -7 8 L 6 8 M -8 9 L 7 9 L 8 3 L 6 9","-9 10 M 0 -12 L 0 9 M 1 -12 L 1 9 M -6 -12 L -7 -6 L -7 -12 L 8 -12 L 8 -6 L 7 -12 M -3 9 L 4 9","-9 10 M -7 -7 L -7 -9 L -6 -11 L -5 -12 L -3 -12 L -2 -11 L -1 -9 L 0 -5 L 0 9 M -7 -9 L -5 -11 L -3 -11 L -1 -9 M 8 -7 L 8 -9 L 7 -11 L 6 -12 L 4 -12 L 3 -11 L 2 -9 L 1 -5 L 1 9 M 8 -9 L 6 -11 L 4 -11 L 2 -9 M -3 9 L 4 9","-7 7 M -1 -12 L -3 -11 L -4 -9 L -4 -7 L -3 -5 L -1 -4 L 1 -4 L 3 -5 L 4 -7 L 4 -9 L 3 -11 L 1 -12 L -1 -12","-11 11 M -8 6 L -7 9 L -3 9 L -5 5 L -7 1 L -8 -2 L -8 -6 L -7 -9 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 7 -9 L 8 -6 L 8 -2 L 7 1 L 5 5 L 3 9 L 7 9 L 8 6 M -5 5 L -6 2 L -7 -2 L -7 -6 L -6 -9 L -4 -11 L -2 -12 M 2 -12 L 4 -11 L 6 -9 L 7 -6 L 7 -2 L 6 2 L 5 5 M -7 8 L -4 8 M 4 8 L 7 8","-11 11 M -7 -13 L -8 -8 M 8 -13 L 7 -8 M -3 -4 L -4 1 M 4 -4 L 3 1 M -7 5 L -8 10 M 8 5 L 7 10 M -7 -11 L 7 -11 M -7 -10 L 7 -10 M -3 -2 L 3 -2 M -3 -1 L 3 -1 M -7 7 L 7 7 M -7 8 L 7 8","-11 12 M 0 -12 L 0 9 M 1 -12 L 1 9 M -9 -5 L -8 -6 L -6 -5 L -5 -1 L -4 1 L -3 2 L -1 3 M -8 -6 L -7 -5 L -6 -1 L -5 1 L -4 2 L -1 3 L 2 3 L 5 2 L 6 1 L 7 -1 L 8 -5 L 9 -6 M 2 3 L 4 2 L 5 1 L 6 -1 L 7 -5 L 9 -6 L 10 -5 M -3 -12 L 4 -12 M -3 9 L 4 9","-10 10 M 6 -12 L -7 9 M 7 -12 L -6 9 M -6 -12 L -7 -6 L -7 -12 L 7 -12 M -7 9 L 7 9 L 7 3 L 6 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-11 12 M -1 -5 L -4 -4 L -6 -2 L -7 0 L -8 3 L -8 6 L -7 8 L -4 9 L -2 9 L 0 8 L 3 5 L 5 2 L 7 -2 L 8 -5 M -1 -5 L -3 -4 L -5 -2 L -6 0 L -7 3 L -7 6 L -6 8 L -4 9 M -1 -5 L 1 -5 L 3 -4 L 4 -2 L 6 6 L 7 8 L 8 9 M 1 -5 L 2 -4 L 3 -2 L 5 6 L 6 8 L 8 9 L 9 9","-11 10 M 2 -12 L -1 -11 L -3 -9 L -5 -5 L -6 -2 L -7 2 L -8 8 L -9 16 M 2 -12 L 0 -11 L -2 -9 L -4 -5 L -5 -2 L -6 2 L -7 8 L -8 16 M 2 -12 L 4 -12 L 6 -11 L 7 -10 L 7 -7 L 6 -5 L 5 -4 L 2 -3 L -2 -3 M 4 -12 L 6 -10 L 6 -7 L 5 -5 L 4 -4 L 2 -3 M -2 -3 L 2 -2 L 4 0 L 5 2 L 5 5 L 4 7 L 3 8 L 0 9 L -2 9 L -4 8 L -5 7 L -6 4 M -2 -3 L 1 -2 L 3 0 L 4 2 L 4 5 L 3 7 L 2 8 L 0 9","-9 9 M -7 -5 L -5 -5 L -3 -4 L -2 -2 L 3 13 L 4 15 L 5 16 M -5 -5 L -4 -4 L -3 -2 L 2 13 L 3 15 L 5 16 L 7 16 M 8 -5 L 7 -3 L 5 0 L -5 11 L -7 14 L -8 16","-9 10 M 4 -4 L 2 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 L 6 2 L 6 -1 L 5 -3 L 1 -8 L 0 -10 L 0 -12 L 1 -13 L 3 -13 L 5 -12 L 7 -10 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 5 L 5 2 L 5 -2 L 4 -4 L 2 -7 L 1 -9 L 1 -11 L 2 -12 L 4 -12 L 7 -10","-9 9 M 6 -2 L 4 -4 L 2 -5 L -2 -5 L -4 -4 L -4 -2 L -2 0 L 1 1 M -2 -5 L -3 -4 L -3 -2 L -1 0 L 1 1 M 1 1 L -4 2 L -6 4 L -6 6 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 M 1 1 L -3 2 L -5 4 L -5 6 L -4 8 L -2 9","-11 11 M -3 -4 L -5 -3 L -7 -1 L -8 2 L -8 5 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 5 6 L 7 3 L 8 0 L 8 -3 L 6 -5 L 4 -5 L 2 -3 L 0 1 L -2 6 L -5 16 M -8 5 L -6 7 L -4 8 L -1 8 L 2 7 L 5 5 L 7 3 M 8 -3 L 6 -4 L 4 -4 L 2 -2 L 0 1 L -2 7 L -4 16","-10 10 M -9 -2 L -7 -4 L -5 -5 L -3 -5 L -1 -4 L 0 -3 L 1 0 L 1 4 L 0 8 L -3 16 M -8 -3 L -6 -4 L -2 -4 L 0 -3 M 8 -5 L 7 -2 L 6 0 L 1 7 L -2 12 L -4 16 M 7 -5 L 6 -2 L 5 0 L 1 7","-11 11 M -10 -1 L -9 -3 L -7 -5 L -4 -5 L -3 -4 L -3 -2 L -4 2 L -6 9 M -5 -5 L -4 -4 L -4 -2 L -5 2 L -7 9 M -4 2 L -2 -2 L 0 -4 L 2 -5 L 4 -5 L 6 -4 L 7 -3 L 7 0 L 6 5 L 3 16 M 4 -5 L 6 -3 L 6 0 L 5 5 L 2 16","-6 6 M 0 -5 L -2 2 L -3 6 L -3 8 L -2 9 L 1 9 L 3 7 L 4 5 M 1 -5 L -1 2 L -2 6 L -2 8 L -1 9","-11 11 M -7 -7 L 7 7 M 7 -7 L -7 7","-10 10 M -4 -5 L -8 9 M -3 -5 L -7 9 M 6 -5 L 7 -4 L 8 -4 L 7 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 L -5 1 M -3 1 L -1 2 L 1 8 L 2 9 M -3 1 L -2 2 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5","-10 10 M -7 -12 L -5 -12 L -3 -11 L -2 -10 L -1 -8 L 5 6 L 6 8 L 7 9 M -5 -12 L -3 -10 L -2 -8 L 4 6 L 5 8 L 7 9 L 8 9 M 0 -5 L -8 9 M 0 -5 L -7 9","-12 11 M -5 -5 L -11 16 M -4 -5 L -10 16 M -5 -2 L -6 4 L -6 7 L -4 9 L -2 9 L 0 8 L 2 6 L 4 3 M 6 -5 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 7 -5 L 4 6 L 4 8 L 5 9","-10 10 M -4 -5 L -6 9 M -3 -5 L -4 1 L -5 6 L -6 9 M 7 -5 L 6 -1 L 4 3 M 8 -5 L 7 -2 L 6 0 L 4 3 L 2 5 L -1 7 L -3 8 L -6 9 M -7 -5 L -3 -5","-9 9 M 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 L 6 2 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L 0 -5 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 5 L 5 2 L 5 -2 L 4 -4","-11 11 M -2 -4 L -6 9 M -2 -4 L -5 9 M 4 -4 L 4 9 M 4 -4 L 5 9 M -9 -2 L -7 -4 L -4 -5 L 9 -5 M -9 -2 L -7 -3 L -4 -4 L 9 -4","-12 11 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -2 L -5 3 L -5 6 L -4 8 L -3 9 M -6 -5 L -5 -4 L -5 -2 L -6 3 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -5 L 7 -9 L 6 -11 L 4 -12 L 2 -12 L 0 -10 L 0 -8 L 1 -5 L 3 -2 L 5 0 L 8 2 M 1 8 L 3 5 L 4 3 L 5 0 L 6 -5 L 6 -9 L 5 -11 L 4 -12","-10 9 M -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 L 6 2 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -10 16 M 0 9 L 2 8 L 4 5 L 5 2 L 5 -2 L 4 -4 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -9 16","-10 11 M 9 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 5 L 5 2 L 5 -1 L 4 -3 L 3 -4 L 1 -5 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8 M -1 9 L 1 8 L 3 5 L 4 2 L 4 -2 L 3 -4 M 3 -4 L 9 -4","-10 10 M 1 -4 L -2 9 M 1 -4 L -1 9 M -8 -2 L -6 -4 L -3 -5 L 8 -5 M -8 -2 L -6 -3 L -3 -4 L 8 -4","-10 10 M -9 -1 L -8 -3 L -6 -5 L -3 -5 L -2 -4 L -2 -2 L -4 4 L -4 7 L -2 9 M -4 -5 L -3 -4 L -3 -2 L -5 4 L -5 7 L -4 8 L -2 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 7 -3 L 6 -5 L 5 -4 L 6 -3 L 7 0 M 6 3 L 7 -3","-13 13 M 0 -9 L -1 -8 L 0 -7 L 1 -8 L 0 -9 M -9 0 L 9 0 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-12 11 M -8 -1 L -6 -3 L -3 -4 L -4 -5 L -6 -4 L -8 -1 L -9 2 L -9 5 L -8 8 L -7 9 L -5 9 L -3 8 L -1 5 L 0 2 M -9 5 L -8 7 L -7 8 L -5 8 L -3 7 L -1 5 M -1 2 L -1 5 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 L 8 2 L 8 -1 L 7 -4 L 6 -5 L 5 -4 L 7 -3 L 8 -1 M -1 5 L 0 7 L 1 8 L 3 8 L 5 7 L 7 5","-9 8 M 2 -12 L 0 -11 L -1 -10 L -1 -9 L 0 -8 L 3 -7 L 6 -7 M 3 -7 L -1 -6 L -3 -5 L -4 -3 L -4 -1 L -2 1 L 1 2 L 4 2 M 3 -7 L 0 -6 L -2 -5 L -3 -3 L -3 -1 L -1 1 L 1 2 M 1 2 L -3 3 L -5 4 L -6 6 L -6 8 L -4 10 L 1 12 L 2 13 L 2 15 L 0 16 L -2 16 M 1 2 L -2 3 L -4 4 L -5 6 L -5 8 L -3 10 L 1 12","-12 11 M 3 -12 L -3 16 M 4 -12 L -4 16 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -2 L -5 3 L -5 6 L -3 8 L 0 8 L 2 7 L 5 4 L 7 1 M -6 -5 L -5 -4 L -5 -2 L -6 3 L -6 6 L -5 8 L -3 9 L 0 9 L 2 8 L 4 6 L 6 3 L 7 1 L 9 -5","-9 9 M 2 -12 L 0 -11 L -1 -10 L -1 -9 L 0 -8 L 3 -7 L 8 -7 L 8 -8 L 5 -7 L 1 -5 L -2 -3 L -5 0 L -6 3 L -6 5 L -5 7 L -2 9 L 1 11 L 2 13 L 2 15 L 1 16 L -1 16 L -2 15 M 3 -6 L -1 -3 L -4 0 L -5 3 L -5 5 L -4 7 L -2 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 6 M 3 -12 L 2 -11 L 0 1 M 3 -11 L 0 1 M 3 -12 L 4 -11 L 0 1 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-9 9 M -2 -12 L -4 -5 M -1 -12 L -4 -5 M 7 -12 L 5 -5 M 8 -12 L 5 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 11 M 2 -16 L -6 13 M 7 -16 L -1 13 M 8 -8 L 7 -7 L 8 -6 L 9 -7 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -7 L -4 -5 L -3 -4 L 4 0 L 6 2 M -5 -7 L -3 -5 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 13 M 9 -4 L 8 -3 L 9 -2 L 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -5 9 L -8 8 L -9 6 L -9 3 L -8 1 L -2 -3 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 8 9 L 9 9 L 10 8 L 10 7 M -5 9 L -7 8 L -8 6 L -8 3 L -7 1 L -5 -1 M -5 -7 L -4 -5 L 4 6 L 6 8 L 8 9","-4 5 M 3 -12 L 1 -5 M 4 -12 L 1 -5","-7 8 M 8 -16 L 4 -13 L 1 -10 L -1 -7 L -3 -3 L -4 2 L -4 6 L -3 11 L -2 14 L -1 16 M 4 -13 L 1 -9 L -1 -5 L -2 -2 L -3 3 L -3 8 L -2 13 L -1 16","-8 7 M 1 -16 L 2 -14 L 3 -11 L 4 -6 L 4 -2 L 3 3 L 1 7 L -1 10 L -4 13 L -8 16 M 1 -16 L 2 -13 L 3 -8 L 3 -3 L 2 2 L 1 5 L -1 9 L -4 13","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-5 6 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-13 13 M -9 0 L 9 0","-5 6 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-11 11 M 9 -16 L -9 16","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -3 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12","-10 11 M 2 -8 L -3 9 M 4 -12 L -2 9 M 4 -12 L 1 -9 L -2 -7 L -4 -6 M 3 -9 L -1 -7 L -4 -6","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 5 -3 L 2 -1 L -2 1 L -5 3 L -7 5 L -9 9 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 L -2 1 M -8 7 L -7 6 L -5 6 L 0 8 L 3 8 L 5 7 L 6 5 M -5 6 L 0 9 L 3 9 L 5 8 L 6 5","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 4 -3 L 1 -2 M 4 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 M -1 -2 L 1 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -2 L 3 -1 L 4 0 L 5 2 L 5 5 L 4 7 L 3 8 L 1 9","-10 11 M 6 -11 L 0 9 M 7 -12 L 1 9 M 7 -12 L -8 3 L 8 3","-10 11 M -1 -12 L -6 -2 M -1 -12 L 9 -12 M -1 -11 L 4 -11 L 9 -12 M -6 -2 L -5 -3 L -2 -4 L 1 -4 L 4 -3 L 5 -2 L 6 0 L 6 3 L 5 6 L 3 8 L 0 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 4 L -7 3 L -6 4 L -7 5 M 1 -4 L 3 -3 L 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 L 0 9","-10 11 M 7 -9 L 6 -8 L 7 -7 L 8 -8 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 5 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 L -1 -3 L -3 -2 L -5 0 L -6 2 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 6 L -5 8 M 0 9 L 2 8 L 4 6 L 5 4 L 5 0 L 4 -2","-10 11 M -4 -12 L -6 -6 M 9 -12 L 8 -9 L 6 -6 L 1 0 L -1 3 L -2 5 L -3 9 M 6 -6 L 0 0 L -2 3 L -3 5 L -4 9 M -5 -9 L -2 -12 L 0 -12 L 5 -9 M -4 -10 L -2 -11 L 0 -11 L 5 -9 L 7 -9 L 8 -10 L 9 -12","-10 11 M 1 -12 L -2 -11 L -3 -10 L -4 -8 L -4 -5 L -3 -3 L -1 -2 L 2 -2 L 6 -3 L 7 -4 L 8 -6 L 8 -9 L 7 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -2 -10 L -3 -8 L -3 -5 L -2 -3 L -1 -2 M 2 -2 L 5 -3 L 6 -4 L 7 -6 L 7 -9 L 6 -11 L 4 -12 M -1 -2 L -5 -1 L -7 1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 4 8 L 5 7 L 6 5 L 6 2 L 5 0 L 4 -1 L 2 -2 M -1 -2 L -4 -1 L -6 1 L -7 3 L -7 6 L -6 8 L -4 9 M 0 9 L 3 8 L 4 7 L 5 5 L 5 1 L 4 -1","-10 11 M 7 -5 L 6 -3 L 4 -1 L 2 0 L -1 0 L -3 -1 L -4 -2 L -5 -4 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 4 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -4 L 7 0 L 6 3 L 4 6 L 2 8 L -1 9 L -4 9 L -6 8 L -7 6 L -7 5 L -6 4 L -5 5 L -6 6 M -3 -1 L -4 -3 L -4 -7 L -3 -9 L -1 -11 L 1 -12 M 6 -11 L 7 -9 L 7 -4 L 6 0 L 5 3 L 3 6 L 1 8 L -1 9","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 7 L -3 8 L -2 9 L -1 8","-5 6 M 1 -5 L 0 -4 L 1 -3 L 2 -4 L 1 -5 M -2 9 L -3 8 L -2 7 L -1 8 L -1 9 L -2 11 L -4 13","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-10 11 M -3 -8 L -2 -7 L -3 -6 L -4 -7 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 1 -2 L -1 -1 L -1 1 L 0 2 L 2 2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 M -2 7 L -3 8 L -2 9 L -1 8 L -2 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 3 -12 L -10 9 M 3 -12 L 4 9 M 2 -10 L 3 9 M -6 3 L 3 3 M -12 9 L -6 9 M 0 9 L 6 9","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 L 4 -2 M -5 -2 L 4 -2 L 6 -1 L 7 1 L 7 3 L 6 6 L 4 8 L 0 9 L -12 9 M 4 -2 L 5 -1 L 6 1 L 6 3 L 5 6 L 3 8 L 0 9","-10 11 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 L 6 4 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -7 L 8 -3 L 7 1 L 5 5 L 3 7 L 1 8 L -3 9 L -12 9 M 3 -12 L 5 -11 L 6 -10 L 7 -7 L 7 -3 L 6 1 L 4 5 L 2 7 L 0 8 L -3 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M 2 -6 L 0 2 M -6 -12 L 9 -12 L 8 -6 L 8 -12 M -5 -2 L 1 -2 M -12 9 L 3 9 L 5 4 L 2 9","-12 10 M -3 -12 L -9 9 M -2 -12 L -8 9 M 2 -6 L 0 2 M -6 -12 L 9 -12 L 8 -6 L 8 -12 M -5 -2 L 1 -2 M -12 9 L -5 9","-10 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 0 9 L 3 8 L 5 6 L 7 2 M 2 -12 L 0 -11 L -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 4 L -5 7 L -4 8 L -2 9 M 0 9 L 2 8 L 4 6 L 6 2 M 3 2 L 10 2","-13 13 M -4 -12 L -10 9 M -3 -12 L -9 9 M 9 -12 L 3 9 M 10 -12 L 4 9 M -7 -12 L 0 -12 M 6 -12 L 13 -12 M -6 -2 L 6 -2 M -13 9 L -6 9 M 0 9 L 7 9","-6 7 M 3 -12 L -3 9 M 4 -12 L -2 9 M 0 -12 L 7 -12 M -6 9 L 1 9","-9 9 M 6 -12 L 1 5 L 0 7 L -1 8 L -3 9 L -5 9 L -7 8 L -8 6 L -8 4 L -7 3 L -6 4 L -7 5 M 5 -12 L 0 5 L -1 7 L -3 9 M 2 -12 L 9 -12","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M 11 -12 L -6 1 M 1 -3 L 5 9 M 0 -3 L 4 9 M -6 -12 L 1 -12 M 7 -12 L 13 -12 M -12 9 L -5 9 M 1 9 L 7 9","-10 10 M -1 -12 L -7 9 M 0 -12 L -6 9 M -4 -12 L 3 -12 M -10 9 L 5 9 L 7 3 L 4 9","-13 14 M -4 -12 L -10 9 M -4 -12 L -3 9 M -3 -12 L -2 7 M 10 -12 L -3 9 M 10 -12 L 4 9 M 11 -12 L 5 9 M -7 -12 L -3 -12 M 10 -12 L 14 -12 M -13 9 L -7 9 M 1 9 L 8 9","-12 13 M -3 -12 L -9 9 M -3 -12 L 4 6 M -3 -9 L 4 9 M 10 -12 L 4 9 M -6 -12 L -3 -12 M 7 -12 L 13 -12 M -12 9 L -6 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -4 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 6 -12 L 9 -11 L 10 -9 L 10 -7 L 9 -4 L 7 -2 L 3 -1 L -5 -1 M 6 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 6 -2 L 3 -1 M -12 9 L -5 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M 1 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -4 9 M -1 9 L 1 8 L 3 6 L 5 3 L 6 0 L 7 -4 L 7 -7 L 6 -10 L 4 -12 M -6 7 L -6 6 L -5 4 L -3 3 L -2 3 L 0 4 L 1 6 L 1 13 L 2 14 L 4 14 L 5 12 L 5 11 M 1 6 L 2 12 L 3 13 L 4 13 L 5 12","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 L -5 -2 M 5 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 L 4 -2 M 0 -2 L 2 -1 L 3 0 L 4 8 L 5 9 L 7 9 L 8 7 L 8 6 M 3 0 L 5 7 L 6 8 L 7 8 L 8 7 M -12 9 L -5 9","-11 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -7 L -4 -5 L -3 -4 L 4 0 L 6 2 M -5 -7 L -3 -5 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -9 9 L -8 7 L -7 7","-10 11 M 3 -12 L -3 9 M 4 -12 L -2 9 M -3 -12 L -6 -6 L -4 -12 L 11 -12 L 10 -6 L 10 -12 M -6 9 L 1 9","-12 13 M -4 -12 L -7 -1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 3 8 L 5 6 L 6 3 L 10 -12 M -3 -12 L -6 -1 L -7 3 L -7 6 L -6 8 L -4 9 M -7 -12 L 0 -12 M 7 -12 L 13 -12","-10 10 M -4 -12 L -3 9 M -3 -12 L -2 7 M 10 -12 L -3 9 M -6 -12 L 0 -12 M 6 -12 L 12 -12","-13 13 M -5 -12 L -7 9 M -4 -12 L -6 7 M 3 -12 L -7 9 M 3 -12 L 1 9 M 4 -12 L 2 7 M 11 -12 L 1 9 M -8 -12 L -1 -12 M 8 -12 L 14 -12","-11 11 M -4 -12 L 3 9 M -3 -12 L 4 9 M 10 -12 L -10 9 M -6 -12 L 0 -12 M 6 -12 L 12 -12 M -12 9 L -6 9 M 0 9 L 6 9","-10 11 M -4 -12 L 0 -2 L -3 9 M -3 -12 L 1 -2 L -2 9 M 11 -12 L 1 -2 M -6 -12 L 0 -12 M 7 -12 L 13 -12 M -6 9 L 1 9","-11 11 M 9 -12 L -10 9 M 10 -12 L -9 9 M -3 -12 L -6 -6 L -4 -12 L 10 -12 M -10 9 L 4 9 L 6 3 L 3 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-10 11 M 6 -5 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8","-10 9 M -2 -12 L -6 1 L -6 4 L -5 7 L -4 8 M -1 -12 L -5 1 M -5 1 L -4 -2 L -2 -4 L 0 -5 L 2 -5 L 4 -4 L 5 -3 L 6 -1 L 6 2 L 5 5 L 3 8 L 0 9 L -2 9 L -4 8 L -5 5 L -5 1 M 4 -4 L 5 -2 L 5 2 L 4 5 L 2 8 L 0 9 M -5 -12 L -1 -12","-9 9 M 5 -2 L 5 -1 L 6 -1 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8","-10 11 M 8 -12 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 9 -12 L 5 2 L 4 6 L 4 8 L 5 9 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8 M 5 -12 L 9 -12","-9 9 M -5 4 L -1 3 L 2 2 L 5 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8","-7 8 M 8 -11 L 7 -10 L 8 -9 L 9 -10 L 9 -11 L 8 -12 L 6 -12 L 4 -11 L 3 -10 L 2 -8 L 1 -5 L -2 9 L -3 13 L -4 15 M 6 -12 L 4 -10 L 3 -8 L 2 -4 L 0 5 L -1 9 L -2 12 L -3 14 L -4 15 L -6 16 L -8 16 L -9 15 L -9 14 L -8 13 L -7 14 L -8 15 M -3 -5 L 7 -5","-10 10 M 7 -5 L 3 9 L 2 12 L 0 15 L -3 16 L -6 16 L -8 15 L -9 14 L -9 13 L -8 12 L -7 13 L -8 14 M 6 -5 L 2 9 L 1 12 L -1 15 L -3 16 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8","-10 11 M -2 -12 L -8 9 M -1 -12 L -7 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 6 -1 L 4 5 L 4 8 L 5 9 M 3 -5 L 5 -3 L 5 -1 L 3 5 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M -5 -12 L -1 -12","-6 7 M 3 -12 L 2 -11 L 3 -10 L 4 -11 L 3 -12 M -5 -1 L -4 -3 L -2 -5 L 1 -5 L 2 -4 L 2 -1 L 0 5 L 0 8 L 1 9 M 0 -5 L 1 -4 L 1 -1 L -1 5 L -1 8 L 0 9 L 3 9 L 5 7 L 6 5","-6 7 M 4 -12 L 3 -11 L 4 -10 L 5 -11 L 4 -12 M -4 -1 L -3 -3 L -1 -5 L 2 -5 L 3 -4 L 3 -1 L 0 9 L -1 12 L -2 14 L -3 15 L -5 16 L -7 16 L -8 15 L -8 14 L -7 13 L -6 14 L -7 15 M 1 -5 L 2 -4 L 2 -1 L -1 9 L -2 12 L -3 14 L -5 16","-10 10 M -2 -12 L -8 9 M -1 -12 L -7 9 M 6 -4 L 5 -3 L 6 -2 L 7 -3 L 7 -4 L 6 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 L -5 1 M -3 1 L -1 2 L 1 8 L 2 9 M -3 1 L -2 2 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 M -5 -12 L -1 -12","-5 7 M 3 -12 L -1 2 L -2 6 L -2 8 L -1 9 L 2 9 L 4 7 L 5 5 M 4 -12 L 0 2 L -1 6 L -1 8 L 0 9 M 0 -12 L 4 -12","-17 16 M -16 -1 L -15 -3 L -13 -5 L -10 -5 L -9 -4 L -9 -2 L -10 2 L -12 9 M -11 -5 L -10 -4 L -10 -2 L -11 2 L -13 9 M -10 2 L -8 -2 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -3 L 1 -1 L -2 9 M -2 -5 L 0 -3 L 0 -1 L -3 9 M 0 2 L 2 -2 L 4 -4 L 6 -5 L 8 -5 L 10 -4 L 11 -3 L 11 -1 L 9 5 L 9 8 L 10 9 M 8 -5 L 10 -3 L 10 -1 L 8 5 L 8 8 L 9 9 L 12 9 L 14 7 L 15 5","-12 11 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -2 L -5 2 L -7 9 M -6 -5 L -5 -4 L -5 -2 L -6 2 L -8 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 6 -1 L 4 5 L 4 8 L 5 9 M 3 -5 L 5 -3 L 5 -1 L 3 5 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5","-9 9 M 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 5 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 L 6 2 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L 0 -5 M 0 -5 L -2 -4 L -4 -1 L -5 2 L -5 6 L -4 8 M 0 9 L 2 8 L 4 5 L 5 2 L 5 -2 L 4 -4","-11 10 M -10 -1 L -9 -3 L -7 -5 L -4 -5 L -3 -4 L -3 -2 L -4 2 L -8 16 M -5 -5 L -4 -4 L -4 -2 L -5 2 L -9 16 M -4 2 L -3 -1 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -3 L 7 -1 L 7 2 L 6 5 L 4 8 L 1 9 L -1 9 L -3 8 L -4 5 L -4 2 M 5 -4 L 6 -2 L 6 2 L 5 5 L 3 8 L 1 9 M -12 16 L -5 16","-10 10 M 6 -5 L 0 16 M 7 -5 L 1 16 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 5 L 4 2 M -1 -5 L -3 -4 L -5 -1 L -6 2 L -6 6 L -5 8 M -3 16 L 4 16","-9 8 M -8 -1 L -7 -3 L -5 -5 L -2 -5 L -1 -4 L -1 -2 L -2 2 L -4 9 M -3 -5 L -2 -4 L -2 -2 L -3 2 L -5 9 M -2 2 L 0 -2 L 2 -4 L 4 -5 L 6 -5 L 7 -4 L 7 -3 L 6 -2 L 5 -3 L 6 -4","-8 9 M 6 -3 L 6 -2 L 7 -2 L 7 -3 L 6 -4 L 3 -5 L 0 -5 L -3 -4 L -4 -3 L -4 -1 L -3 0 L 4 4 L 5 5 M -4 -2 L -3 -1 L 4 3 L 5 4 L 5 7 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -6 6 L -5 6 L -5 7","-7 7 M 2 -12 L -2 2 L -3 6 L -3 8 L -2 9 L 1 9 L 3 7 L 4 5 M 3 -12 L -1 2 L -2 6 L -2 8 L -1 9 M -4 -5 L 5 -5","-12 11 M -11 -1 L -10 -3 L -8 -5 L -5 -5 L -4 -4 L -4 -1 L -6 5 L -6 7 L -4 9 M -6 -5 L -5 -4 L -5 -1 L -7 5 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 2 M 6 -5 L 4 2 L 3 6 L 3 8 L 4 9 L 7 9 L 9 7 L 10 5 M 7 -5 L 5 2 L 4 6 L 4 8 L 5 9","-10 10 M -9 -1 L -8 -3 L -6 -5 L -3 -5 L -2 -4 L -2 -1 L -4 5 L -4 7 L -2 9 M -4 -5 L -3 -4 L -3 -1 L -5 5 L -5 7 L -4 8 L -2 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 -1 L 7 -5 L 6 -5 L 7 -3","-15 14 M -14 -1 L -13 -3 L -11 -5 L -8 -5 L -7 -4 L -7 -1 L -9 5 L -9 7 L -7 9 M -9 -5 L -8 -4 L -8 -1 L -10 5 L -10 7 L -9 8 L -7 9 L -5 9 L -3 8 L -1 6 L 0 4 M 2 -5 L 0 4 L 0 7 L 1 8 L 3 9 L 5 9 L 7 8 L 9 6 L 10 4 L 11 0 L 11 -5 L 10 -5 L 11 -3 M 3 -5 L 1 4 L 1 7 L 3 9","-10 10 M -7 -1 L -5 -4 L -3 -5 L 0 -5 L 1 -3 L 1 0 M -1 -5 L 0 -3 L 0 0 L -1 4 L -2 6 L -4 8 L -6 9 L -7 9 L -8 8 L -8 7 L -7 6 L -6 7 L -7 8 M -1 4 L -1 7 L 0 9 L 3 9 L 5 8 L 7 5 M 7 -4 L 6 -3 L 7 -2 L 8 -3 L 8 -4 L 7 -5 L 6 -5 L 4 -4 L 2 -2 L 1 0 L 0 4 L 0 7 L 1 9","-11 10 M -10 -1 L -9 -3 L -7 -5 L -4 -5 L -3 -4 L -3 -1 L -5 5 L -5 7 L -3 9 M -5 -5 L -4 -4 L -4 -1 L -6 5 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 3 6 L 5 2 M 8 -5 L 4 9 L 3 12 L 1 15 L -2 16 L -5 16 L -7 15 L -8 14 L -8 13 L -7 12 L -6 13 L -7 14 M 7 -5 L 3 9 L 2 12 L 0 15 L -2 16","-10 10 M 7 -5 L 6 -3 L 4 -1 L -4 5 L -6 7 L -7 9 M -6 -1 L -5 -3 L -3 -5 L 0 -5 L 4 -3 M -5 -3 L -3 -4 L 0 -4 L 4 -3 L 6 -3 M -6 7 L -4 7 L 0 8 L 3 8 L 5 7 M -4 7 L 0 9 L 3 9 L 5 7 L 6 5","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 6 M 4 -12 L 3 -12 L 2 -11 L 0 2 M 4 -11 L 3 -11 L 0 2 M 4 -11 L 4 -10 L 0 2 M 4 -12 L 5 -11 L 5 -10 L 0 2 M -2 6 L -3 7 L -3 8 L -2 9 L -1 9 L 0 8 L 0 7 L -1 6 L -2 6 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7","-9 9 M -2 -12 L -4 -5 M -1 -12 L -4 -5 M 7 -12 L 5 -5 M 8 -12 L 5 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 11 M 2 -16 L -6 13 M 7 -16 L -1 13 M 8 -7 L 8 -8 L 7 -8 L 7 -6 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -2 L 4 1 L 5 3 L 5 6 L 4 8 M -4 -6 L -3 -4 L 4 0 L 5 2 M -3 -11 L -4 -9 L -4 -7 L -3 -5 L 3 -2 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -6 3 L -6 5 L -7 5 L -7 4","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 10 -3 L 10 -4 L 9 -4 L 9 -2 L 11 -2 L 11 -4 L 10 -5 L 9 -5 L 7 -4 L 5 -2 L 0 6 L -2 8 L -4 9 L -7 9 L -10 8 L -11 6 L -11 4 L -10 2 L -9 1 L -7 0 L -2 -2 L 0 -3 L 2 -5 L 3 -7 L 3 -9 L 2 -11 L 0 -12 L -2 -11 L -3 -9 L -3 -6 L -2 0 L -1 3 L 0 5 L 2 8 L 4 9 L 6 9 L 7 7 L 7 6 M -6 9 L -10 8 M -9 8 L -10 6 L -10 4 L -9 2 L -8 1 L -6 0 M -2 -2 L -1 1 L 2 7 L 4 8 M -7 9 L -8 8 L -9 6 L -9 4 L -8 2 L -7 1 L -5 0 L 0 -3 M -3 -6 L -2 -3 L -1 0 L 1 4 L 3 7 L 5 8 L 6 8 L 7 7","-4 5 M 3 -12 L 1 -5 M 4 -12 L 1 -5","-8 8 M 8 -16 L 6 -15 L 3 -13 L 0 -10 L -2 -7 L -4 -3 L -5 1 L -5 6 L -4 10 L -3 13 L -1 16 M 1 -10 L -1 -7 L -3 -3 L -4 2 L -4 10 M 8 -16 L 5 -14 L 2 -11 L 0 -8 L -1 -6 L -2 -3 L -3 1 L -4 10 M -4 2 L -3 11 L -2 14 L -1 16","-8 8 M 1 -16 L 3 -13 L 4 -10 L 5 -6 L 5 -1 L 4 3 L 2 7 L 0 10 L -3 13 L -6 15 L -8 16 M 4 -10 L 4 -2 L 3 3 L 1 7 L -1 10 M 1 -16 L 2 -14 L 3 -11 L 4 -2 M 4 -10 L 3 -1 L 2 3 L 1 6 L 0 8 L -2 11 L -5 14 L -8 16","-8 9 M 2 -12 L 1 -11 L 3 -1 L 2 0 M 2 -12 L 2 0 M 2 -12 L 3 -11 L 1 -1 L 2 0 M -3 -9 L -2 -9 L 6 -3 L 7 -3 M -3 -9 L 7 -3 M -3 -9 L -3 -8 L 7 -4 L 7 -3 M 7 -9 L 6 -9 L -2 -3 L -3 -3 M 7 -9 L -3 -3 M 7 -9 L 7 -8 L -3 -4 L -3 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-5 6 M -1 9 L -2 9 L -3 8 L -3 7 L -2 6 L -1 6 L 0 7 L 0 9 L -1 11 L -2 12 L -4 13 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7 M -1 9 L -1 10 L -2 12","-13 13 M -9 0 L 9 0","-5 6 M -2 6 L -3 7 L -3 8 L -2 9 L -1 9 L 0 8 L 0 7 L -1 6 L -2 6 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 11 M 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 2 -12 M -1 -10 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 2 7 L 4 5 L 5 3 L 6 0 L 7 -4 L 7 -8 L 6 -10 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -3 9 M -1 9 L 1 8 L 3 5 L 4 3 L 5 0 L 6 -4 L 6 -9 L 5 -11 L 4 -12","-10 11 M 2 -8 L -3 9 L -1 9 M 5 -12 L 3 -8 L -2 9 M 5 -12 L -1 9 M 5 -12 L 2 -9 L -1 -7 L -3 -6 M 2 -8 L 0 -7 L -3 -6","-10 11 M -3 -7 L -3 -8 L -2 -8 L -2 -6 L -4 -6 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 5 -3 L -5 3 L -7 5 L -9 9 M 6 -11 L 7 -9 L 7 -7 L 6 -5 L 4 -3 L 1 -1 M 4 -12 L 5 -11 L 6 -9 L 6 -7 L 5 -5 L 3 -3 L -5 3 M -8 7 L -7 6 L -5 6 L 0 7 L 5 7 L 6 6 M -5 6 L 0 8 L 5 8 M -5 6 L 0 9 L 3 9 L 5 8 L 6 6 L 6 5","-10 11 M -3 -7 L -3 -8 L -2 -8 L -2 -6 L -4 -6 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 4 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 L 1 -2 M 6 -11 L 7 -9 L 7 -7 L 6 -5 L 5 -4 M 4 -12 L 5 -11 L 6 -9 L 6 -7 L 5 -5 L 3 -3 L 1 -2 M -1 -2 L 1 -2 L 4 -1 L 5 0 L 6 2 L 6 5 L 5 7 L 3 8 L 0 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -6 3 L -6 5 L -7 5 L -7 4 M 4 0 L 5 2 L 5 5 L 4 7 M 1 -2 L 3 -1 L 4 1 L 4 5 L 3 7 L 2 8 L 0 9","-10 11 M 5 -8 L 0 9 L 2 9 M 8 -12 L 6 -8 L 1 9 M 8 -12 L 2 9 M 8 -12 L -8 3 L 8 3","-10 11 M -1 -12 L -6 -2 M -1 -12 L 9 -12 M -1 -11 L 7 -11 M -2 -10 L 3 -10 L 7 -11 L 9 -12 M -6 -2 L -5 -3 L -2 -4 L 1 -4 L 4 -3 L 5 -2 L 6 0 L 6 3 L 5 6 L 3 8 L -1 9 L -4 9 L -6 8 L -7 7 L -8 5 L -8 3 L -6 3 L -6 5 L -7 5 L -7 4 M 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 M 1 -4 L 3 -3 L 4 -1 L 4 3 L 3 6 L 1 8 L -1 9","-10 11 M 7 -8 L 7 -9 L 6 -9 L 6 -7 L 8 -7 L 8 -9 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -3 9 L 0 9 L 3 8 L 5 6 L 6 4 L 6 1 L 5 -1 L 4 -2 L 2 -3 L -1 -3 L -3 -2 L -4 -1 L -5 1 M -2 -9 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 4 6 L 5 4 L 5 1 L 4 -1 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -3 9 M 0 9 L 2 8 L 3 7 L 4 4 L 4 0 L 3 -2 L 2 -3","-10 11 M -4 -12 L -6 -6 M 9 -12 L 8 -9 L 6 -6 L 2 -1 L 0 2 L -1 5 L -2 9 M 0 1 L -2 5 L -3 9 M 6 -6 L 0 0 L -2 3 L -3 5 L -4 9 L -2 9 M -5 -9 L -2 -12 L 0 -12 L 5 -9 M -3 -11 L 0 -11 L 5 -9 M -5 -9 L -3 -10 L 0 -10 L 5 -9 L 7 -9 L 8 -10 L 9 -12","-10 11 M 1 -12 L -2 -11 L -3 -10 L -4 -8 L -4 -5 L -3 -3 L -1 -2 L 2 -2 L 5 -3 L 7 -4 L 8 -6 L 8 -9 L 7 -11 L 5 -12 L 1 -12 M 3 -12 L -2 -11 M -2 -10 L -3 -8 L -3 -4 L -2 -3 M -3 -3 L 0 -2 M 1 -2 L 5 -3 M 6 -4 L 7 -6 L 7 -9 L 6 -11 M 7 -11 L 3 -12 M 1 -12 L -1 -10 L -2 -8 L -2 -4 L -1 -2 M 2 -2 L 4 -3 L 5 -4 L 6 -6 L 6 -10 L 5 -12 M -1 -2 L -5 -1 L -7 1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 4 8 L 5 7 L 6 5 L 6 2 L 5 0 L 4 -1 L 2 -2 M 0 -2 L -5 -1 M -4 -1 L -6 1 L -7 3 L -7 6 L -6 8 M -7 8 L -2 9 L 4 8 M 4 7 L 5 5 L 5 2 L 4 0 M 4 -1 L 1 -2 M -1 -2 L -3 -1 L -5 1 L -6 3 L -6 6 L -5 8 L -4 9 M 0 9 L 2 8 L 3 7 L 4 5 L 4 1 L 3 -1 L 2 -2","-10 11 M 6 -4 L 5 -2 L 4 -1 L 2 0 L -1 0 L -3 -1 L -4 -2 L -5 -4 L -5 -7 L -4 -9 L -2 -11 L 1 -12 L 4 -12 L 6 -11 L 7 -10 L 8 -7 L 8 -4 L 7 0 L 6 3 L 4 6 L 2 8 L -1 9 L -4 9 L -6 8 L -7 6 L -7 4 L -5 4 L -5 6 L -6 6 L -6 5 M -3 -2 L -4 -4 L -4 -7 L -3 -9 M 6 -10 L 7 -8 L 7 -4 L 6 0 L 5 3 L 3 6 M -1 0 L -2 -1 L -3 -3 L -3 -7 L -2 -10 L -1 -11 L 1 -12 M 4 -12 L 5 -11 L 6 -9 L 6 -4 L 5 0 L 4 3 L 3 5 L 1 8 L -1 9","-5 6 M 1 -5 L 0 -4 L 0 -3 L 1 -2 L 2 -2 L 3 -3 L 3 -4 L 2 -5 L 1 -5 M 1 -4 L 1 -3 L 2 -3 L 2 -4 L 1 -4 M -2 6 L -3 7 L -3 8 L -2 9 L -1 9 L 0 8 L 0 7 L -1 6 L -2 6 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7","-5 6 M 1 -5 L 0 -4 L 0 -3 L 1 -2 L 2 -2 L 3 -3 L 3 -4 L 2 -5 L 1 -5 M 1 -4 L 1 -3 L 2 -3 L 2 -4 L 1 -4 M -1 9 L -2 9 L -3 8 L -3 7 L -2 6 L -1 6 L 0 7 L 0 9 L -1 11 L -2 12 L -4 13 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7 M -1 9 L -1 10 L -2 12","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-10 11 M -3 -7 L -3 -8 L -2 -8 L -2 -6 L -4 -6 L -4 -8 L -3 -10 L -2 -11 L 1 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -5 L 7 -4 L 5 -3 L 1 -2 L -1 -1 L -1 1 L 1 2 L 2 2 M 3 -12 L 8 -11 M 7 -11 L 8 -9 L 8 -7 L 7 -5 L 6 -4 L 4 -3 M 5 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -5 L 5 -4 L 1 -2 L 0 -1 L 0 1 L 1 2 M -2 6 L -3 7 L -3 8 L -2 9 L -1 9 L 0 8 L 0 7 L -1 6 L -2 6 M -2 7 L -2 8 L -1 8 L -1 7 L -2 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 3 -12 L -9 8 M 1 -8 L 2 9 M 2 -10 L 3 8 M 3 -12 L 3 -10 L 4 7 L 4 9 M -6 3 L 2 3 M -12 9 L -6 9 M -1 9 L 6 9 M -9 8 L -11 9 M -9 8 L -7 9 M 2 8 L 0 9 M 2 7 L 1 9 M 4 7 L 5 9","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 M 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 M 5 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -4 L 4 -2 M -4 -2 L 4 -2 L 6 -1 L 7 1 L 7 3 L 6 6 L 4 8 L 0 9 L -12 9 M 5 -1 L 6 1 L 6 3 L 5 6 L 3 8 M 4 -2 L 5 0 L 5 3 L 4 6 L 2 8 L 0 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9","-10 11 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 L 6 4 M -1 -10 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -2 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M -6 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -7 L 8 -3 L 7 1 L 5 5 L 3 7 L 1 8 L -3 9 L -12 9 M 5 -11 L 6 -10 L 7 -7 L 7 -3 L 6 1 L 4 5 L 2 7 M 3 -12 L 5 -10 L 6 -7 L 6 -3 L 5 1 L 3 5 L 0 8 L -3 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M 3 -6 L 1 2 M -6 -12 L 9 -12 L 8 -6 M -4 -2 L 2 -2 M -12 9 L 3 9 L 5 4 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M 5 -12 L 8 -11 M 6 -12 L 8 -10 M 7 -12 L 8 -9 M 8 -12 L 8 -6 M 3 -6 L 1 -2 L 1 2 M 2 -4 L 0 -2 L 1 0 M 2 -3 L -1 -2 L 1 -1 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9 M -2 9 L 3 8 M 0 9 L 3 7 M 3 7 L 5 4","-12 10 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M 3 -6 L 1 2 M -6 -12 L 9 -12 L 8 -6 M -4 -2 L 2 -2 M -12 9 L -4 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M 5 -12 L 8 -11 M 6 -12 L 8 -10 M 7 -12 L 8 -9 M 8 -12 L 8 -6 M 3 -6 L 1 -2 L 1 2 M 2 -4 L 0 -2 L 1 0 M 2 -3 L -1 -2 L 1 -1 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9","-10 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 5 -12 L 2 -12 L -1 -11 L -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 4 L -6 7 L -5 8 L -2 9 L 0 9 L 3 8 L 5 6 L 7 2 M -1 -10 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 5 L -5 7 M 4 6 L 5 5 L 6 2 M 2 -12 L 0 -11 L -2 -8 L -3 -6 L -4 -3 L -5 1 L -5 6 L -4 8 L -2 9 M 0 9 L 2 8 L 4 5 L 5 2 M 2 2 L 10 2 M 3 2 L 5 3 M 4 2 L 5 5 M 8 2 L 6 4 M 9 2 L 6 3","-13 13 M -4 -12 L -10 9 M -3 -12 L -9 9 M -2 -12 L -8 9 M 8 -12 L 2 9 M 9 -12 L 3 9 M 10 -12 L 4 9 M -7 -12 L 1 -12 M 5 -12 L 13 -12 M -6 -2 L 6 -2 M -13 9 L -5 9 M -1 9 L 7 9 M -6 -12 L -3 -11 M -5 -12 L -4 -10 M -1 -12 L -3 -10 M 0 -12 L -3 -11 M 6 -12 L 9 -11 M 7 -12 L 8 -10 M 11 -12 L 9 -10 M 12 -12 L 9 -11 M -9 8 L -12 9 M -9 7 L -11 9 M -8 7 L -7 9 M -9 8 L -6 9 M 3 8 L 0 9 M 3 7 L 1 9 M 4 7 L 5 9 M 3 8 L 6 9","-7 7 M 2 -12 L -4 9 M 3 -12 L -3 9 M 4 -12 L -2 9 M -1 -12 L 7 -12 M -7 9 L 1 9 M 0 -12 L 3 -11 M 1 -12 L 2 -10 M 5 -12 L 3 -10 M 6 -12 L 3 -11 M -3 8 L -6 9 M -3 7 L -5 9 M -2 7 L -1 9 M -3 8 L 0 9","-9 10 M 5 -12 L 0 5 L -1 7 L -3 9 M 6 -12 L 2 1 L 1 4 L 0 6 M 7 -12 L 3 1 L 1 6 L -1 8 L -3 9 L -5 9 L -7 8 L -8 6 L -8 4 L -7 3 L -6 3 L -5 4 L -5 5 L -6 6 L -7 6 M -7 4 L -7 5 L -6 5 L -6 4 L -7 4 M 2 -12 L 10 -12 M 3 -12 L 6 -11 M 4 -12 L 5 -10 M 8 -12 L 6 -10 M 9 -12 L 6 -11","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M 10 -11 L -5 0 M -1 -3 L 3 9 M 0 -3 L 4 9 M 1 -4 L 5 8 M -6 -12 L 2 -12 M 7 -12 L 13 -12 M -12 9 L -4 9 M 0 9 L 7 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M 8 -12 L 10 -11 M 12 -12 L 10 -11 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9 M 3 8 L 1 9 M 3 7 L 2 9 M 4 7 L 6 9","-10 10 M -1 -12 L -7 9 M 0 -12 L -6 9 M 1 -12 L -5 9 M -4 -12 L 4 -12 M -10 9 L 5 9 L 7 3 M -3 -12 L 0 -11 M -2 -12 L -1 -10 M 2 -12 L 0 -10 M 3 -12 L 0 -11 M -6 8 L -9 9 M -6 7 L -8 9 M -5 7 L -4 9 M -6 8 L -3 9 M 0 9 L 5 8 M 2 9 L 6 6 M 4 9 L 7 3","-14 14 M -5 -12 L -11 8 M -5 -11 L -4 7 L -4 9 M -4 -12 L -3 7 M -3 -12 L -2 6 M 9 -12 L -2 6 L -4 9 M 9 -12 L 3 9 M 10 -12 L 4 9 M 11 -12 L 5 9 M -8 -12 L -3 -12 M 9 -12 L 14 -12 M -14 9 L -8 9 M 0 9 L 8 9 M -7 -12 L -5 -11 M -6 -12 L -5 -10 M 12 -12 L 10 -10 M 13 -12 L 10 -11 M -11 8 L -13 9 M -11 8 L -9 9 M 4 8 L 1 9 M 4 7 L 2 9 M 5 7 L 6 9 M 4 8 L 7 9","-12 13 M -3 -12 L -9 8 M -3 -12 L 4 9 M -2 -12 L 4 6 M -1 -12 L 5 6 M 10 -11 L 5 6 L 4 9 M -6 -12 L -1 -12 M 7 -12 L 13 -12 M -12 9 L -6 9 M -5 -12 L -2 -11 M -4 -12 L -2 -10 M 8 -12 L 10 -11 M 12 -12 L 10 -11 M -9 8 L -11 9 M -9 8 L -7 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 5 L -6 7 M 3 6 L 5 3 L 6 0 L 7 -4 L 7 -8 L 6 -10 M 1 -12 L -1 -11 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 6 L -5 8 L -4 9 M -1 9 L 1 8 L 3 5 L 4 3 L 5 0 L 6 -4 L 6 -9 L 5 -11 L 4 -12","-12 11 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M -6 -12 L 6 -12 L 9 -11 L 10 -9 L 10 -7 L 9 -4 L 7 -2 L 3 -1 L -5 -1 M 8 -11 L 9 -9 L 9 -7 L 8 -4 L 6 -2 M 6 -12 L 7 -11 L 8 -9 L 8 -7 L 7 -4 L 5 -2 L 3 -1 M -12 9 L -4 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9","-11 11 M 1 -12 L -2 -11 L -4 -9 L -6 -6 L -7 -3 L -8 1 L -8 4 L -7 7 L -6 8 L -4 9 L -1 9 L 2 8 L 4 6 L 6 3 L 7 0 L 8 -4 L 8 -7 L 7 -10 L 6 -11 L 4 -12 L 1 -12 M -3 -9 L -5 -6 L -6 -3 L -7 1 L -7 5 L -6 7 M 3 6 L 5 3 L 6 0 L 7 -4 L 7 -8 L 6 -10 M 1 -12 L -1 -11 L -3 -8 L -4 -6 L -5 -3 L -6 1 L -6 6 L -5 8 L -4 9 M -1 9 L 1 8 L 3 5 L 4 3 L 5 0 L 6 -4 L 6 -9 L 5 -11 L 4 -12 M -6 6 L -5 4 L -3 3 L -2 3 L 0 4 L 1 6 L 2 11 L 3 12 L 4 12 L 5 11 M 2 12 L 3 13 L 4 13 M 1 6 L 1 13 L 2 14 L 4 14 L 5 11 L 5 10","-12 12 M -3 -12 L -9 9 M -2 -12 L -8 9 M -1 -12 L -7 9 M -6 -12 L 5 -12 L 8 -11 L 9 -9 L 9 -7 L 8 -4 L 7 -3 L 4 -2 L -4 -2 M 7 -11 L 8 -9 L 8 -7 L 7 -4 L 6 -3 M 5 -12 L 6 -11 L 7 -9 L 7 -7 L 6 -4 L 4 -2 M 0 -2 L 2 -1 L 3 0 L 5 6 L 6 7 L 7 7 L 8 6 M 5 7 L 6 8 L 7 8 M 3 0 L 4 8 L 5 9 L 7 9 L 8 6 L 8 5 M -12 9 L -4 9 M -5 -12 L -2 -11 M -4 -12 L -3 -10 M 0 -12 L -2 -10 M 1 -12 L -2 -11 M -8 8 L -11 9 M -8 7 L -10 9 M -7 7 L -6 9 M -8 8 L -5 9","-11 12 M 8 -10 L 9 -10 L 10 -12 L 9 -6 L 9 -8 L 8 -10 L 7 -11 L 4 -12 L 0 -12 L -3 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -2 L 4 1 L 5 3 L 5 6 L 4 8 M -4 -6 L -3 -4 L 4 0 L 5 2 M -3 -11 L -4 -9 L -4 -7 L -3 -5 L 3 -2 L 5 0 L 6 2 L 6 5 L 5 7 L 4 8 L 1 9 L -3 9 L -6 8 L -7 7 L -8 5 L -8 3 L -9 9 L -8 7 L -7 7","-11 11 M 2 -12 L -4 9 M 3 -12 L -3 9 M 4 -12 L -2 9 M -5 -12 L -7 -6 M 11 -12 L 10 -6 M -5 -12 L 11 -12 M -7 9 L 1 9 M -4 -12 L -7 -6 M -2 -12 L -6 -9 M 0 -12 L -5 -11 M 7 -12 L 10 -11 M 8 -12 L 10 -10 M 9 -12 L 10 -9 M 10 -12 L 10 -6 M -3 8 L -6 9 M -3 7 L -5 9 M -2 7 L -1 9 M -3 8 L 0 9","-12 13 M -4 -12 L -7 -1 L -8 3 L -8 6 L -7 8 L -4 9 L 0 9 L 3 8 L 5 6 L 6 3 L 10 -11 M -3 -12 L -6 -1 L -7 3 L -7 7 L -6 8 M -2 -12 L -5 -1 L -6 3 L -6 7 L -4 9 M -7 -12 L 1 -12 M 7 -12 L 13 -12 M -6 -12 L -3 -11 M -5 -12 L -4 -10 M -1 -12 L -3 -10 M 0 -12 L -3 -11 M 8 -12 L 10 -11 M 12 -12 L 10 -11","-10 10 M -4 -12 L -4 -10 L -3 7 L -3 9 M -3 -11 L -2 6 M -2 -12 L -1 5 M 9 -11 L -3 9 M -6 -12 L 1 -12 M 6 -12 L 12 -12 M -5 -12 L -4 -10 M -1 -12 L -2 -10 M 0 -12 L -3 -11 M 7 -12 L 9 -11 M 11 -12 L 9 -11","-13 13 M -5 -12 L -5 -10 L -7 7 L -7 9 M -4 -11 L -6 6 M -3 -12 L -5 5 M 3 -12 L -5 5 L -7 9 M 3 -12 L 3 -10 L 1 7 L 1 9 M 4 -11 L 2 6 M 5 -12 L 3 5 M 11 -11 L 3 5 L 1 9 M -8 -12 L 0 -12 M 3 -12 L 5 -12 M 8 -12 L 14 -12 M -7 -12 L -4 -11 M -6 -12 L -5 -10 M -2 -12 L -4 -9 M -1 -12 L -4 -11 M 9 -12 L 11 -11 M 13 -12 L 11 -11","-11 11 M -4 -12 L 2 9 M -3 -12 L 3 9 M -2 -12 L 4 9 M 9 -11 L -9 8 M -6 -12 L 1 -12 M 6 -12 L 12 -12 M -12 9 L -6 9 M -1 9 L 6 9 M -5 -12 L -3 -10 M -1 -12 L -2 -10 M 0 -12 L -2 -11 M 7 -12 L 9 -11 M 11 -12 L 9 -11 M -9 8 L -11 9 M -9 8 L -7 9 M 2 8 L 0 9 M 2 7 L 1 9 M 3 7 L 5 9","-11 11 M -5 -12 L -1 -2 L -4 9 M -4 -12 L 0 -2 L -3 9 M -3 -12 L 1 -2 L -2 9 M 10 -11 L 1 -2 M -7 -12 L 0 -12 M 7 -12 L 13 -12 M -7 9 L 1 9 M -6 -12 L -4 -11 M -2 -12 L -3 -10 M -1 -12 L -4 -11 M 8 -12 L 10 -11 M 12 -12 L 10 -11 M -3 8 L -6 9 M -3 7 L -5 9 M -2 7 L -1 9 M -3 8 L 0 9","-11 11 M 8 -12 L -10 9 M 9 -12 L -9 9 M 10 -12 L -8 9 M 10 -12 L -4 -12 L -6 -6 M -10 9 L 4 9 L 6 3 M -3 -12 L -6 -6 M -2 -12 L -5 -9 M 0 -12 L -4 -11 M 0 9 L 4 8 M 2 9 L 5 6 M 3 9 L 6 3","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-11 11 M 5 -5 L 3 2 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M 6 -5 L 4 2 L 4 8 M 5 -5 L 7 -5 L 5 2 L 4 6 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -4 -4 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9","-9 10 M -2 -12 L -4 -5 L -5 1 L -5 5 L -4 7 L -3 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 2 L 7 0 L 6 -3 L 5 -4 L 3 -5 L 1 -5 L -1 -4 L -2 -3 L -3 -1 L -4 2 M -1 -12 L -3 -5 L -4 -1 L -4 5 L -3 8 M 4 7 L 5 5 L 6 2 L 6 -1 L 5 -3 M -5 -12 L 0 -12 L -2 -5 L -4 2 M 1 9 L 3 7 L 4 5 L 5 2 L 5 -1 L 4 -4 L 3 -5 M -4 -12 L -1 -11 M -3 -12 L -2 -10","-9 9 M 5 -1 L 5 -2 L 4 -2 L 4 0 L 6 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 5 M -3 -3 L -4 -1 L -5 2 L -5 5 L -4 7 M 0 -5 L -2 -3 L -3 -1 L -4 2 L -4 5 L -3 8 L -2 9","-11 11 M 7 -12 L 4 -1 L 3 3 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M 8 -12 L 5 -1 L 4 3 L 4 8 M 4 -12 L 9 -12 L 5 2 L 4 6 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -5 -3 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9 M 5 -12 L 8 -11 M 6 -12 L 7 -10","-9 9 M -5 4 L -1 3 L 2 2 L 5 0 L 6 -2 L 5 -4 L 3 -5 L 0 -5 L -3 -4 L -5 -1 L -6 2 L -6 4 L -5 7 L -4 8 L -2 9 L 0 9 L 3 8 L 5 6 M -3 -3 L -4 -1 L -5 2 L -5 5 L -4 7 M 0 -5 L -2 -3 L -3 -1 L -4 2 L -4 5 L -3 8 L -2 9","-8 8 M 8 -10 L 8 -11 L 7 -11 L 7 -9 L 9 -9 L 9 -11 L 8 -12 L 6 -12 L 4 -11 L 2 -9 L 1 -7 L 0 -4 L -1 0 L -3 9 L -4 12 L -5 14 L -7 16 M 2 -8 L 1 -5 L 0 0 L -2 9 L -3 12 M 6 -12 L 4 -10 L 3 -8 L 2 -5 L 1 0 L -1 8 L -2 11 L -3 13 L -5 15 L -7 16 L -9 16 L -10 15 L -10 13 L -8 13 L -8 15 L -9 15 L -9 14 M -4 -5 L 7 -5","-10 11 M 6 -5 L 2 9 L 1 12 L -1 15 L -3 16 M 7 -5 L 3 9 L 1 13 M 6 -5 L 8 -5 L 4 9 L 2 13 L 0 15 L -3 16 L -6 16 L -8 15 L -9 14 L -9 12 L -7 12 L -7 14 L -8 14 L -8 13 M 4 2 L 4 -1 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 4 L -6 7 L -5 8 L -3 9 L -1 9 L 1 8 L 2 7 L 3 5 L 4 2 M -4 -3 L -5 -1 L -6 2 L -6 5 L -5 7 M -1 -5 L -3 -3 L -4 -1 L -5 2 L -5 5 L -4 8 L -3 9","-11 11 M -3 -12 L -9 9 L -7 9 M -2 -12 L -8 9 M -6 -12 L -1 -12 L -7 9 M -5 2 L -3 -2 L -1 -4 L 1 -5 L 3 -5 L 5 -4 L 6 -2 L 6 1 L 4 6 M 5 -4 L 5 0 L 4 4 L 4 8 M 5 -2 L 3 3 L 3 6 L 4 8 L 5 9 L 7 9 L 9 7 L 10 5 M -5 -12 L -2 -11 M -4 -12 L -3 -10","-7 6 M 1 -12 L 1 -10 L 3 -10 L 3 -12 L 1 -12 M 2 -12 L 2 -10 M 1 -11 L 3 -11 M -6 -1 L -5 -3 L -3 -5 L -1 -5 L 0 -4 L 1 -2 L 1 1 L -1 6 M 0 -4 L 0 0 L -1 4 L -1 8 M 0 -2 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5","-7 6 M 3 -12 L 3 -10 L 5 -10 L 5 -12 L 3 -12 M 4 -12 L 4 -10 M 3 -11 L 5 -11 M -5 -1 L -4 -3 L -2 -5 L 0 -5 L 1 -4 L 2 -2 L 2 1 L 0 8 L -1 11 L -2 13 L -4 15 L -6 16 L -8 16 L -9 15 L -9 13 L -7 13 L -7 15 L -8 15 L -8 14 M 1 -4 L 1 1 L -1 8 L -2 11 L -3 13 M 1 -2 L 0 2 L -2 9 L -3 12 L -4 14 L -6 16","-11 11 M -3 -12 L -9 9 L -7 9 M -2 -12 L -8 9 M -6 -12 L -1 -12 L -7 9 M 7 -3 L 7 -4 L 6 -4 L 6 -2 L 8 -2 L 8 -4 L 7 -5 L 5 -5 L 3 -4 L -1 0 L -3 1 M -5 1 L -3 1 L -1 2 L 0 3 L 2 7 L 3 8 L 5 8 M -1 3 L 1 7 L 2 8 M -3 1 L -2 2 L 0 8 L 1 9 L 3 9 L 5 8 L 7 5 M -5 -12 L -2 -11 M -4 -12 L -3 -10","-6 6 M 2 -12 L -1 -1 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5 M 3 -12 L 0 -1 L -1 3 L -1 8 M -1 -12 L 4 -12 L 0 2 L -1 6 M 0 -12 L 3 -11 M 1 -12 L 2 -10","-18 17 M -17 -1 L -16 -3 L -14 -5 L -12 -5 L -11 -4 L -10 -2 L -10 1 L -12 9 M -11 -4 L -11 1 L -13 9 M -11 -2 L -12 2 L -14 9 L -12 9 M -10 1 L -8 -2 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -2 L 1 1 L -1 9 M 0 -4 L 0 1 L -2 9 M 0 -2 L -1 2 L -3 9 L -1 9 M 1 1 L 3 -2 L 5 -4 L 7 -5 L 9 -5 L 11 -4 L 12 -2 L 12 1 L 10 6 M 11 -4 L 11 0 L 10 4 L 10 8 M 11 -2 L 9 3 L 9 6 L 10 8 L 11 9 L 13 9 L 15 7 L 16 5","-12 12 M -11 -1 L -10 -3 L -8 -5 L -6 -5 L -5 -4 L -4 -2 L -4 1 L -6 9 M -5 -4 L -5 1 L -7 9 M -5 -2 L -6 2 L -8 9 L -6 9 M -4 1 L -2 -2 L 0 -4 L 2 -5 L 4 -5 L 6 -4 L 7 -2 L 7 1 L 5 6 M 6 -4 L 6 0 L 5 4 L 5 8 M 6 -2 L 4 3 L 4 6 L 5 8 L 6 9 L 8 9 L 10 7 L 11 5","-10 10 M -1 -5 L -4 -4 L -6 -1 L -7 2 L -7 4 L -6 7 L -5 8 L -2 9 L 1 9 L 4 8 L 6 5 L 7 2 L 7 0 L 6 -3 L 5 -4 L 2 -5 L -1 -5 M -4 -3 L -5 -1 L -6 2 L -6 5 L -5 7 M 4 7 L 5 5 L 6 2 L 6 -1 L 5 -3 M -1 -5 L -3 -3 L -4 -1 L -5 2 L -5 5 L -4 8 L -2 9 M 1 9 L 3 7 L 4 5 L 5 2 L 5 -1 L 4 -4 L 2 -5","-11 11 M -10 -1 L -9 -3 L -7 -5 L -5 -5 L -4 -4 L -3 -2 L -3 1 L -4 5 L -7 16 M -4 -4 L -4 1 L -5 5 L -8 16 M -4 -2 L -5 2 L -9 16 M -3 2 L -2 -1 L -1 -3 L 0 -4 L 2 -5 L 4 -5 L 6 -4 L 7 -3 L 8 0 L 8 2 L 7 5 L 5 8 L 2 9 L 0 9 L -2 8 L -3 5 L -3 2 M 6 -3 L 7 -1 L 7 2 L 6 5 L 5 7 M 4 -5 L 5 -4 L 6 -1 L 6 2 L 5 5 L 4 7 L 2 9 M -12 16 L -4 16 M -8 15 L -11 16 M -8 14 L -10 16 M -7 14 L -6 16 M -8 15 L -5 16","-11 10 M 5 -5 L -1 16 M 6 -5 L 0 16 M 5 -5 L 7 -5 L 1 16 M 3 2 L 3 -1 L 2 -4 L 0 -5 L -2 -5 L -5 -4 L -7 -1 L -8 2 L -8 4 L -7 7 L -6 8 L -4 9 L -2 9 L 0 8 L 1 7 L 2 5 L 3 2 M -5 -3 L -6 -1 L -7 2 L -7 5 L -6 7 M -2 -5 L -4 -3 L -5 -1 L -6 2 L -6 5 L -5 8 L -4 9 M -4 16 L 4 16 M 0 15 L -3 16 M 0 14 L -2 16 M 1 14 L 2 16 M 0 15 L 3 16","-9 9 M -8 -1 L -7 -3 L -5 -5 L -3 -5 L -2 -4 L -1 -2 L -1 2 L -3 9 M -2 -4 L -2 2 L -4 9 M -2 -2 L -3 2 L -5 9 L -3 9 M 7 -3 L 7 -4 L 6 -4 L 6 -2 L 8 -2 L 8 -4 L 7 -5 L 5 -5 L 3 -4 L 1 -2 L -1 2","-8 9 M 6 -2 L 6 -3 L 5 -3 L 5 -1 L 7 -1 L 7 -3 L 6 -4 L 3 -5 L 0 -5 L -3 -4 L -4 -3 L -4 -1 L -3 1 L -1 2 L 2 3 L 4 4 L 5 6 M -3 -4 L -4 -1 M -3 0 L -1 1 L 2 2 L 4 3 M 5 4 L 4 8 M -4 -3 L -3 -1 L -1 0 L 2 1 L 4 2 L 5 4 L 5 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -6 5 L -4 5 L -4 7 L -5 7 L -5 6","-7 7 M 2 -12 L -1 -1 L -2 3 L -2 6 L -1 8 L 0 9 L 2 9 L 4 7 L 5 5 M 3 -12 L 0 -1 L -1 3 L -1 8 M 2 -12 L 4 -12 L 0 2 L -1 6 M -4 -5 L 6 -5","-12 12 M -11 -1 L -10 -3 L -8 -5 L -6 -5 L -5 -4 L -4 -2 L -4 1 L -6 6 M -5 -4 L -5 0 L -6 4 L -6 8 M -5 -2 L -7 3 L -7 6 L -6 8 L -4 9 L -2 9 L 0 8 L 2 6 L 4 3 M 6 -5 L 4 3 L 4 6 L 5 8 L 6 9 L 8 9 L 10 7 L 11 5 M 7 -5 L 5 3 L 5 8 M 6 -5 L 8 -5 L 6 2 L 5 6","-10 10 M -9 -1 L -8 -3 L -6 -5 L -4 -5 L -3 -4 L -2 -2 L -2 1 L -4 6 M -3 -4 L -3 0 L -4 4 L -4 8 M -3 -2 L -5 3 L -5 6 L -4 8 L -2 9 L 0 9 L 2 8 L 4 6 L 6 3 L 7 -1 L 7 -5 L 6 -5 L 6 -4 L 7 -2","-15 15 M -14 -1 L -13 -3 L -11 -5 L -9 -5 L -8 -4 L -7 -2 L -7 1 L -9 6 M -8 -4 L -8 0 L -9 4 L -9 8 M -8 -2 L -10 3 L -10 6 L -9 8 L -7 9 L -5 9 L -3 8 L -1 6 L 0 3 M 2 -5 L 0 3 L 0 6 L 1 8 L 3 9 L 5 9 L 7 8 L 9 6 L 11 3 L 12 -1 L 12 -5 L 11 -5 L 11 -4 L 12 -2 M 3 -5 L 1 3 L 1 8 M 2 -5 L 4 -5 L 2 2 L 1 6","-11 11 M -8 -1 L -6 -4 L -4 -5 L -2 -5 L 0 -4 L 1 -2 L 1 0 M -2 -5 L -1 -4 L -1 0 L -2 4 L -3 6 L -5 8 L -7 9 L -9 9 L -10 8 L -10 6 L -8 6 L -8 8 L -9 8 L -9 7 M 0 -3 L 0 0 L -1 4 L -1 7 M 8 -3 L 8 -4 L 7 -4 L 7 -2 L 9 -2 L 9 -4 L 8 -5 L 6 -5 L 4 -4 L 2 -2 L 1 0 L 0 4 L 0 8 L 1 9 M -2 4 L -2 6 L -1 8 L 1 9 L 3 9 L 5 8 L 7 5","-11 11 M -10 -1 L -9 -3 L -7 -5 L -5 -5 L -4 -4 L -3 -2 L -3 1 L -5 6 M -4 -4 L -4 0 L -5 4 L -5 8 M -4 -2 L -6 3 L -6 6 L -5 8 L -3 9 L -1 9 L 1 8 L 3 6 L 5 2 M 7 -5 L 3 9 L 2 12 L 0 15 L -2 16 M 8 -5 L 4 9 L 2 13 M 7 -5 L 9 -5 L 5 9 L 3 13 L 1 15 L -2 16 L -5 16 L -7 15 L -8 14 L -8 12 L -6 12 L -6 14 L -7 14 L -7 13","-10 10 M 7 -5 L 6 -3 L 4 -1 L -4 5 L -6 7 L -7 9 M 6 -3 L -3 -3 L -5 -2 L -6 0 M 4 -3 L 0 -4 L -3 -4 L -4 -3 M 4 -3 L 0 -5 L -3 -5 L -5 -3 L -6 0 M -6 7 L 3 7 L 5 6 L 6 4 M -4 7 L 0 8 L 3 8 L 4 7 M -4 7 L 0 9 L 3 9 L 5 7 L 6 4","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 5 M 0 -12 L -1 -10 L 0 2 L 1 -10 L 0 -12 M 0 -10 L 0 -4 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 6 -9 L 5 -8 L 6 -7 L 7 -8 L 7 -9 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 7 2 M -7 -7 L -5 -5 L -3 -4 L 3 -2 L 5 -1 L 6 0 L 7 2 L 7 6 L 5 8 L 2 9 L -2 9 L -5 8 L -7 6 L -7 5 L -6 4 L -5 5 L -6 6","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-12 13 M 9 -4 L 8 -3 L 9 -2 L 10 -3 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -5 9 L -8 8 L -9 6 L -9 3 L -8 1 L -2 -3 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -7 L -4 -4 L -2 -1 L 3 6 L 5 8 L 8 9 L 9 9 L 10 8 L 10 7 M -5 9 L -7 8 L -8 6 L -8 3 L -7 1 L -5 -1 M -5 -7 L -4 -5 L 4 6 L 6 8 L 8 9","-4 4 M 0 -12 L -1 -5 M 1 -12 L -1 -5","-7 7 M 4 -16 L 2 -14 L 0 -11 L -2 -7 L -3 -2 L -3 2 L -2 7 L 0 11 L 2 14 L 4 16 M 2 -14 L 0 -10 L -1 -7 L -2 -2 L -2 2 L -1 7 L 0 10 L 2 14","-7 7 M -4 -16 L -2 -14 L 0 -11 L 2 -7 L 3 -2 L 3 2 L 2 7 L 0 11 L -2 14 L -4 16 M -2 -14 L 0 -10 L 1 -7 L 2 -2 L 2 2 L 1 7 L 0 10 L -2 14","-8 8 M 0 -6 L 0 6 M -5 -3 L 5 3 M 5 -3 L -5 3","-13 13 M 0 -9 L 0 9 M -9 0 L 9 0","-4 4 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-13 13 M -9 0 L 9 0","-4 4 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-11 11 M 9 -16 L -9 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -4 -10 L -5 -8 L -6 -3 L -6 0 L -5 5 L -4 7 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 5 L 6 0 L 6 -3 L 5 -8 L 4 -10 L 3 -11 L 1 -12","-10 10 M -4 -8 L -2 -9 L 1 -12 L 1 9 M 0 -11 L 0 9 M -4 9 L 5 9","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 3 -2 L -2 0 L -4 1 L -6 3 L -7 6 L -7 9 M 2 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 2 -2 L -2 0 M -7 7 L -6 6 L -4 6 L 1 8 L 4 8 L 6 7 L 7 6 M -4 6 L 1 9 L 5 9 L 6 8 L 7 6 L 7 4","-10 10 M -6 -8 L -5 -7 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 L -1 -3 M 2 -12 L 4 -11 L 5 -9 L 5 -6 L 4 -4 L 2 -3 M 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 5 -1 L 6 2 L 6 5 L 5 7 L 4 8 L 2 9","-10 10 M 2 -10 L 2 9 M 3 -12 L 3 9 M 3 -12 L -8 3 L 8 3 M -1 9 L 6 9","-10 10 M -5 -12 L -7 -2 M -7 -2 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 4 L -6 5 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -5 -12 L 5 -12 M -5 -11 L 0 -11 L 5 -12","-10 10 M 5 -9 L 4 -8 L 5 -7 L 6 -8 L 6 -9 L 5 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L 0 -4 L -3 -3 L -5 -1 L -6 2 M 0 -12 L -2 -11 L -4 -9 L -5 -7 L -6 -3 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 2 L 5 -1 L 3 -3 L 1 -4","-10 10 M -7 -12 L -7 -6 M -7 -8 L -6 -10 L -4 -12 L -2 -12 L 3 -9 L 5 -9 L 6 -10 L 7 -12 M -6 -10 L -4 -11 L -2 -11 L 3 -9 M 7 -12 L 7 -9 L 6 -6 L 2 -1 L 1 1 L 0 4 L 0 9 M 6 -6 L 1 -1 L 0 1 L -1 4 L -1 9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 6 -6 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -2 -12 L -4 -11 L -5 -9 L -5 -6 L -4 -4 L -2 -3 M 2 -3 L 4 -4 L 5 -6 L 5 -9 L 4 -11 L 2 -12 M -2 -3 L -5 -2 L -6 -1 L -7 1 L -7 5 L -6 7 L -5 8 L -2 9 L 2 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 2 -3 M -2 -3 L -4 -2 L -5 -1 L -6 1 L -6 5 L -5 7 L -4 8 L -2 9 M 2 9 L 4 8 L 5 7 L 6 5 L 6 1 L 5 -1 L 4 -2 L 2 -3","-10 10 M 6 -5 L 5 -2 L 3 0 L 0 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 0 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 5 L -5 6 M -1 1 L -3 0 L -5 -2 L -6 -5 L -6 -6 L -5 -9 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 5 -9 L 6 -6 L 6 0 L 5 4 L 4 6 L 2 8 L 0 9","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 0 4 L -1 5 L 0 6 L 1 5 L 0 4","-4 4 M 0 -3 L -1 -2 L 0 -1 L 1 -2 L 0 -3 M 1 5 L 0 6 L -1 5 L 0 4 L 1 5 L 1 7 L -1 9","-12 12 M 8 -9 L -8 0 L 8 9","-13 13 M -9 -3 L 9 -3 M -9 3 L 9 3","-12 12 M -8 -9 L 8 0 L -8 9","-9 9 M -5 -8 L -4 -7 L -5 -6 L -6 -7 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 1 -12 L 4 -11 L 5 -10 L 6 -8 L 6 -6 L 5 -4 L 4 -3 L 0 -1 L 0 2 M 1 -12 L 3 -11 L 4 -10 L 5 -8 L 5 -6 L 4 -4 L 2 -2 M 0 7 L -1 8 L 0 9 L 1 8 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 0 -12 L -7 9 M 0 -12 L 7 9 M 0 -9 L 6 9 M -5 3 L 4 3 M -9 9 L -3 9 M 3 9 L 9 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 3 -2 M -5 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 3 -2 L 5 -1 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 3 9","-11 10 M 6 -9 L 7 -6 L 7 -12 L 6 -9 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -9 9 M 1 -12 L 3 -11 L 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 L 3 8 L 1 9","-11 10 M -6 -12 L -6 9 M -5 -12 L -5 9 M 1 -6 L 1 2 M -9 -12 L 7 -12 L 7 -6 L 6 -12 M -5 -2 L 1 -2 M -9 9 L 7 9 L 7 3 L 6 9","-11 9 M -6 -12 L -6 9 M -5 -12 L -5 9 M 1 -6 L 1 2 M -9 -12 L 7 -12 L 7 -6 L 6 -12 M -5 -2 L 1 -2 M -9 9 L -2 9","-11 12 M 6 -9 L 7 -6 L 7 -12 L 6 -9 L 4 -11 L 1 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 L -3 8 L -1 9 M 6 1 L 6 9 M 7 1 L 7 9 M 3 1 L 10 1","-12 12 M -7 -12 L -7 9 M -6 -12 L -6 9 M 6 -12 L 6 9 M 7 -12 L 7 9 M -10 -12 L -3 -12 M 3 -12 L 10 -12 M -6 -2 L 6 -2 M -10 9 L -3 9 M 3 9 L 10 9","-5 6 M 0 -12 L 0 9 M 1 -12 L 1 9 M -3 -12 L 4 -12 M -3 9 L 4 9","-7 8 M 3 -12 L 3 5 L 2 8 L 0 9 L -2 9 L -4 8 L -5 6 L -5 4 L -4 3 L -3 4 L -4 5 M 2 -12 L 2 5 L 1 8 L 0 9 M -1 -12 L 6 -12","-12 10 M -7 -12 L -7 9 M -6 -12 L -6 9 M 7 -12 L -6 1 M -1 -3 L 7 9 M -2 -3 L 6 9 M -10 -12 L -3 -12 M 3 -12 L 9 -12 M -10 9 L -3 9 M 3 9 L 9 9","-9 9 M -4 -12 L -4 9 M -3 -12 L -3 9 M -7 -12 L 0 -12 M -7 9 L 8 9 L 8 3 L 7 9","-12 13 M -7 -12 L -7 9 M -6 -12 L 0 6 M -7 -12 L 0 9 M 7 -12 L 0 9 M 7 -12 L 7 9 M 8 -12 L 8 9 M -10 -12 L -6 -12 M 7 -12 L 11 -12 M -10 9 L -4 9 M 4 9 L 11 9","-11 12 M -6 -12 L -6 9 M -5 -12 L 7 7 M -5 -10 L 7 9 M 7 -12 L 7 9 M -9 -12 L -5 -12 M 4 -12 L 10 -12 M -9 9 L -3 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 0 L -6 4 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 4 L 7 0 L 7 -3 L 6 -7 L 5 -9 L 3 -11 L 1 -12","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -5 -1 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -5 L 6 -3 L 5 -2 L 3 -1 M -9 9 L -2 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -1 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 0 L -6 4 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 4 L 7 0 L 7 -3 L 6 -7 L 5 -9 L 3 -11 L 1 -12 M -4 7 L -4 6 L -3 4 L -1 3 L 0 3 L 2 4 L 3 6 L 4 13 L 5 14 L 7 14 L 8 12 L 8 11 M 3 6 L 4 10 L 5 12 L 6 13 L 7 13 L 8 12","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 L -5 -2 M 3 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 3 -2 M -9 9 L -2 9 M 0 -2 L 2 -1 L 3 0 L 6 7 L 7 8 L 8 8 L 9 7 M 2 -1 L 3 1 L 5 8 L 6 9 L 8 9 L 9 7 L 9 6","-10 10 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 1 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -7 L -6 -5 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 7 2 M -7 -7 L -5 -5 L -3 -4 L 3 -2 L 5 -1 L 6 0 L 7 2 L 7 6 L 5 8 L 2 9 L -1 9 L -4 8 L -6 6 L -7 3 L -7 9 L -6 6","-9 10 M 0 -12 L 0 9 M 1 -12 L 1 9 M -6 -12 L -7 -6 L -7 -12 L 8 -12 L 8 -6 L 7 -12 M -3 9 L 4 9","-12 12 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -12 M -6 -12 L -6 3 L -5 6 L -3 8 L -1 9 M -10 -12 L -3 -12 M 4 -12 L 10 -12","-10 10 M -7 -12 L 0 9 M -6 -12 L 0 6 M 7 -12 L 0 9 M -9 -12 L -3 -12 M 3 -12 L 9 -12","-12 12 M -8 -12 L -4 9 M -7 -12 L -4 4 M 0 -12 L -4 9 M 0 -12 L 4 9 M 1 -12 L 4 4 M 8 -12 L 4 9 M -11 -12 L -4 -12 M 5 -12 L 11 -12","-10 10 M -7 -12 L 6 9 M -6 -12 L 7 9 M 7 -12 L -7 9 M -9 -12 L -3 -12 M 3 -12 L 9 -12 M -9 9 L -3 9 M 3 9 L 9 9","-10 11 M -7 -12 L 0 -1 L 0 9 M -6 -12 L 1 -1 L 1 9 M 8 -12 L 1 -1 M -9 -12 L -3 -12 M 4 -12 L 10 -12 M -3 9 L 4 9","-10 10 M 6 -12 L -7 9 M 7 -12 L -6 9 M -6 -12 L -7 -6 L -7 -12 L 7 -12 M -7 9 L 7 9 L 7 3 L 6 9","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-9 11 M -4 -3 L -4 -2 L -5 -2 L -5 -3 L -4 -4 L -2 -5 L 2 -5 L 4 -4 L 5 -3 L 6 -1 L 6 6 L 7 8 L 8 9 M 5 -3 L 5 6 L 6 8 L 8 9 L 9 9 M 5 -1 L 4 0 L -2 1 L -5 2 L -6 4 L -6 6 L -5 8 L -2 9 L 1 9 L 3 8 L 5 6 M -2 1 L -4 2 L -5 4 L -5 6 L -4 8 L -2 9","-11 10 M -6 -12 L -6 9 M -5 -12 L -5 9 M -5 -2 L -3 -4 L -1 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -1 9 L -3 8 L -5 6 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -9 -12 L -5 -12","-10 9 M 5 -2 L 4 -1 L 5 0 L 6 -1 L 6 -2 L 4 -4 L 2 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9","-10 11 M 5 -12 L 5 9 M 6 -12 L 6 9 M 5 -2 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 3 8 L 5 6 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 M 2 -12 L 6 -12 M 5 9 L 9 9","-10 9 M -6 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M 5 1 L 5 -2 L 4 -4 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9","-7 6 M 3 -11 L 2 -10 L 3 -9 L 4 -10 L 4 -11 L 3 -12 L 1 -12 L -1 -11 L -2 -9 L -2 9 M 1 -12 L 0 -11 L -1 -9 L -1 9 M -5 -5 L 3 -5 M -5 9 L 2 9","-9 10 M -1 -5 L -3 -4 L -4 -3 L -5 -1 L -5 1 L -4 3 L -3 4 L -1 5 L 1 5 L 3 4 L 4 3 L 5 1 L 5 -1 L 4 -3 L 3 -4 L 1 -5 L -1 -5 M -3 -4 L -4 -2 L -4 2 L -3 4 M 3 4 L 4 2 L 4 -2 L 3 -4 M 4 -3 L 5 -4 L 7 -5 L 7 -4 L 5 -4 M -4 3 L -5 4 L -6 6 L -6 7 L -5 9 L -2 10 L 3 10 L 6 11 L 7 12 M -6 7 L -5 8 L -2 9 L 3 9 L 6 10 L 7 12 L 7 13 L 6 15 L 3 16 L -3 16 L -6 15 L -7 13 L -7 12 L -6 10 L -3 9","-11 11 M -6 -12 L -6 9 M -5 -12 L -5 9 M -5 -2 L -3 -4 L 0 -5 L 2 -5 L 5 -4 L 6 -2 L 6 9 M 2 -5 L 4 -4 L 5 -2 L 5 9 M -9 -12 L -5 -12 M -9 9 L -2 9 M 2 9 L 9 9","-5 6 M 0 -12 L -1 -11 L 0 -10 L 1 -11 L 0 -12 M 0 -5 L 0 9 M 1 -5 L 1 9 M -3 -5 L 1 -5 M -3 9 L 4 9","-5 6 M 1 -12 L 0 -11 L 1 -10 L 2 -11 L 1 -12 M 2 -5 L 2 13 L 1 15 L -1 16 L -3 16 L -4 15 L -4 14 L -3 13 L -2 14 L -3 15 M 1 -5 L 1 13 L 0 15 L -1 16 M -2 -5 L 2 -5","-11 10 M -6 -12 L -6 9 M -5 -12 L -5 9 M 5 -5 L -5 5 M 0 1 L 6 9 M -1 1 L 5 9 M -9 -12 L -5 -12 M 2 -5 L 8 -5 M -9 9 L -2 9 M 2 9 L 8 9","-5 6 M 0 -12 L 0 9 M 1 -12 L 1 9 M -3 -12 L 1 -12 M -3 9 L 4 9","-16 17 M -11 -5 L -11 9 M -10 -5 L -10 9 M -10 -2 L -8 -4 L -5 -5 L -3 -5 L 0 -4 L 1 -2 L 1 9 M -3 -5 L -1 -4 L 0 -2 L 0 9 M 1 -2 L 3 -4 L 6 -5 L 8 -5 L 11 -4 L 12 -2 L 12 9 M 8 -5 L 10 -4 L 11 -2 L 11 9 M -14 -5 L -10 -5 M -14 9 L -7 9 M -3 9 L 4 9 M 8 9 L 15 9","-11 11 M -6 -5 L -6 9 M -5 -5 L -5 9 M -5 -2 L -3 -4 L 0 -5 L 2 -5 L 5 -4 L 6 -2 L 6 9 M 2 -5 L 4 -4 L 5 -2 L 5 9 M -9 -5 L -5 -5 M -9 9 L -2 9 M 2 9 L 9 9","-10 10 M -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 1 -5 L -1 -5 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 M 1 9 L 3 8 L 5 6 L 6 3 L 6 1 L 5 -2 L 3 -4 L 1 -5","-11 10 M -6 -5 L -6 16 M -5 -5 L -5 16 M -5 -2 L -3 -4 L -1 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -1 9 L -3 8 L -5 6 M 1 -5 L 3 -4 L 5 -2 L 6 1 L 6 3 L 5 6 L 3 8 L 1 9 M -9 -5 L -5 -5 M -9 16 L -2 16","-10 10 M 5 -5 L 5 16 M 6 -5 L 6 16 M 5 -2 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 3 8 L 5 6 M -1 -5 L -3 -4 L -5 -2 L -6 1 L -6 3 L -5 6 L -3 8 L -1 9 M 2 16 L 9 16","-9 8 M -4 -5 L -4 9 M -3 -5 L -3 9 M -3 1 L -2 -2 L 0 -4 L 2 -5 L 5 -5 L 6 -4 L 6 -3 L 5 -2 L 4 -3 L 5 -4 M -7 -5 L -3 -5 M -7 9 L 0 9","-8 9 M 5 -3 L 6 -5 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -2 -5 L -4 -4 L -5 -3 L -5 -1 L -4 0 L -2 1 L 3 3 L 5 4 L 6 5 M -5 -2 L -4 -1 L -2 0 L 3 2 L 5 3 L 6 4 L 6 7 L 5 8 L 3 9 L -1 9 L -3 8 L -4 7 L -5 5 L -5 9 L -4 7","-7 8 M -2 -12 L -2 5 L -1 8 L 1 9 L 3 9 L 5 8 L 6 6 M -1 -12 L -1 5 L 0 8 L 1 9 M -5 -5 L 3 -5","-11 11 M -6 -5 L -6 6 L -5 8 L -2 9 L 0 9 L 3 8 L 5 6 M -5 -5 L -5 6 L -4 8 L -2 9 M 5 -5 L 5 9 M 6 -5 L 6 9 M -9 -5 L -5 -5 M 2 -5 L 6 -5 M 5 9 L 9 9","-9 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M 6 -5 L 0 9 M -8 -5 L -2 -5 M 2 -5 L 8 -5","-12 12 M -8 -5 L -4 9 M -7 -5 L -4 6 M 0 -5 L -4 9 M 0 -5 L 4 9 M 1 -5 L 4 6 M 8 -5 L 4 9 M -11 -5 L -4 -5 M 5 -5 L 11 -5","-10 10 M -6 -5 L 5 9 M -5 -5 L 6 9 M 6 -5 L -6 9 M -8 -5 L -2 -5 M 2 -5 L 8 -5 M -8 9 L -2 9 M 2 9 L 8 9","-10 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M 6 -5 L 0 9 L -2 13 L -4 15 L -6 16 L -7 16 L -8 15 L -7 14 L -6 15 M -8 -5 L -2 -5 M 2 -5 L 8 -5","-9 9 M 5 -5 L -6 9 M 6 -5 L -5 9 M -5 -5 L -6 -1 L -6 -5 L 6 -5 M -6 9 L 6 9 L 6 5 L 5 9","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        new string[] {"-8 8","-5 6 M 0 -12 L -1 -11 L -1 -9 L 0 -1 M 0 -12 L 0 2 L 1 2 M 0 -12 L 1 -12 L 1 2 M 1 -12 L 2 -11 L 2 -9 L 1 -1 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-9 9 M -4 -12 L -5 -11 L -5 -5 M -4 -11 L -5 -5 M -4 -12 L -3 -11 L -5 -5 M 5 -12 L 4 -11 L 4 -5 M 5 -11 L 4 -5 M 5 -12 L 6 -11 L 4 -5","-10 11 M 1 -16 L -6 16 M 7 -16 L 0 16 M -6 -3 L 8 -3 M -7 3 L 7 3","-10 10 M -2 -16 L -2 13 M 2 -16 L 2 13 M 6 -7 L 6 -8 L 5 -8 L 5 -6 L 7 -6 L 7 -8 L 6 -10 L 5 -11 L 2 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 3 L -5 3 L -5 5 L -6 5 L -6 4","-12 12 M 9 -12 L -9 9 M -4 -12 L -2 -10 L -2 -8 L -3 -6 L -5 -5 L -7 -5 L -9 -7 L -9 -9 L -8 -11 L -6 -12 L -4 -12 L -2 -11 L 1 -10 L 4 -10 L 7 -11 L 9 -12 M 5 2 L 3 3 L 2 5 L 2 7 L 4 9 L 6 9 L 8 8 L 9 6 L 9 4 L 7 2 L 5 2","-13 13 M 9 -3 L 9 -4 L 8 -4 L 8 -2 L 10 -2 L 10 -4 L 9 -5 L 8 -5 L 7 -4 L 6 -2 L 4 3 L 2 6 L 0 8 L -2 9 L -6 9 L -8 8 L -9 6 L -9 3 L -8 1 L -2 -3 L 0 -5 L 1 -7 L 1 -9 L 0 -11 L -2 -12 L -4 -11 L -5 -9 L -5 -6 L -4 -3 L -2 0 L 2 5 L 5 8 L 7 9 L 9 9 L 10 7 L 10 6 M -7 8 L -8 6 L -8 3 L -7 1 L -6 0 M 0 -5 L 1 -9 M 1 -7 L 0 -11 M -4 -11 L -5 -7 M -4 -4 L -2 -1 L 2 4 L 5 7 L 7 8 M -4 9 L -6 8 L -7 6 L -7 3 L -6 1 L -2 -3 M -5 -9 L -4 -5 L -1 -1 L 3 4 L 6 7 L 8 8 L 9 8 L 10 7","-4 5 M 1 -12 L 0 -11 L 0 -5 M 1 -11 L 0 -5 M 1 -12 L 2 -11 L 0 -5","-7 7 M 3 -16 L 1 -14 L -1 -11 L -3 -7 L -4 -2 L -4 2 L -3 7 L -1 11 L 1 14 L 3 16 M -1 -10 L -2 -7 L -3 -3 L -3 3 L -2 7 L -1 10 M 1 -14 L 0 -12 L -1 -9 L -2 -3 L -2 3 L -1 9 L 0 12 L 1 14","-7 7 M -3 -16 L -1 -14 L 1 -11 L 3 -7 L 4 -2 L 4 2 L 3 7 L 1 11 L -1 14 L -3 16 M 1 -10 L 2 -7 L 3 -3 L 3 3 L 2 7 L 1 10 M -1 -14 L 0 -12 L 1 -9 L 2 -3 L 2 3 L 1 9 L 0 12 L -1 14","-8 8 M 0 -12 L -1 -11 L 1 -1 L 0 0 M 0 -12 L 0 0 M 0 -12 L 1 -11 L -1 -1 L 0 0 M -5 -9 L -4 -9 L 4 -3 L 5 -3 M -5 -9 L 5 -3 M -5 -9 L -5 -8 L 5 -4 L 5 -3 M 5 -9 L 4 -9 L -4 -3 L -5 -3 M 5 -9 L -5 -3 M 5 -9 L 5 -8 L -5 -4 L -5 -3","-12 13 M 0 -9 L 0 8 L 1 8 M 0 -9 L 1 -9 L 1 8 M -8 -1 L 9 -1 L 9 0 M -8 -1 L -8 0 L 9 0","-5 6 M 2 8 L 1 9 L 0 9 L -1 8 L -1 7 L 0 6 L 1 6 L 2 7 L 2 10 L 1 12 L -1 13 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7 M 1 9 L 2 10 M 2 8 L 1 12","-13 13 M -9 0 L 9 0","-5 6 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-11 12 M 9 -16 L -9 16 L -8 16 M 9 -16 L 10 -16 L -8 16","-10 10 M -1 -12 L -4 -11 L -6 -8 L -7 -3 L -7 0 L -6 5 L -4 8 L -1 9 L 1 9 L 4 8 L 6 5 L 7 0 L 7 -3 L 6 -8 L 4 -11 L 1 -12 L -1 -12 M -4 -10 L -5 -8 L -6 -4 L -6 1 L -5 5 L -4 7 M 4 7 L 5 5 L 6 1 L 6 -4 L 5 -8 L 4 -10 M -1 -12 L -3 -11 L -4 -9 L -5 -4 L -5 1 L -4 6 L -3 8 L -1 9 M 1 9 L 3 8 L 4 6 L 5 1 L 5 -4 L 4 -9 L 3 -11 L 1 -12","-10 10 M -1 -10 L -1 9 M 0 -10 L 0 8 M 1 -12 L 1 9 M 1 -12 L -2 -9 L -4 -8 M -5 9 L 5 9 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-10 10 M -6 -8 L -6 -7 L -5 -7 L -5 -8 L -6 -8 M -6 -9 L -5 -9 L -4 -8 L -4 -7 L -5 -6 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 3 -2 L -2 0 L -4 1 L -6 3 L -7 6 L -7 9 M 5 -10 L 6 -8 L 6 -6 L 5 -4 M 2 -12 L 4 -11 L 5 -8 L 5 -6 L 4 -4 L 2 -2 L -2 0 M -7 7 L -6 6 L -4 6 L 1 7 L 5 7 L 7 6 M -4 6 L 1 8 L 5 8 L 6 7 M -4 6 L 1 9 L 5 9 L 6 8 L 7 6 L 7 4","-10 10 M -6 -8 L -6 -7 L -5 -7 L -5 -8 L -6 -8 M -6 -9 L -5 -9 L -4 -8 L -4 -7 L -5 -6 L -6 -6 L -7 -7 L -7 -8 L -6 -10 L -5 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -9 L 6 -6 L 5 -4 L 2 -3 M 4 -11 L 5 -9 L 5 -6 L 4 -4 M 1 -12 L 3 -11 L 4 -9 L 4 -6 L 3 -4 L 1 -3 M -1 -3 L 2 -3 L 4 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 3 L -4 4 L -4 5 L -5 6 L -6 6 M 5 0 L 6 2 L 6 5 L 5 7 M 1 -3 L 3 -2 L 4 -1 L 5 2 L 5 5 L 4 8 L 2 9 M -6 4 L -6 5 L -5 5 L -5 4 L -6 4","-10 10 M 1 -9 L 1 9 M 2 -10 L 2 8 M 3 -12 L 3 9 M 3 -12 L -8 3 L 8 3 M -2 9 L 6 9 M 1 8 L -1 9 M 1 7 L 0 9 M 3 7 L 4 9 M 3 8 L 5 9","-10 10 M -5 -12 L -7 -2 L -5 -4 L -2 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -2 9 L -5 8 L -6 7 L -7 5 L -7 4 L -6 3 L -5 3 L -4 4 L -4 5 L -5 6 L -6 6 M 5 -2 L 6 0 L 6 4 L 5 6 M 1 -5 L 3 -4 L 4 -3 L 5 0 L 5 4 L 4 7 L 3 8 L 1 9 M -6 4 L -6 5 L -5 5 L -5 4 L -6 4 M -5 -12 L 5 -12 M -5 -11 L 3 -11 M -5 -10 L -1 -10 L 3 -11 L 5 -12","-10 10 M 4 -9 L 4 -8 L 5 -8 L 5 -9 L 4 -9 M 5 -10 L 4 -10 L 3 -9 L 3 -8 L 4 -7 L 5 -7 L 6 -8 L 6 -9 L 5 -11 L 3 -12 L 0 -12 L -3 -11 L -5 -9 L -6 -7 L -7 -3 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 2 L 6 -1 L 4 -3 L 1 -4 L -1 -4 L -3 -3 L -4 -2 L -5 0 M -4 -9 L -5 -7 L -6 -3 L -6 3 L -5 6 L -4 7 M 5 6 L 6 4 L 6 1 L 5 -1 M 0 -12 L -2 -11 L -3 -10 L -4 -8 L -5 -4 L -5 3 L -4 6 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 4 L 5 1 L 4 -2 L 3 -3 L 1 -4","-10 10 M -7 -12 L -7 -6 M 7 -12 L 7 -9 L 6 -6 L 2 -1 L 1 1 L 0 5 L 0 9 M 1 0 L 0 2 L -1 5 L -1 9 M 6 -6 L 1 -1 L -1 2 L -2 5 L -2 9 L 0 9 M -7 -8 L -6 -10 L -4 -12 L -2 -12 L 3 -9 L 5 -9 L 6 -10 L 7 -12 M -5 -10 L -4 -11 L -2 -11 L 0 -10 M -7 -8 L -6 -9 L -4 -10 L -2 -10 L 3 -9","-10 10 M -2 -12 L -5 -11 L -6 -9 L -6 -6 L -5 -4 L -2 -3 L 2 -3 L 5 -4 L 6 -6 L 6 -9 L 5 -11 L 2 -12 L -2 -12 M -4 -11 L -5 -9 L -5 -6 L -4 -4 M 4 -4 L 5 -6 L 5 -9 L 4 -11 M -2 -12 L -3 -11 L -4 -9 L -4 -6 L -3 -4 L -2 -3 M 2 -3 L 3 -4 L 4 -6 L 4 -9 L 3 -11 L 2 -12 M -2 -3 L -5 -2 L -6 -1 L -7 1 L -7 5 L -6 7 L -5 8 L -2 9 L 2 9 L 5 8 L 6 7 L 7 5 L 7 1 L 6 -1 L 5 -2 L 2 -3 M -5 -1 L -6 1 L -6 5 L -5 7 M 5 7 L 6 5 L 6 1 L 5 -1 M -2 -3 L -4 -2 L -5 1 L -5 5 L -4 8 L -2 9 M 2 9 L 4 8 L 5 5 L 5 1 L 4 -2 L 2 -3","-10 10 M -5 5 L -5 6 L -4 6 L -4 5 L -5 5 M 5 -3 L 4 -1 L 3 0 L 1 1 L -1 1 L -4 0 L -6 -2 L -7 -5 L -7 -6 L -6 -9 L -4 -11 L -1 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -6 L 7 0 L 6 4 L 5 6 L 3 8 L 0 9 L -3 9 L -5 8 L -6 6 L -6 5 L -5 4 L -4 4 L -3 5 L -3 6 L -4 7 L -5 7 M -5 -2 L -6 -4 L -6 -7 L -5 -9 M 4 -10 L 5 -9 L 6 -6 L 6 0 L 5 4 L 4 6 M -1 1 L -3 0 L -4 -1 L -5 -4 L -5 -7 L -4 -10 L -3 -11 L -1 -12 M 1 -12 L 3 -11 L 4 -9 L 5 -6 L 5 1 L 4 5 L 3 7 L 2 8 L 0 9","-5 6 M 0 -5 L -1 -4 L -1 -3 L 0 -2 L 1 -2 L 2 -3 L 2 -4 L 1 -5 L 0 -5 M 0 -4 L 0 -3 L 1 -3 L 1 -4 L 0 -4 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-5 6 M 0 -5 L -1 -4 L -1 -3 L 0 -2 L 1 -2 L 2 -3 L 2 -4 L 1 -5 L 0 -5 M 0 -4 L 0 -3 L 1 -3 L 1 -4 L 0 -4 M 2 8 L 1 9 L 0 9 L -1 8 L -1 7 L 0 6 L 1 6 L 2 7 L 2 10 L 1 12 L -1 13 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7 M 1 9 L 2 10 M 2 8 L 1 12","-12 12 M 8 -9 L -8 0 L 8 9","-12 13 M -8 -5 L 9 -5 L 9 -4 M -8 -5 L -8 -4 L 9 -4 M -8 3 L 9 3 L 9 4 M -8 3 L -8 4 L 9 4","-12 12 M -8 -9 L 8 0 L -8 9","-9 10 M -5 -7 L -5 -8 L -4 -8 L -4 -6 L -6 -6 L -6 -8 L -5 -10 L -4 -11 L -2 -12 L 2 -12 L 5 -11 L 6 -10 L 7 -8 L 7 -6 L 6 -4 L 5 -3 L 1 -1 M 5 -10 L 6 -9 L 6 -5 L 5 -4 M 2 -12 L 4 -11 L 5 -9 L 5 -5 L 4 -3 L 3 -2 M 0 -1 L 0 2 L 1 2 L 1 -1 L 0 -1 M 0 6 L -1 7 L -1 8 L 0 9 L 1 9 L 2 8 L 2 7 L 1 6 L 0 6 M 0 7 L 0 8 L 1 8 L 1 7 L 0 7","-13 14 M 5 -4 L 4 -6 L 2 -7 L -1 -7 L -3 -6 L -4 -5 L -5 -2 L -5 1 L -4 3 L -2 4 L 1 4 L 3 3 L 4 1 M -1 -7 L -3 -5 L -4 -2 L -4 1 L -3 3 L -2 4 M 5 -7 L 4 1 L 4 3 L 6 4 L 8 4 L 10 2 L 11 -1 L 11 -3 L 10 -6 L 9 -8 L 7 -10 L 5 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -10 L -8 -8 L -9 -6 L -10 -3 L -10 0 L -9 3 L -8 5 L -6 7 L -4 8 L -1 9 L 2 9 L 5 8 L 7 7 L 8 6 M 6 -7 L 5 1 L 5 3 L 6 4","-10 10 M 0 -12 L -7 8 M -1 -9 L 5 9 M 0 -9 L 6 9 M 0 -12 L 7 9 M -5 3 L 4 3 M -9 9 L -3 9 M 2 9 L 9 9 M -7 8 L -8 9 M -7 8 L -5 9 M 5 8 L 3 9 M 5 7 L 4 9 M 6 7 L 8 9","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 M 6 -10 L 7 -8 L 7 -6 L 6 -4 M 3 -12 L 5 -11 L 6 -9 L 6 -5 L 5 -3 L 3 -2 M -4 -2 L 3 -2 L 6 -1 L 7 0 L 8 2 L 8 5 L 7 7 L 6 8 L 3 9 L -9 9 M 6 0 L 7 2 L 7 5 L 6 7 M 3 -2 L 5 -1 L 6 1 L 6 6 L 5 8 L 3 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 10 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 2 9 L 4 8 L 6 6 L 7 4 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 1 -12 L 4 -11 L 6 -9 L 7 -7 L 8 -4 L 8 1 L 7 4 L 6 6 L 4 8 L 1 9 L -9 9 M 5 -9 L 6 -7 L 7 -4 L 7 1 L 6 4 L 5 6 M 1 -12 L 3 -11 L 5 -8 L 6 -4 L 6 1 L 5 5 L 3 8 L 1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 10 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 7 -12 L 7 -6 M -4 -2 L 2 -2 M 2 -6 L 2 2 M -9 9 L 7 9 L 7 3 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M 2 -12 L 7 -11 M 4 -12 L 7 -10 M 5 -12 L 7 -9 M 6 -12 L 7 -6 M 2 -6 L 1 -2 L 2 2 M 2 -4 L 0 -2 L 2 0 M 2 -3 L -2 -2 L 2 -1 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9 M 2 9 L 7 8 M 4 9 L 7 7 M 5 9 L 7 6 M 6 9 L 7 3","-11 9 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 7 -12 L 7 -6 M -4 -2 L 2 -2 M 2 -6 L 2 2 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M 2 -12 L 7 -11 M 4 -12 L 7 -10 M 5 -12 L 7 -9 M 6 -12 L 7 -6 M 2 -6 L 1 -2 L 2 2 M 2 -4 L 0 -2 L 2 0 M 2 -3 L -2 -2 L 2 -1 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 12 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 2 -12 L -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -4 L -8 1 L -7 4 L -6 6 L -4 8 L -1 9 L 2 9 L 4 8 L 6 8 L 7 9 L 7 1 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 6 2 L 6 7 M 5 1 L 5 7 L 4 8 M 2 1 L 10 1 M 3 1 L 5 2 M 4 1 L 5 3 M 8 1 L 7 3 M 9 1 L 7 2","-12 12 M -7 -12 L -7 9 M -6 -11 L -6 8 M -5 -12 L -5 9 M 5 -12 L 5 9 M 6 -11 L 6 8 M 7 -12 L 7 9 M -10 -12 L -2 -12 M 2 -12 L 10 -12 M -5 -2 L 5 -2 M -10 9 L -2 9 M 2 9 L 10 9 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 3 -12 L 5 -11 M 4 -12 L 5 -10 M 8 -12 L 7 -10 M 9 -12 L 7 -11 M -7 8 L -9 9 M -7 7 L -8 9 M -5 7 L -4 9 M -5 8 L -3 9 M 5 8 L 3 9 M 5 7 L 4 9 M 7 7 L 8 9 M 7 8 L 9 9","-6 6 M -1 -12 L -1 9 M 0 -11 L 0 8 M 1 -12 L 1 9 M -4 -12 L 4 -12 M -4 9 L 4 9 M -3 -12 L -1 -11 M -2 -12 L -1 -10 M 2 -12 L 1 -10 M 3 -12 L 1 -11 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-8 8 M 1 -12 L 1 5 L 0 8 L -1 9 M 2 -11 L 2 5 L 1 8 M 3 -12 L 3 5 L 2 8 L -1 9 L -3 9 L -5 8 L -6 6 L -6 4 L -5 3 L -4 3 L -3 4 L -3 5 L -4 6 L -5 6 M -5 4 L -5 5 L -4 5 L -4 4 L -5 4 M -2 -12 L 6 -12 M -1 -12 L 1 -11 M 0 -12 L 1 -10 M 4 -12 L 3 -10 M 5 -12 L 3 -11","-12 10 M -7 -12 L -7 9 M -6 -11 L -6 8 M -5 -12 L -5 9 M 6 -11 L -5 0 M -2 -2 L 5 9 M -1 -2 L 6 9 M -1 -4 L 7 9 M -10 -12 L -2 -12 M 3 -12 L 9 -12 M -10 9 L -2 9 M 2 9 L 9 9 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 6 -11 M 8 -12 L 6 -11 M -7 8 L -9 9 M -7 7 L -8 9 M -5 7 L -4 9 M -5 8 L -3 9 M 5 7 L 3 9 M 5 7 L 8 9","-9 9 M -4 -12 L -4 9 M -3 -11 L -3 8 M -2 -12 L -2 9 M -7 -12 L 1 -12 M -7 9 L 8 9 L 8 3 M -6 -12 L -4 -11 M -5 -12 L -4 -10 M -1 -12 L -2 -10 M 0 -12 L -2 -11 M -4 8 L -6 9 M -4 7 L -5 9 M -2 7 L -1 9 M -2 8 L 0 9 M 3 9 L 8 8 M 5 9 L 8 7 M 6 9 L 8 6 M 7 9 L 8 3","-13 13 M -8 -12 L -8 8 M -8 -12 L -1 9 M -7 -12 L -1 6 M -6 -12 L 0 6 M 6 -12 L -1 9 M 6 -12 L 6 9 M 7 -11 L 7 8 M 8 -12 L 8 9 M -11 -12 L -6 -12 M 6 -12 L 11 -12 M -11 9 L -5 9 M 3 9 L 11 9 M -10 -12 L -8 -11 M 9 -12 L 8 -10 M 10 -12 L 8 -11 M -8 8 L -10 9 M -8 8 L -6 9 M 6 8 L 4 9 M 6 7 L 5 9 M 8 7 L 9 9 M 8 8 L 10 9","-12 12 M -7 -12 L -7 8 M -7 -12 L 7 9 M -6 -12 L 6 6 M -5 -12 L 7 6 M 7 -11 L 7 9 M -10 -12 L -5 -12 M 4 -12 L 10 -12 M -10 9 L -4 9 M -9 -12 L -7 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11 M -7 8 L -9 9 M -7 8 L -5 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M 5 6 L 6 4 L 7 1 L 7 -4 L 6 -7 L 5 -9 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 1 9 L 3 8 L 5 5 L 6 1 L 6 -4 L 5 -8 L 3 -11 L 1 -12","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -5 L 7 -3 L 6 -2 L 3 -1 L -4 -1 M 6 -10 L 7 -8 L 7 -5 L 6 -3 M 3 -12 L 5 -11 L 6 -9 L 6 -4 L 5 -2 L 3 -1 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-11 11 M -1 -12 L -4 -11 L -6 -9 L -7 -7 L -8 -3 L -8 0 L -7 4 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 4 L 8 0 L 8 -3 L 7 -7 L 6 -9 L 4 -11 L 1 -12 L -1 -12 M -5 -9 L -6 -7 L -7 -4 L -7 1 L -6 4 L -5 6 M 5 6 L 6 4 L 7 1 L 7 -4 L 6 -7 L 5 -9 M -1 -12 L -3 -11 L -5 -8 L -6 -4 L -6 1 L -5 5 L -3 8 L -1 9 M 1 9 L 3 8 L 5 5 L 6 1 L 6 -4 L 5 -8 L 3 -11 L 1 -12 M -4 6 L -3 4 L -1 3 L 0 3 L 2 4 L 3 6 L 4 12 L 5 14 L 7 14 L 8 12 L 8 10 M 4 10 L 5 12 L 6 13 L 7 13 M 3 6 L 5 11 L 6 12 L 7 12 L 8 11","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -4 -12 L -4 9 M -9 -12 L 3 -12 L 6 -11 L 7 -10 L 8 -8 L 8 -6 L 7 -4 L 6 -3 L 3 -2 L -4 -2 M 6 -10 L 7 -8 L 7 -6 L 6 -4 M 3 -12 L 5 -11 L 6 -9 L 6 -5 L 5 -3 L 3 -2 M 0 -2 L 2 -1 L 3 1 L 5 7 L 6 9 L 8 9 L 9 7 L 9 5 M 5 5 L 6 7 L 7 8 L 8 8 M 2 -1 L 3 0 L 6 6 L 7 7 L 8 7 L 9 6 M -9 9 L -1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -3 -12 L -4 -10 M -2 -12 L -4 -11 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9","-10 10 M 6 -9 L 7 -12 L 7 -6 L 6 -9 L 4 -11 L 1 -12 L -2 -12 L -5 -11 L -7 -9 L -7 -6 L -6 -4 L -3 -2 L 3 0 L 5 1 L 6 3 L 6 6 L 5 8 M -6 -6 L -5 -4 L -3 -3 L 3 -1 L 5 0 L 6 2 M -5 -11 L -6 -9 L -6 -7 L -5 -5 L -3 -4 L 3 -2 L 6 0 L 7 2 L 7 5 L 6 7 L 5 8 L 2 9 L -1 9 L -4 8 L -6 6 L -7 3 L -7 9 L -6 6","-10 10 M -8 -12 L -8 -6 M -1 -12 L -1 9 M 0 -11 L 0 8 M 1 -12 L 1 9 M 8 -12 L 8 -6 M -8 -12 L 8 -12 M -4 9 L 4 9 M -7 -12 L -8 -6 M -6 -12 L -8 -9 M -5 -12 L -8 -10 M -3 -12 L -8 -11 M 3 -12 L 8 -11 M 5 -12 L 8 -10 M 6 -12 L 8 -9 M 7 -12 L 8 -6 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-12 12 M -7 -12 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 -11 M -6 -11 L -6 4 L -5 6 M -5 -12 L -5 4 L -4 7 L -3 8 L -1 9 M -10 -12 L -2 -12 M 4 -12 L 10 -12 M -9 -12 L -7 -11 M -8 -12 L -7 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11","-10 10 M -7 -12 L 0 9 M -6 -12 L 0 6 L 0 9 M -5 -12 L 1 6 M 7 -11 L 0 9 M -9 -12 L -2 -12 M 3 -12 L 9 -12 M -8 -12 L -6 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 5 -12 L 7 -11 M 8 -12 L 7 -11","-12 12 M -8 -12 L -4 9 M -7 -12 L -4 4 L -4 9 M -6 -12 L -3 4 M 0 -12 L -3 4 L -4 9 M 0 -12 L 4 9 M 1 -12 L 4 4 L 4 9 M 2 -12 L 5 4 M 8 -11 L 5 4 L 4 9 M -11 -12 L -3 -12 M 0 -12 L 2 -12 M 5 -12 L 11 -12 M -10 -12 L -7 -11 M -9 -12 L -7 -10 M -5 -12 L -6 -10 M -4 -12 L -6 -11 M 6 -12 L 8 -11 M 10 -12 L 8 -11","-10 10 M -7 -12 L 5 9 M -6 -12 L 6 9 M -5 -12 L 7 9 M 6 -11 L -6 8 M -9 -12 L -2 -12 M 3 -12 L 9 -12 M -9 9 L -3 9 M 2 9 L 9 9 M -8 -12 L -5 -10 M -4 -12 L -5 -10 M -3 -12 L -5 -11 M 4 -12 L 6 -11 M 8 -12 L 6 -11 M -6 8 L -8 9 M -6 8 L -4 9 M 5 8 L 3 9 M 5 7 L 4 9 M 5 7 L 8 9","-11 11 M -8 -12 L -1 -1 L -1 9 M -7 -12 L 0 -1 L 0 8 M -6 -12 L 1 -1 L 1 9 M 7 -11 L 1 -1 M -10 -12 L -3 -12 M 4 -12 L 10 -12 M -4 9 L 4 9 M -9 -12 L -7 -11 M -4 -12 L -6 -11 M 5 -12 L 7 -11 M 9 -12 L 7 -11 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-10 10 M 7 -12 L -7 -12 L -7 -6 M 5 -12 L -7 9 M 6 -12 L -6 9 M 7 -12 L -5 9 M -7 9 L 7 9 L 7 3 M -6 -12 L -7 -6 M -5 -12 L -7 -9 M -4 -12 L -7 -10 M -2 -12 L -7 -11 M 2 9 L 7 8 M 4 9 L 7 7 M 5 9 L 7 6 M 6 9 L 7 3","-7 7 M -3 -16 L -3 16 M -2 -16 L -2 16 M -3 -16 L 4 -16 M -3 16 L 4 16","-7 7 M -7 -12 L 7 12","-7 7 M 2 -16 L 2 16 M 3 -16 L 3 16 M -4 -16 L 3 -16 M -4 16 L 3 16","-11 11 M -8 2 L 0 -3 L 8 2 M -8 2 L 0 -2 L 8 2","-10 10 M -10 16 L 10 16","-6 6 M -2 -12 L 3 -6 M -2 -12 L -3 -11 L 3 -6","-9 11 M -4 -2 L -4 -3 L -3 -3 L -3 -1 L -5 -1 L -5 -3 L -4 -4 L -2 -5 L 2 -5 L 4 -4 L 5 -3 L 6 -1 L 6 6 L 7 8 L 8 9 M 4 -3 L 5 -1 L 5 6 L 6 8 M 2 -5 L 3 -4 L 4 -2 L 4 6 L 5 8 L 8 9 L 9 9 M 4 0 L 3 1 L -2 2 L -5 3 L -6 5 L -6 6 L -5 8 L -2 9 L 1 9 L 3 8 L 4 6 M -4 3 L -5 5 L -5 6 L -4 8 M 3 1 L -1 2 L -3 3 L -4 5 L -4 6 L -3 8 L -2 9","-11 10 M -6 -12 L -6 9 L -5 8 L -3 8 M -5 -11 L -5 7 M -9 -12 L -4 -12 L -4 8 M -4 -2 L -3 -4 L -1 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -1 9 L -3 8 L -4 6 M 5 -2 L 6 0 L 6 4 L 5 6 M 1 -5 L 3 -4 L 4 -3 L 5 0 L 5 4 L 4 7 L 3 8 L 1 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10","-10 9 M 5 -1 L 5 -2 L 4 -2 L 4 0 L 6 0 L 6 -2 L 4 -4 L 2 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M -5 -2 L -6 0 L -6 4 L -5 6 M -1 -5 L -3 -4 L -4 -3 L -5 0 L -5 4 L -4 7 L -3 8 L -1 9","-10 11 M 4 -12 L 4 9 L 9 9 M 5 -11 L 5 8 M 1 -12 L 6 -12 L 6 9 M 4 -2 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 3 8 L 4 6 M -5 -2 L -6 0 L -6 4 L -5 6 M -1 -5 L -3 -4 L -4 -3 L -5 0 L -5 4 L -4 7 L -3 8 L -1 9 M 2 -12 L 4 -11 M 3 -12 L 4 -10 M 6 7 L 7 9 M 6 8 L 8 9","-10 9 M -5 1 L 6 1 L 6 -1 L 5 -3 L 4 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 M 5 0 L 5 -1 L 4 -3 M -5 -2 L -6 0 L -6 4 L -5 6 M 4 1 L 4 -2 L 3 -4 L 1 -5 M -1 -5 L -3 -4 L -4 -3 L -5 0 L -5 4 L -4 7 L -3 8 L -1 9","-7 7 M 5 -10 L 5 -11 L 4 -11 L 4 -9 L 6 -9 L 6 -11 L 5 -12 L 2 -12 L 0 -11 L -1 -10 L -2 -7 L -2 9 M 0 -10 L -1 -7 L -1 8 M 2 -12 L 1 -11 L 0 -9 L 0 9 M -5 -5 L 4 -5 M -5 9 L 3 9 M -2 8 L -4 9 M -2 7 L -3 9 M 0 7 L 1 9 M 0 8 L 2 9","-9 10 M 6 -4 L 7 -3 L 8 -4 L 7 -5 L 6 -5 L 4 -4 L 3 -3 M -1 -5 L -3 -4 L -4 -3 L -5 -1 L -5 1 L -4 3 L -3 4 L -1 5 L 1 5 L 3 4 L 4 3 L 5 1 L 5 -1 L 4 -3 L 3 -4 L 1 -5 L -1 -5 M -3 -3 L -4 -1 L -4 1 L -3 3 M 3 3 L 4 1 L 4 -1 L 3 -3 M -1 -5 L -2 -4 L -3 -2 L -3 2 L -2 4 L -1 5 M 1 5 L 2 4 L 3 2 L 3 -2 L 2 -4 L 1 -5 M -4 3 L -5 4 L -6 6 L -6 7 L -5 9 L -4 10 L -1 11 L 3 11 L 6 12 L 7 13 M -4 9 L -1 10 L 3 10 L 6 11 M -6 7 L -5 8 L -2 9 L 3 9 L 6 10 L 7 12 L 7 13 L 6 15 L 3 16 L -3 16 L -6 15 L -7 13 L -7 12 L -6 10 L -3 9 M -3 16 L -5 15 L -6 13 L -6 12 L -5 10 L -3 9","-11 12 M -6 -12 L -6 9 M -5 -11 L -5 8 M -9 -12 L -4 -12 L -4 9 M -4 -1 L -3 -3 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -3 L 7 0 L 7 9 M 5 -3 L 6 0 L 6 8 M 3 -5 L 4 -4 L 5 -1 L 5 9 M -9 9 L -1 9 M 2 9 L 10 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9 M 5 8 L 3 9 M 5 7 L 4 9 M 7 7 L 8 9 M 7 8 L 9 9","-6 6 M -1 -12 L -1 -10 L 1 -10 L 1 -12 L -1 -12 M 0 -12 L 0 -10 M -1 -11 L 1 -11 M -1 -5 L -1 9 M 0 -4 L 0 8 M -4 -5 L 1 -5 L 1 9 M -4 9 L 4 9 M -3 -5 L -1 -4 M -2 -5 L -1 -3 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-7 6 M 0 -12 L 0 -10 L 2 -10 L 2 -12 L 0 -12 M 1 -12 L 1 -10 M 0 -11 L 2 -11 M 0 -5 L 0 12 L -1 15 L -2 16 M 1 -4 L 1 11 L 0 14 M -3 -5 L 2 -5 L 2 11 L 1 14 L 0 15 L -2 16 L -5 16 L -6 15 L -6 13 L -4 13 L -4 15 L -5 15 L -5 14 M -2 -5 L 0 -4 M -1 -5 L 0 -3","-11 11 M -6 -12 L -6 9 M -5 -11 L -5 8 M -9 -12 L -4 -12 L -4 9 M 5 -4 L -4 5 M 0 1 L 7 9 M 0 2 L 6 9 M -1 2 L 5 9 M 2 -5 L 9 -5 M -9 9 L -1 9 M 2 9 L 9 9 M -8 -12 L -6 -11 M -7 -12 L -6 -10 M 3 -5 L 5 -4 M 8 -5 L 5 -4 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9 M 5 7 L 3 9 M 4 7 L 8 9","-6 6 M -1 -12 L -1 9 M 0 -11 L 0 8 M -4 -12 L 1 -12 L 1 9 M -4 9 L 4 9 M -3 -12 L -1 -11 M -2 -12 L -1 -10 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9","-17 17 M -12 -5 L -12 9 M -11 -4 L -11 8 M -15 -5 L -10 -5 L -10 9 M -10 -1 L -9 -3 L -8 -4 L -6 -5 L -3 -5 L -1 -4 L 0 -3 L 1 0 L 1 9 M -1 -3 L 0 0 L 0 8 M -3 -5 L -2 -4 L -1 -1 L -1 9 M 1 -1 L 2 -3 L 3 -4 L 5 -5 L 8 -5 L 10 -4 L 11 -3 L 12 0 L 12 9 M 10 -3 L 11 0 L 11 8 M 8 -5 L 9 -4 L 10 -1 L 10 9 M -15 9 L -7 9 M -4 9 L 4 9 M 7 9 L 15 9 M -14 -5 L -12 -4 M -13 -5 L -12 -3 M -12 8 L -14 9 M -12 7 L -13 9 M -10 7 L -9 9 M -10 8 L -8 9 M -1 8 L -3 9 M -1 7 L -2 9 M 1 7 L 2 9 M 1 8 L 3 9 M 10 8 L 8 9 M 10 7 L 9 9 M 12 7 L 13 9 M 12 8 L 14 9","-11 12 M -6 -5 L -6 9 M -5 -4 L -5 8 M -9 -5 L -4 -5 L -4 9 M -4 -1 L -3 -3 L -2 -4 L 0 -5 L 3 -5 L 5 -4 L 6 -3 L 7 0 L 7 9 M 5 -3 L 6 0 L 6 8 M 3 -5 L 4 -4 L 5 -1 L 5 9 M -9 9 L -1 9 M 2 9 L 10 9 M -8 -5 L -6 -4 M -7 -5 L -6 -3 M -6 8 L -8 9 M -6 7 L -7 9 M -4 7 L -3 9 M -4 8 L -2 9 M 5 8 L 3 9 M 5 7 L 4 9 M 7 7 L 8 9 M 7 8 L 9 9","-10 10 M -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 4 8 L 6 6 L 7 3 L 7 1 L 6 -2 L 4 -4 L 1 -5 L -1 -5 M -5 -2 L -6 0 L -6 4 L -5 6 M 5 6 L 6 4 L 6 0 L 5 -2 M -1 -5 L -3 -4 L -4 -3 L -5 0 L -5 4 L -4 7 L -3 8 L -1 9 M 1 9 L 3 8 L 4 7 L 5 4 L 5 0 L 4 -3 L 3 -4 L 1 -5","-11 10 M -6 -5 L -6 16 M -5 -4 L -5 15 M -9 -5 L -4 -5 L -4 16 M -4 -2 L -3 -4 L -1 -5 L 1 -5 L 4 -4 L 6 -2 L 7 1 L 7 3 L 6 6 L 4 8 L 1 9 L -1 9 L -3 8 L -4 6 M 5 -2 L 6 0 L 6 4 L 5 6 M 1 -5 L 3 -4 L 4 -3 L 5 0 L 5 4 L 4 7 L 3 8 L 1 9 M -9 16 L -1 16 M -8 -5 L -6 -4 M -7 -5 L -6 -3 M -6 15 L -8 16 M -6 14 L -7 16 M -4 14 L -3 16 M -4 15 L -2 16","-10 10 M 4 -4 L 4 16 M 5 -3 L 5 15 M 3 -4 L 5 -4 L 6 -5 L 6 16 M 4 -2 L 3 -4 L 1 -5 L -1 -5 L -4 -4 L -6 -2 L -7 1 L -7 3 L -6 6 L -4 8 L -1 9 L 1 9 L 3 8 L 4 6 M -5 -2 L -6 0 L -6 4 L -5 6 M -1 -5 L -3 -4 L -4 -3 L -5 0 L -5 4 L -4 7 L -3 8 L -1 9 M 1 16 L 9 16 M 4 15 L 2 16 M 4 14 L 3 16 M 6 14 L 7 16 M 6 15 L 8 16","-9 8 M -4 -5 L -4 9 M -3 -4 L -3 8 M -7 -5 L -2 -5 L -2 9 M 5 -3 L 5 -4 L 4 -4 L 4 -2 L 6 -2 L 6 -4 L 5 -5 L 3 -5 L 1 -4 L -1 -2 L -2 1 M -7 9 L 1 9 M -6 -5 L -4 -4 M -5 -5 L -4 -3 M -4 8 L -6 9 M -4 7 L -5 9 M -2 7 L -1 9 M -2 8 L 0 9","-8 9 M 5 -3 L 6 -5 L 6 -1 L 5 -3 L 4 -4 L 2 -5 L -2 -5 L -4 -4 L -5 -3 L -5 -1 L -4 1 L -2 2 L 3 3 L 5 4 L 6 7 M -4 -4 L -5 -1 M -4 0 L -2 1 L 3 2 L 5 3 M 6 4 L 5 8 M -5 -3 L -4 -1 L -2 0 L 3 1 L 5 2 L 6 4 L 6 7 L 5 8 L 3 9 L -1 9 L -3 8 L -4 7 L -5 5 L -5 9 L -4 7","-7 8 M -2 -10 L -2 4 L -1 7 L 0 8 L 2 9 L 4 9 L 6 8 L 7 6 M -1 -10 L -1 5 L 0 7 M -2 -10 L 0 -12 L 0 5 L 1 8 L 2 9 M -5 -5 L 4 -5","-11 12 M -6 -5 L -6 4 L -5 7 L -4 8 L -2 9 L 1 9 L 3 8 L 4 7 L 5 5 M -5 -4 L -5 5 L -4 7 M -9 -5 L -4 -5 L -4 5 L -3 8 L -2 9 M 5 -5 L 5 9 L 10 9 M 6 -4 L 6 8 M 2 -5 L 7 -5 L 7 9 M -8 -5 L -6 -4 M -7 -5 L -6 -3 M 7 7 L 8 9 M 7 8 L 9 9","-9 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M -4 -5 L 1 7 M 6 -4 L 1 7 L 0 9 M -8 -5 L -1 -5 M 2 -5 L 8 -5 M -7 -5 L -4 -3 M -2 -5 L -4 -4 M 4 -5 L 6 -4 M 7 -5 L 6 -4","-12 12 M -8 -5 L -4 9 M -7 -5 L -4 6 M -6 -5 L -3 6 M 0 -5 L -3 6 L -4 9 M 0 -5 L 4 9 M 1 -5 L 4 6 M 0 -5 L 2 -5 L 5 6 M 8 -4 L 5 6 L 4 9 M -11 -5 L -3 -5 M 5 -5 L 11 -5 M -10 -5 L -7 -4 M -4 -5 L -6 -4 M 6 -5 L 8 -4 M 10 -5 L 8 -4","-10 10 M -6 -5 L 4 9 M -5 -5 L 5 9 M -4 -5 L 6 9 M 5 -4 L -5 8 M -8 -5 L -1 -5 M 2 -5 L 8 -5 M -8 9 L -2 9 M 1 9 L 8 9 M -7 -5 L -5 -4 M -2 -5 L -4 -4 M 3 -5 L 5 -4 M 7 -5 L 5 -4 M -5 8 L -7 9 M -5 8 L -3 9 M 4 8 L 2 9 M 5 8 L 7 9","-10 9 M -6 -5 L 0 9 M -5 -5 L 0 7 M -4 -5 L 1 7 M 6 -4 L 1 7 L -2 13 L -4 15 L -6 16 L -8 16 L -9 15 L -9 13 L -7 13 L -7 15 L -8 15 L -8 14 M -8 -5 L -1 -5 M 2 -5 L 8 -5 M -7 -5 L -4 -3 M -2 -5 L -4 -4 M 4 -5 L 6 -4 M 7 -5 L 6 -4","-9 9 M 4 -5 L -6 9 M 5 -5 L -5 9 M 6 -5 L -4 9 M 6 -5 L -6 -5 L -6 -1 M -6 9 L 6 9 L 6 5 M -5 -5 L -6 -1 M -4 -5 L -6 -2 M -3 -5 L -6 -3 M -1 -5 L -6 -4 M 1 9 L 6 8 M 3 9 L 6 7 M 4 9 L 6 6 M 5 9 L 6 5","-7 7 M 2 -16 L 0 -15 L -1 -14 L -2 -12 L -2 -10 L -1 -8 L 0 -7 L 1 -5 L 1 -3 L -1 -1 M 0 -15 L -1 -13 L -1 -11 L 0 -9 L 1 -8 L 2 -6 L 2 -4 L 1 -2 L -3 0 L 1 2 L 2 4 L 2 6 L 1 8 L 0 9 L -1 11 L -1 13 L 0 15 M -1 1 L 1 3 L 1 5 L 0 7 L -1 8 L -2 10 L -2 12 L -1 14 L 0 15 L 2 16","-4 4 M 0 -16 L 0 16","-7 7 M -2 -16 L 0 -15 L 1 -14 L 2 -12 L 2 -10 L 1 -8 L 0 -7 L -1 -5 L -1 -3 L 1 -1 M 0 -15 L 1 -13 L 1 -11 L 0 -9 L -1 -8 L -2 -6 L -2 -4 L -1 -2 L 3 0 L -1 2 L -2 4 L -2 6 L -1 8 L 0 9 L 1 11 L 1 13 L 0 15 M 1 1 L -1 3 L -1 5 L 0 7 L 1 8 L 2 10 L 2 12 L 1 14 L 0 15 L -2 16","-12 12 M -9 3 L -9 1 L -8 -2 L -6 -3 L -4 -3 L -2 -2 L 2 1 L 4 2 L 6 2 L 8 1 L 9 -1 M -9 1 L -8 -1 L -6 -2 L -4 -2 L -2 -1 L 2 2 L 4 3 L 6 3 L 8 2 L 9 -1 L 9 -3","-8 8 M -8 -12 L -8 9 L -7 9 L -7 -12 L -6 -12 L -6 9 L -5 9 L -5 -12 L -4 -12 L -4 9 L -3 9 L -3 -12 L -2 -12 L -2 9 L -1 9 L -1 -12 L 0 -12 L 0 9 L 1 9 L 1 -12 L 2 -12 L 2 9 L 3 9 L 3 -12 L 4 -12 L 4 9 L 5 9 L 5 -12 L 6 -12 L 6 9 L 7 9 L 7 -12 L 8 -12 L 8 9"},
        // Dot Matrix 5*8 Pixel upper left -6 -21, lower right 6 9 https://www.sparkfun.com/datasheets/LCD/ADM1602K-NSW-FBS-3.3v.pdf
        new string[] {"-6 12", "-6 12 M 0 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 6", "-6 12 M -3 -12 M -3 -9 M -3 -6 M 3 -12 M 3 -9 M 3 -6", // !"
            "-6 12 M -3 -12 M -3 -9 M -3 -6 M -3 -3 M -3 0 M -3 3 M -3 6 M 3 -12 M 3 -9 M 3 -6 M 3 -3 M 3 0 M 3 3 M 3 6 M -6 -6 M 0 -6 M 6 -6 M -6 0 M 0 0 M 6 0", // #
            "-6 12 M 0 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 6 M 0 6 M -6 3 M -3 3 M 3 3 M 6 0 M 3 -3 M -3 -3 M -6 -6 M -3 -9 M 3 -9 M 6 -9", // $
            "-6 12 M -6 -12 M -3 -12 M -3 -9 M -6 -9 M 6 -12 M 6 -9 M 3 -6 M 0 -3 M -3 0 M -6 3 M -6 6 M 3 6 M 6 6 M 6 3 M 3 3", // %
            "-6 12 M 6 6 M 3 3 M 0 0 M -3 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -9 M 0 -6 M -6 0 M -6 3 M -3 6 M 0 6 M 6 0","-6 12 M -3 -12 M 0 -12 M 0 -9 M -3 -6", // &'
            "-6 12 M 3 -12 M 0 -9 M -3 -6 M -3 -3 M -3 0 M 0 3 M 3 6","-6 12 M -3 -12 M 0 -9 M 3 -6 M 3 -3 M 3 0 M 0 3 M -3 6", // ()
            "-6 12 M -6 -6 M -3 -3 M 0 -3 M 3 -3 M 6 -6 M 6 0 M -6 0 M 6 0 M 0 -12 M 0 -9 M 0 -6 M 0 0 M 0 3 M 0 6","-6 12 M -6 -3 M -3 -3 M 0 -3 M 3 -3 M 6 -3 M 0 -9 M 0 -6 M 0 0 M 0 3", // *+
            "-6 12 M -3 0 M 0 0 M 0 3 M -3 6", "-6 12 M -6 0 M -3 0 M 0 0 M 3 0 M 6 0", "-6 12 M -3 6 M 0 6 M -3 3 M 0 3", // ,-.
            "-6 12 M -6 6 M -6 3 M -3 0 M 0 -3 M 3 -6 M 6 -9 M 6 -12","-6 12 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 6 3 M 3 6 M 0 6 M -3 6 M -3 0 M 0 -3 M 3 -6", // 0
            "-6 12 M -3 -9 M 0 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 3 M -3 6 M 0 6 M 3 6","-6 12 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 0 M -3 3 M -6 6 M -3 6 M 0 6 M 3 6 M 6 6", // 12
            "-6 12 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 6 0 M 6 3 M 3 6 M 0 6 M -3 6 M -6 3", // 3
            "-6 12 M 3 6 M 3 3 M 3 0 M 3 -3 M 3 -6 M 3 -9 M 3 -12 M 0 -9 M -3 -6 M -6 -3 M -6 0 M -3 0 M 0 0 M 6 0", // 4
            "-6 12 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 6 -3 M 3 -6 M 0 -6 M -3 -6 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12", // 5
            "-6 12 M 6 -9 M 3 -12 M 0 -12 M -3 -12 M -6 -9 M -6 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 3 -3 M 0 -3 M -3 -3", // 6
            "-6 12 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12 M 6 -9 M 6 -6 M 3 -3 M 0 0 M -3 3 M -6 6", // 7
            "-6 12 M -6 -6 M -6 -9 M -3 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 -3 M -3 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0", // 8
            "-6 12 M 3 -3 M 0 -3 M -3 -3 M -6 -6 M -6 -9 M -3 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 6 -3 M 3 0 M 0 3 M -3 6", // 9
            "-6 12 M -3 -9 M 0 -9 M 0 -6 M -3 -6 M -3 0 M 0 0 M 0 3 M -3 3","-6 12 M -3 -9 M 0 -9 M 0 -6 M -3 -6 M -3 0 M 0 0 M 0 3 M -3 6", //:;
            "-6 12 M 3 -12 M 0 -9 M -3 -6 M -6 -3 M -3 0 M 0 3 M 3 6","-6 12 M -6 -6 M -3 -6 M 0 -6 M 3 -6 M 6 -6 M 6 0 M 3 0 M 0 0 M -3 0 M -6 0", // <=
            "-6 12 M -3 -12 M 0 -9 M 3 -6 M 6 -3 M 3 0 M 0 3 M -6 6", "-6 12 M -6 -9 M -3 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 0 M 0 6", // >?
            "-6 12 M 3 -6 M 0 -3 M 3 0 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 3 -12 M 0 -12 M -3 -12 M -6 -9 M -6 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 6",// @
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 6 3 M 6 6 M -3 -3 M 0 -3 M 3 -3", //A
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 -3 M -3 -3 M 6 0 M 6 3 M 3 6 M 0 6 M -3 6",// B
            "-6 12 M 6 3 M 3 6 M 0 6 M -3 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9",// C
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -9 M 6 -6 M 6 -3 M 6 0 M 3 3 M 0 6 M -3 6",// D
            "-6 12 M 6 6 M 3 6 M 0 6 M -3 6 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12 M -3 -3 M 0 -3 M 3 -3",// E
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12 M -3 -3 M 0 -3 M 3 -3",// F
            "-6 12 M 0 -3 M 3 -3 M 6 -3 M 6 0 M 6 3 M 6 6 M 3 6 M 0 6 M -3 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9", // G
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M 6 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 6 3 M 6 6 M -3 -3 M 0 -3 M 3 -3", // H
            "-6 12 M -3 -12 M 0 -12 M 3 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 3 M -3 6 M 0 6 M 3 6", // I
            "-6 12 M -0 -12 M 3 -12 M 6 -12 M 3 -9 M 3 -6 M 3 -3 M 3 0 M 3 3 M 0 6 M -3 6 M -6 3", // J
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M 6 -12 M 3 -9 M 0 -6 M -3 -3 M 0 0 M 3 3 M 6 6", // K
            "-6 12 M 6 6 M 3 6 M 0 6 M -3 6 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12", // L
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -6 M 0 -3 M 3 -6 M 6 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 6 3 M 6 6", //M
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -6 M 0 -3 M 3 0 M 6 6 M 6 3 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 6 -12", // N
            "-6 12 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 6 3 M 3 6 M 0 6 M -3 6", // O
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 -3 M -3 -3",// P
            "-6 12 M 0 6 M -3 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 6 -3 M 6 0 M 3 3 M 0 0 M 6 6", // Q
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -9 M 6 -6 M 3 -3 M 0 -3 M -3 -3 M 0 0 M 3 3 M 6 6",// R
            "-6 12 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 3 -3 M 0 -3 M -3 -3 M -6 -6 M -6 -9 M -3 -12 M 0 -12 M 3 -12 M 6 -9", // S
            "-6 12 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 3 M 0 6", // T
            "-6 12 M -6 -12 M -6 -9 M -6 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 6 -12", // U
            "-6 12 M -6 -12 M -6 -9 M -6 -6 M -3 -3 M -3 0 M 0 3 M 0 6 M 3 0 M 3 -3 M 6 -6 M 6 -9 M 6 -12", //  V
            "-6 12 M -6 -12 M -6 -9 M -6 -6 M -6 -3 M -6 0 M -6 3 M -6 6 M -3 3 M 0 0 M 3 3 M 6 6 M 6 3 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 6 -12", // W
            "-6 12 M -6 -12 M -6 -9 M -3 -6 M 0 -3 M 3 0 M 6 3 M 6 6 M -6 6 M -6 3 M -3 0 M 3 -6 M 6 -9 M 6 -12", // X
            "-6 12 M -6 -12 M -6 -9 M -3 -6 M 0 -3 M 0 0 M 0 3 M 0 6 M 3 -6 M 6 -9 M 6 -12", // Y
            "-6 12 M -6 -12 M -3 -12 M 0 -12 M 3 -12 M 6 -12 M 6 -9 M 3 -6 M 0 -3 M -3 0 M -6 3 M -6 6 M -3 6 M 0 6 M 3 6 M 6 6", // Z
            "-6 12 M 3 -12 M 0 -12 M -3 -12 M -3 -9 M -3 -6 M -3 -3 M -3 0 M -3 3 M -3 6 M 0 6 M 3 6", // [
            "-6 12 M -6 -12 M -6 -9 M -3 -6 M 0 -3 M 3 0 M 6 3 M 6 6", // \
            "-6 12 M -3 -12 M 0 -12 M 3 -12 M 3 -9 M 3 -6 M 3 -3 M 3 0 M 3 3 M 3 6 M 0 6 M -3 6", // ]
            "-6 12 M -6 -6 M -3 -9 M 0 -12 M 3 -9 M 6 -6", // ^ 
            "-6 12 M -6 6 M -3 6 M 0 6 M 3 6 M 6 6","-6 12 M -3 -12 M 0 -9 M 3 -6", // _`
            "-6 12 M -3 -6 M 0 -6 M 3 -6 M 6 -3 M 6 0 M 6 3 M 6 6 M 3 6 M 0 6 M -3 6 M -6 3 M -3 0 M 0 0 M 3 0", // a 
            "-6 12 M -6 -12 M -6 -9 M -6 -6 M -6 -3 M -6 0 M -6 3 M -6 6 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 6 -3 M 3 -6 M 0 -6 M -3 -3",// b
            "-6 12 M 3 -6 M 0 -6 M -3 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 3", //c
            "-6 12 M 3 -3 M 0 -6 M -3 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6 M 6 6 M 6 3 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 6 -12",//d
            "-6 12 M -3 0 M 0 0 M 3 0 M 6 0 M 6 -3 M 3 -6 M 0 -6 M -3 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 6",//e
            "-6 12 M -3 6 M -3 3 M -3 0 M -3 -3 M -3 -6 M -3 -9 M 0 -12 M 3 -12 M 6 -9 M -6 -6 M 0 -6",//f
            "-6 12 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 6 -3 M 6 -6 M 6 -9 M 3 -9 M 0 -9 M -3 -9 M -6 -6 M -6 -3 M -3 0 M 0 0 M 3 0", //g
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M -3 -3 M 0 -6 M 3 -6 M 6 -3 M 6 0 M 6 3 M 6 6",   //h
            "-6 12 M -3 6 M 0 6 M 3 6 M 0 3 M 0 0 M 0 -3 M 0 -6 M -3 -6 M 0 -12", //i
            "-6 12 M -6 3 M -3 6 M 0 6 M 3 3 M 3 0 M 3 -3 M 3 -6 M 0 -6 M 3 -12", //j
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -6 -9 M -6 -12 M 3 -6 M 0 -3 M -3 0 M 0 3 M 3 6", //k
            "-6 12 M -3 6 M 0 6 M 3 6 M 0 3 M 0 0 M 0 -3 M 0 -6 M 0 -9 M 0 -12 M -3 -12", //l
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -3 -6 M 0 -3 M 0 0 M 3 -6 M 6 -3 M 6 0 M 6 3 M 6 6", //m
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -3 -3 M 0 -6 M 0 -6 M 3 -6 M 6 -3 M 6 0 M 6 3 M 6 6", //n
            "-6 12 M 3 6 M 0 6 M -3 6 M -6 3 M -6 0 M -6 -3 M -3 -6 M 0 -6 M 3 -6 M 6 -3 M 6 0 M 6 3", //o
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -3 -6 M 0 -6 M 3 -6 M 6 -3 M 3 0 M 0 0 M -3 0", //p
            "-6 12 M 3 0 M 0 0 M -3 0 M -6 -3 M -3 -6 M 0 -6 M 3 -3 M 6 -6 M 6 -3 M 6 0 M 6 3 M 6 6", //q
            "-6 12 M -6 6 M -6 3 M -6 0 M -6 -3 M -6 -6 M -3 -3 M 0 -6 M 3 -6 M 6 -3", //r
            "-6 12 M -6 6 M -3 6 M 0 6 M 3 6 M 6 3 M 3 0 M 0 0 M -3 0 M -6 -3 M -3 -6 M 0 -6 M 3 -6", //s
            "-6 12 M -6 -6 M -3 -6 M 0 -6 M -3 -12 M -3 -9 M -3 -3 M -3 0 M -3 3 M 0 6 M 3 6 M 6 3", //t
            "-6 12 M -6 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 6 M 3 3 M 6 6 M 6 3 M 6 0 M 6 -3 M 6 -6", //u
            "-6 12 M -6 -6 M -6 -3 M -6 0 M M -3 3 M 0 6 M 3 3 M 6 0 M 6 -3 M 6 -6", //v
            "-6 12 M -6 -6 M -6 -3 M -6 0 M -6 3 M -3 6 M 0 3 M 0 0 M 3 6 M 6 3 M 6 0 M 6 -3 M 6 -6", //w
            "-6 12 M -6 -6 M -3 -3 M 0 0 M 3 3 M 6 6 M -6 6 M -3 3 M 3 -3 M 6 -6", //x
            "-6 12 M -6 -6 M -6 -3 M -3 0 M 0 0 M 3 0 M -3 6 M 0 6 M 3 6 M 6 3 M 6 0 M 6 -3 M 6 -6", //y
            "-6 12 M -6 -6 M -3 -6 M 0 -6 M 3 -6 M 6 -6 M 3-3 M 0 0 M -3 3 M -6 6 M -3 6 M 0 6 M 3 6 M 6 6",//z
            "-6 12 M 3 -12 M 0 -9 M 0 -6 M -3 -3 M 0 0 M 0 3 M 3 6","-6 12 M 0 -12 M 0 -9 M 0 -6 M 0 -3 M 0 0 M 0 3 M 0 6",//{|
            "-6 12 M -3 -12 M 0 -9 M 0 -6 M 3 -3 M 0 0 M 0 3 M -3 6","-6 12 M -6 -9 M -3 -12 M 0 -9 M 3 -6 M 6 -9", //}~
           } };
        #endregion

        public static string[] fontFileName()
        {
            if (Directory.Exists("fonts"))
                return Directory.GetFiles("fonts");
            return new string[0];
        }
        public static void reset()
        {
            gcFontName = "standard"; gcText = ""; gcFont = 0; gcAttachPoint = 7;
            gcHeight = 0; gcWidth = 0; gcAngle = 0; gcSpacing = 1; gcOffX = 0; gcOffY = 0;
            gcPauseLine = false; gcPauseWord = false; gcPauseChar = false; gcodePenIsUp = false;
            useLFF = false; gcLineDistance = 1.5; gcFontDistance = 0;
        }

        public static bool getCode(StringBuilder gcodeString)
        {
            double scale = gcHeight / 21;
            string tmp1 = gcText.Replace('\r', '|');
            gcodeString.AppendFormat("( Text: {0} )\r\n", tmp1.Replace('\n', ' '));
            string[] fileContent = new string[] { "" };

            string fileName = "";
            if (gcFontName.IndexOf(@"fonts\") >= 0)
                fileName = gcFontName;
            else
                fileName = @"fonts\" + gcFontName + ".lff";
            bool fontFound = false;
            if (gcFontName != "")
            {
                if (File.Exists(fileName))
                {
                    fileContent = File.ReadAllLines(fileName);
                    scale = gcHeight / 9;
                    useLFF = true;
                    offsetY = 0;
                    gcLineDistance = 1.667 * gcSpacing;
                    fontFound = true;
                    foreach (string line in fileContent)
                    {
                        if (line.IndexOf("LetterSpacing") >= 0)
                        {
                            string[] tmp = line.Split(':');
                            gcLetterSpacing = Convert.ToDouble(tmp[1].Trim());
                        }
                        if (line.IndexOf("WordSpacing") >= 0)
                        {
                            string[] tmp = line.Split(':');
                            gcWordSpacing = Convert.ToDouble(tmp[1].Trim());
                        }
                    }
                }
                else
                {
                    gcodeString.AppendFormat("( Font '{0}' not found )\r\n", gcFontName);
                    gcodeString.Append("( Using alternative font )\r\n");
                }
            }
            if ((gcAttachPoint == 2) || (gcAttachPoint == 5) || (gcAttachPoint == 8))
                gcOffX -= gcWidth / 2;
            if ((gcAttachPoint == 3) || (gcAttachPoint == 6) || (gcAttachPoint == 9))
                gcOffX -= gcWidth;
            if ((gcAttachPoint == 4) || (gcAttachPoint == 5) || (gcAttachPoint == 6))
                gcOffY -= gcHeight / 2;
            if ((gcAttachPoint == 1) || (gcAttachPoint == 2) || (gcAttachPoint == 3))
                gcOffY -= gcHeight;

            string[] lines;
            if (gcText.IndexOf("\\P") >= 0)
            { gcText = gcText.Replace("\\P", "\n"); }
            lines = gcText.Split('\n');
            offsetX = 0;
            offsetY = 9 * scale + ((double)lines.Length - 1) * gcHeight * gcLineDistance;// (double)nUDFontLine.Value;
            if (useLFF)
                offsetY = ((double)lines.Length - 1) * gcHeight * gcLineDistance;// (double)nUDFontLine.Value;

            for (int txtIndex = 0; txtIndex < gcText.Length; txtIndex++)
            {
                gcodePenUp(gcodeString);
                int chrIndex = (int)gcText[txtIndex] - 32;
                int chrIndexLFF = (int)gcText[txtIndex];

                if (gcText[txtIndex] == '\n')                   // next line
                {
                    offsetX = 0;
                    offsetY -= gcHeight * gcLineDistance;

                    if (gcPauseLine)
                    {
                        gcode.Pause(gcodeString, "Pause before line");
                    }
                }
                else if (useLFF)
                    drawCharLFF(gcodeString, ref fileContent, chrIndexLFF, scale); // regular char
                else
                {
                    if ((chrIndex < 0) || (chrIndex > 95))     // no valid char
                    {
                        offsetX += 2 * gcSpacing;                   // apply space
                        if (gcPauseWord)
                        {
                            gcode.Pause(gcodeString, "Pause before word");
                        }
                    }
                    else
                    {
                        gcodeString.AppendFormat("( Char: {0})\r\n", gcText[txtIndex]);
                        if (gcPauseChar)
                        {
                            gcode.Pause(gcodeString, "Pause before char");
                        }
                        if (gcPauseChar && (gcText[txtIndex] == ' '))
                        {
                            gcode.Pause(gcodeString, "Pause before word");
                        }
                        drawChar(gcodeString, fontChar[gcFont][chrIndex], scale); // regular char
                    }
                }
            }
            gcode.PenUp(gcodeString);
            return fontFound;
        }

        // http://forum.librecad.org/Some-questions-about-the-LFF-fonts-td5715159.html
        private static double drawCharLFF(StringBuilder gcodeString, ref string[] txtFont, int index, double scale, bool isCopy = false)
        {
            int lineIndex = 0;
            double maxX = 0;
            char[] charsToTrim = { '#' };

            if (index <= 32)
            {
                offsetX += gcWordSpacing * scale;
                if (gcPauseWord)
                {
                    gcode.Pause(gcodeString, "Pause before word");
                }
            }
            else
            {
                for (lineIndex = 0; lineIndex < txtFont.Length; lineIndex++)
                {
                    if ((txtFont[lineIndex].Length > 0) && (txtFont[lineIndex][0] == '['))
                    {
                        string nrString = txtFont[lineIndex].Substring(1, txtFont[lineIndex].IndexOf(']') - 1);
                        nrString = nrString.Trim('#');// charsToTrim);
                        int chrIndex = 0;
                        try
                        {
                            chrIndex = Convert.ToInt32(nrString, 16);
                        }
                        catch { MessageBox.Show("Line " + txtFont[lineIndex] + "  Found " + nrString); }
                        if (chrIndex == index)                                              // found char
                        {
                            charXOld = -1; charYOld = -1;
                            if (gcPauseChar)
                                gcode.Pause(gcodeString, "Pause before char");

                            int pathIndex;
                            for (pathIndex = lineIndex + 1; pathIndex < txtFont.Length; pathIndex++)
                            {
                                if (txtFont[pathIndex].Length < 2)
                                    break;
                                if (txtFont[pathIndex][0] == 'C')       // copy other char first
                                {
                                    int copyIndex = Convert.ToInt16(txtFont[pathIndex].Substring(1, 4), 16);
                                    maxX = drawCharLFF(gcodeString, ref txtFont, copyIndex, scale, true);
                                }
                                else
                                    maxX = Math.Max(maxX, drawPathLFF(gcodeString, txtFont[pathIndex], offsetX + gcOffX, offsetY + gcOffY, scale));
                            }
                            break;
                        }
                    }
                }
                if (!isCopy)
                    offsetX += maxX + gcLetterSpacing * scale + gcFontDistance; ;// + (double)nUDFontDistance.Value;
            }
            return maxX;
        }

        private static double charX, charY, charXOld = 0, charYOld = 0;
        private static double drawPathLFF(StringBuilder gcodeString, string txtPath, double offX, double offY, double scale)
        {
            string[] points = txtPath.Split(';');
            int cnt = 0;
            double xx, yy, x, y, xOld = 0, yOld = 0, bulge = 0, maxX = 0;
            foreach (string point in points)
            {
                string[] scoord = point.Split(',');
                charX = Convert.ToDouble(scoord[0]);
                xx = charX * scale;
                maxX = Math.Max(maxX, xx);
                xx += offX;
                charY = Convert.ToDouble(scoord[1]);
                yy = charY * scale + offY;
                if (gcAngle == 0)
                { x = xx; y = yy; }
                else
                {
                    x = xx * Math.Cos(gcAngle * Math.PI / 180) - yy * Math.Sin(gcAngle * Math.PI / 180);
                    y = xx * Math.Sin(gcAngle * Math.PI / 180) + yy * Math.Cos(gcAngle * Math.PI / 180);
                }

                if (scoord.Length > 2)
                {
                    if (scoord[2].IndexOf('A') >= 0)
                        bulge = Convert.ToDouble(scoord[2].Substring(1));
                    //AddRoundCorner(gcodeString, bulge, xOld + offX, yOld + offY, x + offX, y + offY);
                    AddRoundCorner(gcodeString, bulge, xOld, yOld, x, y);
                }
                else if (cnt == 0)
                {
                    if ((charX != charXOld) || (charY != charYOld))
                    {
                        gcodePenUp(gcodeString);
                        //gcodeMove(gcodeString, 0, (float)(x + offX), (float)(y + offY));
                        gcodeMove(gcodeString, 0, (float)x, (float)y);
                        gcodePenDown(gcodeString);
                    }
                }
                else
                    gcodeMove(gcodeString, 1, (float)x, (float)y);
                //gcodeMove(gcodeString, 1, (float)(x + offX), (float)(y + offY));
                cnt++;
                xOld = x; yOld = y;
                charXOld = charX; charYOld = charY;
            }
            return maxX;
        }

        // break down path of a single char into pieces
        private static void drawChar(StringBuilder gcodeString, string svgtxt, double scale)
        {
            string separators = @"(?=[A-Za-z])";
            var tokens = Regex.Split(svgtxt, separators).Where(t => !string.IsNullOrEmpty(t));
            string[] svgsplit = svgtxt.Split(' ');
            offsetX -= double.Parse(svgsplit[0]) * scale;
            //var offY = double.Parse(svgsplit[1]) * scale;
            foreach (string token in tokens)
            { drawPath(gcodeString, token, offsetX + gcOffX, offsetY + gcOffY, (float)scale); }
            offsetX += double.Parse(svgsplit[1]) * scale + gcFontDistance;
        }

        // draw a piece of the path
        private static void drawPath(StringBuilder gcodeString, string svgPath, double offX, double offY, float scale)
        {
            var cmd = svgPath.Take(1).Single();
            string remainingargs = svgPath.Substring(1);
            string argSeparators = @"[\s,]|(?=-)";
            var splitArgs = Regex
                .Split(remainingargs, argSeparators)
                .Where(t => !string.IsNullOrEmpty(t));

            float[] floatArgs = splitArgs.Select(arg => float.Parse(arg.Replace('.', ',')) * scale).ToArray();
            if (cmd == 'M')
            {
                gcodePenUp(gcodeString);
                for (int i = 0; i < floatArgs.Length; i += 2)
                { gcodeMove(gcodeString, 0, floatArgs[i] + (float)offX, (float)offY - floatArgs[i + 1]); }
                gcodePenDown(gcodeString);
            }
            if (cmd == 'L')
            {
                for (int i = 0; i < floatArgs.Length; i += 2)
                { gcodeMove(gcodeString, 1, floatArgs[i] + (float)offX, (float)offY - floatArgs[i + 1]); }
            }
        }

        private static void gcodeMove(StringBuilder gcodeString, int gnr, float x, float y, string cmd = "")
        {
            if (gnr == 0)
                gcode.MoveToRapid(gcodeString, x, y, cmd);
            else
                gcode.MoveTo(gcodeString, x, y, cmd);
        }

        private static void gcodePenDown(StringBuilder gcodeString)
        {
            gcode.PenDown(gcodeString, "");
            gcodePenIsUp = false;
        }
        private static void gcodePenUp(StringBuilder gcodeString)
        {
            if (!gcodePenIsUp)
                gcode.PenUp(gcodeString, "");
            gcodePenIsUp = true;
        }

        private static void AddRoundCorner(StringBuilder gcodeString, double bulge, double p1x, double p1y, double p2x, double p2y)
        {
            //Definition of bulge, from Autodesk DXF fileformat specs
            double angle = Math.Abs(Math.Atan(bulge) * 4);
            bool girou = false;

            //For my method, this angle should always be less than 180. 
            if (angle > Math.PI)
            {
                angle = Math.PI * 2 - angle;
                girou = true;
            }

            //Distance between the two vertexes, the angle between Center-P1 and P1-P2 and the arc radius
            double distance = Math.Sqrt(Math.Pow(p1x - p2x, 2) + Math.Pow(p1y - p2y, 2));
            double alpha = (Math.PI - angle) / 2;
            double ratio = distance * Math.Sin(alpha) / Math.Sin(angle);
            if (angle == Math.PI)
                ratio = distance / 2;

            double xc, yc, direction;

            //Used to invert the signal of the calculations below
            if (bulge < 0)
                direction = 1;
            else
                direction = -1;

            //calculate the arc center
            double part = Math.Sqrt(Math.Pow(2 * ratio / distance, 2) - 1);

            if (!girou)
            {
                xc = ((p1x + p2x) / 2) - direction * ((p1y - p2y) / 2) * part;
                yc = ((p1y + p2y) / 2) + direction * ((p1x - p2x) / 2) * part;
            }
            else
            {
                xc = ((p1x + p2x) / 2) + direction * ((p1y - p2y) / 2) * part;
                yc = ((p1y + p2y) / 2) - direction * ((p1x - p2x) / 2) * part;
            }

            string cmt = "";
            //            if (dxfComments) { cmt = "Bulge " + bulge.ToString(); }
            if (bulge > 0)
                gcode.Arc(gcodeString, 3, (float)p2x, (float)p2y, (float)(xc - p1x), (float)(yc - p1y), cmt);
            else
                gcode.Arc(gcodeString, 2, (float)p2x, (float)p2y, (float)(xc - p1x), (float)(yc - p1y), cmt);
        }

        #endregion

        #region Métodos para la conversión del archivo de la clase GCodeFromImage

        private void getSettings()
        {
            svgToolIndex = svgPalette.init();       // svgPalette.cs
        }
                
        private static string lastFile = "";
       
        public void loadExtern(string file)
        {
            if (!File.Exists(file)) return;
            lastFile = file;
            loadedImage = new Bitmap(Image.FromFile(file));
            originalImage = new Bitmap(Image.FromFile(file));
            processLoading();
        }
                
        public void loadClipboard()
        {
            IDataObject iData = Clipboard.GetDataObject();
            if (iData.GetDataPresent(DataFormats.Bitmap))
            {
                lastFile = "";
                loadedImage = new Bitmap(Clipboard.GetImage());
                originalImage = new Bitmap(Clipboard.GetImage());
                processLoading();
            }
        }

        public void loadURL(string url)
        {
            pictureBox1.Load(url);
            originalImage = new Bitmap(pictureBox1.Image);
        }

        private void processLoading()
        {
            lblStatus.Text = "Opening file...";
            Refresh();
            tBarBrightness.Value = 0;
            tBarContrast.Value = 0;
            tBarGamma.Value = 100;
            lblBrightness.Text = Convert.ToString(tBarBrightness.Value);
            lblContrast.Text = Convert.ToString(tBarContrast.Value);
            lblGamma.Text = Convert.ToString(tBarGamma.Value / 100.0f);
            if (cbGrayscale.Checked) originalImage = imgGrayscale(originalImage);
            adjustedImage = new Bitmap(originalImage);
            resultImage = new Bitmap(originalImage);
            ratio = (originalImage.Width + 0.0f) / originalImage.Height;//Save ratio for future use if needled
            if (cbLockRatio.Checked) nUDHeight.Value = nUDWidth.Value / (decimal)ratio; //Initialize y size
            userAdjust();
            lblStatus.Text = "Done";
            getSettings();
        }

        float ratio; //Used to lock the aspect ratio when the option is selected

        //Interpolate a 8 bit grayscale value (0-255) between min,max
        private Int32 interpolate(Int32 grayValue, Int32 min, Int32 max)
        {
            Int32 dif = max - min;
            return (min + ((grayValue * dif) / 255));
        }


        //Apply dirthering to an image (Convert to 1 bit)
        private Bitmap imgDirther(Bitmap input)
        {
            lblStatus.Text = "Dirthering...";
            Refresh();
            var masks = new byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };
            var output = new Bitmap(input.Width, input.Height, PixelFormat.Format1bppIndexed);
            var data = new sbyte[input.Width, input.Height];
            var inputData = input.LockBits(new Rectangle(0, 0, input.Width, input.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var scanLine = inputData.Scan0;
                var line = new byte[inputData.Stride];
                for (var y = 0; y < inputData.Height; y++, scanLine += inputData.Stride)
                {
                    Marshal.Copy(scanLine, line, 0, line.Length);
                    for (var x = 0; x < input.Width; x++)
                    {
                        data[x, y] = (sbyte)(64 * (GetGreyLevel(line[x * 3 + 2], line[x * 3 + 1], line[x * 3 + 0]) - 0.5));
                    }
                }
            }
            finally
            {
                input.UnlockBits(inputData);
            }
            var outputData = output.LockBits(new Rectangle(0, 0, output.Width, output.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            try
            {
                var scanLine = outputData.Scan0;
                for (var y = 0; y < outputData.Height; y++, scanLine += outputData.Stride)
                {
                    var line = new byte[outputData.Stride];
                    for (var x = 0; x < input.Width; x++)
                    {
                        var j = data[x, y] > 0;
                        if (j) line[x / 8] |= masks[x % 8];
                        var error = (sbyte)(data[x, y] - (j ? 32 : -32));
                        if (x < input.Width - 1) data[x + 1, y] += (sbyte)(7 * error / 16);
                        if (y < input.Height - 1)
                        {
                            if (x > 0) data[x - 1, y + 1] += (sbyte)(3 * error / 16);
                            data[x, y + 1] += (sbyte)(5 * error / 16);
                            if (x < input.Width - 1) data[x + 1, y + 1] += (sbyte)(1 * error / 16);
                        }
                    }
                    Marshal.Copy(line, 0, scanLine, outputData.Stride);
                }
            }
            finally
            {
                output.UnlockBits(outputData);
            }
            lblStatus.Text = "Done";
            Refresh();
            return (output);
        }
        private static double GetGreyLevel(byte r, byte g, byte b)//aux for dirthering
        {
            return (r * 0.299 + g * 0.587 + b * 0.114) / 255;
        }
        //Adjust brightness contrast and gamma of an image      
        private Bitmap imgBalance(Bitmap img, int brigh, int cont, int gam)
        {
            lblStatus.Text = "Balancing...";
            Refresh();
            ImageAttributes imageAttributes;
            float brightness = (brigh / 100.0f) + 1.0f;
            float contrast = (cont / 100.0f) + 1.0f;
            float gamma = 1 / (gam / 100.0f);
            float adjustedBrightness = brightness - 1.0f;
            Bitmap output;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
            new float[] {contrast, 0, 0, 0, 0}, // scale red
            new float[] {0, contrast, 0, 0, 0}, // scale green
            new float[] {0, 0, contrast, 0, 0}, // scale blue
            new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
            new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            output = new Bitmap(img);
            imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(output);
            g.DrawImage(output, new Rectangle(0, 0, output.Width, output.Height)
            , 0, 0, output.Width, output.Height,
            GraphicsUnit.Pixel, imageAttributes);
            lblStatus.Text = "Done";
            Refresh();
            return (output);
        }
        //Return a grayscale version of an image
        private Bitmap imgGrayscale(Bitmap original)
        {
            lblStatus.Text = "Grayscaling...";
            Refresh();
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);//create a blank bitmap the same size as original
            Graphics g = Graphics.FromImage(newBitmap);//get a graphics object from the new image
            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
                {
                    new float[] {.299f, .299f, .299f, 0, 0},
                    new float[] {.587f, .587f, .587f, 0, 0},
                    new float[] {.114f, .114f, .114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
                });
            ImageAttributes attributes = new ImageAttributes();//create some image attributes
            attributes.SetColorMatrix(colorMatrix);//set the color matrix attribute

            //draw the original image on the new image using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();//dispose the Graphics object
            lblStatus.Text = "Done";
            Refresh();
            return (newBitmap);
        }
        //Return a inverted colors version of a image
        private Bitmap imgInvert(Bitmap original)
        {
            lblStatus.Text = "Inverting...";
            Refresh();
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);//create a blank bitmap the same size as original
            Graphics g = Graphics.FromImage(newBitmap);//get a graphics object from the new image
            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
                {
                    new float[] {-1, 0, 0, 0, 0},
                    new float[] {0, -1, 0, 0, 0},
                    new float[] {0, 0, -1, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {1, 1, 1, 0, 1}
                });
            ImageAttributes attributes = new ImageAttributes();//create some image attributes
            attributes.SetColorMatrix(colorMatrix);//set the color matrix attribute

            //draw the original image on the new image using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            g.Dispose();//dispose the Graphics object
            lblStatus.Text = "Done";
            Refresh();
            return (newBitmap);
        }

        // Resize image to desired width/height for gcode generation
        //  http://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        public static Bitmap imgResize(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.AssumeLinear;//.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;// HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.None;//.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.None;//.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        //Invoked when the user input any value for image adjust
        private void userAdjust()
        {
            try
            {
                if (adjustedImage == null) return;//if no image, do nothing
                //Apply resize to original image
                Int32 xSize;//Total X pixels of resulting image for GCode generation
                Int32 ySize;//Total Y pixels of resulting image for GCode generation
                xSize = (int)(nUDWidth.Value / nUDReso.Value);
                ySize = (int)(nUDHeight.Value / nUDReso.Value); //Convert.ToInt32(float.Parse(tbHeight.Text, CultureInfo.InvariantCulture.NumberFormat) / float.Parse(tbRes.Text, CultureInfo.InvariantCulture.NumberFormat));
                adjustedImage = imgResize(originalImage, xSize, ySize);
                //Apply balance to adjusted (resized) image
                adjustedImage = imgBalance(adjustedImage, tBarBrightness.Value, tBarContrast.Value, tBarGamma.Value);
                //Reset dirthering to adjusted (resized and balanced) image
                //              cbDirthering.Text = "GrayScale 8 bit";
                //Display image
                if (rbModeDither.Checked)// cbDirthering.Text == "Dirthering FS 1 bit")
                {
                    lblStatus.Text = "Dirtering...";
                    adjustedImage = imgDirther(adjustedImage);
                    pictureBox1.Image = adjustedImage;
                    lblStatus.Text = "Done";
                }
                else
                    pictureBox1.Image = adjustedImage;
                lblColors.Text = "Colors:";
                updateLabelColor = true;
                resultImage = new Bitmap(adjustedImage);

            }
            catch (Exception e)
            {
                MessageBox.Show("Error resizing/balancing image: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool updateLabelColor = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (updateLabelColor)
                countImageColors();
        }
        private void countImageColors()
        {
            Dictionary<Color, int> colors = new Dictionary<Color, int>();
            Color newcol;
            for (int y = 0; y < adjustedImage.Height - 1; y++)
            {
                for (int x = 0; x < adjustedImage.Width - 1; x++)
                {
                    newcol = adjustedImage.GetPixel(x, y);
                    if (colors.ContainsKey(newcol))
                        colors[newcol] = colors[newcol] + 1;
                    else
                        colors.Add(newcol, 1);
                }
            }
            lblColors.Text = "Colors: " + colors.Count.ToString();
            updateLabelColor = false;
        }

        //Contrast adjusted by user
        private void tBarContrast_Scroll(object sender, EventArgs e)
        {
            lblContrast.Text = Convert.ToString(tBarContrast.Value);
            Refresh();
            userAdjust();
        }
        //Brightness adjusted by user
        private void tBarBrightness_Scroll(object sender, EventArgs e)
        {
            lblBrightness.Text = Convert.ToString(tBarBrightness.Value);
            Refresh();
            userAdjust();
        }
        //Gamma adjusted by user
        private void tBarGamma_Scroll(object sender, EventArgs e)
        {
            lblGamma.Text = Convert.ToString(tBarGamma.Value / 100.0f);
            Refresh();
            userAdjust();
        }
        //Quick preview of the original image. Todo: use a new image container for fas return to processed image
        private void btnCheckOrig_MouseDown(object sender, MouseEventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            getSettings();
            usedColors = new string[svgToolIndex + 1];
            countColors = new int[svgToolIndex + 1];
            Array.Resize<string>(ref usedColors, svgToolIndex + 1);     //usedColors = string[svgToolIndex + 1];
            Array.Resize<int>(ref countColors, svgToolIndex + 1);     //countColors = new int[svgToolIndex + 1];
            for (int i = 0; i <= svgToolIndex; i++)
            { countColors[i] = 0; usedColors[i] = ""; }    // usedColors[i] = "";
            generateResultImage();
            pictureBox1.Image = resultImage;
        }
        //Reload the processed image after temporal preiew of the original image
        private void btnCheckOrig_MouseUp(object sender, MouseEventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            pictureBox1.Image = adjustedImage;
        }

        //CheckBox lockAspectRatio checked. Set as mandatory the user setted width and calculate the height by using the original aspect ratio
        private void cbLockRatio_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLockRatio.Checked)
            {
                nUDHeight.Value = nUDWidth.Value / (decimal)ratio; //Initialize y size
                if (adjustedImage == null) return;//if no image, do nothing
                userAdjust();
            }
        }
        //On form load
        private void Form1_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Done";
            getSettings();
            autoZoomToolStripMenuItem_Click(this, null);//Set preview zoom mode
        }

        //Generate a "minimalist" gcode line based on the actual and last coordinates and laser power
        float coordX;//X
        float coordY;//Y
        float lastX;//Last x/y  coords for compare
        float lastY;

        // colorMap will store x-start; x-stop values for each color and line(y)
        private static Dictionary<int, List<int>[]> colorMap = new Dictionary<int, List<int>[]>();
        private static int colorStart = -2, colorEnd = -2, lastTool = -2, lastLine = -2;
        int myToolNumber;
        private void setColorMap(int col, int line, int direction)
        {
            Color myColor = adjustedImage.GetPixel(col, (adjustedImage.Height - 1) - line);  //Get pixel color
            myToolNumber = svgPalette.getToolNr(myColor, (int)nUDMode.Value);     // find nearest color in palette
            svgPalette.countPixel();
            if (((cbExceptAlpha.Checked) && (myColor.A == 0)) || (myToolNumber < 0))
                myToolNumber = -1;

            if ((myToolNumber >= 0) && (!colorMap.ContainsKey(myToolNumber)))
            {
                colorMap.Add(myToolNumber, new List<int>[adjustedImage.Height]);
                for (int i = 0; i < adjustedImage.Height; i++)
                { colorMap[myToolNumber][i] = new List<int>(); }
            }

            if (lastTool != myToolNumber)
            {
                if (lastTool >= 0)
                    colorMap[lastTool][line].Add(colorEnd);// -direction);     // finish old color
                colorStart = col;
                colorEnd = col;
                if (myToolNumber >= 0)
                    colorMap[myToolNumber][line].Add(colorStart);   // start new color
            }
            else
            {
                if ((lastLine != line) && (lastLine >= 0) && (myToolNumber >= 0))
                {
                    colorMap[myToolNumber][lastLine].Add(colorEnd);// -direction); // finish old line
                    colorStart = col;
                    colorMap[myToolNumber][line].Add(colorStart);   // start new line
                }
                colorEnd = col;
            }
            lastTool = myToolNumber;
            lastLine = line;
        }

        private void convertColorMap(float resol)
        {
            int tool, skipTooNr = 0;
            int key;
            gcode.MoveToRapid(finalString, 0, 0);          // move to start pos
            for (int index = 0; index < svgToolIndex; index++)  // go through sorted by pixel-amount list
            {
                svgPalette.setIndex(index);                     // set index in class
                key = svgPalette.indexToolNr();                 // if tool-nr == known key go on
                if (colorMap.ContainsKey(key))
                {
                    tool = svgPalette.indexToolNr();            // use tool in palette order
                    if (cbSkipToolOrder.Checked)
                        tool = skipTooNr++;                     // or start tool-nr at 0

                    finalString.AppendLine("\r\n( +++++ Tool change +++++ )");
                    gcode.Tool(finalString, tool, svgPalette.indexName());  // + svgPalette.pixelCount());
                    for (int y = 0; y < adjustedImage.Height; y++)
                    {
                        while (colorMap[key][y].Count > 1)          // stat at line 0 and check line by line
                            drawColorMap(resol, key, y, 0, true);
                    }
                }
            }
        }

        // check recursive line by line for same color near by given x-value
        private void drawColorMap(float resol, int color, int line, int startIndex, bool first)
        {
            int start, stop, newIndex;
            float coordy = resol * (float)line;
            if (colorMap[color][line].Count > 1)
            {
                if ((startIndex % 2 == 0) && ((startIndex + 1) < colorMap[color][line].Count)) //////
                {
                    start = colorMap[color][line][startIndex];
                    stop = colorMap[color][line][startIndex + 1];
                    colorMap[color][line].RemoveRange(startIndex, 2);
                }
                else
                {
                    start = colorMap[color][line][startIndex];
                    stop = colorMap[color][line][startIndex - 1];
                    colorMap[color][line].RemoveRange(startIndex - 1, 2);
                }
                float coordX = resol * (float)start;
                if (first)
                {
                    gcode.MoveToRapid(finalString, coordX, coordy);          // move to start pos
                    gcode.PenDown(finalString);
                }
                else
                    gcode.MoveTo(finalString, coordX, coordy);          // move to start pos
                coordX = resol * (float)stop;
                gcode.MoveTo(finalString, coordX, coordy);              // move to end pos

                if (line < (adjustedImage.Height - 1))
                {
                    var nextLine = colorMap[color][line + 1];   // check for pixel nearby
                    bool end = true;
                    for (int k = -1; k <= 1; k++)
                    {   // check recursive line by line for same color near by given x-value
                        newIndex = nextLine.IndexOf(stop + k);
                        if (newIndex >= 0)
                        {
                            drawColorMap(resol, color, line + 1, newIndex, false);
                            end = false;
                            break;
                        }
                    }
                    if (end)
                        gcode.PenUp(finalString);
                }
                else
                    gcode.PenUp(finalString);
            }
        }

        private static string debugColorMap()
        {
            string temp = "";
            int i = 0;
            foreach (var pair in colorMap)
            {
                i = 0;
                temp += "\r\n\r\n NEW COLOR\r\n";
                foreach (var y in pair.Value)
                {
                    temp += pair.Key + " " + i.ToString() + "  ";
                    foreach (int pixel in y)
                    {
                        temp += pixel.ToString() + "|";
                    }
                    temp += "\r\n"; i++;
                }
            }
            return temp;
        }


        private void drawHeight(int col, int lin, float coordX, float coordY)
        {
            Color myColor = adjustedImage.GetPixel(col, (adjustedImage.Height - 1) - lin);          // Get pixel color
            double height = 255 - Math.Round((double)(myColor.R + myColor.G + myColor.B) / 3);  // calc height
            float coordZ = (float)((double)nUDZTop.Value - height * (double)(nUDZTop.Value - nUDZBot.Value) / 255);    // calc Z value
            string feed = string.Format("F{0}", gcode.gcodeXYFeed); ;
            gcode.MoveTo(finalString, coordX, coordY, coordZ, "");
        }


        private int lastToolNumber = -1;
        private float lastDrawX, lastDrawY;
        bool lastIfBackground = false;
        private void drawPixel(int col, int lin, float coordX, float coordY, int edge, int dir)
        {
            Color myColor = adjustedImage.GetPixel(col, (adjustedImage.Height - 1) - lin);  //Get pixel color
            int myToolNumber = svgPalette.getToolNr(myColor, (int)nUDMode.Value);     // find nearest color in palette
            svgPalette.countPixel();
            bool ifBackground = false;
            float myX = coordX;
            float myY = coordY;

            if (((cbExceptAlpha.Checked) && (myColor.A == 0)) || (myToolNumber < 0))
            {
                ifBackground = true;
                myToolNumber = -1;
                svgPalette.setUse(false);
            }
            else
                svgPalette.setUse(true);

            if (edge == 0)
            {
                if (dir == -1) myX += (float)nUDReso.Value;
                if (dir == -2) myX += (float)nUDReso.Value;
                if (dir == 2) myY += (float)nUDReso.Value;
            }
            if ((lastToolNumber != myToolNumber) || (edge > 0))
            {
                if ((edge != 1) && !lastIfBackground)
                { lineEnd(myX, myY); }
                if (myToolNumber >= 0) gcodeStringIndex = myToolNumber;
                if ((edge != 2) && !ifBackground)
                { lineStart(myX, myY); }
                lastDrawX = coordX;
                lastDrawY = coordY;
            }

            lastToolNumber = myToolNumber;
            lastX = coordX; lastY = coordY;
            lastIfBackground = ifBackground;
        }
        private void lineEnd(float x, float y, string txt = "")   // finish line with old pen
        {
            gcode.MoveTo(tmpString, x, y, txt);          // move to end pos
            gcode.PenUp(tmpString);                             // pen up
            gcodeString[gcodeStringIndex].Append(tmpString);    // take over code if...
            tmpString.Clear();
        }
        private void lineStart(float x, float y, string txt = "")
        {
            gcode.MoveToRapid(tmpString, x, y, txt);         // rapid move to start pos
            gcode.PenDown(tmpString);                           // pen down
        }

        //Generate button click
        public void btnGenerate_Click(object sender, EventArgs e)
        {
            getSettings();
            colorMap.Clear();
            if (cbExceptColor.Checked)
                svgPalette.setExceptionColor(cbExceptColor.BackColor);
            else
                svgPalette.clrExceptionColor();

            gcodeStringIndex = 0;
            for (int i = 0; i < svgToolMax; i++)
            {
                gcodeString[i] = new StringBuilder();
                gcodeString[i].Clear();
            }
            finalString.Clear();
            gcode.setup();

            if (rBProcessZ.Checked)
            {
                generateHeightData();
                return;
            }//            gcodeString.Clear();
            if (adjustedImage == null) return;  //if no image, do nothing
            float resol = (float)nUDReso.Value;
            float w = (float)nUDWidth.Value;
            float h = (float)nUDHeight.Value;

            if ((resol <= 0) | (adjustedImage.Width < 1) | (adjustedImage.Height < 1) | (w < 1) | (h < 1))
            {
                MessageBox.Show("Check widht, height and resolution values.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Int32 lin;//top/botom pixel
            Int32 col;//Left/right pixel

            lblStatus.Text = "Generating file...";
            Refresh();
            //Generate picture Gcode
            Int32 pixTot = adjustedImage.Width * adjustedImage.Height;
            Int32 pixBurned = 0;
            int edge = 0;
            int direction = 0;
            if (!gcodeSpindleToggle) gcode.SpindleOn(finalString, "Start spindle");
            gcode.PenUp(finalString);                             // pen up
            //            colorMap= new Dictionary<int, new List<int>[adjustedImage.Width]>();
            //////////////////////////////////////////////
            // Generate Gcode lines by Horozontal scanning
            //////////////////////////////////////////////
            #region horizontal and setColorMap
            if (rbEngravingPattern1.Checked)
            {
                //Start image
                lin = adjustedImage.Height - 1;//top tile
                col = 0;//Left pixel
                tmpString.Clear();
                lastX = 0;//reset last positions
                lastY = resol * (float)lin;
                while (lin >= 0)
                {
                    //Y coordinate
                    coordY = resol * (float)lin;
                    edge = 1;                           // line starts
                    direction = 1;                      // left to right
                    while (col < adjustedImage.Width)   //From left to right
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        setColorMap(col, lin, direction);
                        //       drawPixel(col,lin,coordX, coordY,edge,dir);
                        edge = 0;
                        pixBurned++;
                        col++;
                        if (col >= adjustedImage.Width - 1) edge = 2; // line ends
                    }
                    col--;
                    lin--;
                    coordY = resol * (float)lin;
                    edge = 1;                           // line starts
                    direction = -1;                     // right to left
                    while ((col >= 0) & (lin >= 0))     //From right to left
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        setColorMap(col, lin, direction);
                        //      drawPixel(col, lin, coordX, coordY,edge,dir);
                        edge = 0;
                        pixBurned++;
                        col--;
                        if (col <= 0) edge = 2;         // line ends
                    }
                    col++;
                    lin--;
                    lblStatus.Text = "Generating GCode... " + Convert.ToString((pixBurned * 100) / pixTot) + "%";
                    Refresh();
                }
                if (myToolNumber >= 0)
                    colorMap[myToolNumber][0].Add(colorEnd);
            }
            #endregion
            //////////////////////////////////////////////
            // Generate Gcode lines by Diagonal scanning
            //////////////////////////////////////////////
            #region diagonal and  drawPixel
            else
            {
                //Start image
                col = 0;
                lin = 0;
                lastX = 0;//reset last positions
                lastY = 0;
                while ((col < adjustedImage.Width) | (lin < adjustedImage.Height))
                {
                    edge = 1;
                    direction = 2;    // up-left to low-right

                    while ((col < adjustedImage.Width) & (lin >= 0))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        drawPixel(col, lin, coordX, coordY, edge, direction);
                        edge = 0;
                        pixBurned++;
                        col++;
                        lin--;
                        if ((col >= adjustedImage.Width - 1) || (lin <= 0)) edge = 2;  //&& (lin == 0)
                    }
                    col--;
                    lin++;

                    if (col >= adjustedImage.Width - 1) lin++;
                    else col++;
                    edge = 1;
                    direction = -2;    // low-right to up-left 
                    while ((col >= 0) & (lin < adjustedImage.Height))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        drawPixel(col, lin, coordX, coordY, edge, direction);
                        edge = 0;
                        pixBurned++;
                        col--;
                        lin++;
                        if ((col <= 0) || (lin >= adjustedImage.Height - 1)) edge = 2;  //&& (lin >= adjustedImage.Height-1)
                    }
                    col++;
                    lin--;
                    if (lin >= adjustedImage.Height - 1) col++;
                    else lin++;
                    lblStatus.Text = "Generating GCode... " + Convert.ToString((pixBurned * 100) / pixTot) + "%";
                    Refresh();
                }


            }
            #endregion

            Refresh();
            lblStatus.Text = "Done (" + Convert.ToString(pixBurned) + "/" + Convert.ToString(pixTot) + ")";
            imagegcode = "( Generated by GRBL-Plotter )\r\n";

            int arrayIndex;
            int skipTooNr = 0;
            int tool = 0;

            svgPalette.sortByPixelCount();    // sort by color area (max. first)

            if (!rbEngravingPattern1.Checked)   // diagonal
            {
                for (int i = 0; i < svgToolIndex; i++)
                {
                    svgPalette.setIndex(i);
                    arrayIndex = svgPalette.indexToolNr();
                    if ((arrayIndex >= 0) && (gcodeString[arrayIndex].Length > 1))
                    {
                        if ((gcodeToolChange) && svgPalette.indexUse())
                        {
                            tool = svgPalette.indexToolNr();
                            if (cbSkipToolOrder.Checked)
                                tool = skipTooNr++;
                            finalString.AppendLine("\r\n( +++++ Tool change +++++ )");
                            gcode.Tool(finalString, tool, svgPalette.indexName());  // + svgPalette.pixelCount());
                        }
                        finalString.Append(gcodeString[svgPalette.indexToolNr()]);
                    }
                }

                if (!gcodeSpindleToggle) gcode.SpindleOff(finalString, "Stop spindle");
                imagegcode += gcode.GetHeader("Image import") + finalString.Replace(',', '.').ToString() + gcode.GetFooter();
            }
            else
            {               // horizontal
                gcode.reduceGCode = true;
                convertColorMap(resol);
                if (!gcodeSpindleToggle) gcode.SpindleOff(finalString, "Stop spindle");
                imagegcode += gcode.GetHeader("Image import") + finalString.Replace(',', '.').ToString() + gcode.GetFooter();
                //     imagegcode += debugColorMap();
            }
        }

        private void generateHeightData()
        {
            getSettings();
            finalString.Clear();
            gcode.setup();
            gcode.reduceGCode = cBCompress.Checked;

            if (adjustedImage == null) return;  //if no image, do nothing
            float resol = (float)nUDReso.Value;
            float w = (float)nUDWidth.Value;
            float h = (float)nUDHeight.Value;

            if ((resol <= 0) | (adjustedImage.Width < 1) | (adjustedImage.Height < 1) | (w < 1) | (h < 1))
            {
                MessageBox.Show("Check widht, height and resolution values.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Int32 lin;//top/botom pixel
            Int32 col;//Left/right pixel

            lblStatus.Text = "Generating file...";
            Refresh();
            //Generate picture Gcode
            Int32 pixTot = adjustedImage.Width * adjustedImage.Height;
            Int32 pixBurned = 0;
            //int direction = 0;
            if (!gcodeSpindleToggle) gcode.SpindleOn(finalString, "Start spindle");

            if (rbEngravingPattern1.Checked)
            {
                //Start image
                lin = adjustedImage.Height - 1;//top tile
                col = 0;//Left pixel
                lastX = 0;//reset last positions
                lastY = resol * (float)lin;
                coordX = resol * (float)col;
                coordY = resol * (float)lin;
                gcode.PenUp(finalString);                             // pen up
                gcode.MoveToRapid(finalString, coordX, coordY, "");         // rapid move to start pos
                while (lin >= 0)
                {
                    //Y coordinate
                    coordY = resol * (float)lin;
                    //direction = 1;                      // left to right
                    while (col < adjustedImage.Width)   //From left to right
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        drawHeight(col, lin, coordX, coordY);
                        pixBurned++;
                        col++;
                    }
                    col--;
                    lin--;
                    coordY = resol * (float)lin;
                    //direction = -1;                     // right to left
                    while ((col >= 0) & (lin >= 0))     //From right to left
                    {
                        //X coordinate
                        coordX = resol * (float)col;
                        drawHeight(col, lin, coordX, coordY);
                        pixBurned++;
                        col--;
                    }
                    col++;
                    lin--;
                    lblStatus.Text = "Generating GCode... " + Convert.ToString((pixBurned * 100) / pixTot) + "%";
                    Refresh();
                }
            }
            else
            {
                //Start image
                col = 0;
                lin = 0;
                lastX = 0;//reset last positions
                lastY = 0;
                while ((col < adjustedImage.Width) | (lin < adjustedImage.Height))
                {
                    //direction = 2;    // up-left to low-right

                    while ((col < adjustedImage.Width) & (lin >= 0))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        drawHeight(col, lin, coordX, coordY);
                        pixBurned++;
                        col++;
                        lin--;
                    }
                    col--;
                    lin++;

                    if (col >= adjustedImage.Width - 1) lin++;
                    else col++;
                    //direction = -2;    // low-right to up-left 
                    while ((col >= 0) & (lin < adjustedImage.Height))
                    {
                        //Y coordinate
                        coordY = resol * (float)lin;
                        //X coordinate
                        coordX = resol * (float)col;

                        drawHeight(col, lin, coordX, coordY);
                        pixBurned++;
                        col--;
                        lin++;
                    }
                    col++;
                    lin--;
                    if (lin >= adjustedImage.Height - 1) col++;
                    else lin++;
                    lblStatus.Text = "Generating GCode... " + Convert.ToString((pixBurned * 100) / pixTot) + "%";
                    Refresh();
                }
            }
            gcode.PenUp(finalString);                             // pen up
            if (!gcodeSpindleToggle) gcode.SpindleOff(finalString, "Stop spindle");

            imagegcode = "( Generated by GRBL-Plotter )\r\n";
            imagegcode += gcode.GetHeader("Image import") + finalString.Replace(',', '.').ToString() + gcode.GetFooter();
        }

        private static string debug_string;
        private static string[] usedColors = new string[svgToolIndex + 1];
        private static int[] countColors = new int[svgToolIndex + 1];
        private void btnList_Click(object sender, EventArgs e)
        {
            getSettings();
            Array.Resize<string>(ref usedColors, svgToolIndex + 1);     //usedColors = string[svgToolIndex + 1];
            Array.Resize<int>(ref countColors, svgToolIndex + 1);     //countColors = new int[svgToolIndex + 1];

            for (int i = 0; i <= svgToolIndex; i++)
            { usedColors[i] = ""; countColors[i] = 0; }
            generateResultImage();

            string tool_string = "";
            string not_used = "\r\nAll current palette colors:\r\n";
            Dictionary<int, string> values = new Dictionary<int, string>();

            debug_string = "Palette has " + (svgToolIndex - 1).ToString() + " colors\r\n\r\n";
            if (cbSkipToolOrder.Checked)
                debug_string += "Colors / Tools used in image (sort by pixel count):\r\nTool nr not from palette\r\n ";
            else
                debug_string += "Colors / Tools used in image:\r\n ";
            for (int i = 0; i <= svgToolIndex; i++)
            {
                if (usedColors[i].Length > 1)
                {
                    if (i < 2)
                        debug_string += (i - 2).ToString() + ") Exception color " + usedColors[i] + "\r\n";
                    else
                    {
                        tool_string += (i - 2).ToString() + ") " + usedColors[i] + "\r\n";
                        while (values.ContainsKey(countColors[i]))
                            countColors[i]++;
                        values.Add(countColors[i], usedColors[i]);
                    }
                }
            }
            for (int i = 0; i < svgToolIndex - 1; i++)
            { not_used += (i).ToString() + ") " + svgPalette.getToolName(i) + "\r\n"; }

            if (cbSkipToolOrder.Checked)
            {
                if (values.Count() > 0)
                {
                    tool_string = "";
                    var list = values.Keys.ToList();
                    list.Sort();            // sort by pixelamount
                    list.Reverse();         // but descending order
                    int i = 0;
                    foreach (var key in list)
                    {
                        tool_string += (i++).ToString() + ") " + " " + key + "  " + values[key] + "\r\n";
                    }
                }
            }
            MessageBox.Show(debug_string + tool_string + not_used, "List of pens");
        }

        private void generateResultImage()
        {
            int x, y;
            Color myColor, newColor;
            if (cbExceptColor.Checked)
                svgPalette.setExceptionColor(cbExceptColor.BackColor);
            else
                svgPalette.clrExceptionColor();
            int myToolNr, myIndex;
            int mode = (int)nUDMode.Value;
            for (y = 0; y < adjustedImage.Height; y++)
            {
                for (x = 0; x < adjustedImage.Width; x++)
                {
                    myColor = adjustedImage.GetPixel(x, y);                 // Get pixel color}
                    if (((cbExceptAlpha.Checked) && (myColor.A == 0)))      // skip exception
                    {
                        newColor = Color.White; myToolNr = -2; usedColors[0] = "Alpha = 0      " + myColor.ToString();
                    }
                    else
                    {
                        myToolNr = svgPalette.getToolNr(myColor, mode);     // find nearest color in palette
                        if (myToolNr < 0)
                            newColor = Color.White;
                        else
                            newColor = svgPalette.getColor();   // Color.FromArgb(255, r, g, b);
                    }
                    myIndex = myToolNr + 2;
                    countColors[myIndex]++;
                    if (usedColors[myIndex].Length < 1)
                        usedColors[myIndex] = svgPalette.getName() + "      " + svgPalette.getColor().ToString();
                    resultImage.SetPixel(x, y, newColor);
                }
            }
        }

        //Horizontal mirroing
        private void btnHorizMirror_Click(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            lblStatus.Text = "Mirroing...";
            Refresh();
            adjustedImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            originalImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Image = adjustedImage;
            lblStatus.Text = "Done";
        }
        //Vertical mirroing
        private void btnVertMirror_Click(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            lblStatus.Text = "Mirroing...";
            Refresh();
            adjustedImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            originalImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pictureBox1.Image = adjustedImage;
            lblStatus.Text = "Done";
        }
        //Rotate right
        private void btnRotateRight_Click(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            lblStatus.Text = "Rotating...";
            Refresh();
            adjustedImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            originalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            ratio = 1 / ratio;
            decimal s = nUDHeight.Value;
            nUDHeight.Value = nUDWidth.Value;
            nUDWidth.Value = s;
            pictureBox1.Image = adjustedImage;
            autoZoomToolStripMenuItem_Click(this, null);
            lblStatus.Text = "Done";
        }
        //Rotate left
        private void btnRotateLeft_Click(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            lblStatus.Text = "Rotating...";
            Refresh();
            adjustedImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
            originalImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
            ratio = 1 / ratio;
            decimal s = nUDHeight.Value;
            nUDHeight.Value = nUDWidth.Value;
            nUDWidth.Value = s;
            pictureBox1.Image = adjustedImage;
            autoZoomToolStripMenuItem_Click(this, null);
            lblStatus.Text = "Done";
        }
        //Invert image color
        private void btnInvert_Click(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            adjustedImage = imgInvert(adjustedImage);
            originalImage = imgInvert(originalImage);
            pictureBox1.Image = adjustedImage;
        }

        // private void cbDirthering_SelectedIndexChanged(object sender, EventArgs e)
        private void rbModeGray_CheckedChanged(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            if (rbModeDither.Checked)// cbDirthering.Text == "Dirthering FS 1 bit")
            {
                lblStatus.Text = "Dirtering...";
                adjustedImage = imgDirther(adjustedImage);
                pictureBox1.Image = adjustedImage;
                lblStatus.Text = "Done";
            }
            else
                userAdjust();
            updateLabelColor = true;

        }
        private void autoZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.Width = panel1.Width;
            pictureBox1.Height = panel1.Height;
            pictureBox1.Top = 0;
            pictureBox1.Left = 0;
        }

        private void nUDWidth_ValueChanged(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            if (cbLockRatio.Checked)
            {
                nUDHeight.Value = (decimal)((float)nUDWidth.Value / ratio);
            }
            userAdjust();
        }

        private void nUDHeight_ValueChanged(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            if (cbLockRatio.Checked)
            {
                nUDWidth.Value = (decimal)((float)nUDHeight.Value * ratio);
            }
            userAdjust();
        }

        private void nUDReso_ValueChanged(object sender, EventArgs e)
        {
            if (adjustedImage == null) return;//if no image, do nothing
            userAdjust();
        }

        private void cbGrayscale_CheckedChanged(object sender, EventArgs e)
        {
            if (cbGrayscale.Checked)
                originalImage = imgGrayscale(originalImage);
            else
            {   //if (fileLoaded)
                //     originalImage = new Bitmap(Image.FromFile(lastFile));
                // else
                //     originalImage = new Bitmap(Properties.Resources.modell);
                originalImage = new Bitmap(loadedImage);
            }
            adjustedImage = new Bitmap(originalImage);
            userAdjust();
        }

        private void GCodeFromImage_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.locationImageForm = Location;
        }

        private void GCodeFromImage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                loadClipboard();
                e.SuppressKeyPress = true;
            }
        }

        private Point oldPoint = new Point(0, 0);
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Location != oldPoint) || (e.Button == MouseButtons.Left))
            {
                Color clr = GetColorAt(e.Location);
                if (e.Button == MouseButtons.Left)
                {
                    int i = svgPalette.getToolNr(clr, (int)nUDMode.Value);
                    lblStatus.Text = clr.ToString() + " = " + svgPalette.getToolName(i);
                    cbExceptColor.BackColor = clr;
                }
                float zoom = (float)nUDWidth.Value / pictureBox1.Width;
                //        toolTip1.SetToolTip(pictureBox1, (e.X * zoom).ToString() + "  " + (e.Y * zoom).ToString());
                toolTip1.SetToolTip(pictureBox1, (e.X * zoom).ToString() + "  " + (e.Y * zoom).ToString() + "   " + clr.ToString());
                oldPoint = e.Location;
            }
        }

        private void cbExceptColor_CheckedChanged(object sender, EventArgs e)
        {
            if (cbExceptColor.Checked)
                svgPalette.setExceptionColor(cbExceptColor.BackColor);
            else
                svgPalette.clrExceptionColor();
        }

        private Color GetColorAt(Point point)
        {
            float zoom = ((float)nUDWidth.Value / (float)nUDReso.Value) / pictureBox1.Width;
            int x = (int)(point.X * zoom);
            if (x < 0) x = 0;
            if (x >= adjustedImage.Width - 1) x = adjustedImage.Width - 1;
            int y = (int)(point.Y * zoom);  //(adjustedImage.Height - 1) - (int)(point.Y * zoom);
            if (y < 0) y = 0;
            if (y >= adjustedImage.Height - 1) y = adjustedImage.Height - 1;
            return adjustedImage.GetPixel(x, y);
        }

        #endregion

        #region Métodos para la conversión del archivo de la clase GCodeFromShape

        private void makeRect(float x1, float y1, float x2, float y2, float r, bool cw = true)
        {   // start bottom left
            if (cw)
            {
                gcode.MoveTo(gcodeString, x1, y2 - r, "");          //BL to TL
                if (r > 0) { gcode.Arc(gcodeString, 2, x1 + r, y2, r, 0, ""); }
                gcode.MoveTo(gcodeString, x2 - r, y2, "");          // TL to TR
                if (r > 0) { gcode.Arc(gcodeString, 2, x2, y2 - r, 0, -r, ""); }
                gcode.MoveTo(gcodeString, x2, y1 + r, "");          // TR to BR
                if (r > 0) { gcode.Arc(gcodeString, 2, x2 - r, y1, -r, 0, ""); }
                gcode.MoveTo(gcodeString, x1 + r, y1, "");          // BR to BL
                if (r > 0) { gcode.Arc(gcodeString, 2, x1, y1 + r, 0, r, ""); }
            }
            else
            {
                if (r > 0) { gcode.Arc(gcodeString, 3, x1 + r, y1, r, 0, ""); }
                gcode.MoveTo(gcodeString, x2 - r, y1, "");          // to BR
                if (r > 0) { gcode.Arc(gcodeString, 3, x2, y1 + r, 0, r, ""); }
                gcode.MoveTo(gcodeString, x2, y2 - r, "");           // to TR
                if (r > 0) { gcode.Arc(gcodeString, 3, x2 - r, y2, -r, 0, ""); }
                gcode.MoveTo(gcodeString, x1 + r, y2, "");           // to TL
                if (r > 0) { gcode.Arc(gcodeString, 3, x1, y2 - r, 0, -r, ""); }
                gcode.MoveTo(gcodeString, x1, y1 + r, "");           // to BL 
            }

        }
        private void getOffset(float x, float y)
        {
            if (rBOrigin1.Checked) { offsetX = 0; offsetY = -y; }
            if (rBOrigin2.Checked) { offsetX = -x / 2; offsetY = -y; }
            if (rBOrigin3.Checked) { offsetX = -x; offsetY = -y; }
            if (rBOrigin4.Checked) { offsetX = 0; offsetY = -y / 2; }
            if (rBOrigin5.Checked) { offsetX = -x / 2; offsetY = -y / 2; }
            if (rBOrigin6.Checked) { offsetX = -x; offsetY = -y / 2; }
            if (rBOrigin7.Checked) { offsetX = 0; offsetY = 0; }
            if (rBOrigin8.Checked) { offsetX = -x / 2; offsetY = 0; }
            if (rBOrigin9.Checked) { offsetX = -x; offsetY = 0; }
        }

        private void saveSettings()
        {
            Properties.Settings.Default.toolDiameter = nUDToolDiameter.Value;
            Properties.Settings.Default.toolZStep = nUDToolZStep.Value;
            Properties.Settings.Default.toolFeedXY = nUDToolFeedXY.Value;
            Properties.Settings.Default.toolFeedZ = nUDToolFeedZ.Value;
            Properties.Settings.Default.toolOverlap = nUDToolOverlap.Value;
            Properties.Settings.Default.toolSpindleSpeed = nUDToolSpindleSpeed.Value;
            Properties.Settings.Default.shapeX = nUDShapeX.Value;
            Properties.Settings.Default.shapeY = nUDShapeY.Value;
            Properties.Settings.Default.shapeR = nUDShapeR.Value;
            if (rBShape1.Checked) Properties.Settings.Default.shapeType = 1;
            if (rBShape2.Checked) Properties.Settings.Default.shapeType = 2;
            if (rBShape3.Checked) Properties.Settings.Default.shapeType = 3;
            Properties.Settings.Default.importGCZDown = nUDImportGCZDown.Value;
            if (rBToolpath1.Checked) Properties.Settings.Default.toolPath = 1;
            if (rBToolpath2.Checked) Properties.Settings.Default.toolPath = 2;
            if (rBToolpath3.Checked) Properties.Settings.Default.toolPath = 3;
            if (rBOrigin1.Checked) Properties.Settings.Default.shapeOrigin = 1;
            if (rBOrigin2.Checked) Properties.Settings.Default.shapeOrigin = 2;
            if (rBOrigin3.Checked) Properties.Settings.Default.shapeOrigin = 3;
            if (rBOrigin4.Checked) Properties.Settings.Default.shapeOrigin = 4;
            if (rBOrigin5.Checked) Properties.Settings.Default.shapeOrigin = 5;
            if (rBOrigin6.Checked) Properties.Settings.Default.shapeOrigin = 6;
            if (rBOrigin7.Checked) Properties.Settings.Default.shapeOrigin = 7;
            if (rBOrigin8.Checked) Properties.Settings.Default.shapeOrigin = 8;
            if (rBOrigin9.Checked) Properties.Settings.Default.shapeOrigin = 9;
            Properties.Settings.Default.Save();
        }

        private void updateControls()
        { }

        #endregion

        #region Métodos para la conversión del archivo de la clase GCodeFromSVG

        public static string convertFromText(string svgText, bool importMM = false)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(svgText);
            MemoryStream stream = new MemoryStream(byteArray);
            svgCode = XElement.Load(stream, LoadOptions.None);
            //MessageBox.Show(svgCode.ToString());
            fromClipboard = true;
            importInMM = importMM;
            return convertSVG(svgCode, "from Clipboard");                   // startConvert(svgCode);
        }
        public static string convertFromFile(string file)
        {
            fromClipboard = false;
            importInMM = false;
            if (file == "")
            { MessageBox.Show("Empty file name"); return ""; }
            if (file.Substring(0, 4) == "http")
            {
                string content = "";
                using (var wc = new System.Net.WebClient())
                {
                    try { content = wc.DownloadString(file); }
                    catch { MessageBox.Show("Could not load content from " + file); return ""; }
                }
                if ((content != "") && (content.IndexOf("<?xml") == 0))
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(content);
                    MemoryStream stream = new MemoryStream(byteArray);
                    svgCode = XElement.Load(stream, LoadOptions.None);
                    System.Windows.Clipboard.SetData("image/svg+xml", stream);
                    return convertSVG(svgCode, file);                   // startConvert(svgCode);
                }
                else
                    MessageBox.Show("This is probably not a SVG document.\r\nFirst line: " + content.Substring(0, 50));
            }
            else
            {
                if (File.Exists(file))
                {
                    try
                    {
                        svgCode = XElement.Load(file, LoadOptions.None);    // PreserveWhitespace);
                        return convertSVG(svgCode, file);                   // startConvert(svgCode);
                    }
                    catch (Exception e)
                    { MessageBox.Show("Error '" + e.ToString() + "' in XML file " + file + "\r\n\r\nTry to save file with other encoding e.g. UTF-8"); return ""; }
                }
                else { MessageBox.Show("File does not exist: " + file); return ""; }
            }
            return "";
        }

        private static string convertSVG(XElement svgCode, string info)
        {
            gcodeStringIndex = 0;
            for (int i = 0; i < svgToolMax; i++)    // hold gcode snippes for later sorting
            {
                gcodeString[i] = new StringBuilder();
                gcodeString[i].Clear();
            }
            gcode.setup();          // initialize GCode creation (get stored settings for export)
            finalString.Clear();

            if (gcodeUseSpindle) gcode.SpindleOn(finalString, "Start spindle - Option Z-Axis");
            gcode.PenUp(finalString, "SVG Start ");
            startConvert(svgCode);

            if (svgToolSort)
            {
                int toolnr;
                svgPalette.sortByToolNr();
                for (int i = 0; i < svgToolIndex; i++)
                {
                    svgPalette.setIndex(i);                 // set index in svgPalette
                    toolnr = svgPalette.indexToolNr();      // get value from set index
                    if ((toolnr >= 0) && (gcodeString[toolnr].Length > 1))
                    {
                        finalString.Append("\r\n\r\n");
                        if ((gcodeToolChange) && svgPalette.indexUse())
                        {
                            gcode.Tool(finalString, toolnr, svgPalette.indexName());
                        }
                        finalString.Append(gcodeString[toolnr]);
                    }
                }
            }
            else
                finalString.Append(gcodeString[0]);
            if (gcodeUseSpindle) gcode.SpindleOff(finalString, "Stop spindle - Option Z-Axis");
            string header = gcode.GetHeader("SVG import", info);
            string footer = gcode.GetFooter();

            string output = "";
            if (Properties.Settings.Default.importSVGRepeatEnable)
            {
                for (int i = 0; i < Properties.Settings.Default.importSVGRepeat; i++)
                    output += finalString.ToString().Replace(',', '.');

                return header + output + footer;
            }
            else
                return header + finalString.ToString().Replace(',', '.') + footer;
        }

        /// <summary>
        /// Set defaults and parse main element of SVG-XML
        /// </summary>
        private static void startConvert(XElement svgCode)
        {
            svgBezierAccuracy = (int)Properties.Settings.Default.importSVGBezier;
            svgScaleApply = Properties.Settings.Default.importSVGRezise;
            svgMaxSize = (float)Properties.Settings.Default.importSVGMaxSize;
            svgClosePathExtend = Properties.Settings.Default.importSVGPathExtend;
            svgPauseElement = Properties.Settings.Default.importSVGPauseElement;
            svgPausePenDown = Properties.Settings.Default.importSVGPausePenDown;
            svgComments = Properties.Settings.Default.importSVGAddComments;
            svgConvertToMM = Properties.Settings.Default.importUnitmm;

            gcodeReduce = Properties.Settings.Default.importSVGReduce;
            gcodeReduceVal = (float)Properties.Settings.Default.importSVGReduceLimit;

            gcodeXYFeed = (float)Properties.Settings.Default.importGCXYFeed;
            gcodeZApply = Properties.Settings.Default.importGCZEnable;
            gcodeZUp = (float)Properties.Settings.Default.importGCZUp;
            gcodeZDown = (float)Properties.Settings.Default.importGCZDown;
            gcodeZFeed = (float)Properties.Settings.Default.importGCZFeed;
            gcodeUseSpindle = Properties.Settings.Default.importGCZEnable;

            svgToolColor = Properties.Settings.Default.importSVGToolColor;
            svgToolSort = Properties.Settings.Default.importSVGToolSort;
            gcodeToolChange = Properties.Settings.Default.importGCTool;

            svgNodesOnly = Properties.Settings.Default.importSVGNodesOnly;

            svgToolIndex = svgPalette.init();

            countSubPath = 0;
            startFirstElement = true;
            gcodeScale = 1;
            svgWidthPx = 0; svgHeightPx = 0;
            currentX = 0; currentY = 0;
            offsetX = 0; offsetY = 0;
            firstX = null;
            firstY = null;
            lastX = 0;
            lastY = 0;

            matrixElement.SetIdentity();
            oldMatrixElement.SetIdentity();
            for (int i = 0; i < matrixGroup.Length; i++)
                matrixGroup[i].SetIdentity();

            parseGlobals(svgCode);
            if (!svgNodesOnly)
                parseBasicElements(svgCode, 1);
            parsePath(svgCode, 1);
            parseGroup(svgCode, 1);
            return;
        }

        /// <summary>
        /// Parse SVG dimension (viewbox, width, height)
        /// </summary>
        private static XNamespace nspace = "http://www.w3.org/2000/svg";
        private static void parseGlobals(XElement svgCode)
        {   // One px unit is defined to be equal to one user unit. Thus, a length of "5px" is the same as a length of "5".
            Matrix tmp = new Matrix(1, 0, 0, 1, 0, 0); // m11, m12, m21, m22, offsetx, offsety
            svgWidthPx = 0;
            svgHeightPx = 0;
            float vbOffX = 0;
            float vbOffY = 0;
            float vbWidth = 0;
            float vbHeight = 0;
            float scale = 1;
            string tmpString = "";

            if (svgCode.Attribute("viewBox") != null)
            {
                string viewbox = svgCode.Attribute("viewBox").Value.Replace(' ', '|');
                var split = viewbox.Split('|');
                vbOffX = -floatParse(split[0]);
                vbOffY = -floatParse(split[1]);
                vbWidth = floatParse(split[2]);
                vbHeight = floatParse(split[3].TrimEnd(')'));
                tmp.M11 = 1; tmp.M22 = -1;      // flip Y
                tmp.OffsetY = vbHeight;
                if (svgComments) gcodeString[gcodeStringIndex].AppendLine("( SVG viewbox :" + viewbox + " )");
            }

            if (svgCode.Attribute("width") != null)
            {
                tmpString = svgCode.Attribute("width").Value;
                svgWidthPx = floatParse(tmpString);
                if (importInMM)
                {
                    if (tmpString.IndexOf("mm") > 0)
                        svgWidthPx = svgWidthPx / 3.543307f;
                }
                scale = 1;
                if (svgConvertToMM && tmpString.IndexOf("in") > 0)
                    scale = 25.4f;
                if (!svgConvertToMM && tmpString.IndexOf("mm") > 0)
                    scale = (1 / 25.4f);
                tmpString = removeUnit(tmpString);
                float svgWidthUnit = floatParse(tmpString);//floatParse(svgCode.Attribute("width").Value.Replace("mm", ""));

                if (svgComments) gcodeString[gcodeStringIndex].AppendLine("( SVG width :" + svgCode.Attribute("width").Value + " )");
                tmp.M11 = scale * svgWidthUnit / svgWidthPx; // get desired scale
                if (fromClipboard)
                    tmp.M11 = 1 / 3.543307;         // https://www.w3.org/TR/SVG/coords.html#Units
                if (vbWidth > 0)
                {
                    tmp.M11 = scale * svgWidthPx / vbWidth;
                    tmp.OffsetX = vbOffX * svgWidthUnit / vbWidth;
                }
            }

            if (svgCode.Attribute("height") != null)
            {
                tmpString = svgCode.Attribute("height").Value;
                svgHeightPx = floatParse(tmpString);
                if (importInMM)
                {
                    if (tmpString.IndexOf("mm") > 0)
                        svgHeightPx = svgHeightPx / 3.543307f;
                }
                scale = 1;
                if (svgConvertToMM && tmpString.IndexOf("in") > 0)
                    scale = 25.4f;
                if (!svgConvertToMM && tmpString.IndexOf("mm") > 0)
                    scale = (1 / 25.4f);
                tmpString = removeUnit(tmpString);
                float svgHeightUnit = floatParse(tmpString);// svgCode.Attribute("height").Value.Replace("mm", ""));

                if (svgComments) gcodeString[gcodeStringIndex].AppendLine("( SVG height :" + svgCode.Attribute("height").Value + " )");
                tmp.M22 = -scale * svgHeightUnit / svgHeightPx;   // get desired scale and flip vertical
                tmp.OffsetY = scale * svgHeightUnit;

                if (fromClipboard)
                {
                    tmp.M22 = -1 / 3.543307;
                    tmp.OffsetY = svgHeightUnit / 3.543307;     // https://www.w3.org/TR/SVG/coords.html#Units
                }
                if (vbHeight > 0)
                {
                    tmp.M22 = -scale * svgHeightPx / vbHeight;
                    tmp.OffsetY = -vbOffX * svgHeightUnit / vbHeight + svgHeightPx;
                }
            }

            float newWidth = Math.Max(svgWidthPx, vbWidth);     // use value from 'width' or 'viewbox' parameter
            float newHeight = Math.Max(svgHeightPx, vbHeight);
            if ((newWidth > 0) && (newHeight > 0))
            {
                if (svgScaleApply)
                {
                    gcodeScale = svgMaxSize / Math.Max(newWidth, newHeight);        // calc. factor to get desired max. size
                    tmp.Scale((double)gcodeScale, (double)gcodeScale);
                    if (svgConvertToMM)                         // https://www.w3.org/TR/SVG/coords.html#Units
                        tmp.Scale(3.543307, 3.543307);
                    else
                        tmp.Scale(90, 90);

                    if (svgComments)
                        gcodeString[gcodeStringIndex].AppendFormat("( Scale to X={0} Y={1} f={2} )\r\n", newWidth * gcodeScale, newHeight * gcodeScale, gcodeScale);
                }
            }
            else
                if (svgComments) gcodeString[gcodeStringIndex].Append("( SVG Dimension not given )\r\n");

            for (int i = 0; i < matrixGroup.Length; i++)
            { matrixGroup[i] = tmp; }
            matrixElement = tmp;
            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( Inital Matrix {0} )\r\n", tmp.ToString());

            return;
        }

        /// <summary>
        /// Parse Group-Element and included elements
        /// </summary>
        private static void parseGroup(XElement svgCode, int level)
        {
            foreach (XElement groupElement in svgCode.Elements(nspace + "g"))
            {
                if (svgComments)
                    if (groupElement.Attribute("id") != null)
                        gcodeString[gcodeStringIndex].Append("\r\n( Group level:" + level.ToString() + " id=" + groupElement.Attribute("id").Value + " )\r\n");
                parseTransform(groupElement, true, level);   // transform will be applied in gcodeMove
                if (!svgNodesOnly)
                    parseBasicElements(groupElement, level);
                parsePath(groupElement, level);
                parseGroup(groupElement, level + 1);
            }
            return;
        }

        /// <summary>
        /// Parse Transform information - more information here: http://www.w3.org/TR/SVG/coords.html
        /// transform will be applied in gcodeMove
        /// </summary>
        private static bool parseTransform(XElement element, bool isGroup, int level)
        {
            Matrix tmp = new Matrix(1, 0, 0, 1, 0, 0); // m11, m12, m21, m22, offsetx, offsety
            bool transf = false;
            if (element.Attribute("transform") != null)
            {
                transf = true;
                string transform = element.Attribute("transform").Value;
                if ((transform != null) && (transform.IndexOf("translate") >= 0))
                {
                    var coord = getTextBetween(transform, "translate(", ")");
                    var split = coord.Split(',');
                    if (coord.IndexOf(',') < 0)
                        split = coord.Split(' ');

                    //MessageBox.Show(transform+"\r\n>"+coord + "< "+ split[0]);
                    tmp.OffsetX = floatParse(split[0]);
                    if (split.Length > 1)
                        tmp.OffsetY = floatParse(split[1].TrimEnd(')'));
                    if (svgComments) gcodeString[gcodeStringIndex].Append(string.Format("( SVG-Translate {0} {1} )\r\n", tmp.OffsetX, tmp.OffsetY));
                }
                if ((transform != null) && (transform.IndexOf("scale") >= 0))
                {
                    var coord = getTextBetween(transform, "scale(", ")");
                    var split = coord.Split(',');
                    if (coord.IndexOf(',') < 0)
                        split = coord.Split(' ');
                    tmp.M11 = floatParse(split[0]);
                    if (split.Length > 1)
                    { tmp.M22 = floatParse(split[1]); }
                    else
                    {
                        tmp.M11 = floatParse(coord);
                        tmp.M22 = floatParse(coord);
                    }
                    if (svgComments) gcodeString[gcodeStringIndex].Append(string.Format("( SVG-Scale {0} {1} )\r\n", tmp.M11, tmp.M22));
                }
                if ((transform != null) && (transform.IndexOf("rotate") >= 0))
                {
                    var coord = getTextBetween(transform, "rotate(", ")");
                    var split = coord.Split(',');
                    if (coord.IndexOf(',') < 0)
                        split = coord.Split(' ');
                    float angle = floatParse(split[0]) * (float)Math.PI / 180;
                    tmp.M11 = Math.Cos(angle); tmp.M12 = Math.Sin(angle);
                    tmp.M21 = -Math.Sin(angle); tmp.M22 = Math.Cos(angle);

                    if (svgComments) gcodeString[gcodeStringIndex].Append(string.Format("( SVG-Rotate {0} )\r\n", angle));
                }
                if ((transform != null) && (transform.IndexOf("matrix") >= 0))
                {
                    var coord = getTextBetween(transform, "matrix(", ")");
                    var split = coord.Split(',');
                    if (coord.IndexOf(',') < 0)
                        split = coord.Split(' ');
                    tmp.M11 = floatParse(split[0]);     // a    scale x         a c e
                    tmp.M12 = floatParse(split[1]);     // b                    b d f
                    tmp.M21 = floatParse(split[2]);     // c                    0 0 1
                    tmp.M22 = floatParse(split[3]);     // d    scale y
                    tmp.OffsetX = floatParse(split[4]); // e    offset x
                    tmp.OffsetY = floatParse(split[5]); // f    offset y
                    if (svgComments) gcodeString[gcodeStringIndex].Append(string.Format("\r\n( SVG-Matrix {0} {1} {2} )\r\n", coord.Replace(',', '|'), level, isGroup));
                }
                //}
                if (isGroup)
                {
                    matrixGroup[level].SetIdentity();
                    if (level > 0)
                    {
                        for (int i = level; i < matrixGroup.Length; i++)
                        { matrixGroup[i] = Matrix.Multiply(tmp, matrixGroup[level - 1]); }
                    }
                    else
                    { matrixGroup[level] = tmp; }
                    matrixElement = matrixGroup[level];
                }
                else
                {
                    matrixElement = Matrix.Multiply(tmp, matrixGroup[level]);
                }

                if (svgComments && transf)
                {
                    for (int i = 0; i <= level; i++)
                        gcodeString[gcodeStringIndex].AppendFormat("( gc-Matrix level[{0}] {1} )\r\n", i, matrixGroup[i].ToString());

                    if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( gc-Scale {0} {1} )\r\n", matrixElement.M11, matrixElement.M22);
                    if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( gc-Offset {0} {1} )\r\n", matrixElement.OffsetX, matrixElement.OffsetY);
                }
            }
            return transf;
        }
        private static string getTextBetween(string source, string s1, string s2)
        {
            int start = source.IndexOf(s1) + s1.Length;
            char c;
            for (int i = start; i < source.Length; i++)
            {
                c = source[i];
                if (!(Char.IsNumber(c) || c == '.' || c == ',' || c == ' ' || c == '-' || c == 'e'))    // also exponent
                    return source.Substring(start, i - start);
            }
            return source.Substring(start, source.Length - start - 1);
        }
        private static float floatParse(string str, float ext = 1)
        {       // https://www.w3.org/TR/SVG/coords.html#Units
            bool percent = false;
            float factor = 1;
            if (str.IndexOf("pt") > 0) { factor = 1.25f; }
            if (str.IndexOf("pc") > 0) { factor = 15f; }
            if (str.IndexOf("mm") > 0) { factor = 3.543307f; }
            if (str.IndexOf("cm") > 0) { factor = 35.43307f; }
            if (str.IndexOf("in") > 0) { factor = 90f; }
            if (str.IndexOf("em") > 0) { factor = 150f; }
            if (str.IndexOf("%") > 0) { percent = true; }
            str = str.Replace("pt", "").Replace("pc", "").Replace("mm", "").Replace("cm", "").Replace("in", "").Replace("em ", "").Replace("%", "").Replace("px", "");

            if (str.Length > 0)
            {
                if (percent)
                    return float.Parse(str, CultureInfo.InvariantCulture.NumberFormat) * ext / 100;
                else
                    return float.Parse(str, CultureInfo.InvariantCulture.NumberFormat) * factor;
            }
            else return 0f;
        }
        private static string removeUnit(string str)
        { return str.Replace("pt", "").Replace("pc", "").Replace("mm", "").Replace("cm", "").Replace("in", "").Replace("em ", "").Replace("%", "").Replace("px", ""); }

        private static string getColor(XElement pathElement)
        {
            string style = "";
            string stroke_color = "000000";        // default=black
            if (pathElement.Attribute("style") != null)
            {
                int start, end;
                style = pathElement.Attribute("style").Value;
                start = style.IndexOf("stroke:#");
                if (start >= 0)
                {
                    end = style.IndexOf(';', start);
                    if (end > start)
                        stroke_color = style.Substring(start + 8, end - start - 8);
                }
                return stroke_color;
            }
            return "";
        }

        /// <summary>
        /// Convert Basic shapes (up to now: line, rect, circle) check: http://www.w3.org/TR/SVG/shapes.html
        /// </summary>
        private static void parseBasicElements(XElement svgCode, int level)
        {
            string[] forms = { "rect", "circle", "ellipse", "line", "polyline", "polygon", "text", "image" };
            foreach (var form in forms)
            {
                foreach (var pathElement in svgCode.Elements(nspace + form))
                {
                    if (pathElement != null)
                    {
                        string myColor = getColor(pathElement);
                        int myTool = svgPalette.getToolNr(myColor, 0);

                        if ((svgToolSort) && (myTool >= 0))
                            gcodeStringIndex = myTool;

                        if (svgComments)
                        {
                            if (pathElement.Attribute("id") != null)
                                gcodeString[gcodeStringIndex].Append("\r\n( Basic shape level:" + level.ToString() + " id=" + pathElement.Attribute("id").Value + " )\r\n");
                            gcodeString[gcodeStringIndex].AppendFormat("( SVG color=#{0})\r\n", myColor);
                        }
                        gcodeTool(myTool);

                        if (startFirstElement)
                        { gcodePenUp("1st shape"); startFirstElement = false; }

                        offsetX = 0; offsetY = 0;

                        oldMatrixElement = matrixElement;
                        bool avoidG23 = false;
                        parseTransform(pathElement, false, level);  // transform will be applied in gcodeMove

                        float x = 0, y = 0, x1 = 0, y1 = 0, x2 = 0, y2 = 0, width = 0, height = 0, rx = 0, ry = 0, cx = 0, cy = 0, r = 0;
                        string[] points = { "" };
                        if (pathElement.Attribute("x") != null) x = floatParse(pathElement.Attribute("x").Value);
                        if (pathElement.Attribute("y") != null) y = floatParse(pathElement.Attribute("y").Value);
                        if (pathElement.Attribute("x1") != null) x1 = floatParse(pathElement.Attribute("x1").Value);
                        if (pathElement.Attribute("y1") != null) y1 = floatParse(pathElement.Attribute("y1").Value);
                        if (pathElement.Attribute("x2") != null) x2 = floatParse(pathElement.Attribute("x2").Value);
                        if (pathElement.Attribute("y2") != null) y2 = floatParse(pathElement.Attribute("y2").Value);
                        if (pathElement.Attribute("width") != null) width = floatParse(pathElement.Attribute("width").Value, svgWidthPx);
                        if (pathElement.Attribute("height") != null) height = floatParse(pathElement.Attribute("height").Value, svgHeightPx);
                        if (pathElement.Attribute("rx") != null) rx = floatParse(pathElement.Attribute("rx").Value);
                        if (pathElement.Attribute("ry") != null) ry = floatParse(pathElement.Attribute("ry").Value);
                        if (pathElement.Attribute("cx") != null) cx = floatParse(pathElement.Attribute("cx").Value);
                        if (pathElement.Attribute("cy") != null) cy = floatParse(pathElement.Attribute("cy").Value);
                        if (pathElement.Attribute("r") != null) r = floatParse(pathElement.Attribute("r").Value);
                        if (pathElement.Attribute("points") != null) points = pathElement.Attribute("points").Value.Split(' ');

                        if (svgPauseElement || svgPausePenDown) { gcode.Pause(gcodeString[gcodeStringIndex], "Pause before path"); }
                        if (form == "rect")
                        {
                            if (ry == 0) { ry = rx; }
                            else if (rx == 0) { rx = ry; }
                            else if (rx != ry) { rx = Math.Min(rx, ry); ry = rx; }   // only same r for x and y are possible
                            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( SVG-Rect x:{0} y:{1} width:{2} height:{3} rx:{4} ry:{5})\r\n", x, y, width, height, rx, ry);
                            /*                            if (Math.Round((rx * 2),2) >= Math.Round(Math.Min(width, height),2))
                                                        {   rx = ry = (float)(Math.Round(Math.Min(width, height) / 2 - 0.01,2));
                                                            gcodeString[gcodeStringIndex].AppendFormat("( Corrected rx:{0} ry:{1})\r\n", rx, ry);
                                                        }   */
                            x += offsetX; y += offsetY;
                            gcodeStartPath(x + rx, y + height, form);
                            gcodeMoveTo(x + width - rx, y + height, form);
                            if (rx > 0) gcodeArcToCCW(x + width, y + height - ry, 0, -ry, form, avoidG23);  // +ry
                            gcodeMoveTo(x + width, y + ry, form);                        // upper right
                            if (rx > 0) gcodeArcToCCW(x + width - rx, y, -rx, 0, form, avoidG23);
                            gcodeMoveTo(x + rx, y, form);                                // upper left
                            if (rx > 0) gcodeArcToCCW(x, y + ry, 0, ry, form, avoidG23);                    // -ry
                            gcodeMoveTo(x, y + height - ry, form);                       // lower left
                            if (rx > 0) gcodeArcToCCW(x + rx, y + height, rx, 0, form, avoidG23);

                            gcodeMoveTo(x + rx, y + height, form);  // repeat first point to avoid back draw after last G3
                            gcodeStopPath(form);
                        }
                        else if (form == "circle")
                        {
                            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( circle cx:{0} cy:{1} r:{2} )\r\n", cx, cy, r);
                            cx += offsetX; cy += offsetY;
                            gcodeStartPath(cx + r, cy, form);
                            gcodeArcToCCW(cx + r, cy, -r, 0, form, avoidG23);
                            gcodeStopPath(form);
                        }
                        else if (form == "ellipse")
                        {
                            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( ellipse cx:{0} cy:{1} rx:{2}  ry:{2})\r\n", cx, cy, rx, ry);
                            cx += offsetX; cy += offsetY;
                            gcodeStartPath(cx + rx, cy, form);
                            isReduceOk = true;
                            calcArc(cx + rx, cy, rx, ry, 0, 1, 1, cx - rx, cy);
                            calcArc(cx - rx, cy, rx, ry, 0, 1, 1, cx + rx, cy);
                            gcodeStopPath(form);

                        }
                        else if (form == "line")
                        {
                            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( SVG-Line x1:{0} y1:{1} x2:{2} y2:{3} )\r\n", x1, y1, x2, y2);
                            x1 += offsetX; y1 += offsetY;
                            gcodeStartPath(x1, y1, form);
                            gcodeMoveTo(x2, y2, form);
                            gcodeStopPath(form);
                        }
                        else if ((form == "polyline") || (form == "polygon"))
                        {
                            offsetX = 0;// (float)matrixElement.OffsetX;
                            offsetY = 0;// (float)matrixElement.OffsetY;
                            if (svgComments) gcodeString[gcodeStringIndex].AppendFormat("( SVG-Polyline )\r\n");
                            int index = 0;
                            for (index = 0; index < points.Length; index++)
                            {
                                if (points[index].Length > 0)
                                    break;
                            }
                            if (points[index].IndexOf(",") >= 0)
                            {
                                string[] coord = points[index].Split(',');
                                x = floatParse(coord[0]); y = floatParse(coord[1]);
                                x1 = x; y1 = y;
                                gcodeStartPath(x, y, form);
                                isReduceOk = true;
                                for (int i = index + 1; i < points.Length; i++)
                                {
                                    if (points[i].Length > 3)
                                    {
                                        coord = points[i].Split(',');
                                        x = floatParse(coord[0]); y = floatParse(coord[1]);
                                        x += offsetX; y += offsetY;
                                        gcodeMoveTo(x, y, form);
                                    }
                                }
                                if (form == "polygon")
                                    gcodeMoveTo(x1, y1, form);
                                gcodeStopPath(form);
                            }
                            else
                                gcodeString[gcodeStringIndex].AppendLine("( polygon coordinates - missing ',')");
                        }
                        else if ((form == "text") || (form == "image"))
                        {
                            gcodeString[gcodeStringIndex].AppendLine("( +++++++++++++++++++++++++++++++++ )");
                            gcodeString[gcodeStringIndex].AppendLine("( ++++++ " + form + " is not supported ++++ )");
                            if (form == "text")
                            {
                                gcodeString[gcodeStringIndex].AppendLine("( ++ Convert Object to Path first + )");
                            }
                            gcodeString[gcodeStringIndex].AppendLine("( +++++++++++++++++++++++++++++++++ )");
                        }
                        else
                        { if (svgComments) gcodeString[gcodeStringIndex].Append("( ++++++ Unknown Shape: " + form + " )"); }

                        matrixElement = oldMatrixElement;
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Convert all Path commands, check: http://www.w3.org/TR/SVG/paths.html
        /// Split command tokens
        /// </summary>
        private static void parsePath(XElement svgCode, int level)
        {
            foreach (var pathElement in svgCode.Elements(nspace + "path"))
            {
                if (pathElement != null)
                {
                    offsetX = 0;// (float)matrixElement.OffsetX;
                    offsetY = 0;// (float)matrixElement.OffsetY;
                    currentX = offsetX; currentY = offsetX;
                    firstX = null; firstY = null;
                    startPath = true;
                    startSubPath = true;
                    lastX = offsetX; lastY = offsetY;
                    string d = pathElement.Attribute("d").Value;
                    string id = d;
                    if (id.Length > 20)
                        id = id.Substring(0, 20);

                    string myColor = getColor(pathElement);
                    int myTool = svgPalette.getToolNr(myColor, 0);// svgToolTable[myIndex].toolnr;

                    if ((svgToolSort) && (myTool >= 0))
                    {
                        if (penIsDown) gcodePenUp("Start path");
                        gcodeStringIndex = myTool;
                    }

                    // gcodeString[gcodeStringIndex].Append("( Start path )\r\n");
                    if (svgComments)
                    {
                        if (pathElement.Attribute("id") != null)
                            gcodeString[gcodeStringIndex].Append("\r\n( Path level:" + level.ToString() + " id=" + pathElement.Attribute("id").Value + " )\r\n");
                        else
                            gcodeString[gcodeStringIndex].Append("\r\n( SVG path=" + id + " )\r\n");
                        gcodeString[gcodeStringIndex].AppendFormat("\r\n(SVG color=#{0})\r\n", myColor);
                    }

                    if (pathElement.Attribute("id") != null)
                        id = pathElement.Attribute("id").Value;

                    oldMatrixElement = matrixElement;
                    parseTransform(pathElement, false, level);        // transform will be applied in gcodeMove

                    gcodeTool(myTool);

                    if (d.Length > 0)
                    {
                        // split complete path in to command-tokens
                        if (svgPauseElement || svgPausePenDown) { gcode.Pause(gcodeString[gcodeStringIndex], "Pause before path"); }
                        string separators = @"(?=[A-Za-z-[e]])";
                        var tokens = Regex.Split(d, separators).Where(t => !string.IsNullOrEmpty(t));
                        int objCount = 0;
                        foreach (string token in tokens)
                            objCount += parsePathCommand(token);
                    }
                    gcodePenUp("End path");

                    matrixElement = oldMatrixElement;
                }
            }
            return;
        }

        private static bool penIsDown = true;
        private static bool startSubPath = true;
        private static int countSubPath = 0;
        private static bool startPath = true;
        private static bool startFirstElement = true;
        private static float svgWidthPx, svgHeightPx;
        private static float offsetX, offsetY;
        private static float currentX, currentY;
        private static float? firstX, firstY;
        private static float lastX, lastY;
        private static float cxMirror = 0, cyMirror = 0;
        private static StringBuilder secondMove = new StringBuilder();

        /// <summary>
        /// Convert all Path commands, check: http://www.w3.org/TR/SVG/paths.html
        /// Convert command tokens
        /// </summary>
        private static int parsePathCommand(string svgPath)
        {
            var command = svgPath.Take(1).Single();
            char cmd = char.ToUpper(command);
            bool absolute = (cmd == command);
            string remainingargs = svgPath.Substring(1);
            string argSeparators = @"[\s,]|(?=(?<!e)-)";// @"[\s,]|(?=-)|(-{,2})";        // support also -1.2e-3 orig. @"[\s,]|(?=-)"; 
            var splitArgs = Regex
                .Split(remainingargs, argSeparators)
                .Where(t => !string.IsNullOrEmpty(t));
            // get command coordinates
            float[] floatArgs = splitArgs.Select(arg => floatParse(arg)).ToArray();
            int objCount = 0;

            switch (cmd)
            {
                case 'M':       // Start a new sub-path at the given (x,y) coordinate
                    for (int i = 0; i < floatArgs.Length; i += 2)
                    {
                        objCount++;
                        if (absolute || startPath)
                        { currentX = floatArgs[i] + offsetX; currentY = floatArgs[i + 1] + offsetY; }
                        else
                        { currentX = floatArgs[i] + lastX; currentY = floatArgs[i + 1] + lastY; }
                        if (startSubPath)
                        {
                            if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Start new subpath at {0} {1} )\r\n", floatArgs[i], floatArgs[i + 1]); }
                            //                            pathCount = 0;
                            if (countSubPath++ > 0)
                                gcodeStopPath("Stop Path");
                            if (svgNodesOnly)
                                gcodeDotOnly(currentX, currentY, command.ToString());
                            else
                                gcodeStartPath(currentX, currentY, command.ToString());
                            isReduceOk = true;
                            firstX = currentX; firstY = currentY;
                            startPath = false;
                            startSubPath = false;
                        }
                        else
                        {
                            if (svgNodesOnly)
                                gcodeDotOnly(currentX, currentY, command.ToString());
                            else if (i <= 1)
                            { gcodeStartPath(currentX, currentY, command.ToString()); }//gcodeMoveTo(currentX, currentY, command.ToString());  // G1
                            else
                                gcodeMoveTo(currentX, currentY, command.ToString());  // G1
                        }
                        if (firstX == null) { firstX = currentX; }
                        if (firstY == null) { firstY = currentY; }
                        lastX = currentX; lastY = currentY;
                    }
                    cxMirror = currentX; cyMirror = currentY;
                    break;

                case 'Z':       // Close the current subpath
                    if (!svgNodesOnly)
                    {
                        if (firstX == null) { firstX = currentX; }
                        if (firstY == null) { firstY = currentY; }
                        gcodeMoveTo((float)firstX, (float)firstY, command.ToString());    // G1
                    }
                    lastX = (float)firstX; lastY = (float)firstY;
                    firstX = null; firstY = null;
                    startSubPath = true;
                    if ((svgClosePathExtend) && (!svgNodesOnly))
                    { gcodeString[gcodeStringIndex].Append(secondMove); }
                    gcodeStopPath("Z");
                    break;

                case 'L':       // Draw a line from the current point to the given (x,y) coordinate
                    for (int i = 0; i < floatArgs.Length; i += 2)
                    {
                        objCount++;
                        if (absolute)
                        { currentX = floatArgs[i] + offsetX; currentY = floatArgs[i + 1] + offsetY; }
                        else
                        { currentX = lastX + floatArgs[i]; currentY = lastY + floatArgs[i + 1]; }
                        if (svgNodesOnly)
                            gcodeDotOnly(currentX, currentY, command.ToString());
                        else
                            gcodeMoveTo(currentX, currentY, command.ToString());
                        lastX = currentX; lastY = currentY;
                        cxMirror = currentX; cyMirror = currentY;
                    }
                    startSubPath = true;
                    break;

                case 'H':       // Draws a horizontal line from the current point (cpx, cpy) to (x, cpy)
                    for (int i = 0; i < floatArgs.Length; i++)
                    {
                        objCount++;
                        if (absolute)
                        { currentX = floatArgs[i] + offsetX; currentY = lastY; }
                        else
                        { currentX = lastX + floatArgs[i]; currentY = lastY; }
                        if (svgNodesOnly)
                            gcodeDotOnly(currentX, currentY, command.ToString());
                        else
                            gcodeMoveTo(currentX, currentY, command.ToString());
                        lastX = currentX; lastY = currentY;
                        cxMirror = currentX; cyMirror = currentY;
                    }
                    startSubPath = true;
                    break;

                case 'V':       // Draws a vertical line from the current point (cpx, cpy) to (cpx, y)
                    for (int i = 0; i < floatArgs.Length; i++)
                    {
                        objCount++;
                        if (absolute)
                        { currentX = lastX; currentY = floatArgs[i] + offsetY; }
                        else
                        { currentX = lastX; currentY = lastY + floatArgs[i]; }
                        if (svgNodesOnly)
                            gcodeDotOnly(currentX, currentY, command.ToString());
                        else
                            gcodeMoveTo(currentX, currentY, command.ToString());
                        lastX = currentX; lastY = currentY;
                        cxMirror = currentX; cyMirror = currentY;
                    }
                    startSubPath = true;
                    break;

                case 'A':       // Draws an elliptical arc from the current point to (x, y)
                    if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Command {0} {1} )\r\n", command.ToString(), ((absolute == true) ? "absolute" : "relative")); }
                    for (int rep = 0; rep < floatArgs.Length; rep += 7)
                    {
                        objCount++;
                        if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( draw arc nr. {0} )\r\n", (1 + rep / 6)); }
                        float rx, ry, rot, large, sweep, nx, ny;
                        rx = floatArgs[rep]; ry = floatArgs[rep + 1];
                        rot = floatArgs[rep + 2];
                        large = floatArgs[rep + 3];
                        sweep = floatArgs[rep + 4];
                        if (absolute)
                        {
                            nx = floatArgs[rep + 5] + offsetX; ny = floatArgs[rep + 6] + offsetY;
                        }
                        else
                        {
                            nx = floatArgs[rep + 5] + lastX; ny = floatArgs[rep + 6] + lastY;
                        }
                        if (svgNodesOnly)
                            gcodeDotOnly(currentX, currentY, command.ToString());
                        else
                            calcArc(lastX, lastY, rx, ry, rot, large, sweep, nx, ny);
                        lastX = nx; lastY = ny;
                    }
                    startSubPath = true;
                    break;

                case 'C':       // Draws a cubic Bézier curve from the current point to (x,y)
                    if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Command {0} {1} )\r\n", command.ToString(), ((absolute == true) ? "absolute" : "relative")); }
                    for (int rep = 0; rep < floatArgs.Length; rep += 6)
                    {
                        objCount++;
                        if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( draw curve nr. {0} )\r\n", (1 + rep / 6)); }
                        if ((rep + 5) < floatArgs.Length)
                        {
                            float cx1, cy1, cx2, cy2, cx3, cy3;
                            if (absolute)
                            {
                                cx1 = floatArgs[rep] + offsetX; cy1 = floatArgs[rep + 1] + offsetY;
                                cx2 = floatArgs[rep + 2] + offsetX; cy2 = floatArgs[rep + 3] + offsetY;
                                cx3 = floatArgs[rep + 4] + offsetX; cy3 = floatArgs[rep + 5] + offsetY;
                            }
                            else
                            {
                                cx1 = lastX + floatArgs[rep]; cy1 = lastY + floatArgs[rep + 1];
                                cx2 = lastX + floatArgs[rep + 2]; cy2 = lastY + floatArgs[rep + 3];
                                cx3 = lastX + floatArgs[rep + 4]; cy3 = lastY + floatArgs[rep + 5];
                            }
                            points = new Point[4];
                            points[0] = new Point(lastX, lastY);
                            points[1] = new Point(cx1, cy1);
                            points[2] = new Point(cx2, cy2);
                            points[3] = new Point(cx3, cy3);
                            var b = GetBezierApproximation(points, svgBezierAccuracy);
                            if (svgNodesOnly)
                            {
                                //gcodeDotOnly(cx1, cy1, command.ToString());
                                //gcodeDotOnly(cx2, cy2, command.ToString());
                                gcodeDotOnly(cx3, cy3, command.ToString());
                            }
                            else
                            {
                                for (int i = 1; i < b.Points.Count; i++)
                                    gcodeMoveTo((float)b.Points[i].X, (float)b.Points[i].Y, command.ToString());
                            }
                            cxMirror = cx3 - (cx2 - cx3); cyMirror = cy3 - (cy2 - cy3);
                            lastX = cx3; lastY = cy3;
                        }
                        else
                        { gcodeString[gcodeStringIndex].AppendFormat("( Missing argument after {0} )\r\n", rep); }
                    }
                    startSubPath = true;
                    break;

                case 'S':       // Draws a cubic Bézier curve from the current point to (x,y)
                    if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Command {0} {1} )\r\n", command.ToString(), ((absolute == true) ? "absolute" : "relative")); }
                    for (int rep = 0; rep < floatArgs.Length; rep += 4)
                    {
                        objCount++;
                        if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( draw curve nr. {0} )\r\n", (1 + rep / 4)); }
                        float cx2, cy2, cx3, cy3;
                        if (absolute)
                        {
                            cx2 = floatArgs[rep] + offsetX; cy2 = floatArgs[rep + 1] + offsetY;
                            cx3 = floatArgs[rep + 2] + offsetX; cy3 = floatArgs[rep + 3] + offsetY;
                        }
                        else
                        {
                            cx2 = lastX + floatArgs[rep]; cy2 = lastY + floatArgs[rep + 1];
                            cx3 = lastX + floatArgs[rep + 2]; cy3 = lastY + floatArgs[rep + 3];
                        }
                        points = new Point[4];
                        points[0] = new Point(lastX, lastY);
                        points[1] = new Point(cxMirror, cyMirror);
                        points[2] = new Point(cx2, cy2);
                        points[3] = new Point(cx3, cy3);
                        var b = GetBezierApproximation(points, svgBezierAccuracy);
                        if (svgNodesOnly)
                        {
                            //gcodeDotOnly(cxMirror, cyMirror, command.ToString());
                            //gcodeDotOnly(cx2, cy2, command.ToString());
                            gcodeDotOnly(cx3, cy3, command.ToString());
                        }
                        else
                        {
                            for (int i = 1; i < b.Points.Count; i++)
                                gcodeMoveTo((float)b.Points[i].X, (float)b.Points[i].Y, command.ToString());
                        }
                        cxMirror = cx3 - (cx2 - cx3); cyMirror = cy3 - (cy2 - cy3);
                        lastX = cx3; lastY = cy3;
                    }
                    startSubPath = true;
                    break;

                case 'Q':       // Draws a quadratic Bézier curve from the current point to (x,y)
                    if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Command {0} {1} )\r\n", command.ToString(), ((absolute == true) ? "absolute" : "relative")); }
                    for (int rep = 0; rep < floatArgs.Length; rep += 4)
                    {
                        objCount++;
                        if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( draw curve nr. {0} )\r\n", (1 + rep / 4)); }
                        float cx2, cy2, cx3, cy3;
                        if (absolute)
                        {
                            cx2 = floatArgs[rep] + offsetX; cy2 = floatArgs[rep + 1] + offsetY;
                            cx3 = floatArgs[rep + 2] + offsetX; cy3 = floatArgs[rep + 3] + offsetY;
                        }
                        else
                        {
                            cx2 = lastX + floatArgs[rep]; cy2 = lastY + floatArgs[rep + 1];
                            cx3 = lastX + floatArgs[rep + 2]; cy3 = lastY + floatArgs[rep + 3];
                        }

                        float qpx1 = (cx2 - lastX) * 2 / 3 + lastX;     // shorten control points to 2/3 length to use 
                        float qpy1 = (cy2 - lastY) * 2 / 3 + lastY;     // qubic function
                        float qpx2 = (cx2 - cx3) * 2 / 3 + cx3;
                        float qpy2 = (cy2 - cy3) * 2 / 3 + cy3;
                        points = new Point[4];
                        points[0] = new Point(lastX, lastY);
                        points[1] = new Point(qpx1, qpy1);
                        points[2] = new Point(qpx2, qpy2);
                        points[3] = new Point(cx3, cy3);
                        cxMirror = cx3 - (cx2 - cx3); cyMirror = cy3 - (cy2 - cy3);
                        lastX = cx3; lastY = cy3;
                        var b = GetBezierApproximation(points, svgBezierAccuracy);
                        if (svgNodesOnly)
                        {
                            //gcodeDotOnly(qpx1, qpy1, command.ToString());
                            //gcodeDotOnly(qpx2, qpy2, command.ToString());
                            gcodeDotOnly(cx3, cy3, command.ToString());
                        }
                        else
                        {
                            for (int i = 1; i < b.Points.Count; i++)
                                gcodeMoveTo((float)b.Points[i].X, (float)b.Points[i].Y, command.ToString());
                        }
                    }
                    startSubPath = true;
                    break;

                case 'T':       // Draws a quadratic Bézier curve from the current point to (x,y)
                    if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( Command {0} {1} )\r\n", command.ToString(), ((absolute == true) ? "absolute" : "relative")); }
                    for (int rep = 0; rep < floatArgs.Length; rep += 2)
                    {
                        objCount++;
                        if (svgComments) { gcodeString[gcodeStringIndex].AppendFormat("( draw curve nr. {0} )\r\n", (1 + rep / 2)); }
                        float cx3, cy3;
                        if (absolute)
                        {
                            cx3 = floatArgs[rep] + offsetX; cy3 = floatArgs[rep + 1] + offsetY;
                        }
                        else
                        {
                            cx3 = lastX + floatArgs[rep]; cy3 = lastY + floatArgs[rep + 1];
                        }

                        float qpx1 = (cxMirror - lastX) * 2 / 3 + lastX;     // shorten control points to 2/3 length to use 
                        float qpy1 = (cyMirror - lastY) * 2 / 3 + lastY;     // qubic function
                        float qpx2 = (cxMirror - cx3) * 2 / 3 + cx3;
                        float qpy2 = (cyMirror - cy3) * 2 / 3 + cy3;
                        points = new Point[4];
                        points[0] = new Point(lastX, lastY);
                        points[1] = new Point(qpx1, qpy1);
                        points[2] = new Point(qpx2, qpy2);
                        points[3] = new Point(cx3, cy3);
                        cxMirror = cx3; cyMirror = cy3;
                        lastX = cx3; lastY = cy3;
                        var b = GetBezierApproximation(points, svgBezierAccuracy);
                        if (svgNodesOnly)
                        {
                            //gcodeDotOnly(qpx1, qpy1, command.ToString());
                            //gcodeDotOnly(qpx2, qpy2, command.ToString());
                            gcodeDotOnly(cx3, cy3, command.ToString());
                        }
                        else
                        {
                            for (int i = 1; i < b.Points.Count; i++)
                                gcodeMoveTo((float)b.Points[i].X, (float)b.Points[i].Y, command.ToString());
                        }
                    }
                    startSubPath = true;
                    break;

                default:
                    if (svgComments) gcodeString[gcodeStringIndex].Append("( *********** unknown: " + command.ToString() + " ***** )\r\n");
                    break;
            }
            return objCount;
        }

        /// <summary>
        /// Calculate Path-Arc-Command - Code from https://github.com/vvvv/SVG/blob/master/Source/Paths/SvgArcSegment.cs
        /// </summary>
        private static void calcArc(float StartX, float StartY, float RadiusX, float RadiusY, float Angle, float Size, float Sweep, float EndX, float EndY)
        {
            if (RadiusX == 0.0f && RadiusY == 0.0f)
            {
                //              graphicsPath.AddLine(this.Start, this.End);
                return;
            }
            double sinPhi = Math.Sin(Angle * Math.PI / 180.0);
            double cosPhi = Math.Cos(Angle * Math.PI / 180.0);
            double x1dash = cosPhi * (StartX - EndX) / 2.0 + sinPhi * (StartY - EndY) / 2.0;
            double y1dash = -sinPhi * (StartX - EndX) / 2.0 + cosPhi * (StartY - EndY) / 2.0;
            double root;
            double numerator = RadiusX * RadiusX * RadiusY * RadiusY - RadiusX * RadiusX * y1dash * y1dash - RadiusY * RadiusY * x1dash * x1dash;
            float rx = RadiusX;
            float ry = RadiusY;
            if (numerator < 0.0)
            {
                float s = (float)Math.Sqrt(1.0 - numerator / (RadiusX * RadiusX * RadiusY * RadiusY));

                rx *= s;
                ry *= s;
                root = 0.0;
            }
            else
            {
                root = ((Size == 1 && Sweep == 1) || (Size == 0 && Sweep == 0) ? -1.0 : 1.0) * Math.Sqrt(numerator / (RadiusX * RadiusX * y1dash * y1dash + RadiusY * RadiusY * x1dash * x1dash));
            }
            double cxdash = root * rx * y1dash / ry;
            double cydash = -root * ry * x1dash / rx;
            double cx = cosPhi * cxdash - sinPhi * cydash + (StartX + EndX) / 2.0;
            double cy = sinPhi * cxdash + cosPhi * cydash + (StartY + EndY) / 2.0;
            double theta1 = CalculateVectorAngle(1.0, 0.0, (x1dash - cxdash) / rx, (y1dash - cydash) / ry);
            double dtheta = CalculateVectorAngle((x1dash - cxdash) / rx, (y1dash - cydash) / ry, (-x1dash - cxdash) / rx, (-y1dash - cydash) / ry);
            if (Sweep == 0 && dtheta > 0)
            {
                dtheta -= 2.0 * Math.PI;
            }
            else if (Sweep == 1 && dtheta < 0)
            {
                dtheta += 2.0 * Math.PI;
            }
            int segments = (int)Math.Ceiling((double)Math.Abs(dtheta / (Math.PI / 2.0)));
            double delta = dtheta / segments;
            double t = 8.0 / 3.0 * Math.Sin(delta / 4.0) * Math.Sin(delta / 4.0) / Math.Sin(delta / 2.0);

            double startX = StartX;
            double startY = StartY;

            for (int i = 0; i < segments; ++i)
            {
                double cosTheta1 = Math.Cos(theta1);
                double sinTheta1 = Math.Sin(theta1);
                double theta2 = theta1 + delta;
                double cosTheta2 = Math.Cos(theta2);
                double sinTheta2 = Math.Sin(theta2);

                double endpointX = cosPhi * rx * cosTheta2 - sinPhi * ry * sinTheta2 + cx;
                double endpointY = sinPhi * rx * cosTheta2 + cosPhi * ry * sinTheta2 + cy;

                double dx1 = t * (-cosPhi * rx * sinTheta1 - sinPhi * ry * cosTheta1);
                double dy1 = t * (-sinPhi * rx * sinTheta1 + cosPhi * ry * cosTheta1);

                double dxe = t * (cosPhi * rx * sinTheta2 + sinPhi * ry * cosTheta2);
                double dye = t * (sinPhi * rx * sinTheta2 - cosPhi * ry * cosTheta2);

                points = new Point[4];
                points[0] = new Point(startX, startY);
                points[1] = new Point((startX + dx1), (startY + dy1));
                points[2] = new Point((endpointX + dxe), (endpointY + dye));
                points[3] = new Point(endpointX, endpointY);
                var b = GetBezierApproximation(points, svgBezierAccuracy);
                for (int k = 1; k < b.Points.Count; k++)
                    gcodeMoveTo(b.Points[k], "arc");
                //gcodeMove(1, (float)b.Points[k].X, (float)b.Points[k].Y, 0, 0, "arc");

                theta1 = theta2;
                startX = (float)endpointX;
                startY = (float)endpointY;
            }
        }
        private static double CalculateVectorAngle(double ux, double uy, double vx, double vy)
        {
            double ta = Math.Atan2(uy, ux);
            double tb = Math.Atan2(vy, vx);
            if (tb >= ta)
            { return tb - ta; }
            return Math.PI * 2 - (ta - tb);
        }
        // helper functions
        private static float fsqrt(float x) { return (float)Math.Sqrt(x); }
        private static float fvmag(float x, float y) { return fsqrt(x * x + y * y); }
        private static float fdistance(float x1, float y1, float x2, float y2) { return fvmag(x2 - x1, y2 - y1); }

        /// <summary>
        /// Calculate Bezier line segments
        /// from http://stackoverflow.com/questions/13940983/how-to-draw-bezier-curve-by-several-points
        /// </summary>
        private static Point[] points;
        private static PolyLineSegment GetBezierApproximation(Point[] controlPoints, int outputSegmentCount)
        {
            Point[] points = new Point[outputSegmentCount + 1];
            for (int i = 0; i <= outputSegmentCount; i++)
            {
                double t = (double)i / outputSegmentCount;
                points[i] = GetBezierPoint(t, controlPoints, 0, controlPoints.Length);
            }
            return new PolyLineSegment(points, true);
        }
        private static Point GetBezierPoint(double t, Point[] controlPoints, int index, int count)
        {
            if (count == 1)
                return controlPoints[index];
            var P0 = GetBezierPoint(t, controlPoints, index, count - 1);
            var P1 = GetBezierPoint(t, controlPoints, index + 1, count - 1);
            double x = (1 - t) * P0.X + t * P1.X;
            return new Point(x, (1 - t) * P0.Y + t * P1.Y);
        }

        // Prepare G-Code

        /// <summary>
        /// Transform XY coordinate using matrix and scale  
        /// </summary>
        /// <param name="pointStart">coordinate to transform</param>
        /// <returns>transformed coordinate</returns>
        private static Point translateXY(float x, float y)
        {
            Point coord = new Point(x, y);
            return translateXY(coord);
        }
        private static Point translateXY(Point pointStart)
        {
            Point pointResult = matrixElement.Transform(pointStart);
            return pointResult;
        }
        /// <summary>
        /// Transform I,J coordinate using matrix and scale  
        /// </summary>
        /// <param name="pointStart">coordinate to transform</param>
        /// <returns>transformed coordinate</returns>
        private static Point translateIJ(float i, float j)
        {
            Point coord = new Point(i, j);
            return translateIJ(coord);
        }
        private static Point translateIJ(Point pointStart)
        {
            Point pointResult = pointStart;
            double tmp_i = pointStart.X, tmp_j = pointStart.Y;
            pointResult.X = tmp_i * matrixElement.M11 + tmp_j * matrixElement.M21;  // - tmp
            pointResult.Y = tmp_i * matrixElement.M12 + tmp_j * matrixElement.M22; // tmp_i*-matrix     // i,j are relative - no offset needed, but perhaps rotation
            return pointResult;
        }


        private static void gcodeDotOnly(float x, float y, string cmt)
        {
            gcodeStartPath(x, y, cmt);
            gcodePenDown(cmt);
            gcodePenUp(cmt);
            //            gcode.PenUp(gcodeString[gcodeStringIndex], cmt);
            //            penIsDown = false;
        }


        /// <summary>
        /// Insert G0 and Pen down gcode command
        /// </summary>
        private static void gcodeStartPath(float x, float y, string cmt)
        {
            Point coord = translateXY(x, y);
            lastGCX = coord.X; lastGCY = coord.Y;
            lastSetGCX = coord.X; lastSetGCY = coord.Y;
            gcode.MoveToRapid(gcodeString[gcodeStringIndex], coord, cmt);
            if (svgPausePenDown) { gcode.Pause(gcodeString[gcodeStringIndex], "Pause before Pen Down"); }
            //            gcode.PenDown(gcodeString[gcodeStringIndex], cmt);
            //            penIsDown = true;
            penIsDown = false;
            isReduceOk = false;
        }
        /// <summary>
        /// Insert Pen-up gcode command
        /// </summary>
        private static void gcodeStopPath(string cmt)
        {
            if (gcodeReduce)
            {
                if ((lastSetGCX != lastGCX) || (lastSetGCY != lastGCY)) // restore last skipped point for accurat G2/G3 use
                    gcode.MoveTo(gcodeString[gcodeStringIndex], new System.Windows.Point(lastGCX, lastGCY), "restore Point");
            }
            //            gcode.PenUp(gcodeString[gcodeStringIndex], cmt);
            //            penIsDown = false;
            gcodePenUp(cmt);
        }

        /// <summary>
        /// Insert G1 gcode command
        /// </summary>
        private static void gcodeMoveTo(float x, float y, string cmt)
        {
            Point coord = new Point(x, y);
            gcodeMoveTo(coord, cmt);
        }

        private static bool isReduceOk = false;
        private static bool rejectPoint = false;
        private static double lastGCX = 0, lastGCY = 0, lastSetGCX = 0, lastSetGCY = 0, distance;
        /// <summary>
        /// Insert G1 gcode command
        /// </summary>
        private static void gcodeMoveTo(Point orig, string cmt)
        {
            Point coord = translateXY(orig);
            rejectPoint = false;
            gcodePenDown(cmt);
            if (gcodeReduce && isReduceOk)
            {
                distance = Math.Sqrt(((coord.X - lastSetGCX) * (coord.X - lastSetGCX)) + ((coord.Y - lastSetGCY) * (coord.Y - lastSetGCY)));
                if (distance < gcodeReduceVal)      // discard actual G1 movement
                {
                    rejectPoint = true;
                }
                else
                {
                    lastSetGCX = coord.X; lastSetGCY = coord.Y;
                }
            }
            if (!gcodeReduce || !rejectPoint)       // write GCode
            {
                gcode.MoveTo(gcodeString[gcodeStringIndex], coord, cmt);
            }
            lastGCX = coord.X; lastGCY = coord.Y;
        }

        /// <summary>
        /// Insert G2/G3 gcode command
        /// </summary>
        private static void gcodeArcToCCW(float x, float y, float i, float j, string cmt, bool avoidG23 = false)
        {
            Point coordxy = translateXY(x, y);
            Point coordij = translateIJ(i, j);
            if (gcodeReduce && isReduceOk)      // restore last skipped point for accurat G2/G3 use
            {
                if ((lastSetGCX != lastGCX) || (lastSetGCY != lastGCY))
                    gcode.MoveTo(gcodeString[gcodeStringIndex], new System.Windows.Point(lastGCX, lastGCY), cmt);
            }
            gcode.Arc(gcodeString[gcodeStringIndex], 3, coordxy, coordij, cmt, avoidG23);
        }

        /// <summary>
        /// Insert Pen-up gcode command
        /// </summary>
        private static void gcodePenUp(string cmt)
        {
            if (penIsDown)
                gcode.PenUp(gcodeString[gcodeStringIndex], cmt);
            penIsDown = false;
        }
        private static void gcodePenDown(string cmt)
        {
            if (!penIsDown)
                gcode.PenDown(gcodeString[gcodeStringIndex], cmt);
            penIsDown = true;
        }

        /// <summary>
        /// Insert tool change gcode command
        /// </summary>
        private static void gcodeTool(int toolnr)
        {
            if (toolnr >= 0)
            {
                svgPalette.setUse(true);
                if (!svgToolSort)               // if sort, insert tool command later
                {
                    if (gcodeToolNr != toolnr)  // if tool already in use, don't select again
                    {
                        gcode.Tool(gcodeString[gcodeStringIndex], toolnr, svgPalette.getName());
                        gcodeToolNr = toolnr;
                    }
                }
            }
        }

        #endregion

        #region Métodos para la conversión del archivo de la clase GCodeFromText



        #endregion

        #region Métodos para la conversión del archivo de la clase gcodeRelated

        public static void setup()
        {
            setDecimalPlaces((int)Properties.Settings.Default.importGCDecPlaces);
            gcodeXYFeed = (float)Properties.Settings.Default.importGCXYFeed;

            gcodeComments = Properties.Settings.Default.importGCAddComments;
            gcodeSpindleToggle = Properties.Settings.Default.importGCSpindleToggle;
            gcodeSpindleSpeed = (float)Properties.Settings.Default.importGCSSpeed;
            if (Properties.Settings.Default.importGCSpindleCmd)
                gcodeSpindleCmd = "3";
            else
                gcodeSpindleCmd = "4";

            gcodeZApply = Properties.Settings.Default.importGCZEnable;
            gcodeZUp = (float)Properties.Settings.Default.importGCZUp;
            gcodeZDown = (float)Properties.Settings.Default.importGCZDown;
            gcodeZFeed = (float)Properties.Settings.Default.importGCZFeed;

            gcodePWMEnable = Properties.Settings.Default.importGCPWMEnable;
            gcodePWMUp = (float)Properties.Settings.Default.importGCPWMUp;
            gcodePWMDlyUp = (float)Properties.Settings.Default.importGCPWMDlyUp;
            gcodePWMDown = (float)Properties.Settings.Default.importGCPWMDown;
            gcodePWMDlyDown = (float)Properties.Settings.Default.importGCPWMDlyDown;

            gcodeIndividualTool = Properties.Settings.Default.importGCIndEnable;
            gcodeIndividualUp = Properties.Settings.Default.importGCIndPenUp;
            gcodeIndividualDown = Properties.Settings.Default.importGCIndPenDown;

            gcodeReduce = Properties.Settings.Default.importSVGReduce;
            gcodeReduceVal = (float)Properties.Settings.Default.importSVGReduceLimit;

            gcodeToolChange = Properties.Settings.Default.importGCTool;
            gcodeToolChangeM0 = Properties.Settings.Default.importGCToolM0;

            gcodeCompress = Properties.Settings.Default.importGCCompress;        // reduce code by 
            gcodeRelative = Properties.Settings.Default.importGCRelative;        // reduce code by 
            gcodeNoArcs = Properties.Settings.Default.importGCNoArcs;        // reduce code by 
            gcodeAngleStep = (float)Properties.Settings.Default.importGCSegment;

            gcodeLines = 1;             // counter for GCode lines
            gcodeDistance = 0;          // counter for GCode move distance
            gcodeDownUp = 0;            // counter for GCode Down/Up
            gcodeTime = 0;              // counter for GCode work time
            gcodePauseCounter = 0;      // counter for GCode pause M0 commands
            gcodeToolCounter = 0;
            gcodeToolText = "";
            lastx = -1; lasty = -1; lastz = 0;

            if (gcodeRelative)
            { lastx = 0; lasty = 0; }
        }

        public static bool reduceGCode
        {
            get { return gcodeCompress; }
            set
            {
                gcodeCompress = value;
                setDecimalPlaces((int)Properties.Settings.Default.importGCDecPlaces);
            }
        }

        public static void setDecimalPlaces(int num)
        {
            formatNumber = "0.";
            if (gcodeCompress)
                formatNumber = formatNumber.PadRight(num + 2, '#'); //'0'
            else
                formatNumber = formatNumber.PadRight(num + 2, '0'); //'0'
        }

        // get GCode one or two digits
        public static int getIntGCode(char code, string tmp)
        {
            string cmdG = getStrGCode(code, tmp);
            if (cmdG.Length > 0)
            { return Convert.ToInt16(cmdG.Substring(1)); }
            return -1;
        }
        public static string getStrGCode(char code, string tmp)
        {
            var cmdG = Regex.Matches(tmp, code + "\\d{1,2}");
            if (cmdG.Count > 0)
            { return cmdG[0].ToString(); }
            return "";
        }

        // get value from X,Y,Z F, S etc.
        public static int getIntValue(char code, string tmp)
        {
            string cmdG = getStringValue(code, tmp);
            //            MessageBox.Show(cmdG);
            if (cmdG.Length > 0)
            { return Convert.ToInt16(cmdG.Substring(1)); }
            return -1;
        }
        public static string getStringValue(char code, string tmp)
        {
            var cmdG = Regex.Matches(tmp, code + "-?\\d+(.\\d+)?");
            if (cmdG.Count > 0)
            { return cmdG[cmdG.Count - 1].ToString(); }
            return "";
        }

        public static string frmtCode(int number)      // convert int to string using format pattern
        { return number.ToString(formatCode); }
        public static string frmtNum(float number)     // convert float to string using format pattern
        { return number.ToString(formatNumber); }
        public static string frmtNum(double number)     // convert double to string using format pattern
        { return number.ToString(formatNumber); }

        private static bool gcodeReduce = false;        // if true remove G1 commands if distance is < limit
        private static float gcodeReduceVal = 0.1f;     // limit when to remove G1 commands
        private static StringBuilder secondMove = new StringBuilder();
        private static bool applyXYFeedRate = true; // apply XY feed after each Pen-move

        public static void Pause(StringBuilder gcodeString, string cmt = "")
        {
            if (cmt.Length > 0) cmt = string.Format("({0})", cmt);
            gcodeString.AppendFormat("M{0:00} {1}\r\n", 0, cmt);
            gcodeLines++;
            gcodePauseCounter++;
        }

        public static void SpindleOn(StringBuilder gcodeString, string cmt = "")
        {
            if (cmt.Length > 0) cmt = string.Format("({0})", cmt);
            gcodeString.AppendFormat("M{0} S{1} {2}\r\n", gcodeSpindleCmd, gcodeSpindleSpeed, cmt);
            gcodeLines++;
        }

        public static void SpindleOff(StringBuilder gcodeString, string cmt = "")
        {
            if (cmt.Length > 0) cmt = string.Format("({0})", cmt);
            gcodeString.AppendFormat("M{0} {1}\r\n", frmtCode(5), cmt);
            gcodeLines++;
        }

        public static void PenDown(StringBuilder gcodeString, string cmto = "")
        {
            string cmt = cmto;
            if (gcodeComments) { gcodeString.Append("\r\n"); }
            if (gcodeRelative) { cmt += string.Format("rel {0}", lastz); }
            if (cmt.Length > 0) { cmt = string.Format("({0})", cmt); }

            applyXYFeedRate = true;     // apply XY Feed Rate after each PenDown command (not just after Z-axis)

            if (gcodeSpindleToggle)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen down: Spindle-On");
                SpindleOn(gcodeString, cmto);
            }
            if (gcodeZApply)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen down: Z-Axis");
                float z_relative = gcodeZDown - lastz;
                if (gcodeRelative)
                    gcodeString.AppendFormat("G{0} Z{1} F{2} {3}\r\n", frmtCode(1), frmtNum(z_relative), gcodeZFeed, cmt);
                else
                    gcodeString.AppendFormat("G{0} Z{1} F{2} {3}\r\n", frmtCode(1), frmtNum(gcodeZDown), gcodeZFeed, cmt);

                gcodeTime += Math.Abs((gcodeZUp - gcodeZDown) / gcodeZFeed);
                gcodeLines++; lastg = 1; lastf = gcodeZFeed;
                //                applyXYFeedRate = true;
                lastz = gcodeZDown;
            }
            if (gcodePWMEnable)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen down: Servo control");
                gcodeString.AppendFormat("M{0} S{1} {2}\r\n", gcodeSpindleCmd, gcodePWMDown, cmt);
                gcodeString.AppendFormat("G{0} P{1}\r\n", frmtCode(4), frmtNum(gcodePWMDlyDown));
                gcodeTime += gcodePWMDlyDown;
                gcodeLines++;
            }
            if (gcodeIndividualTool)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen down: Individual Cmd");
                string[] commands = gcodeIndividualDown.Split(';');
                foreach (string cmd in commands)
                { gcodeString.AppendFormat("{0}\r\n", cmd.Trim()); }
                //                gcodeString.AppendFormat("{0}\r\n", gcodeIndividualDown);
            }
            if (gcodeComments) gcodeString.Append("\r\n");

            gcodeDownUp++;
        }

        public static void PenUp(StringBuilder gcodeString, string cmto = "")
        {
            string cmt = cmto;
            if (gcodeComments) { gcodeString.Append("\r\n"); }
            if (gcodeRelative) { cmt += string.Format("rel {0}", lastz); }
            if (cmt.Length > 0) { cmt = string.Format("({0})", cmt); }

            if (gcodeIndividualTool)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen up: Individual Cmd");
                string[] commands = gcodeIndividualUp.Split(';');
                foreach (string cmd in commands)
                { gcodeString.AppendFormat("{0}\r\n", cmd.Trim()); }
                //                gcodeString.AppendFormat("{0}\r\n", gcodeIndividualUp);
            }

            if (gcodePWMEnable)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen up: Servo control");
                gcodeString.AppendFormat("M{0} S{1} {2}\r\n", gcodeSpindleCmd, gcodePWMUp, cmt);
                gcodeString.AppendFormat("G{0} P{1}\r\n", frmtCode(4), frmtNum(gcodePWMDlyUp));
                gcodeTime += gcodePWMDlyUp;
                gcodeLines++;
            }

            if (gcodeZApply)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen up: Z-Axis");
                float z_relative = gcodeZUp - lastz;
                if (gcodeRelative)
                    gcodeString.AppendFormat("G{0} Z{1} {2}\r\n", frmtCode(0), frmtNum(z_relative), cmt); // use G0 without feedrate
                else
                    gcodeString.AppendFormat("G{0} Z{1} {2}\r\n", frmtCode(0), frmtNum(gcodeZUp), cmt); // use G0 without feedrate

                gcodeTime += Math.Abs((gcodeZUp - gcodeZDown) / gcodeZFeed);
                gcodeLines++; lastg = 1; lastf = gcodeZFeed;
                lastz = gcodeZUp;
            }

            if (gcodeSpindleToggle)
            {
                if (gcodeComments) gcodeString.AppendFormat("({0})\r\n", "Pen up: Spindle-Off");
                SpindleOff(gcodeString, cmto);
            }
            if (gcodeComments) gcodeString.Append("\r\n");
        }

        public static float lastx, lasty, lastz, lastg, lastf;

        public static void MoveTo(StringBuilder gcodeString, Point coord, string cmt = "")
        { Move(gcodeString, 1, (float)coord.X, (float)coord.Y, applyXYFeedRate, cmt); }
        public static void MoveTo(StringBuilder gcodeString, float x, float y, string cmt = "")
        { Move(gcodeString, 1, x, y, applyXYFeedRate, cmt); }
        public static void MoveTo(StringBuilder gcodeString, float x, float y, float z, string cmt = "")
        { Move(gcodeString, 1, x, y, z, applyXYFeedRate, cmt); }
        public static void MoveToRapid(StringBuilder gcodeString, Point coord, string cmt = "")
        { Move(gcodeString, 0, (float)coord.X, (float)coord.Y, false, cmt); }
        public static void MoveToRapid(StringBuilder gcodeString, float x, float y, string cmt = "")
        { Move(gcodeString, 0, x, y, false, cmt); }

        private static void Move(StringBuilder gcodeString, int gnr, float x, float y, bool applyFeed, string cmt)
        { Move(gcodeString, gnr, x, y, null, applyFeed, cmt); }
        private static void Move(StringBuilder gcodeString, int gnr, float x, float y, float? z, bool applyFeed, string cmt)
        {
            string feed = "";
            StringBuilder gcodeTmp = new StringBuilder();
            bool isneeded = false;
            float x_relative = x - lastx;
            float y_relative = y - lasty;
            float z_relative = lastz;
            float tz = 0;
            if (z != null)
            {
                z_relative = (float)z - lastz;
                tz = (float)z;
            }

            if (applyFeed && (gnr > 0))
            {
                feed = string.Format("F{0}", gcodeXYFeed);
                applyXYFeedRate = false;                        // don't set feed next time
            }
            if (cmt.Length > 0) cmt = string.Format("({0})", cmt);

            if (gcodeCompress)
            {
                if (((gnr > 0) || (lastx != x) || (lasty != y) || (lastz != tz)))  // else nothing to do
                {
                    if (lastg != gnr) { gcodeTmp.AppendFormat("G{0}", frmtCode(gnr)); isneeded = true; }
                    if (gcodeRelative)
                    {
                        if (lastx != x) { gcodeTmp.AppendFormat("X{0}", frmtNum(x_relative)); isneeded = true; }
                        if (lasty != y) { gcodeTmp.AppendFormat("Y{0}", frmtNum(y_relative)); isneeded = true; }
                        if (z != null)
                        {
                            if (lastz != z) { gcodeTmp.AppendFormat("Z{0}", frmtNum(z_relative)); isneeded = true; }
                        }
                    }
                    else
                    {
                        if (lastx != x) { gcodeTmp.AppendFormat("X{0}", frmtNum(x)); isneeded = true; }
                        if (lasty != y) { gcodeTmp.AppendFormat("Y{0}", frmtNum(y)); isneeded = true; }
                        if (z != null)
                        {
                            if (lastz != z) { gcodeTmp.AppendFormat("Z{0}", frmtNum((float)z)); isneeded = true; }
                        }
                    }

                    if ((gnr == 1) && (lastf != gcodeXYFeed) || applyFeed)
                    {
                        gcodeTmp.AppendFormat("F{0} ", gcodeXYFeed);
                        lastf = gcodeXYFeed;
                        isneeded = true;
                    }
                    gcodeTmp.AppendFormat("{0}\r\n", cmt);
                    if (isneeded)
                        gcodeString.Append(gcodeTmp);
                }
            }
            else
            {
                if (z != null)
                {
                    if (gcodeRelative)
                        gcodeString.AppendFormat("G{0} X{1} Y{2} Z{3} {4} {5}\r\n", frmtCode(gnr), frmtNum(x_relative), frmtNum(y_relative), frmtNum(z_relative), feed, cmt);
                    else
                        gcodeString.AppendFormat("G{0} X{1} Y{2} Z{3} {4} {5}\r\n", frmtCode(gnr), frmtNum(x), frmtNum(y), frmtNum((float)z), feed, cmt);
                }
                else
                {
                    if (gcodeRelative)
                        gcodeString.AppendFormat("G{0} X{1} Y{2} {3} {4}\r\n", frmtCode(gnr), frmtNum(x_relative), frmtNum(y_relative), feed, cmt);
                    else
                        gcodeString.AppendFormat("G{0} X{1} Y{2} {3} {4}\r\n", frmtCode(gnr), frmtNum(x), frmtNum(y), feed, cmt);
                }
            }
            //gcodeDistance += fdistance(lastx, lasty, x, y);
            gcodeTime += fdistance(lastx, lasty, x, y) / gcodeXYFeed;
            lastx = x; lasty = y; lastz = tz; lastg = gnr;
            gcodeLines++;
        }

        public static void splitLine(StringBuilder gcodeString, int gnr, float x1, float y1, float x2, float y2, float maxStep, bool applyFeed, string cmt = "")
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float c = (float)Math.Sqrt(dx * dx + dy * dy);
            float tmpX, tmpY;
            int divid = (int)Math.Ceiling(c / maxStep);
            lastg = -1;
            for (int i = 1; i <= divid; i++)
            {
                tmpX = x1 + i * dx / divid;
                tmpY = y1 + i * dy / divid;
                if (i > 1) { applyFeed = false; cmt = ""; }
                if (gnr == 0)
                { Move(gcodeString, gnr, tmpX, tmpY, false, cmt); }
                else
                { Move(gcodeString, gnr, tmpX, tmpY, applyFeed, cmt); }
            }
        }


        public static void Arc(StringBuilder gcodeString, int gnr, Point coordxy, Point coordij, string cmt = "", bool avoidG23 = false)
        { MoveArc(gcodeString, gnr, (float)coordxy.X, (float)coordxy.Y, (float)coordij.X, (float)coordij.Y, applyXYFeedRate, cmt, avoidG23); }
        public static void Arc(StringBuilder gcodeString, int gnr, float x, float y, float i, float j, string cmt = "", bool avoidG23 = false)
        { MoveArc(gcodeString, gnr, x, y, i, j, applyXYFeedRate, cmt, avoidG23); }
        private static void MoveArc(StringBuilder gcodeString, int gnr, float x, float y, float i, float j, bool applyFeed, string cmt = "", bool avoidG23 = false)
        {
            string feed = "";
            float x_relative = x - lastx;
            float y_relative = y - lasty;

            if (applyFeed)
            {
                feed = string.Format("F{0}", gcodeXYFeed);
                applyXYFeedRate = false;                        // don't set feed next time
            }
            if (cmt.Length > 0) cmt = string.Format("({0})", cmt);
            if (gcodeNoArcs || avoidG23)
            {
                splitArc(gcodeString, gnr, lastx, lasty, x, y, i, j, applyFeed, cmt);
            }
            else
            {
                if (gcodeRelative)
                    gcodeString.AppendFormat("G{0} X{1} Y{2}  I{3} J{4} {5} {6}\r\n", frmtCode(gnr), frmtNum(x_relative), frmtNum(y_relative), frmtNum(i), frmtNum(j), feed, cmt);
                else
                    gcodeString.AppendFormat("G{0} X{1} Y{2}  I{3} J{4} {5} {6}\r\n", frmtCode(gnr), frmtNum(x), frmtNum(y), frmtNum(i), frmtNum(j), feed, cmt);
                lastg = gnr;
            }
            //            gcodeDistance += fdistance(lastx, lasty, x, y);
            gcodeTime += fdistance(lastx, lasty, x, y) / gcodeXYFeed;
            lastx = x; lasty = y; lastf = gcodeXYFeed;
            gcodeLines++;
        }

        public static void splitArc(StringBuilder gcodeString, int gnr, float x1, float y1, float x2, float y2, float i, float j, bool applyFeed, string cmt = "")
        {
            float radius = (float)Math.Sqrt(i * i + j * j);					// get radius of circle
            float cx = x1 + i, cy = y1 + j;                                 // get center point of circle

            float cos1 = (float)i / radius;									// get start angle
            if (cos1 > 1) cos1 = 1;
            if (cos1 < -1) cos1 = -1;
            float a1 = 180 - 180 * (float)(Math.Acos(cos1) / Math.PI);

            if (j > 0) { a1 = -a1; }										// get stop angle
            float cos2 = (float)(x1 + i - x2) / radius;
            if (cos2 > 1) cos2 = 1;
            if (cos2 < -1) cos2 = -1;
            float a2 = 180 - 180 * (float)(Math.Acos(cos2) / Math.PI);

            if ((y1 + j - y2) > 0) { a2 = -a2; }							// get delta angle
            float da = -(360 + a1 - a2);
            if (gnr == 3) { da = Math.Abs(360 + a2 - a1); }
            if (da > 360) { da -= 360; }
            if (da < -360) { da += 360; }

            float step = (float)(Math.Asin((double)gcodeAngleStep / (double)radius) * 180 / Math.PI);
            //            Comment(gcodeString, radius.ToString()+" "+a1.ToString() + " " + a2.ToString() + " " + da.ToString() + " " + step.ToString());
            applyXYFeedRate = true;
            if (da > 0)                                             // if delta >0 go counter clock wise
            {
                for (float angle = (a1 + step); angle < (a1 + da); angle += step)
                {
                    float x = cx + radius * (float)Math.Cos(Math.PI * angle / 180);
                    float y = cy + radius * (float)Math.Sin(Math.PI * angle / 180);
                    MoveTo(gcodeString, x, y, cmt);
                    if (cmt.Length > 1) cmt = "";
                }
            }
            else                                                       // else go clock wise
            {
                for (float angle = (a1 - step); angle > (a1 + da); angle -= step)
                {
                    float x = cx + radius * (float)Math.Cos(Math.PI * angle / 180);
                    float y = cy + radius * (float)Math.Sin(Math.PI * angle / 180);
                    MoveTo(gcodeString, x, y, cmt);
                    if (cmt.Length > 1) cmt = "";
                }
            }
            MoveTo(gcodeString, x2, y2, "End Arc conversion");
        }

        public static void Tool(StringBuilder gcodeString, int toolnr, string cmt = "")
        {
            string toolCmd = "";
            if (gcodeToolChange)                // otherweise no command needed
            {
                if (cmt.Length > 0) cmt = string.Format("({0})", cmt);
                toolCmd = string.Format("M{0} T{1:D2} {2}", frmtCode(6), toolnr, cmt);
                if (gcodeToolChangeM0)
                { gcodeString.AppendFormat("M0 ({0})\r\n", toolCmd); }
                else
                { gcodeString.AppendFormat("{0}\r\n", toolCmd); }

                //                gcodeString.AppendFormat("M{0} T{1:D2} {2}\r\n", frmtCode(6), toolnr, cmt);
                gcodeToolCounter++;
                gcodeLines++;
                gcodeToolText += string.Format("( {0}) ToolNr: {1:D2}, Name: {2})\r\n", gcodeToolCounter, toolnr, cmt);
            }
        }

        public static string GetHeader(string cmt, string source = "")
        {
            gcodeTime += gcodeDistance / gcodeXYFeed;
            string header = "( " + cmt + " by GRBL-Plotter )\r\n";
            if (source.Length > 1)
                header += string.Format("( Source: {0} )\r\n", source);
            if (Properties.Settings.Default.importSVGRepeatEnable)
                header += string.Format("( G-Code repetitions: {0:0} times)\r\n", Properties.Settings.Default.importSVGRepeat);
            header += string.Format("( G-Code lines: {0} )\r\n", gcodeLines);
            header += string.Format("( Pen Down/Up : {0} times )\r\n", gcodeDownUp);
            header += string.Format("( Path length : {0:0.0} units )\r\n", gcodeDistance);
            header += string.Format("( Duration ca.: {0:0.0} min. )\r\n", gcodeTime);
            header += string.Format("( Setup : {0} )\r\n", gcodeLines);

            if (gcodeToolChange)
            {
                header += string.Format("( Tool changes: {0})\r\n", gcodeToolCounter);
                header += gcodeToolText;
            }
            if (gcodePauseCounter > 0)
                header += string.Format("( M0 count    : {0})\r\n", gcodePauseCounter);
            string[] commands = Properties.Settings.Default.importGCHeader.Split(';');
            foreach (string cmd in commands)
                if (cmd.Length > 1)
                { header += string.Format("{0} (Setup - GCode-Header)\r\n", cmd.Trim()); gcodeLines++; }
            if (gcodeRelative)
            { header += string.Format("G91 (Setup relative movement)\r\n"); gcodeLines++; }

            if (Properties.Settings.Default.importUnitGCode)
            {
                if (Properties.Settings.Default.importUnitmm)
                { header += "G21 (use mm as unit - check setup)"; }
                else
                { header += "G20 (use inch as unit - check setup)"; }
            }
            return header;
        }

        public static string GetFooter()
        {
            string footer = "";
            string[] commands = Properties.Settings.Default.importGCFooter.Split(';');
            foreach (string cmd in commands)
                if (cmd.Length > 1)
                { footer += string.Format("{0} (Setup - GCode-Footer)\r\n", cmd.Trim()); gcodeLines++; }
            return footer;
        }

        public static void Comment(StringBuilder gcodeString, string cmt)
        {
            if (cmt.Length > 1)
                gcodeString.AppendFormat("({0})\r\n", cmt);
        }

        // helper functions
        private static float fsqrt(float x) { return (float)Math.Sqrt(x); }
        private static float fvmag(float x, float y) { return fsqrt(x * x + y * y); }
        private static float fdistance(float x1, float y1, float x2, float y2) { return fvmag(x2 - x1, y2 - y1); }

        #endregion

        #region Métodos para la conversión del archivo de la clase svgPalette

        public static class svgPalette
        {
            private static int svgToolMax = 100;            // max amount of tools
            private static palette[] svgToolTable = new palette[svgToolMax];   // load color palette into this array
            private static int svgToolIndex = 0;            // last index
            private static bool svgToolColor = true;        // if true take tool nr. from nearest pallet entry
            private static bool svgToolSort = true;         // if true sort objects by tool-nr. (to avoid back and forth pen change)
            private static string svgPaletteFile = "";          // Path to GIMP plaette to use
            private static bool useException = false;
            private static int tmpIndex = 0;

            public static string getToolName(int index)
            {
                Array.Sort<palette>(svgToolTable, (x, y) => x.toolnr.CompareTo(y.toolnr));
                if (index < 0) index = 0;
                if (index >= svgToolIndex - 2) index = svgToolIndex - 2;
                return svgToolTable[index + 1].name;
            }
            public static void setToolCodeSize(int index, int size)
            {
                Array.Sort<palette>(svgToolTable, (x, y) => x.toolnr.CompareTo(y.toolnr));
                if (index < 0) index = 0;
                if (index >= svgToolIndex - 2) index = svgToolIndex - 2;
                svgToolTable[index + 1].codeSize = size;
            }
            public static void setIndex(int index)
            {
                if ((index >= 0) && (index < svgToolIndex))
                    tmpIndex = index;
            }
            public static int indexToolNr()
            { return svgToolTable[tmpIndex].toolnr; }
            public static bool indexUse()
            { return svgToolTable[tmpIndex].use; }
            public static string indexName()
            { return svgToolTable[tmpIndex].name; }

            public static void sortByToolNr()
            {
                Array.Sort<palette>(svgToolTable, (x, y) => x.toolnr.CompareTo(y.toolnr));    // sort by tool nr
            }
            public static void sortByCodeSize()
            {
                Array.Sort<palette>(svgToolTable, (x, y) => y.codeSize.CompareTo(x.codeSize));    // sort by size
            }
            public static void sortByPixelCount()
            {
                Array.Sort<palette>(svgToolTable, (x, y) => y.pixelCount.CompareTo(x.pixelCount));    // sort by size
            }

            // set tool / color table
            public static int init()    // return number of entries
            {
                svgToolColor = Properties.Settings.Default.importSVGToolColor;
                svgPaletteFile = Properties.Settings.Default.importPalette;
                svgToolSort = Properties.Settings.Default.importSVGToolSort;
                //            gcodeToolChange = Properties.Settings.Default.importGCTool;
                useException = false;
                Array.Resize(ref svgToolTable, svgToolMax);
                svgToolIndex = 2;
                svgToolTable[0].toolnr = -1;
                svgToolTable[0].clr = Color.White;
                svgToolTable[0].use = false;
                svgToolTable[0].diff = int.MaxValue;
                svgToolTable[0].name = "except";
                svgToolTable[0].pixelCount = 0;

                svgToolTable[1].toolnr = 0; svgToolTable[1].pixelCount = 0; svgToolTable[svgToolIndex].use = true; svgToolTable[1].clr = Color.Black; svgToolTable[1].diff = int.MaxValue; svgToolTable[1].name = "black";

                if (svgToolColor)
                {
                    if (File.Exists(svgPaletteFile))
                    {
                        string line, sr, sg, sb, cmt;
                        int ir, ig, ib;
                        System.IO.StreamReader file = new System.IO.StreamReader(svgPaletteFile);
                        while ((line = file.ReadLine()) != null)
                        {
                            if (line.Length > 11)
                            {
                                sr = line.Substring(0, 3);
                                sg = line.Substring(4, 3);
                                sb = line.Substring(8, 3);
                                cmt = line.Substring(12);
                                if (Int32.TryParse(sr, out ir) && Int32.TryParse(sg, out ig) && Int32.TryParse(sb, out ib))
                                {
                                    svgToolTable[svgToolIndex].toolnr = svgToolIndex - 1;
                                    svgToolTable[svgToolIndex].clr = System.Drawing.Color.FromArgb(255, ir, ig, ib);
                                    svgToolTable[svgToolIndex].use = false;
                                    svgToolTable[svgToolIndex].diff = int.MaxValue;
                                    svgToolTable[svgToolIndex].name = cmt;
                                    svgToolTable[svgToolIndex].pixelCount = 0;
                                    if (svgToolIndex < svgToolMax - 1) svgToolIndex++;
                                }
                            }
                        }
                        file.Close();
                        Array.Resize(ref svgToolTable, svgToolIndex);
                    }
                    else
                    {
                        //                   gcodeString[gcodeStringIndex].Append("(!!! SVG-Palette file not found - use black,r,g,b !!!)\r\n");
                        svgToolTable[1].toolnr = 0; svgToolTable[1].pixelCount = 0; svgToolTable[svgToolIndex].use = false; svgToolTable[1].clr = Color.Black; svgToolTable[1].diff = int.MaxValue; svgToolTable[1].name = "black";
                        svgToolTable[2].toolnr = 1; svgToolTable[2].pixelCount = 0; svgToolTable[svgToolIndex].use = false; svgToolTable[2].clr = Color.Red; svgToolTable[2].diff = int.MaxValue; svgToolTable[2].name = "red";
                        svgToolTable[3].toolnr = 2; svgToolTable[3].pixelCount = 0; svgToolTable[svgToolIndex].use = false; svgToolTable[3].clr = Color.Green; svgToolTable[3].diff = int.MaxValue; svgToolTable[3].name = "green";
                        svgToolTable[4].toolnr = 3; svgToolTable[4].pixelCount = 0; svgToolTable[svgToolIndex].use = false; svgToolTable[4].clr = Color.Blue; svgToolTable[4].diff = int.MaxValue; svgToolTable[4].name = "blue";
                        svgToolIndex = 5;
                        Array.Resize(ref svgToolTable, svgToolIndex);
                    }
                }
                else
                {
                    svgToolTable[1].toolnr = 0; svgToolTable[1].pixelCount = 0; svgToolTable[1].use = false; svgToolTable[1].clr = Color.Black; svgToolTable[1].diff = int.MaxValue; svgToolTable[1].name = "black";
                    svgToolIndex = 2;
                    Array.Resize(ref svgToolTable, svgToolIndex);
                }
                return svgToolIndex;
            }

            // set exception color
            public static string setExceptionColor(Color mycolor)
            {
                useException = true;
                Array.Sort<palette>(svgToolTable, (x, y) => x.toolnr.CompareTo(y.toolnr));    // sort by tool nr
                svgToolTable[0].toolnr = -1;
                svgToolTable[0].clr = mycolor;
                svgToolTable[0].use = false;
                svgToolTable[0].diff = int.MaxValue;
                svgToolTable[0].name = "not used";
                return svgToolTable[0].clr.ToString();
            }
            // Clear exception color
            public static void clrExceptionColor()
            { useException = false; }

            // return tool nr of nearest color
            public static int getToolNr(String mycolor, int mode)
            {
                if (mycolor == "")
                    return 0;
                int cr, cg, cb;
                int num = int.Parse(mycolor, System.Globalization.NumberStyles.AllowHexSpecifier);
                cb = num & 255; cg = num >> 8 & 255; cr = num >> 16 & 255;
                return getToolNr(Color.FromArgb(255, cr, cg, cb), mode);
            }
            public static int getToolNr(Color mycolor, int mode)
            {
                int i, start = 1;
                Array.Sort<palette>(svgToolTable, (x, y) => x.toolnr.CompareTo(y.toolnr));    // sort by tool nr
                if (useException) start = 0;  // first element is exception
                for (i = start; i < svgToolIndex; i++)
                {
                    if (mycolor == svgToolTable[i].clr)         // direct hit
                    {
                        tmpIndex = i;
                        return svgToolTable[i].toolnr;
                    }
                    else if (mode == 0)
                        svgToolTable[i].diff = ColorDiff(mycolor, svgToolTable[i].clr);
                    else if (mode == 1)
                        svgToolTable[i].diff = getHueDistance(mycolor.GetHue(), svgToolTable[i].clr.GetHue());
                    else
                        svgToolTable[i].diff = Math.Abs(ColorNum(svgToolTable[i].clr) - ColorNum(mycolor)) +
                                                  getHueDistance(svgToolTable[i].clr.GetHue(), mycolor.GetHue());
                }
                Array.Sort<palette>(svgToolTable, (x, y) => x.diff.CompareTo(y.diff));    // sort by color difference
                tmpIndex = 0;
                return svgToolTable[0].toolnr; ;   // return tool nr of nearest color
            }

            public static void countPixel()
            { svgToolTable[tmpIndex].pixelCount++; }

            public static int pixelCount()
            { return svgToolTable[tmpIndex].pixelCount; }

            public static Color getColor()
            { return svgToolTable[tmpIndex].clr; }

            public static void setUse(bool use)
            { svgToolTable[tmpIndex].use = use; }

            public static String getName()
            { return svgToolTable[tmpIndex].name; }

            // http://stackoverflow.com/questions/27374550/how-to-compare-color-object-and-get-closest-color-in-an-color
            // distance between two hues:
            private static float getHueDistance(float hue1, float hue2)
            { float d = Math.Abs(hue1 - hue2); return d > 180 ? 360 - d : d; }
            // color brightness as perceived:
            private static float getBrightness(Color c)
            { return (c.R * 0.299f + c.G * 0.587f + c.B * 0.114f) / 256f; }
            //  weighed only by saturation and brightness 
            private static float ColorNum(Color c)
            { return c.GetSaturation() * 5 + getBrightness(c) * 4; }
            // distance in RGB space
            private static int ColorDiff(Color c1, Color c2)
            {
                return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                       + (c1.G - c2.G) * (c1.G - c2.G)
                                       + (c1.B - c2.B) * (c1.B - c2.B));
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System.Windows.Forms;

/// <summary>
/// Repräsentiert einen Punkt in einem Skalarfeld.
/// </summary>
public class ScalarFieldPoint
{   
    /// <summary>
    /// Gibt die X-Koordinate des Punktes zurück.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gibt die Y-Koordinate des Punktes zurück.
    /// </summary>
    public double Y { get; }

     /// <summary>
    /// Gibt den Skalarwert an der Stelle zurück.
    /// </summary>
    public double Scalar { get; }

    // <summary>
    /// Initialisiert eine neue Instanz der Klasse <see cref="ScalarFieldPoint"/>.
    /// </summary>
    /// <param name="x">Die X-Koordinate des Punktes.</param>
    /// <param name="y">Die Y-Koordinate des Punktes.</param>
    /// <param name="scalar">Der Skalarwert an der Stelle.</param>
    public ScalarFieldPoint(double x, double y, double scalar)
    {
        X = x;
        Y = y;
        Scalar = scalar;
    }
}

/// <summary>
/// Repräsentiert ein Skalarfeld und bietet Interpolationsmethoden.
/// </summary>
public class ScalarField
{
    private readonly List<ScalarFieldPoint> _points;
    public double MinX { get; }
    public double MaxX { get; }
    public double MinY { get; }
    public double MaxY { get; }
    public int ResolutionX { get; }
    public int ResolutionY { get; }

    /// <summary>
    /// Initialisiert eine neue Instanz der Klasse <see cref="ScalarField"/>.
    /// </summary>
    /// <param name="points">Die Liste der Punkte, die das Skalarfeld definieren.</param>
    /// <param name="resolutionX">Die Auflösung in der X-Richtung.</param>
    /// <param name="resolutionY">Die Auflösung in der Y-Richtung.</param>
    public ScalarField(List<ScalarFieldPoint> points, int resolutionX, int resolutionY)
    {
        _points = points ?? throw new ArgumentNullException(nameof(points));
        ResolutionX = resolutionX;
        ResolutionY = resolutionY;

        MinX = _points.Min(p => p.X);
        MaxX = _points.Max(p => p.X);
        MinY = _points.Min(p => p.Y);
        MaxY = _points.Max(p => p.Y);
    }

    /// <summary>
    /// Führt eine bilineare Interpolation durch, um ein 2D-Array der interpolierten Werte zu erzeugen.
    /// </summary>
    /// <returns>Das 2D-Array der interpolierten Skalarwerte.</returns>
    public double[,] Interpolate()
    {
        double[,] result = new double[ResolutionX, ResolutionY];
        double stepX = (MaxX - MinX) / (ResolutionX - 1);
        double stepY = (MaxY - MinY) / (ResolutionY - 1);

        Parallel.For(0, ResolutionX, i =>
        {
            for (int j = 0; j < ResolutionY; j++)
            {
                double x = MinX + i * stepX;
                double y = MinY + j * stepY;
                result[i, j] = BilinearInterpolate(x, y);
            }
        });

        return result;
    }

    /// <summary>
    /// Führt eine bilineare Interpolation an den angegebenen Koordinaten durch.
    /// </summary>
    /// <param name="x">Die X-Koordinate.</param>
    /// <param name="y">Die Y-Koordinate.</param>
    /// <returns>Der interpolierte Skalarwert.</returns>
    private double BilinearInterpolate(double x, double y)
    {   
        
        ScalarFieldPoint q11 = _points.Where(p => p.X <= x && p.Y <= y)
                                      .OrderByDescending(p => p.X)
                                      .ThenByDescending(p => p.Y)
                                      .FirstOrDefault() ?? new ScalarFieldPoint(double.MinValue, double.MinValue, double.MinValue);

        ScalarFieldPoint q21 = _points.Where(p => p.X >= x && p.Y <= y)
                                      .OrderBy(p => p.X)
                                      .ThenByDescending(p => p.Y)
                                      .FirstOrDefault() ?? new ScalarFieldPoint(double.MaxValue, double.MinValue, double.MinValue);

        ScalarFieldPoint q12 = _points.Where(p => p.X <= x && p.Y >= y)
                                      .OrderByDescending(p => p.X)
                                      .ThenBy(p => p.Y)
                                      .FirstOrDefault() ?? new ScalarFieldPoint(double.MinValue, double.MaxValue, double.MinValue);

        ScalarFieldPoint q22 = _points.Where(p => p.X >= x && p.Y >= y)
                                      .OrderBy(p => p.X)
                                      .ThenBy(p => p.Y)
                                      .FirstOrDefault() ?? new ScalarFieldPoint(double.MaxValue, double.MaxValue, double.MinValue);

        
        double denom = (q21.X - q11.X) * (q12.Y - q11.Y);

        
        if (denom == 0) return q11.Scalar;

        
        return ((q21.X - x) * (q12.Y - y) * q11.Scalar +
                (x - q11.X) * (q12.Y - y) * q21.Scalar +
                (q21.X - x) * (y - q11.Y) * q12.Scalar +
                (x - q11.X) * (y - q11.Y) * q22.Scalar) / denom;
    }
}

/// <summary>
/// Bietet Methoden zum asynchronen Lesen von Skalarfeldpunkten aus einer CSV-Datei.
/// </summary>
public static class CsvReader
{   
     /// <summary>
    /// Liest Skalarfeldpunkte asynchron aus der angegebenen CSV-Datei.
    /// </summary>
    /// <param name="filePath">Der Pfad zur CSV-Datei.</param>
    /// <returns>Eine Liste von Skalarfeldpunkten, die aus der CSV-Datei gelesen wurden.</returns>
    public static async Task<List<ScalarFieldPoint>> ReadCsvAsync(string filePath)
    {   
        // Liste zum Speichern der Werte
        var points = new List<ScalarFieldPoint>();

        // Einlesen der Werte (falls möglich)
        try
        {   
            using (var reader = new StreamReader(filePath))
            {
                bool headerSkipped = false;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    // Überspringen der Kopfzeile der CSV-Datei
                    if (!headerSkipped)
                    {
                        headerSkipped = true;
                        continue;
                    }

                    // Aufteilung in X, Y, Value
                    var values = line?.Split(',');

                    if (values == null || values.Length != 3) continue;

                    // Werte in Datentyp double überführen
                    if (double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                        double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double scalar))
                    {   
                        // Werte zur Liste hinzufügen
                        points.Add(new ScalarFieldPoint(x, y, scalar));
                    }
                    else
                    {   
                        // Fehlermeldung
                        Console.WriteLine($"Failed to parse line: {line}");
                    }
                }
            }
        }
        catch (Exception ex)
        {   
            Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
        }

        return points;
    }
}

/// <summary>
/// Hauptprogramm zum Demonstrieren der Skalarfeldinterpolation und des Plottens.
/// </summary>
class Program
{   
    /// <summary>
    /// Einstiegspunkt des Programms.
    /// </summary>
    /// <param name="args">Befehlszeilenargumente.</param>
    static async Task Main(string[] args)
    {   
        // Aufruf zum Einlesen des Dateipfades
        Console.WriteLine("Please enter the file path:");
        string? filePath = Console.ReadLine();

        // Fehlerbetrachtung (kein Pfad eingegeben oder keine Datei vorhanden)
        if (filePath == null)
        {
            Console.WriteLine("No file path entered.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine("The file does not exist.");
            return;
        }

        // Aufruf der CsvReader-Klasse zum Einlesen der Werte
        var points = await CsvReader.ReadCsvAsync(filePath);

        // Auflösung
        const int resolutionX = 400;
        const int resolutionY = 400;

        // Festlegung des Titels der Karte
        string plotTitle = string.Empty;
        while (true)
        {
        Console.WriteLine("Bitte geben Sie den Titel für die Isolinienkarte ein:");
        plotTitle = Console.ReadLine() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(plotTitle))
        {
            break;
        }

        Console.WriteLine("Der Titel darf nicht leer sein. Bitte geben Sie einen gültigen Titel ein.");
        }
        
        var model = new PlotModel { Title = plotTitle };

        
        var field = new ScalarField(points, resolutionX, resolutionY);
        var interpolatedValues = field.Interpolate();

       
        double minScalar = points.Min(p => p.Scalar);
        double maxScalar = points.Max(p => p.Scalar);

        Console.WriteLine($"Min Value: {minScalar}, Max Value: {maxScalar}");

        // Erstellunf der Heatmap
        var heatmapSeries = new HeatMapSeries
        {
            X0 = field.MinX,
            X1 = field.MaxX,
            Y0 = field.MinY,
            Y1 = field.MaxY,
            Interpolate = true,
            RenderMethod = HeatMapRenderMethod.Bitmap,
            Data = interpolatedValues
        };

        model.Series.Add(heatmapSeries);

       // Erstellung der Isolinien
        var contourSeries = new ContourSeries
        {
            ColumnCoordinates = ArrayFromRange(field.MinX, field.MaxX, resolutionX),
            RowCoordinates = ArrayFromRange(field.MinY, field.MaxY, resolutionY),
            Data = interpolatedValues,
            ContourLevels = GenerateContourLevels(minScalar, maxScalar, 25),
            Color = OxyColors.Black,
        };

       
        model.Series.Add(contourSeries);
        model.Axes.Add(new LinearColorAxis { Position = AxisPosition.Right, Palette = OxyPalettes.Viridis(200) });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "X in m" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Y in m" });

       
        SavePlotAsImage(model, $"{plotTitle}.png");

       
        var plotView = new PlotView { Model = model };
        var form = new Form { ClientSize = new System.Drawing.Size(800, 600) };

        // Beenden des Programms, wenn Ansicht geschlossen wird
        
        form.FormClosed += (sender, e) => Application.Exit();
        form.Controls.Add(plotView);
        plotView.Dock = DockStyle.Fill;
        Application.Run(form);
    }

    
    /// <summary>
    /// Generiert ein Array von Werten zwischen min und max in der angegebenen Auflösung.
    /// </summary>
    /// <param name="min">Der minimale Wert.</param>
    /// <param name="max">Der maximale Wert.</param>
    /// <param name="count">Die Anzahl der Werte im Array.</param>
    /// <returns>Das Array mit den generierten Werten.</returns>
    static double[] ArrayFromRange(double min, double max, int count)
    {
        double[] result = new double[count];
        double step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            result[i] = min + i * step;
        }
        return result;
    }

     /// <summary>
    /// Generiert eine Liste von Isolinien-Ebenen zwischen min und max.
    /// </summary>
    /// <param name="min">Der minimale Skalarwert.</param>
    /// <param name="max">Der maximale Skalarwert.</param>
    /// <param name="count">Die Anzahl der Isolinien-Ebenen.</param>
    /// <returns>Die Liste der Isolinien-Ebenen.</returns>
    static double[] GenerateContourLevels(double min, double max, int levels)
    {
        double[] result = new double[levels];
        double step = (max - min) / (levels - 1);
        for (int i = 0; i < levels; i++)
        {
            result[i] = min + i * step;
        }
        return result;
    }


    //Methode zum Speichern der Karte
    static void SavePlotAsImage(PlotModel model, string filePath)
    {
        var pngExporter = new PngExporter { Width = 800, Height = 600 };
        pngExporter.ExportToFile(model, filePath);
        Console.WriteLine($"Isoline map saved as {filePath}");
    }
}

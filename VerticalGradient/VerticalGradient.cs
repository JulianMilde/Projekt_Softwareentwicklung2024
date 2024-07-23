using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace VertikalgradientBerechnung
{

    /// <summary>
    /// Analysiert die Anomalie, indem sie einen Vertikalgradient-Diagramm erstellt und eine Tiefenberechnung durchführt
    /// </summary>
    public class AnomalyAnalyzer
    {

        /// <summary>
        /// Stellt Methoden zum Lesen von Werten aus einer CSV-Datei zur Verfügung
        /// </summary>
        public class CSVReader
        {
            //Listen für die eingelesenen Werte der x-Koordinate und der magnetischen Feldstärke in zwei verschiedenen Höhen
            public List<double> XValues { get; private set; }
            public List<double> BH1Values { get; private set; }
            public List<double> BH2Values { get; private set; }

            /// <summary>
            /// Ruft Funktion zum Einlesen der CSV-Datei auf
            /// </summary>
            /// <param name="filePath">Der Pfad zur CSV-Datei.</param>
            public CSVReader(string filePath)
            {
                XValues = new List<double>();
                BH1Values = new List<double>();
                BH2Values = new List<double>();

                ReadCSV(filePath);
            }

            /// <summary>
            /// Funktion zum Einlesen der Werte aus der CSV-Datei
            /// </summary>
            /// <param name="filePath">Der Pfad zur CSV-Datei.</param>
            private void ReadCSV(string filePath)
            {
                var lines = File.ReadAllLines(filePath);
                var cultureInfo = new CultureInfo("en-US"); 

                for (int i = 1; i < lines.Length; i++) // erste Zeile überspringen
                {
                    var values = lines[i].Split(',');
                    XValues.Add(double.Parse(values[0], cultureInfo));
                    BH1Values.Add(double.Parse(values[1], cultureInfo));
                    BH2Values.Add(double.Parse(values[2], cultureInfo));
                }
            }
        }

        /// <summary>
        /// Zuständig für Berechnen der Gradienten und Tiefe der Anomalie
        /// </summary>
        public class GradientCalculator
        {
            public List<double> Gradients { get; private set; }
            public int MaxGradientIndex { get; private set; }

            /// <summary>
            /// Ruft Funktionen zum Berechnen der Gradienten und zum Suchen des Index des maximalen Gradienten auf
            /// </summary>
            /// <param name="bH1Values">Die Messwerte der magnetischen Feldstärke auf Höhe 1.</param>
            /// <param name="bH2Values">Die Messwerte der magnetischen Feldstärke auf Höhe 2.</param>
            /// <param name="h1">Die erste Höhe, in der gemessen wurde.</param>
            /// <param name="h2">Die zweite Höhe, in der gemessen wurde.</param>
            public GradientCalculator(List<double> bH1Values, List<double> bH2Values, double h1, double h2)
            {
                Gradients = new List<double>();
                CalculateGradients(bH1Values, bH2Values, h1, h2);
                FindMaxGradientIndex();
            }

            /// <summary>
            /// Funktion zum Berechnen der Gradienten
            /// </summary>
            /// <param name="bH1Values">Die Messwerte der magnetischen Feldstärke auf Höhe 1.</param>
            /// <param name="bH2Values">Die Messwerte der magnetischen Feldstärke auf Höhe 2.</param>
            /// <param name="h1">Die erste Höhe, in der gemessen wurde.</param>
            /// <param name="h2">Die zweite Höhe, in der gemessen wurde.</param>
            private void CalculateGradients(List<double> bH1Values, List<double> bH2Values, double h1, double h2)
            {
                for (int i = 0; i < bH1Values.Count; i++)
                {
                    double gradient = (bH2Values[i] - bH1Values[i]) / (h2 - h1);
                    Gradients.Add(gradient);
                }
            }

            /// <summary>
            /// Funktion zum Suchen des Index des maximalen Gradienten
            /// </summary>
            private void FindMaxGradientIndex()
            {
                double maxGradient = 0;
                for (int i = 0; i < Gradients.Count; i++)
                {
                    if (Math.Abs(Gradients[i]) > Math.Abs(maxGradient))
                    {
                        maxGradient = Gradients[i];
                        MaxGradientIndex = i;
                    }
                }
            }

            /// <summary>
            /// Funktion zum Berechnen der Tiefe der Anomalie
            /// </summary>
            /// <param name="h1">Die erste Höhe, in der gemessen wurde.</param>
            /// <param name="h2">Die zweite Höhe, in der gemessen wurde.</param>
            /// <param name="bH1">Die Messwerte der magnetischen Feldstärke auf Höhe 1.</param>
            /// <param name="bH2">Die Messwerte der magnetischen Feldstärke auf Höhe 2.</param>
            /// <returns></returns>
            public double CalculateDepth(double h1, double h2, double bH1, double bH2)
            {
                double depth = ((h2 - h1) * Math.Pow(bH1 / bH2, 0.333)) / (Math.Pow(bH1 / bH2, 0.333) - 1);
                return depth;
            }
        }

        /// <summary>
        /// Zuständig für das Diagramm des Vertikalgradienten
        /// </summary>
        public class PlotGenerator
        {
            private List<double> xValues;
            private List<double> gradients;

            /// <summary>
            /// Konstruktor für Klasse PlotGenerator
            /// </summary>
            /// <param name="xValues">Werte der x-Koordinate des Messgebiets.</param>
            /// <param name="gradients">Berechnete Gradienten.</param>
            public PlotGenerator(List<double> xValues, List<double> gradients)
            {
                this.xValues = xValues;
                this.gradients = gradients;
            }

            /// <summary>
            /// Erstellt mithilfe von Oxyplot das Diagramm des Vertikalgradienten
            /// </summary>
            public void CreatePlot()
            {
                // Die erste Messung wurde für das gesamte Messgebiet (16x16m) in Abständen von jeweils 2m vorgenommen.
                // Die zweite Messung wurde danach nur auf der Profillinie y = 6m vorgenommen, da auf dieser die höchsten Werte der magnetischen Feldstärke beobachtet wurden und daher die Anomalie dort vermutet wurde. Der Vertikalgradient ist deshalb nur noch in Abhängigkeit von x darzustellen.
                var plotModel = new PlotModel { Title = "Vertikalgradient in Abhängigkeit von X" };

                var lineSeries = new LineSeries
                {
                    Title = "Vertikalgradient",
                    MarkerType = MarkerType.Circle
                };

                for (int i = 0; i < xValues.Count; i++)
                {
                    lineSeries.Points.Add(new DataPoint(xValues[i], gradients[i]));
                }

                plotModel.Series.Add(lineSeries);

                // Diagramm als SVG-Datei speichern
                using (var stream = File.Create("Vertikalgradient.svg"))
                {
                    var svgExporter = new OxyPlot.SkiaSharp.SvgExporter { Width = 800, Height = 600 };
                    svgExporter.Export(plotModel, stream);
                }

                Console.WriteLine("Das Diagramm des Vertikalgradienten wurde als SVG-Datei gespeichert: Vertikalgradient.svg");
            }
        }

        /// <summary>
        /// Methode, die Instanzen von CSVReader, GradientCalculator und PlotGenerator erstellt, die Tiefe ausgibt, die Funktion CreatePlot aufruft und einen Auswertungstext ausgibt
        /// </summary>
        /// <param name="filePath">Der Pfad zur CSV-Datei.</param>
        /// <param name="h1">Die erste Höhe, in der gemessen wurde.</param>
        /// <param name="h2">Die zweite Höhe, in der gemessen wurde.</param>
        public void AnalyzeAnomaly(string filePath, double h1, double h2)
        {
            CSVReader csvReader = new CSVReader(filePath);
            GradientCalculator gradientCalculator = new GradientCalculator(csvReader.BH1Values, csvReader.BH2Values, h1, h2);
            PlotGenerator plotGenerator = new PlotGenerator(csvReader.XValues, gradientCalculator.Gradients);

            // Berechnete Tiefe ausgeben
            double depth = gradientCalculator.CalculateDepth(h1, h2, csvReader.BH1Values[gradientCalculator.MaxGradientIndex], csvReader.BH2Values[gradientCalculator.MaxGradientIndex]);
            Console.WriteLine($"Die berechnete Tiefe der Anomalie ist: {depth}m");

            // Diagramm als SVG-Datei speichern
            plotGenerator.CreatePlot();

            //Auswertungstext
            Console.WriteLine("Bei der durchgeführten magnetischen Kartierung wurde eine Anomalie gefunden.\nSie liegt in einer Tiefe von rund {0}m.\nDie x-Koordinate der Anomalie ist x = {1}m, was auch im Diagramm anhand der größten Amplitude nachvollzogen werden kann.", depth.ToString("#.00#"), csvReader.XValues[gradientCalculator.MaxGradientIndex]);
        }
    }

    /// <summary>
    /// Hauptprogramm zum Einlesen der Höhen und Aufrufen von AnomalyAnalyzer
    /// </summary>
    class Program
    {

        /// <summary>
        /// Einstigespunkt des Programms
        /// </summary>
        /// <param name="args">Befehlszeilenargumente.</param>
        static void Main(string[] args)
        {
            // Kultur für den aktuellen Thread auf Deutsch setzen
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");

            string filePath = "Vertikalgradient.csv";
            double h1 = 0;
            double h2 = 0;

            // Einlesen der Werte für die Höhen der Messungen mit Fehlerbehandlungen
            // im Versuch des Geophysik Praktikums wurde die erste Messung in der Höhe h1 = 2,1m und die zweite Messung in der Höhe h2 = 3,1m durchgeführt.
            try
            {
                h1 = ReadDouble("Bitte geben Sie die Höhe der ersten Messung h1 in Metern ein: ");
                Console.WriteLine("Die eingegebene Höhe der ersten Messung h1 ist: " + h1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler: " + ex.Message);
            }

            try
            {
                h2 = ReadDouble("Bitte geben Sie die Höhe der zweiten Messung h2 in Metern ein: ");
                Console.WriteLine("Die eingegebene Höhe der zweiten Messung h2 ist: " + h2);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler: " + ex.Message);
            }
    

            /// <summary>
            /// Methode zum Einlesen der Höhenwerte aus der Konsole, die falsche Eingaben behandelt und solange läuft, bis gültige Werte eingelesen wurden
            /// </summary>
            /// <param name="prompt">Höheneingabe des Benutzers.</param>
            static double ReadDouble(string prompt)
            {
                while (true)
                {
                    Console.Write(prompt);
                    string? input = Console.ReadLine();

                    // leere Eingabe abfangen
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Console.WriteLine("Die Eingabe darf nicht leer sein. Bitte versuchen Sie es erneut.");
                        continue;
                    }

                    double result;
                    // kann Zahlen mit Dezimalpunkt oder Komma einlesen
                    if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
                        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
                        double.TryParse(input.Replace('.', ','), NumberStyles.Float, new CultureInfo("de-DE"), out result))
                    {
                        // negative Eingaben der Höhe abfangen, weil sie im Kontext keinen Sinn ergeben (im Versuch kann nicht unter der Erde gemessen werden)
                        if (result < 0)
                        {
                            Console.WriteLine("Die Eingabe darf nicht negativ sein. Bitte versuchen Sie es erneut.");
                            continue;
                        }
                        return result;
                    }
                    else
                    {
                        Console.WriteLine("Die Eingabe war keine gültige Zahl. Bitte versuchen Sie es erneut.");
                    }
                }
            }
            AnomalyAnalyzer analyzer = new AnomalyAnalyzer();
            analyzer.AnalyzeAnomaly(filePath, h1, h2);

            Console.WriteLine("Programm erfolgreich abgeschlossen.");
        }
    }
}






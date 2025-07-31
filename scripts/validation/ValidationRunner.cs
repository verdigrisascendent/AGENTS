using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Comprehensive validation runner for Lights in the Dark
/// Validates all aspects against LITD_RULES_CANON.md v8.0
/// </summary>
public partial class ValidationRunner : Node
{
    // Validation suites
    private readonly Dictionary<string, IValidationSuite> validationSuites = new();
    private readonly List<ValidationResult> allResults = new();
    
    // Configuration
    private bool captureScreenshots = true;
    private bool verboseLogging = true;
    private string reportPath = "user://validation_report.json";
    private string screenshotDir = "user://validation_screenshots/";
    
    // Progress tracking
    private int totalTests = 0;
    private int completedTests = 0;
    private int passedTests = 0;
    private int failedTests = 0;
    
    // Visual indicators
    private const string PassIcon = "✅";
    private const string FailIcon = "❌";
    private const string WarningIcon = "⚠️";
    private const string RunningIcon = "🔄";
    private const string CompleteIcon = "✔️";
    
    public override void _Ready()
    {
        InitializeValidationSuites();
        CreateScreenshotDirectory();
    }
    
    private void InitializeValidationSuites()
    {
        // Register all validation suites
        validationSuites["rules"] = new RulesComplianceValidator();
        validationSuites["ui"] = new UIFrontendValidator();
        validationSuites["visual"] = new VisualComplianceValidator();
        validationSuites["gamestate"] = new GameStateValidator();
        validationSuites["hardware"] = new HardwareBridgeValidator();
        validationSuites["performance"] = new PerformanceValidator();
        validationSuites["scenarios"] = new TestScenariosValidator();
        
        // Count total tests
        foreach (var suite in validationSuites.Values)
        {
            totalTests += suite.GetTestCount();
        }
    }
    
    private void CreateScreenshotDirectory()
    {
        var dir = DirAccess.Open("user://");
        if (!dir.DirExists("validation_screenshots"))
        {
            dir.MakeDir("validation_screenshots");
        }
    }
    
    /// <summary>
    /// Run all validation suites
    /// </summary>
    public async Task<ValidationReport> RunFullValidation()
    {
        PrintHeader("LIGHTS IN THE DARK - COMPREHENSIVE VALIDATION");
        PrintInfo($"Running {totalTests} tests across {validationSuites.Count} validation suites");
        
        var startTime = Time.GetUnixTimeFromSystem();
        allResults.Clear();
        completedTests = 0;
        passedTests = 0;
        failedTests = 0;
        
        // Run each validation suite
        foreach (var kvp in validationSuites)
        {
            var suiteName = kvp.Key;
            var suite = kvp.Value;
            
            PrintSection($"Running {suiteName.ToUpper()} Validation Suite");
            var results = await RunValidationSuite(suite, suiteName);
            allResults.AddRange(results);
        }
        
        var endTime = Time.GetUnixTimeFromSystem();
        var duration = endTime - startTime;
        
        // Generate report
        var report = GenerateReport(duration);
        
        // Save report
        SaveReport(report);
        
        // Print summary
        PrintSummary(report);
        
        return report;
    }
    
    /// <summary>
    /// Run a specific validation suite
    /// </summary>
    public async Task<List<ValidationResult>> RunValidationSuite(string suiteName)
    {
        if (!validationSuites.TryGetValue(suiteName, out var suite))
        {
            PrintError($"Unknown validation suite: {suiteName}");
            return new List<ValidationResult>();
        }
        
        PrintHeader($"Running {suiteName.ToUpper()} Validation Suite");
        return await RunValidationSuite(suite, suiteName);
    }
    
    private async Task<List<ValidationResult>> RunValidationSuite(IValidationSuite suite, string suiteName)
    {
        var results = new List<ValidationResult>();
        suite.Initialize();
        
        var tests = suite.GetTests();
        foreach (var test in tests)
        {
            PrintProgress($"{RunningIcon} {test.Name}");
            
            try
            {
                var result = await suite.RunTest(test);
                result.SuiteName = suiteName;
                results.Add(result);
                
                completedTests++;
                if (result.Passed)
                {
                    passedTests++;
                    PrintSuccess($"{PassIcon} {test.Name}");
                }
                else
                {
                    failedTests++;
                    PrintError($"{FailIcon} {test.Name}");
                    if (verboseLogging)
                    {
                        foreach (var error in result.Errors)
                        {
                            PrintError($"   → {error}");
                        }
                    }
                }
                
                // Capture screenshot if needed
                if (captureScreenshots && test.RequiresScreenshot)
                {
                    await CaptureScreenshot($"{suiteName}_{test.Name}");
                }
                
                // Update progress
                UpdateProgress();
            }
            catch (Exception e)
            {
                var errorResult = new ValidationResult
                {
                    TestName = test.Name,
                    SuiteName = suiteName,
                    Passed = false,
                    Errors = new List<string> { $"Exception: {e.Message}" },
                    Duration = 0
                };
                results.Add(errorResult);
                failedTests++;
                PrintError($"{FailIcon} {test.Name} - EXCEPTION: {e.Message}");
            }
        }
        
        suite.Cleanup();
        return results;
    }
    
    private async Task CaptureScreenshot(string name)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        var image = GetViewport().GetTexture().GetImage();
        var filename = $"{screenshotDir}{name}_{Time.GetUnixTimeFromSystem()}.png";
        image.SavePng(filename);
        
        if (verboseLogging)
        {
            PrintInfo($"   📸 Screenshot saved: {filename}");
        }
    }
    
    private ValidationReport GenerateReport(double duration)
    {
        var report = new ValidationReport
        {
            Timestamp = Time.GetUnixTimeFromSystem(),
            Duration = duration,
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            SuccessRate = totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0,
            Results = allResults,
            Summary = GenerateSummary()
        };
        
        // Add suite summaries
        foreach (var suiteName in validationSuites.Keys)
        {
            var suiteResults = allResults.Where(r => r.SuiteName == suiteName).ToList();
            var suitePassed = suiteResults.Count(r => r.Passed);
            var suiteTotal = suiteResults.Count;
            
            report.SuiteSummaries[suiteName] = new SuiteSummary
            {
                Name = suiteName,
                TotalTests = suiteTotal,
                PassedTests = suitePassed,
                FailedTests = suiteTotal - suitePassed,
                SuccessRate = suiteTotal > 0 ? (suitePassed * 100.0 / suiteTotal) : 0
            };
        }
        
        return report;
    }
    
    private string GenerateSummary()
    {
        var summary = "VALIDATION SUMMARY\n";
        summary += "==================\n\n";
        
        // Overall status
        var overallPass = failedTests == 0;
        summary += $"Overall Result: {(overallPass ? $"{PassIcon} PASS" : $"{FailIcon} FAIL")}\n";
        summary += $"Success Rate: {(totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0):F1}%\n\n";
        
        // Suite breakdown
        summary += "Suite Results:\n";
        foreach (var suiteName in validationSuites.Keys)
        {
            var suiteResults = allResults.Where(r => r.SuiteName == suiteName).ToList();
            var suitePassed = suiteResults.Count(r => r.Passed);
            var suiteTotal = suiteResults.Count;
            var suitePass = suitePassed == suiteTotal;
            
            summary += $"  {(suitePass ? PassIcon : FailIcon)} {suiteName}: ";
            summary += $"{suitePassed}/{suiteTotal} passed\n";
        }
        
        // Critical failures
        var criticalFailures = allResults.Where(r => !r.Passed && r.IsCritical).ToList();
        if (criticalFailures.Any())
        {
            summary += $"\n{FailIcon} CRITICAL FAILURES:\n";
            foreach (var failure in criticalFailures)
            {
                summary += $"  - {failure.TestName}: {failure.Errors.FirstOrDefault()}\n";
            }
        }
        
        return summary;
    }
    
    private void SaveReport(ValidationReport report)
    {
        var json = Json.Stringify(report.ToDict());
        var file = FileAccess.Open(reportPath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            file.Close();
            PrintSuccess($"\n{CompleteIcon} Validation report saved to: {reportPath}");
        }
    }
    
    private void UpdateProgress()
    {
        var percentage = (completedTests * 100.0 / totalTests);
        var progressBar = GenerateProgressBar(percentage);
        
        if (!verboseLogging)
        {
            GD.Print($"\rProgress: {progressBar} {percentage:F0}% ({completedTests}/{totalTests})", end: "");
        }
    }
    
    private string GenerateProgressBar(double percentage)
    {
        const int barWidth = 20;
        var filled = (int)(percentage / 100 * barWidth);
        var empty = barWidth - filled;
        
        return "[" + new string('█', filled) + new string('░', empty) + "]";
    }
    
    // Console output helpers
    private void PrintHeader(string text)
    {
        GD.Print($"\n╔{'═'.ToString().PadRight(text.Length + 2, '=')}╗");
        GD.Print($"║ {text} ║");
        GD.Print($"╚{'═'.ToString().PadRight(text.Length + 2, '=')}╝\n");
    }
    
    private void PrintSection(string text)
    {
        GD.Print($"\n▶ {text}");
        GD.Print(new string('─', text.Length + 2));
    }
    
    private void PrintSuccess(string text)
    {
        GD.PrintRich($"[color=green]{text}[/color]");
    }
    
    private void PrintError(string text)
    {
        GD.PrintRich($"[color=red]{text}[/color]");
    }
    
    private void PrintWarning(string text)
    {
        GD.PrintRich($"[color=yellow]{text}[/color]");
    }
    
    private void PrintInfo(string text)
    {
        GD.PrintRich($"[color=cyan]{text}[/color]");
    }
    
    private void PrintProgress(string text)
    {
        if (verboseLogging)
        {
            GD.Print(text);
        }
    }
    
    private void PrintSummary(ValidationReport report)
    {
        PrintHeader("VALIDATION COMPLETE");
        
        // Overall result
        var overallPass = report.FailedTests == 0;
        if (overallPass)
        {
            PrintSuccess($"{PassIcon} ALL TESTS PASSED!");
        }
        else
        {
            PrintError($"{FailIcon} VALIDATION FAILED");
        }
        
        // Statistics
        PrintInfo($"\nTotal Tests: {report.TotalTests}");
        PrintSuccess($"Passed: {report.PassedTests}");
        if (report.FailedTests > 0)
        {
            PrintError($"Failed: {report.FailedTests}");
        }
        PrintInfo($"Success Rate: {report.SuccessRate:F1}%");
        PrintInfo($"Duration: {report.Duration:F2} seconds");
        
        // Suite breakdown
        PrintSection("Suite Breakdown");
        foreach (var summary in report.SuiteSummaries.Values)
        {
            var icon = summary.FailedTests == 0 ? PassIcon : FailIcon;
            GD.Print($"{icon} {summary.Name}: {summary.PassedTests}/{summary.TotalTests} ({summary.SuccessRate:F1}%)");
        }
        
        // Critical failures
        var criticalFailures = report.Results.Where(r => !r.Passed && r.IsCritical).ToList();
        if (criticalFailures.Any())
        {
            PrintSection($"Critical Failures ({criticalFailures.Count})");
            foreach (var failure in criticalFailures)
            {
                PrintError($"• {failure.SuiteName}/{failure.TestName}");
                foreach (var error in failure.Errors.Take(3))
                {
                    PrintError($"  → {error}");
                }
            }
        }
    }
}

// Validation interfaces and classes
public interface IValidationSuite
{
    void Initialize();
    void Cleanup();
    List<ValidationTest> GetTests();
    int GetTestCount();
    Task<ValidationResult> RunTest(ValidationTest test);
}

public class ValidationTest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool RequiresScreenshot { get; set; }
    public bool IsCritical { get; set; }
    public Func<Task<ValidationResult>> TestFunc { get; set; }
}

public class ValidationResult
{
    public string TestName { get; set; }
    public string SuiteName { get; set; }
    public bool Passed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public double Duration { get; set; }
    public bool IsCritical { get; set; }
}

public class ValidationReport
{
    public double Timestamp { get; set; }
    public double Duration { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public string Summary { get; set; }
    public List<ValidationResult> Results { get; set; } = new();
    public Dictionary<string, SuiteSummary> SuiteSummaries { get; set; } = new();
    
    public Godot.Collections.Dictionary ToDict()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["timestamp"] = Timestamp,
            ["duration"] = Duration,
            ["total_tests"] = TotalTests,
            ["passed_tests"] = PassedTests,
            ["failed_tests"] = FailedTests,
            ["success_rate"] = SuccessRate,
            ["summary"] = Summary
        };
        
        var resultsArray = new Godot.Collections.Array();
        foreach (var result in Results)
        {
            resultsArray.Add(new Godot.Collections.Dictionary
            {
                ["test_name"] = result.TestName,
                ["suite_name"] = result.SuiteName,
                ["passed"] = result.Passed,
                ["errors"] = result.Errors,
                ["warnings"] = result.Warnings,
                ["duration"] = result.Duration,
                ["is_critical"] = result.IsCritical
            });
        }
        dict["results"] = resultsArray;
        
        var suitesDict = new Godot.Collections.Dictionary();
        foreach (var kvp in SuiteSummaries)
        {
            suitesDict[kvp.Key] = new Godot.Collections.Dictionary
            {
                ["name"] = kvp.Value.Name,
                ["total_tests"] = kvp.Value.TotalTests,
                ["passed_tests"] = kvp.Value.PassedTests,
                ["failed_tests"] = kvp.Value.FailedTests,
                ["success_rate"] = kvp.Value.SuccessRate
            };
        }
        dict["suite_summaries"] = suitesDict;
        
        return dict;
    }
}

public class SuiteSummary
{
    public string Name { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
}
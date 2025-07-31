using Godot;
using System;

/// <summary>
/// Helper to show validation report location and contents
/// </summary>
public static class ShowValidationReport
{
    public static void ShowReportLocation()
    {
        // Get the user data directory
        var userDir = OS.GetUserDataDir();
        var reportPath = "user://validation_report.json";
        var fullPath = ProjectSettings.GlobalizePath(reportPath);
        
        GD.Print("\n=== VALIDATION REPORT LOCATION ===");
        GD.Print($"User data directory: {userDir}");
        GD.Print($"Report path: {reportPath}");
        GD.Print($"Full system path: {fullPath}");
        
        // Check if file exists
        if (FileAccess.FileExists(reportPath))
        {
            GD.Print("\n✅ Report file EXISTS");
            
            // Read and display content
            var file = FileAccess.Open(reportPath, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                var content = file.GetAsText();
                file.Close();
                
                GD.Print($"\nFile size: {content.Length} bytes");
                GD.Print("\n=== REPORT PREVIEW (first 500 chars) ===");
                GD.Print(content.Substring(0, Math.Min(500, content.Length)) + "...");
            }
        }
        else
        {
            GD.Print("\n❌ Report file NOT FOUND");
            GD.Print("\nThe validation hasn't been run yet, or the file was deleted.");
            GD.Print("Run 'Full Validation' from the Debug Menu (F12) to generate it.");
        }
        
        // Show platform-specific paths
        GD.Print("\n=== PLATFORM-SPECIFIC LOCATIONS ===");
        
        if (OS.GetName() == "macOS")
        {
            GD.Print("On macOS, the file is typically at:");
            GD.Print($"~/Library/Application Support/Godot/app_userdata/{ProjectSettings.GetSetting("application/config/name")}/validation_report.json");
        }
        else if (OS.GetName() == "Windows")
        {
            GD.Print("On Windows, the file is typically at:");
            GD.Print($"%APPDATA%\\Godot\\app_userdata\\{ProjectSettings.GetSetting("application/config/name")}\\validation_report.json");
        }
        else if (OS.GetName() == "Linux")
        {
            GD.Print("On Linux, the file is typically at:");
            GD.Print($"~/.local/share/godot/app_userdata/{ProjectSettings.GetSetting("application/config/name")}/validation_report.json");
        }
        
        // List all files in user directory
        GD.Print("\n=== FILES IN USER DIRECTORY ===");
        var dir = DirAccess.Open("user://");
        if (dir != null)
        {
            dir.ListDirBegin();
            var fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir())
                {
                    GD.Print($"  - {fileName}");
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }
    }
    
    public static void OpenReportLocation()
    {
        var userDir = OS.GetUserDataDir();
        OS.ShellOpen(userDir);
        GD.Print($"\n📂 Opened folder: {userDir}");
    }
    
    public static void CopyReportToDesktop()
    {
        var reportPath = "user://validation_report.json";
        
        if (!FileAccess.FileExists(reportPath))
        {
            GD.PrintErr("No validation report found to copy!");
            return;
        }
        
        // Read the report
        var file = FileAccess.Open(reportPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Could not open validation report!");
            return;
        }
        
        var content = file.GetAsText();
        file.Close();
        
        // Get desktop path
        var desktopPath = OS.GetSystemDir(OS.SystemDir.Desktop);
        var destPath = desktopPath.PathJoin("LITD_validation_report.json");
        
        // Write to desktop
        var destFile = FileAccess.Open(destPath, FileAccess.ModeFlags.Write);
        if (destFile != null)
        {
            destFile.StoreString(content);
            destFile.Close();
            GD.Print($"\n✅ Report copied to: {destPath}");
        }
        else
        {
            GD.PrintErr($"Could not write to: {destPath}");
        }
    }
}
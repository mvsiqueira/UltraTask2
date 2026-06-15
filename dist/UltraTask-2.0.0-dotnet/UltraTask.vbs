Dim dll
dll = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName) & "\UltraTask.dll"
CreateObject("WScript.Shell").Run "dotnet """ & dll & """", 0, False

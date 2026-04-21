[Setup]
AppId={{783eb077-d4fa-4dc4-bbf8-661cd9dc6ee8}
AppName=MarkdownConverter
AppVersion=2.0.9
AppPublisher=AhmedElbehary-Dev
AppPublisherURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter
AppSupportURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter/issues
AppUpdatesURL=https://github.com/AhmedElbehary-Dev/MarkdownConverter/releases
DefaultDirName={autopf}\MarkdownConverter
DefaultGroupName=MarkdownConverter
UninstallDisplayIcon={app}\MarkdownConverter.exe
SetupIconFile=..\..\img\md_converter.ico
Compression=lzma2
SolidCompression=yes
OutputDir=..\..\release
OutputBaseFilename=MarkdownConverter-Setup-x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupMutex=MarkdownConverterSetupMutex
; --- Code Signing (uncomment when you have a certificate) ---
; SignTool=signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a $f
; SignedUninstaller=yes

[Files]
Source: "..\..\bin\Release\net10.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; --- Bundled dependencies (from download-deps.ps1) ---
Source: "deps\wkhtmltox-installer.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion
Source: "deps\libwkhtmltox.dll"; DestDir: "{app}\runtimes\win-x64\native"; Flags: ignoreversion
[Icons]
Name: "{group}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"
Name: "{autodesktop}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Code]
var
  DependencyPage: TOutputProgressWizardPage;
  LogMemo: TNewMemo;
  PathsAddedToSystem: String;

function GetDateTimeString(const DateTimeFormat: String; const DateSeparator, TimeSeparator: Char): String;
begin
  Result := GetDateTimeString(DateTimeFormat, DateSeparator, TimeSeparator);
end;

procedure LogLine(Msg: String);
var
  S: String;
begin
  S := '[' + GetDateTimeString('hh:nn:ss', ':', ':') + '] ' + Msg;
  LogMemo.Lines.Add(S);
  LogMemo.SelStart := Length(LogMemo.Text);
  LogMemo.SelLength := 0;
end;

function IsOnSystemPath(Dir: String): Boolean;
var
  Path: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    Result := Pos(';' + Lowercase(Dir) + ';', ';' + Lowercase(Path) + ';') > 0;
  end;
end;

procedure AddToSystemPath(Dir: String);
var
  Path: String;
begin
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    if Path <> '' then
    begin
      if Copy(Path, Length(Path), 1) <> ';' then
        Path := Path + ';';
    end;
    Path := Path + Dir;
    RegWriteStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path);
    
    if PathsAddedToSystem <> '' then
      PathsAddedToSystem := PathsAddedToSystem + ';';
    PathsAddedToSystem := PathsAddedToSystem + Dir;
  end;
end;

function DetectBrowser(): String;
var
  Paths: array of String;
  I: Integer;
begin
  SetArrayLength(Paths, 4);
  Paths[0] := ExpandConstant('{pf}\Google\Chrome\Application\chrome.exe');
  Paths[1] := ExpandConstant('{pf32}\Google\Chrome\Application\chrome.exe');
  Paths[2] := ExpandConstant('{pf32}\Microsoft\Edge\Application\msedge.exe');
  Paths[3] := ExpandConstant('{pf}\Microsoft\Edge\Application\msedge.exe');
  
  Result := '';
  for I := 0 to 3 do
  begin
    if FileExists(Paths[I]) then
    begin
      Result := Paths[I];
      Exit;
    end;
  end;
end;

procedure RemoveFromSystemPath(Dir: String);
var
  Path: String;
  P: Integer;
begin
  if RegQueryStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path) then
  begin
    Path := ';' + Path + ';';
    Dir := ';' + Dir + ';';
    P := Pos(Lowercase(Dir), Lowercase(Path));
    if P > 0 then
    begin
      Delete(Path, P, Length(Dir) - 1);
      Path := Copy(Path, 2, Length(Path) - 2);
      RegWriteStringValue(HKLM, 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment', 'Path', Path);
    end;
  end;
end;

procedure RunDependencySetup();
var
  BrowserPath, BrowserDir: String;
  WkhtmltopdfBin: String;
  ResultCode: Integer;
  WkhtmltopdfInstalledByUs: Integer;
begin
  DependencyPage.Show;
  PathsAddedToSystem := '';
  WkhtmltopdfInstalledByUs := 0;
  
  DependencyPage.SetProgress(10, 100);
  LogLine(#x2713 + ' libwkhtmltox.dll installed to runtimes directory');
  
  BrowserPath := DetectBrowser();
  DependencyPage.SetProgress(30, 100);
  if BrowserPath <> '' then
  begin
    LogLine(#x2713 + ' Found browser: ' + BrowserPath);
    BrowserDir := ExtractFilePath(BrowserPath);
    if Length(BrowserDir) > 0 then
      BrowserDir := Copy(BrowserDir, 1, Length(BrowserDir) - 1);
    if not IsOnSystemPath(BrowserDir) then
    begin
      AddToSystemPath(BrowserDir);
      LogLine(#x2713 + ' Browser directory added to system PATH');
    end;
  end else
  begin
    LogLine('Browser not found in common locations.');
  end;
  
  DependencyPage.SetProgress(50, 100);
  WkhtmltopdfBin := ExpandConstant('{pf}\wkhtmltopdf\bin');
  if not FileExists(WkhtmltopdfBin + '\wkhtmltopdf.exe') then
  begin
    LogLine('Installing wkhtmltopdf silently...');
    if Exec(ExpandConstant('{tmp}\wkhtmltox-installer.exe'), '/S', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      LogLine(#x2713 + ' wkhtmltopdf installed successfully');
      WkhtmltopdfInstalledByUs := 1;
    end else
    begin
      LogLine('Failed to install wkhtmltopdf (Code: ' + IntToStr(ResultCode) + ')');
    end;
  end else
  begin
    LogLine(#x2713 + ' wkhtmltopdf already present - skipping installation');
  end;
  
  DependencyPage.SetProgress(80, 100);
  if FileExists(WkhtmltopdfBin + '\wkhtmltopdf.exe') then
  begin
    if not IsOnSystemPath(WkhtmltopdfBin) then
    begin
      AddToSystemPath(WkhtmltopdfBin);
      LogLine(#x2713 + ' wkhtmltopdf added to system PATH');
    end;
  end;
  
  LogLine('Writing install manifest...');
  RegWriteDWordValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfInstalled', WkhtmltopdfInstalledByUs);
  if WkhtmltopdfInstalledByUs = 1 then
    RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfUninstallCmd', ExpandConstant('{pf}\wkhtmltopdf\unins000.exe'));
  RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'PathEntriesAdded', PathsAddedToSystem);
  RegWriteStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'InstallDate', GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':'));
  
  DependencyPage.SetProgress(100, 100);
  LogLine('------------------------------');
  LogLine(#x2713 + ' All PDF backends ready');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  sUnInstPath: String;
  sUnInstallString: String;
  iResultCode: Integer;
begin
  if (CurStep = ssInstall) then
  begin
    sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
    sUnInstallString := '';
    if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
      RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
    
    if sUnInstallString <> '' then
    begin
      sUnInstallString := RemoveQuotes(sUnInstallString);
      Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
    end;
  end
  else if (CurStep = ssPostInstall) then
  begin
    RunDependencySetup();
  end;
end;

procedure InitializeWizard;
begin
  DependencyPage := CreateOutputProgressPage('Environment Setup', 'Setting up PDF conversion components...');
  LogMemo := TNewMemo.Create(WizardForm);
  LogMemo.Parent := DependencyPage.Surface;
  LogMemo.Left := 0;
  LogMemo.Top := DependencyPage.ProgressBar.Top + DependencyPage.ProgressBar.Height + 10;
  LogMemo.Width := DependencyPage.SurfaceWidth;
  LogMemo.Height := DependencyPage.SurfaceHeight - LogMemo.Top;
  LogMemo.ReadOnly := True;
  LogMemo.ScrollBars := ssVertical;
  LogMemo.Color := clWindow;
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
var
  WkhtmltopdfInstalled: Cardinal;
  WkhtmltopdfUninstallCmd, PathsAdded, PathItem: String;
  iResultCode, P: Integer;
begin
  if CurStep = usPostUninstall then
  begin
    if RegQueryDWordValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfInstalled', WkhtmltopdfInstalled) then
    begin
      if (WkhtmltopdfInstalled = 1) and RegQueryStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'WkhtmltopdfUninstallCmd', WkhtmltopdfUninstallCmd) then
      begin
        if FileExists(WkhtmltopdfUninstallCmd) then
          Exec(WkhtmltopdfUninstallCmd, '/S', '', SW_HIDE, ewWaitUntilTerminated, iResultCode);
      end;
    end;
    
    if RegQueryStringValue(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter\InstallManifest', 'PathEntriesAdded', PathsAdded) then
    begin
      while Length(PathsAdded) > 0 do
      begin
        P := Pos(';', PathsAdded);
        if P > 0 then
        begin
          PathItem := Copy(PathsAdded, 1, P - 1);
          Delete(PathsAdded, 1, P);
        end else
        begin
          PathItem := PathsAdded;
          PathsAdded := '';
        end;
        if PathItem <> '' then RemoveFromSystemPath(PathItem);
      end;
    end;
    
    RegDeleteKeyIncludingSubkeys(HKLM, 'Software\MarkdownConverterTeam\MarkdownConverter');
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\MarkdownConverterTeam');
    
    DelTree(ExpandConstant('{app}'), True, True, True);
    DelTree(ExpandConstant('{group}'), True, True, False);
  end;
end;
